const USER_STORAGE_KEY = "sb_user";
const LOCAL_ALLERGENS_KEY = "alergenosSeleccionados";
const CACHE_ALLERGENS_KEY = "sb_alergenos";
const HISTORIAL_KEY = "sb_historial";
const ALLERGEN_KEY_ALIASES = Object.freeze({
  gluten: "gluten",
  lacteos: "lacteos",
  "la cteos": "lacteos",
  "l cteos": "lacteos",
  lcteos: "lacteos",
  huevo: "huevo",
  "frutos secos": "frutos secos",
  frutossecos: "frutos secos",
  mariscos: "mariscos",
  soja: "soja"
});
const ALLERGEN_LABEL_BY_KEY = Object.freeze({
  gluten: "Gluten",
  lacteos: "Lácteos",
  huevo: "Huevo",
  "frutos secos": "Frutos secos",
  mariscos: "Mariscos",
  soja: "Soja"
});

document.addEventListener("DOMContentLoaded", () => {
  setupNavigation();
  setupAllergenCards();
  setupSaveButton();
  loadAllergenPreferences();
  renderHistorial();
});

function setupNavigation() {
  const navButtons = document.querySelectorAll(".nav-btn");
  const sections = document.querySelectorAll(".content-section");

  function showSection(sectionId) {
    sections.forEach((s) => s.classList.add("hidden"));
    navButtons.forEach((b) => b.classList.remove("active"));
    const target = document.getElementById(sectionId);
    if (target) target.classList.remove("hidden");
    const activeBtn = document.querySelector(`.nav-btn[data-section="${sectionId}"]`);
    if (activeBtn) activeBtn.classList.add("active");
  }

  // Show section from URL hash (e.g. coming from Comidas page)
  const hash = window.location.hash.replace("#", "");
  if (hash && document.getElementById(hash)) {
    showSection(hash);
  } else {
    // Default: welcome section active
    const homeBtn = document.querySelector('.nav-btn[data-section="welcome-section"]');
    if (homeBtn) homeBtn.classList.add("active");
  }

  navButtons.forEach((button) => {
    button.addEventListener("click", (event) => {
      event.preventDefault();
      const sectionId = button.getAttribute("data-section");
      showSection(sectionId);
    });
  });
}

function setupAllergenCards() {
  document.querySelectorAll('.alergeno-card input[type="checkbox"]').forEach((input) => {
    input.addEventListener("change", () => {
      input.parentElement.classList.toggle("selected", input.checked);
    });
  });
}

function setupSaveButton() {
  const saveButton = document.getElementById("save-alergenos");
  if (!saveButton) {
    return;
  }

  saveButton.addEventListener("click", saveAllergenPreferences);
}

async function loadAllergenPreferences() {
  const user = getCurrentUser();
  if (!user?.email) {
    const fallbackAllergens = getCachedAllergens();
    applyAllergensToForm(fallbackAllergens);
    showStatus("Modo invitado: preferencias guardadas en este navegador.", "warning");
    return;
  }

  try {
    const response = await fetch(`/api/Allergens/User?email=${encodeURIComponent(user.email)}`);
    if (!response.ok) {
      const message = await readErrorMessage(response);
      throw new Error(message);
    }

    const data = await response.json();
    const allergens = normalizeAllergenArray(readAllergensFromPayload(data));
    applyAllergensToForm(allergens);
    cacheAllergens(allergens);
    showStatus("Preferencias cargadas desde Firebase.", "info");
  } catch (error) {
    const fallbackAllergens = getCachedAllergens();
    applyAllergensToForm(fallbackAllergens);
    showStatus(`No se pudieron cargar preferencias remotas. ${error.message}`, "error");
  }
}

async function saveAllergenPreferences() {
  const selectedAllergenKeys = getSelectedAllergenKeys();
  const user = getCurrentUser();
  const saveButton = document.getElementById("save-alergenos");

  if (!user?.email) {
    cacheAllergens(selectedAllergenKeys);
    showStatus("Guardado local (modo invitado). Inicia sesión para persistir en Firebase.", "warning");
    return;
  }

  if (saveButton) {
    saveButton.disabled = true;
    saveButton.textContent = "Guardando...";
  }

  try {
    const response = await fetch("/api/Allergens/User", {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        email: user.email,
        allergens: selectedAllergenKeys
      })
    });

    if (!response.ok) {
      const message = await readErrorMessage(response);
      throw new Error(message);
    }

    const data = await response.json();
    const responseAllergens = normalizeAllergenArray(readAllergensFromPayload(data));
    const effectiveAllergens = responseAllergens.length > 0
      ? responseAllergens
      : normalizeAllergenArray(selectedAllergenKeys);
    cacheAllergens(effectiveAllergens);
    applyAllergensToForm(effectiveAllergens);
    showStatus("Preferencias guardadas en Firebase correctamente.", "success");
  } catch (error) {
    showStatus(`No se pudieron guardar tus preferencias. ${error.message}`, "error");
  } finally {
    if (saveButton) {
      saveButton.disabled = false;
      saveButton.textContent = "Guardar preferencias";
    }
  }
}

function getSelectedAllergenKeys() {
  return normalizeAllergenKeyArray(
    Array.from(document.querySelectorAll('input[name="alergeno"]:checked')).map((checkbox) => checkbox.value)
  );
}

function applyAllergensToForm(allergens) {
  const selectedKeys = new Set(
    normalizeAllergenKeyArray(allergens)
  );
  document.querySelectorAll('.alergeno-card input[type="checkbox"]').forEach((input) => {
    input.checked = selectedKeys.has(normalizeAllergenKey(input.value));
    input.parentElement.classList.toggle("selected", input.checked);
  });
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
      email: parsed.email.trim().toLowerCase(),
      username: typeof parsed.username === "string" ? parsed.username : ""
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

function readAllergensFromPayload(payload) {
  if (!payload || typeof payload !== "object") {
    return [];
  }

  if (Array.isArray(payload.allergenKeys)) {
    return payload.allergenKeys;
  }

  if (Array.isArray(payload.AllergenKeys)) {
    return payload.AllergenKeys;
  }

  if (Array.isArray(payload.allergens)) {
    return payload.allergens;
  }

  if (Array.isArray(payload.Allergens)) {
    return payload.Allergens;
  }

  return [];
}

function normalizeAllergenKey(value) {
  if (typeof value !== "string") {
    return "";
  }

  const token = value
    .trim()
    .toLowerCase()
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/[^a-z0-9\s]/g, " ")
    .replace(/\s+/g, " ")
    .trim();

  if (!token) {
    return "";
  }

  return ALLERGEN_KEY_ALIASES[token] || token;
}

function normalizeAllergenKeyArray(input) {
  if (!Array.isArray(input)) {
    return [];
  }

  const seen = new Set();
  const normalizedKeys = [];

  input.forEach((value) => {
    const key = normalizeAllergenKey(value);
    if (!key || seen.has(key) || !ALLERGEN_LABEL_BY_KEY[key]) {
      return;
    }

    seen.add(key);
    normalizedKeys.push(key);
  });

  return normalizedKeys;
}

function normalizeAllergenArray(input) {
  return normalizeAllergenKeyArray(input).map((key) => ALLERGEN_LABEL_BY_KEY[key]);
}

async function readErrorMessage(response) {
  try {
    const contentType = response.headers.get("content-type") || "";
    if (contentType.includes("application/json")) {
      const payload = await response.json();
      if (typeof payload === "string") {
        return payload;
      }

      if (payload?.message) {
        return payload.message;
      }

      if (Array.isArray(payload?.invalidAllergens) && payload.invalidAllergens.length > 0) {
        return `Alérgenos no válidos: ${payload.invalidAllergens.join(", ")}`;
      }
    }

    const text = await response.text();
    return text || `Error ${response.status}`;
  } catch {
    return `Error ${response.status}`;
  }
}

function showStatus(message, type) {
  const messageNode = document.getElementById("confirmation-message");
  if (!messageNode) {
    return;
  }

  const colorByType = {
    success: "green",
    info: "#1f3b4d",
    warning: "#8a6d3b",
    error: "#b00020"
  };

  messageNode.style.display = "block";
  messageNode.style.color = colorByType[type] || colorByType.info;
  messageNode.textContent = message;

  if (type === "success") {
    setTimeout(() => {
      messageNode.style.display = "none";
    }, 2200);
  }
}

// ── Historial de escaneos ────────────────────────────────────────────────────

function getHistorialKey() {
  const user = getCurrentUser();
  return user ? `${HISTORIAL_KEY}_${user.email}` : HISTORIAL_KEY;
}

function getHistorial() {
  try {
    return JSON.parse(localStorage.getItem(getHistorialKey())) || [];
  } catch {
    return [];
  }
}

function guardarEnHistorial(entrada) {
  const historial = getHistorial();
  // Evitar duplicados consecutivos del mismo código
  if (historial.length > 0 && historial[0].codigo === entrada.codigo) return;
  historial.unshift(entrada);
  // Limitar a 50 entradas
  if (historial.length > 50) historial.pop();
  localStorage.setItem(getHistorialKey(), JSON.stringify(historial));
  renderHistorial();
}

function limpiarHistorial() {
  if (!confirm("¿Borrar todo el historial de escaneos?")) return;
  localStorage.removeItem(getHistorialKey());
  renderHistorial();
}

function renderHistorial() {
  const lista = document.getElementById("historial-lista");
  if (!lista) return;
  const historial = getHistorial();
  if (historial.length === 0) {
    lista.innerHTML = "<p class=\"historial-vacio\">No hay escaneos registrados aún.</p>";
    return;
  }
  lista.innerHTML = historial.map(e => `
    <div class="historial-item">
      ${e.imagen
        ? `<img src="${e.imagen}" alt="${e.nombre}" onerror="this.style.display='none'">`
        : `<img src="/src/media/placeholder.png" alt="sin imagen" style="display:none">`
      }
      <div class="historial-item-info">
        <strong>${e.nombre}</strong>
        <span>${e.codigo} &mdash; ${e.fecha}</span>
      </div>
    </div>
  `).join("");
}
