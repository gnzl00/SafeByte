const USER_STORAGE_KEY = "sb_user";
const LOCAL_ALLERGENS_KEY = "alergenosSeleccionados";
const CACHE_ALLERGENS_KEY = "sb_alergenos";
const FAVORITES_KEY = "sb_favoritos";

window.addEventListener("DOMContentLoaded", () => {
  // Al llegar a la página de login, siempre limpiar sesión
  clearUserSession();
  const signInButton = document.querySelector("#sign-in-btn");
  const signUpButton = document.querySelector("#sign-up-btn");
  const container = document.querySelector(".container");

  signUpButton.addEventListener("click", () => {
    container.classList.add("sign-up-mode");
  });

  signInButton.addEventListener("click", () => {
    container.classList.remove("sign-up-mode");
  });

  document.querySelector(".sign-in-form").addEventListener("submit", async (event) => {
    event.preventDefault();
    setError("login-error", "");

    const email = document.getElementById("login-email").value;
    const password = document.getElementById("login-password").value;

    try {
      const responseData = await loginUser(email, password);
      persistUserSession(responseData?.user);
      redirectToHome();
    } catch (error) {
      setError("login-error", error.message);
    }
  });

  document.querySelector(".sign-up-form").addEventListener("submit", async (event) => {
    event.preventDefault();
    setError("signup-error", "");

    const username = document.getElementById("signup-username").value;
    const email = document.getElementById("signup-email").value;
    const password = document.getElementById("signup-password").value;

    try {
      const responseData = await registerUser(username, email, password);
      persistUserSession(responseData?.user);
      redirectToHome();
    } catch (error) {
      setError("signup-error", error.message);
    }
  });

});

function redirectToHome() {
  window.location.href = "/Home/Home";
}

async function registerUser(username, email, password) {
  const response = await fetch("/api/Auth/Register", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ username, email, password })
  });

  return handleApiResponse(response);
}

async function loginUser(email, password) {
  const response = await fetch("/api/Auth/Login", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    // Se mantiene username por compatibilidad con validación actual del modelo User.
    body: JSON.stringify({ username: email, email, password })
  });

  return handleApiResponse(response);
}

async function handleApiResponse(response) {
  const contentType = response.headers.get("content-type") || "";

  if (contentType.includes("application/json")) {
    const payload = await response.json();
    if (!response.ok) {
      throw new Error(payload?.message || payload?.title || JSON.stringify(payload));
    }

    return payload;
  }

  const text = await response.text();
  if (!response.ok) {
    throw new Error(text || `Error ${response.status}`);
  }

  return { message: text };
}

function persistUserSession(user) {
  if (!user || typeof user.email !== "string") {
    return;
  }

  const normalizedUser = {
    email: user.email.trim().toLowerCase(),
    username: typeof user.username === "string" ? user.username.trim() : ""
  };

  localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(normalizedUser));

  const allergens = normalizeAllergenArray(user.allergens);
  localStorage.setItem(CACHE_ALLERGENS_KEY, JSON.stringify(allergens));
  localStorage.setItem(LOCAL_ALLERGENS_KEY, JSON.stringify(allergens));
}

function clearUserSession() {
  localStorage.removeItem(USER_STORAGE_KEY);
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

function setError(elementId, message) {
  const element = document.getElementById(elementId);
  if (element) {
    element.textContent = message;
  }
}
