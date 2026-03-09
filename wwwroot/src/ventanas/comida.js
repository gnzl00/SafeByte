const USER_STORAGE_KEY = "sb_user";
const LOCAL_ALLERGENS_KEY = "alergenosSeleccionados";
const CACHE_ALLERGENS_KEY = "sb_alergenos";
const FAVORITES_KEY = "sb_favoritos";

let userAllergens = [];
let mostrandoFavoritos = false;

function getFavoritesKey() {
  const user = getCurrentUser();
  return user?.email ? `${FAVORITES_KEY}_${user.email}` : null;
}

document.addEventListener("DOMContentLoaded", async () => {
  document.getElementById("modal-comida").classList.add("hidden");
  userAllergens = await resolveUserAllergens();
  mostrarComidas();
  agregarEventosBusqueda();
  agregarEventoFavoritos();
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

  document.getElementById("compartir-feedback").innerText = "";
  document.getElementById("btn-compartir").onclick = () => compartirComida(comida);
  document.getElementById("modal-comida").classList.remove("hidden");
}

async function compartirComida(comida) {
  const alergenos = comida.alergenos.length > 0 ? comida.alergenos.join(", ") : "Ninguno";
  const recetaLimpia = comida.receta
    .split("\n")
    .map(l => l.trim())
    .filter(l => l.length > 0)
    .join("\n   ");
  const texto = [
    `🍽️ ¡Te comparto esta receta desde Food DNA!`,
    ``,
    `📌 ${comida.nombre}`,
    ``,
    `🧅 Ingredientes:`,
    `   ${comida.ingredientes}`,
    ``,
    `👨‍🍳 Receta:`,
    `   ${recetaLimpia}`,
    ``,
    `⚠️ Alérgenos: ${alergenos}`,
    ``,
    `— Descubre más recetas adaptadas a tus alergias en Food DNA 🌿`
  ].join("\n");
  const feedback = document.getElementById("compartir-feedback");

  if (navigator.share) {
    try {
      await navigator.share({ title: comida.nombre, text: texto });
    } catch (e) {
      if (e.name !== "AbortError") {
        feedback.innerText = "No se pudo compartir.";
      }
    }
  } else {
    try {
      await navigator.clipboard.writeText(texto);
      feedback.innerText = "✓ Copiado al portapapeles";
      setTimeout(() => feedback.innerText = "", 3000);
    } catch {
      feedback.innerText = "No se pudo copiar.";
    }
  }
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
    mostrandoFavoritos = false;
    document.getElementById("btn-favoritos").classList.remove("active");
    const filtered = filtrarComidas(searchInput.value);
    mostrarComidasFiltradas(filtered);
  });
}

function agregarEventoFavoritos() {
  const btn = document.getElementById("btn-favoritos");
  if (!btn) return;

  btn.addEventListener("click", () => {
    mostrandoFavoritos = !mostrandoFavoritos;
    btn.classList.toggle("active", mostrandoFavoritos);
    document.getElementById("search-input").value = "";
    if (mostrandoFavoritos) {
      const favs = getFavorites();
      const comidasFav = comidas.filter(c => favs.includes(c.nombre));
      mostrarComidasFiltradas(comidasFav);
    } else {
      mostrarComidas();
    }
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

  if (comidasFiltradas.length === 0) {
    listaComidas.innerHTML = `<p class="sin-resultados">${mostrandoFavoritos ? "No tienes platos favoritos aún." : "No se encontraron comidas."}</p>`;
    return;
  }

  comidasFiltradas.forEach((comida) => {
    const esFav = isFavorite(comida.nombre);
    const comidaDiv = document.createElement("div");
    comidaDiv.classList.add("comida-item");
    comidaDiv.innerHTML = `
      <button class="fav-btn ${esFav ? 'fav-active' : ''}" data-nombre="${comida.nombre}" title="${esFav ? 'Quitar de favoritos' : 'Añadir a favoritos'}">
        ${esFav ? '⭐' : '☆'}
      </button>
      <img src="${comida.imagen}" alt="${comida.nombre}" class="comida-img" />
      <h3>${comida.nombre}</h3>
    `;

    comidaDiv.querySelector(".fav-btn").addEventListener("click", (e) => {
      e.stopPropagation();
      const user = getCurrentUser();
      if (!user?.email) {
        alert("Debes iniciar sesión para guardar favoritos.");
        return;
      }
      toggleFavorite(comida.nombre);
      if (mostrandoFavoritos) {
        const favs = getFavorites();
        mostrarComidasFiltradas(comidas.filter(c => favs.includes(c.nombre)));
      } else {
        mostrarComidas();
      }
    });

    comidaDiv.addEventListener("click", () => abrirModal(comida));
    listaComidas.appendChild(comidaDiv);
  });
}

function getFavorites() {
  try {
    const key = getFavoritesKey();
    if (!key) return [];
    return JSON.parse(localStorage.getItem(key)) || [];
  } catch { return []; }
}

function isFavorite(nombre) {
  return getFavorites().includes(nombre);
}

function toggleFavorite(nombre) {
  const key = getFavoritesKey();
  if (!key) return;
  const favs = getFavorites();
  const idx = favs.indexOf(nombre);
  if (idx === -1) {
    favs.push(nombre);
  } else {
    favs.splice(idx, 1);
  }
  localStorage.setItem(key, JSON.stringify(favs));
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
