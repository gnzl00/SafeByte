const USER_STORAGE_KEY = "sb_user";
const LOCAL_ALLERGENS_KEY = "alergenosSeleccionados";
const CACHE_ALLERGENS_KEY = "sb_alergenos";

let userAllergens = [];

document.addEventListener("DOMContentLoaded", async () => {
  document.getElementById("modal-comida").classList.add("hidden");
  userAllergens = await resolveUserAllergens();
  mostrarComidas();
  agregarEventosBusqueda();
});

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

function mostrarComidas() {
  mostrarComidasFiltradas(filtrarComidas(document.getElementById("search-input")?.value ?? ""));
}

function abrirModal(comida) {
  document.getElementById("modal-img").src = comida.imagen;
  document.getElementById("modal-title").innerText = comida.nombre;
  document.getElementById("modal-ingredientes").innerText = `Ingredientes: ${comida.ingredientes}`;
  document.getElementById("modal-receta").innerText = `Receta: ${comida.receta}`;
  document.getElementById("modal-alergenos").innerText = `Alérgenos: ${
    comida.alergenos.length > 0 ? comida.alergenos.join(", ") : "Ninguno"
  }`;

  document.getElementById("modal-comida").classList.remove("hidden");
}

document.getElementById("modal-comida").addEventListener("click", (event) => {
  if (event.target === document.getElementById("modal-comida")) {
    document.getElementById("modal-comida").classList.add("hidden");
  }
});

function agregarEventosBusqueda() {
  const searchInput = document.getElementById("search-input");
  if (!searchInput) {
    return;
  }

  searchInput.addEventListener("input", () => {
    const filtered = filtrarComidas(searchInput.value);
    mostrarComidasFiltradas(filtered);
  });
}

function filtrarComidas(searchTerm) {
  const loweredTerm = (searchTerm || "").toLowerCase();

  return comidas.filter((comida) => {
    const hasBlockedAllergen = comida.alergenos.some((alergeno) => userAllergens.includes(alergeno));
    const matchesTerm = comida.nombre.toLowerCase().includes(loweredTerm);
    return !hasBlockedAllergen && matchesTerm;
  });
}

function mostrarComidasFiltradas(comidasFiltradas) {
  const listaComidas = document.getElementById("comidas-lista");
  listaComidas.innerHTML = "";

  comidasFiltradas.forEach((comida) => {
    const comidaDiv = document.createElement("div");
    comidaDiv.classList.add("comida-item");
    comidaDiv.innerHTML = `
      <img src="${comida.imagen}" alt="${comida.nombre}" class="comida-img" />
      <h3>${comida.nombre}</h3>
    `;

    comidaDiv.addEventListener("click", () => abrirModal(comida));
    listaComidas.appendChild(comidaDiv);
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
