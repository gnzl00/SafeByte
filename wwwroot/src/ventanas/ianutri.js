const USER_STORAGE_KEY = "sb_user";
const LOCAL_ALLERGENS_KEY = "alergenosSeleccionados";
const CACHE_ALLERGENS_KEY = "sb_alergenos";

const state = {
  allergens: [],
  selectedMode: "",
  reformulatedPrompt: "",
  suggestions: [],
  activeHistoryId: "",
  history: []
};

const dom = {};

document.addEventListener("DOMContentLoaded", () => {
  bindDom();
  bindEvents();
  initialize();
});

async function initialize() {
  updateCharCounter();
  state.allergens = await resolveUserAllergens();
  renderAllergens(state.allergens);
  await loadHistory();
}

function bindDom() {
  dom.textInput = document.getElementById("ianutri-input");
  dom.modeButtons = Array.from(document.querySelectorAll(".mode-chip"));
  dom.reformulationBox = document.getElementById("reformulation-box");
  dom.reformulatedPrompt = document.getElementById("reformulated-prompt");
  dom.charCounter = document.getElementById("char-counter");
  dom.generateButton = document.getElementById("generate-suggestions");
  dom.plannerStatus = document.getElementById("planner-status");
  dom.resultStatus = document.getElementById("result-status");
  dom.resultEmpty = document.getElementById("result-empty");
  dom.resultSummary = document.getElementById("result-summary");
  dom.globalAlerts = document.getElementById("global-alerts");
  dom.suggestionsList = document.getElementById("suggestions-list");
  dom.allergens = document.getElementById("ianutri-allergens");
  dom.assistantCard = document.getElementById("assistant-card");
  dom.assistantClose = document.getElementById("assistant-close");
  dom.assistantIntro = document.getElementById("assistant-intro");
  dom.assistantItems = document.getElementById("assistant-items");
  dom.assistantSteps = document.getElementById("assistant-steps");
  dom.assistantSafety = document.getElementById("assistant-safety");
  dom.assistantStatus = document.getElementById("assistant-status");
  dom.historyList = document.getElementById("history-list");
  dom.historyDetails = document.getElementById("history-details");
  dom.clearHistoryButton = document.getElementById("clear-history");
  dom.historyStatus = document.getElementById("history-status");
}

function bindEvents() {
  dom.textInput.addEventListener("input", () => {
    updateCharCounter();
    state.reformulatedPrompt = "";
    dom.generateButton.disabled = true;
    hideReformulationPreview();
    setStatus(dom.plannerStatus, "", "info");
  });

  dom.modeButtons.forEach((button) => {
    button.addEventListener("click", async () => {
      state.selectedMode = button.dataset.mode || "";
      setActiveModeButton(state.selectedMode);
      await reformulateRequest();
    });
  });

  dom.generateButton.addEventListener("click", async () => {
    await generateSuggestions();
  });

  dom.assistantClose.addEventListener("click", () => {
    dom.assistantCard.classList.add("hidden");
    setStatus(dom.assistantStatus, "", "info");
  });

  dom.clearHistoryButton.addEventListener("click", async () => {
    await clearHistory();
  });
}

function setActiveModeButton(mode) {
  dom.modeButtons.forEach((button) => {
    button.classList.toggle("active", (button.dataset.mode || "") === mode);
  });
}

function getModeLabel(mode) {
  const lookup = {
    "rapido-basico": "Rapido y basico",
    "cena-ligera": "Cena ligera",
    "menu-semanal": "Menu semanal"
  };

  return lookup[mode] || "Personalizado";
}

async function reformulateRequest() {
  const userInput = (dom.textInput.value || "").trim();
  if (!userInput) {
    setStatus(dom.plannerStatus, "Escribe ingredientes o una idea antes de reformular.", "error");
    dom.generateButton.disabled = true;
    return;
  }

  if (!state.selectedMode) {
    setStatus(dom.plannerStatus, "Selecciona una orientacion para reformular.", "error");
    dom.generateButton.disabled = true;
    return;
  }

  toggleModeLoading(true);
  setStatus(dom.plannerStatus, "Reformulando peticion...", "info");

  try {
    const payload = {
      email: getCurrentUser()?.email || "",
      userInput,
      option: state.selectedMode,
      allergens: state.allergens
    };

    const data = await postJson("/api/IANutri/Reformulate", payload);
    state.reformulatedPrompt = sanitizeReformulationPreview(data?.reformulatedPrompt || "");

    showReformulationPreview(state.reformulatedPrompt);
    dom.generateButton.disabled = !state.reformulatedPrompt;

    if (state.reformulatedPrompt) {
      setStatus(dom.plannerStatus, "Peticion reformulada. Ya puedes generar sugerencias.", "success");
    } else {
      setStatus(dom.plannerStatus, "No se pudo reformular la peticion.", "error");
    }
  } catch (error) {
    state.reformulatedPrompt = "";
    dom.generateButton.disabled = true;
    hideReformulationPreview();
    setStatus(dom.plannerStatus, `Error al reformular: ${error.message}`, "error");
  } finally {
    toggleModeLoading(false);
  }
}

async function generateSuggestions() {
  const userInput = (dom.textInput.value || "").trim();
  if (!userInput) {
    setStatus(dom.plannerStatus, "Escribe ingredientes o una idea para generar opciones.", "error");
    return;
  }

  if (!state.selectedMode) {
    setStatus(dom.plannerStatus, "Selecciona primero una orientacion.", "error");
    return;
  }

  if (!state.reformulatedPrompt) {
    await reformulateRequest();
    if (!state.reformulatedPrompt) {
      return;
    }
  }

  setGenerateLoading(true);
  setStatus(dom.resultStatus, "Generando sugerencias...", "info");

  try {
    const payload = {
      email: getCurrentUser()?.email || "",
      userInput,
      option: getModeLabel(state.selectedMode),
      reformulatedPrompt: state.reformulatedPrompt,
      allergens: state.allergens
    };

    const data = await postJson("/api/IANutri/GenerateSuggestions", payload);
    state.suggestions = Array.isArray(data?.suggestions) ? data.suggestions : [];
    state.activeHistoryId = typeof data?.historyId === "string" ? data.historyId.trim() : "";
    renderSuggestionsResult(data);
    await loadHistory();
    setStatus(dom.resultStatus, "Haz clic en una sugerencia para abrir el asistente de cocina.", "success");
  } catch (error) {
    setStatus(dom.resultStatus, `No se pudieron generar sugerencias: ${error.message}`, "error");
  } finally {
    setGenerateLoading(false);
  }
}

function renderSuggestionsResult(data) {
  dom.resultEmpty.classList.add("hidden");
  dom.suggestionsList.innerHTML = "";

  const summary = (data?.summary || "").trim();
  if (summary) {
    dom.resultSummary.textContent = summary;
    dom.resultSummary.classList.remove("hidden");
  } else {
    dom.resultSummary.classList.add("hidden");
    dom.resultSummary.textContent = "";
  }

  renderGlobalAlerts(data?.globalWarnings, data?.generalSubstitutions);

  const suggestions = Array.isArray(data?.suggestions) ? data.suggestions : [];
  if (suggestions.length === 0) {
    dom.resultEmpty.classList.remove("hidden");
    dom.resultEmpty.querySelector("p").textContent = "No hay sugerencias disponibles para esta peticion.";
    return;
  }

  suggestions.forEach((suggestion, index) => {
    const card = createSuggestionCard(suggestion, index);
    dom.suggestionsList.appendChild(card);
  });
}

async function loadHistory() {
  const user = getCurrentUser();
  if (!user?.email) {
    state.history = [];
    renderHistory([], "Inicia sesion para guardar y reutilizar tu historial IA.");
    dom.clearHistoryButton.disabled = true;
    return;
  }

  try {
    const response = await fetch(`/api/IANutri/History?email=${encodeURIComponent(user.email)}`);
    if (!response.ok) {
      throw new Error(`Error ${response.status}`);
    }

    const data = await response.json();
    const history = Array.isArray(data?.history) ? data.history : [];
    state.history = history.map(mapHistoryForState);
    renderHistory(state.history);
    dom.clearHistoryButton.disabled = state.history.length === 0;
    setStatus(dom.historyStatus, "", "info");
  } catch (error) {
    renderHistory([], "No se pudo cargar el historial en este momento.");
    dom.clearHistoryButton.disabled = true;
    setStatus(dom.historyStatus, `Error cargando historial: ${error.message}`, "error");
  }
}

function renderHistory(history, emptyMessage = "Aun no hay conversaciones guardadas.") {
  dom.historyList.innerHTML = "";

  if (!Array.isArray(history) || history.length === 0) {
    const empty = document.createElement("div");
    empty.className = "history-empty";
    empty.textContent = emptyMessage;
    dom.historyList.appendChild(empty);
    return;
  }

  history.forEach((item) => {
    const card = document.createElement("article");
    card.className = "history-item";
    card.tabIndex = 0;

    const title = document.createElement("h3");
    title.textContent = item.option ? `Modo: ${item.option}` : "Conversacion";

    const input = document.createElement("p");
    input.textContent = `Pedido: ${truncateText(item.userInput, 150)}`;

    const summary = document.createElement("p");
    summary.textContent = `Resumen: ${truncateText(item.summary, 160)}`;

    const meta = document.createElement("span");
    meta.className = "meta";
    meta.textContent = `${formatDateTime(item.createdAtUtc)} - ${item.suggestions.length} sugerencia(s)`;

    card.appendChild(title);
    card.appendChild(input);
    card.appendChild(summary);
    card.appendChild(meta);

    const onSelect = () => restoreFromHistory(item);
    card.addEventListener("click", onSelect);
    card.addEventListener("keydown", (event) => {
      if (event.key === "Enter" || event.key === " ") {
        event.preventDefault();
        onSelect();
      }
    });

    dom.historyList.appendChild(card);
  });
}

function restoreFromHistory(item) {
  state.activeHistoryId = item.id;
  state.reformulatedPrompt = item.reformulatedPrompt;
  state.selectedMode = getModeKeyFromLabel(item.option);
  setActiveModeButton(state.selectedMode);

  dom.textInput.value = item.userInput;
  updateCharCounter();
  showReformulationPreview(item.reformulatedPrompt);
  dom.generateButton.disabled = !state.reformulatedPrompt;

  renderSuggestionsResult({
    summary: item.summary,
    globalWarnings: item.globalWarnings,
    generalSubstitutions: item.generalSubstitutions,
    suggestions: item.suggestions
  });

  setStatus(dom.resultStatus, "Conversacion recuperada. Pulsa una receta para abrir el asistente justo debajo de Resultado.", "success");
}

async function clearHistory() {
  const user = getCurrentUser();
  if (!user?.email) {
    setStatus(dom.historyStatus, "Debes iniciar sesion para borrar historial remoto.", "error");
    return;
  }

  dom.clearHistoryButton.disabled = true;
  setStatus(dom.historyStatus, "Borrando historial...", "info");

  try {
    const response = await fetch(`/api/IANutri/History?email=${encodeURIComponent(user.email)}`, {
      method: "DELETE"
    });

    const contentType = response.headers.get("content-type") || "";
    const body = contentType.includes("application/json") ? await response.json() : await response.text();
    if (!response.ok) {
      throw new Error(body?.message || body?.Message || body || `Error ${response.status}`);
    }

    state.history = [];
    state.activeHistoryId = "";
    renderHistory([]);
    setStatus(dom.historyStatus, "Historial eliminado correctamente.", "success");
  } catch (error) {
    setStatus(dom.historyStatus, `No se pudo borrar historial: ${error.message}`, "error");
  } finally {
    dom.clearHistoryButton.disabled = state.history.length === 0;
  }
}

function renderGlobalAlerts(globalWarnings, generalSubstitutions) {
  dom.globalAlerts.innerHTML = "";
  const warnings = normalizeTextArray(globalWarnings, 6);
  const substitutions = normalizeTextArray(generalSubstitutions, 8);
  const warningLines = [];
  const positiveLines = [];

  warnings.forEach((warning) => {
    warningLines.push({
      type: classifyWarningSeverity(warning),
      text: `Aviso: ${warning}`
    });
  });

  substitutions.forEach((item) => {
    positiveLines.push({
      type: "success",
      text: `Sustitucion sugerida: ${item}`
    });
  });

  if (warningLines.length === 0 && positiveLines.length === 0) {
    dom.globalAlerts.classList.add("hidden");
    return;
  }

  if (warningLines.length > 0) {
    dom.globalAlerts.appendChild(
      buildAlertGroup("Puntos a tener en cuenta", "warning", warningLines));
  }

  if (positiveLines.length > 0) {
    dom.globalAlerts.appendChild(
      buildAlertGroup("Puntos positivos y sustituciones seguras", "success", positiveLines));
  }

  dom.globalAlerts.classList.remove("hidden");
}

function buildAlertGroup(title, groupType, lines) {
  const group = document.createElement("section");
  group.className = `alert-group ${groupType}`;

  const heading = document.createElement("h3");
  heading.className = "alert-group-title";
  heading.textContent = title;

  const items = document.createElement("div");
  items.className = "alert-group-items";

  lines.forEach((line) => {
    const item = document.createElement("div");
    item.className = `alert-item alert-${line.type}`;
    item.textContent = line.text;
    items.appendChild(item);
  });

  group.appendChild(heading);
  group.appendChild(items);
  return group;
}

function createSuggestionCard(suggestion, index) {
  const safeSuggestion = normalizeSuggestion(suggestion);
  const card = document.createElement("article");
  card.className = "suggestion-card";
  card.tabIndex = 0;

  const head = document.createElement("div");
  head.className = "suggestion-head";

  const title = document.createElement("h3");
  title.textContent = `${index + 1}. ${safeSuggestion.title}`;

  const meta = document.createElement("span");
  meta.className = "meta";
  meta.textContent = `Tiempo estimado: ${safeSuggestion.estimatedTime} | Dificultad: ${safeSuggestion.difficulty}`;

  head.appendChild(title);
  head.appendChild(meta);

  const description = document.createElement("p");
  description.textContent = safeSuggestion.description;

  const ingredients = document.createElement("p");
  ingredients.textContent = `Ingredientes: ${safeSuggestion.ingredients.slice(0, 8).join(", ")}`;

  const steps = document.createElement("p");
  steps.textContent = `Pasos clave: ${safeSuggestion.steps.slice(0, 3).join(" | ")}`;

  const badgeRow = document.createElement("div");
  badgeRow.className = "badge-row";

  const normalizedDetectedAllergens = normalizeDetectedAllergens(safeSuggestion.allergensDetected);
  if (normalizedDetectedAllergens.length === 0) {
    addBadge(badgeRow, "Alergenos detectados: Ninguno", "success");
  } else {
    const hasConflict = normalizedDetectedAllergens.some((allergen) =>
      containsIgnoreCase(state.allergens, allergen));
    addBadge(
      badgeRow,
      `Alergenos detectados: ${normalizedDetectedAllergens.join(", ")}`,
      hasConflict ? "danger" : "warning");
  }

  if (safeSuggestion.safeSubstitutions.length > 0) {
    addBadge(badgeRow, `Sustituciones: ${safeSuggestion.safeSubstitutions[0]}`, "success");
  }

  if (safeSuggestion.allergyWarning) {
    addBadge(
      badgeRow,
      safeSuggestion.allergyWarning,
      classifyAllergyWarningSeverity(safeSuggestion.allergyWarning));
  }

  card.appendChild(head);
  card.appendChild(description);
  card.appendChild(ingredients);
  card.appendChild(steps);
  if (badgeRow.childElementCount > 0) {
    card.appendChild(badgeRow);
  }

  const openAssistant = async () => {
    await launchCookingAssistant(safeSuggestion);
  };

  card.addEventListener("click", openAssistant);
  card.addEventListener("keydown", async (event) => {
    if (event.key === "Enter" || event.key === " ") {
      event.preventDefault();
      await openAssistant();
    }
  });

  return card;
}

async function launchCookingAssistant(recipe) {
  dom.assistantCard.classList.remove("hidden");
  dom.assistantIntro.textContent = `Preparando guia para ${recipe.title}...`;
  dom.assistantItems.innerHTML = "";
  dom.assistantSteps.innerHTML = "";
  dom.assistantSafety.innerHTML = "";
  setStatus(dom.assistantStatus, "Generando asistente de cocina...", "info");

  try {
    const payload = {
      email: getCurrentUser()?.email || "",
      allergens: state.allergens,
      historyId: state.activeHistoryId,
      recipe
    };

    const data = await postJson("/api/IANutri/CookingAssistant", payload);
    renderAssistant(data, recipe);
    setStatus(dom.assistantStatus, "Guia lista. Puedes cocinar paso a paso.", "success");
    setStatus(dom.resultStatus, `Asistente abierto para "${recipe.title}" justo debajo de Resultado.`, "success");
    highlightAssistantCard();
    dom.assistantCard.scrollIntoView({ behavior: "smooth", block: "start" });
  } catch (error) {
    renderAssistantFallback(recipe);
    setStatus(dom.assistantStatus, `No se pudo generar guia IA: ${error.message}`, "error");
    setStatus(dom.resultStatus, `Se abrio una guia base para "${recipe.title}" debajo de Resultado.`, "warning");
    highlightAssistantCard();
  }
}

function renderAssistant(data, fallbackRecipe) {
  const title = textOrFallback(data?.recipeTitle, fallbackRecipe.title);
  const intro = textOrFallback(data?.intro, `Vamos a cocinar ${title} paso a paso.`);
  const requiredItems = normalizeTextArray(data?.requiredItems, 14);
  const steps = normalizeTextArray(data?.stepByStep, 16);
  const safetyNotes = normalizeTextArray(data?.safetyNotes, 8);

  dom.assistantIntro.textContent = intro;
  renderList(dom.assistantItems, requiredItems.length > 0 ? requiredItems : fallbackRecipe.ingredients);
  renderList(dom.assistantSteps, steps.length > 0 ? steps : fallbackRecipe.steps, true);

  const finalSafety = safetyNotes.length > 0
    ? safetyNotes
    : [`Evita trazas de: ${state.allergens.join(", ") || "ningun alergeno configurado"}.`];
  renderList(dom.assistantSafety, finalSafety);
}

function renderAssistantFallback(recipe) {
  dom.assistantIntro.textContent = `Sigue estos pasos base para preparar ${recipe.title}.`;
  renderList(dom.assistantItems, recipe.ingredients);
  renderList(dom.assistantSteps, recipe.steps, true);
  renderList(dom.assistantSafety, [
    `Confirma etiquetas y evita contaminacion cruzada con: ${state.allergens.join(", ") || "ningun alergeno configurado"}.`
  ]);
}

function renderList(container, items, ordered = false) {
  container.innerHTML = "";
  const cleanItems = normalizeTextArray(items, 20);

  cleanItems.forEach((item) => {
    const element = document.createElement("li");
    element.textContent = item;
    container.appendChild(element);
  });

  if (ordered) {
    container.start = 1;
  }
}

function normalizeSuggestion(suggestion) {
  return {
    title: textOrFallback(suggestion?.title, "Plato sugerido"),
    description: textOrFallback(suggestion?.description, "Sin descripcion adicional."),
    estimatedTime: textOrFallback(suggestion?.estimatedTime, "20-30 min"),
    difficulty: textOrFallback(suggestion?.difficulty, "Media"),
    ingredients: normalizeTextArray(suggestion?.ingredients, 20, ["Ajusta ingredientes a tu despensa."]),
    steps: normalizeTextArray(suggestion?.steps, 20, ["Cocina con fuego medio y ajusta condimentos."]),
    allergensDetected: normalizeTextArray(suggestion?.allergensDetected, 8),
    safeSubstitutions: normalizeTextArray(suggestion?.safeSubstitutions, 8),
    allergyWarning: textOrFallback(suggestion?.allergyWarning, "")
  };
}

function mapHistoryForState(item) {
  return {
    id: textOrFallback(item?.id, ""),
    userInput: textOrFallback(item?.userInput, ""),
    option: textOrFallback(item?.option, "Personalizado"),
    reformulatedPrompt: sanitizeReformulationPreview(item?.reformulatedPrompt || ""),
    summary: textOrFallback(item?.summary, ""),
    globalWarnings: normalizeTextArray(item?.globalWarnings, 8),
    generalSubstitutions: normalizeTextArray(item?.generalSubstitutions, 8),
    suggestions: Array.isArray(item?.suggestions)
      ? item.suggestions.map(normalizeSuggestion)
      : [],
    createdAtUtc: textOrFallback(item?.createdAtUtc, "")
  };
}

function getModeKeyFromLabel(label) {
  const normalized = (label || "").trim().toLowerCase();
  if (normalized.includes("rapido")) {
    return "rapido-basico";
  }

  if (normalized.includes("cena")) {
    return "cena-ligera";
  }

  if (normalized.includes("semanal")) {
    return "menu-semanal";
  }

  return "";
}

function textOrFallback(value, fallback) {
  if (typeof value !== "string") {
    return fallback;
  }

  const trimmed = value.trim();
  return trimmed || fallback;
}

function normalizeTextArray(input, limit = 20, fallback = []) {
  if (!Array.isArray(input)) {
    return [...fallback];
  }

  const seen = new Set();
  const result = [];
  input.forEach((item) => {
    if (typeof item !== "string") {
      return;
    }

    const clean = item.trim();
    if (!clean) {
      return;
    }

    const key = clean.toLowerCase();
    if (seen.has(key)) {
      return;
    }

    seen.add(key);
    result.push(clean);
  });

  if (result.length === 0) {
    return [...fallback];
  }

  return result.slice(0, limit);
}

function formatDateTime(isoString) {
  if (!isoString) {
    return "Sin fecha";
  }

  const date = new Date(isoString);
  if (Number.isNaN(date.getTime())) {
    return "Sin fecha";
  }

  return date.toLocaleString("es-ES", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit"
  });
}

function truncateText(text, maxLength) {
  if (!text || text.length <= maxLength) {
    return text;
  }

  return `${text.slice(0, maxLength - 1).trim()}...`;
}

function renderAllergens(allergens) {
  dom.allergens.innerHTML = "";
  if (!allergens || allergens.length === 0) {
    const chip = document.createElement("span");
    chip.className = "chip chip-muted";
    chip.textContent = "Sin alergenos guardados";
    dom.allergens.appendChild(chip);
    return;
  }

  allergens.forEach((allergen) => {
    const chip = document.createElement("span");
    chip.className = "chip chip-muted";
    chip.textContent = allergen;
    dom.allergens.appendChild(chip);
  });
}

function updateCharCounter() {
  const length = (dom.textInput.value || "").length;
  dom.charCounter.textContent = `${length}/600`;
}

function toggleModeLoading(isLoading) {
  dom.modeButtons.forEach((button) => {
    button.disabled = isLoading;
  });
}

function setGenerateLoading(isLoading) {
  dom.generateButton.disabled = isLoading;
  dom.generateButton.textContent = isLoading ? "Generando..." : "Generar sugerencias";
}

function showReformulationPreview(text) {
  const clean = (text || "").trim();
  if (!clean) {
    hideReformulationPreview();
    return;
  }

  dom.reformulatedPrompt.textContent = clean;
  dom.reformulationBox.classList.remove("hidden");
}

function hideReformulationPreview() {
  dom.reformulatedPrompt.textContent = "";
  dom.reformulationBox.classList.add("hidden");
}

function sanitizeReformulationPreview(text) {
  if (typeof text !== "string") {
    return "";
  }

  const blockedTokens = ["modelo", "gpt", "openai", "enfoque:", "notas:"];
  const lines = text
    .replace(/\r/g, "\n")
    .split("\n")
    .map((line) => line.trim())
    .filter((line) => line.length > 0)
    .map((line) => line.replace(/^[-*•]\s*/, ""))
    .filter((line) => !blockedTokens.some((token) => line.toLowerCase().includes(token)));

  return lines.join(" ").trim();
}

function highlightAssistantCard() {
  dom.assistantCard.classList.remove("assistant-highlight");
  window.setTimeout(() => {
    dom.assistantCard.classList.add("assistant-highlight");
    window.setTimeout(() => dom.assistantCard.classList.remove("assistant-highlight"), 1300);
  }, 20);
}

function setStatus(node, message, type) {
  if (!node) {
    return;
  }

  node.textContent = message || "";
  node.classList.remove("error", "info", "success", "warning");
  if (!message) {
    return;
  }

  if (type === "error") {
    node.classList.add("error");
    return;
  }

  if (type === "warning") {
    node.classList.add("warning");
    return;
  }

  if (type === "success") {
    node.classList.add("success");
    return;
  }

  node.classList.add("info");
}

function addBadge(container, text, type) {
  const badge = document.createElement("span");
  badge.className = `badge ${type}`;
  badge.textContent = text;
  container.appendChild(badge);
}

function classifyWarningSeverity(text) {
  const value = (text || "").toLowerCase();
  if (containsAnyToken(value, ["peligro", "riesgo alto", "no apto", "contraindicado"])) {
    return "danger";
  }

  return "warning";
}

function classifyAllergyWarningSeverity(text) {
  const value = (text || "").toLowerCase();
  if (containsAnyToken(value, ["segura", "apta", "sin ", "no contiene", "ninguno"])) {
    return "success";
  }

  if (containsAnyToken(value, ["atencion", "riesgo", "peligro", "alerg", "no apto", "contraindicado"])) {
    return "danger";
  }

  return "warning";
}

function normalizeDetectedAllergens(values) {
  const items = normalizeTextArray(values, 8);
  return items.filter((item) => !isNoneToken(item));
}

function isNoneToken(value) {
  const token = (value || "").trim().toLowerCase();
  if (!token) {
    return true;
  }

  return containsAnyToken(token, ["ninguno", "ninguna", "sin alergenos", "none", "no detectado"]);
}

function containsIgnoreCase(list, value) {
  const expected = (value || "").trim().toLowerCase();
  if (!expected || !Array.isArray(list)) {
    return false;
  }

  return list.some((item) => (item || "").trim().toLowerCase() === expected);
}

function containsAnyToken(value, tokens) {
  return tokens.some((token) => value.includes(token));
}

async function postJson(url, payload) {
  const response = await fetch(url, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });

  const contentType = response.headers.get("content-type") || "";
  let body;
  if (contentType.includes("application/json")) {
    body = await response.json();
  } else {
    body = await response.text();
  }

  if (!response.ok) {
    const message =
      (typeof body === "string" && body) ||
      body?.message ||
      body?.Message ||
      `Error ${response.status}`;
    throw new Error(message);
  }

  return body;
}

async function resolveUserAllergens() {
  const cached = getCachedAllergens();
  const user = getCurrentUser();
  if (!user?.email) {
    return cached;
  }

  try {
    const response = await fetch(`/api/Allergens/User?email=${encodeURIComponent(user.email)}`);
    if (!response.ok) {
      return cached;
    }

    const data = await response.json();
    const remote = normalizeAllergenArray(data?.allergens);
    cacheAllergens(remote);
    return remote;
  } catch {
    return cached;
  }
}

function getCurrentUser() {
  try {
    const raw = localStorage.getItem(USER_STORAGE_KEY);
    if (!raw) {
      return null;
    }

    const parsed = JSON.parse(raw);
    if (!parsed || typeof parsed.email !== "string") {
      return null;
    }

    return {
      email: parsed.email.trim().toLowerCase()
    };
  } catch {
    return null;
  }
}

function cacheAllergens(allergens) {
  const normalized = normalizeAllergenArray(allergens);
  localStorage.setItem(CACHE_ALLERGENS_KEY, JSON.stringify(normalized));
  localStorage.setItem(LOCAL_ALLERGENS_KEY, JSON.stringify(normalized));
}

function getCachedAllergens() {
  const cached = readAllergensFromStorage(CACHE_ALLERGENS_KEY);
  if (cached.length > 0) {
    return cached;
  }

  return readAllergensFromStorage(LOCAL_ALLERGENS_KEY);
}

function readAllergensFromStorage(storageKey) {
  try {
    const raw = localStorage.getItem(storageKey);
    if (!raw) {
      return [];
    }

    return normalizeAllergenArray(JSON.parse(raw));
  } catch {
    return [];
  }
}

function normalizeAllergenArray(input) {
  if (!Array.isArray(input)) {
    return [];
  }

  const seen = new Set();
  const result = [];

  input.forEach((value) => {
    if (typeof value !== "string") {
      return;
    }

    const trimmed = value.trim();
    if (!trimmed) {
      return;
    }

    const key = trimmed.toLowerCase();
    if (seen.has(key)) {
      return;
    }

    seen.add(key);
    result.push(trimmed);
  });

  return result;
}



