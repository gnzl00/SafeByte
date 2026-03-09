(() => {
  const NAVBAR_USER_STORAGE_KEY = "sb_user";
  const NAVBAR_SESSION_STORAGE_KEYS = ["sb_user", "sb_alergenos", "alergenosSeleccionados"];

  document.addEventListener("DOMContentLoaded", () => {
    const menuRoot = document.querySelector("[data-user-menu]");
    const trigger = menuRoot?.querySelector("[data-user-menu-trigger]");
    const panel = menuRoot?.querySelector("[data-user-menu-panel]");

    if (!menuRoot || !trigger || !panel) {
      return;
    }

    hydrateUserData(menuRoot);

    const openMenu = () => {
      menuRoot.classList.add("open");
      trigger.setAttribute("aria-expanded", "true");
      panel.setAttribute("aria-hidden", "false");
    };

    const closeMenu = () => {
      menuRoot.classList.remove("open");
      trigger.setAttribute("aria-expanded", "false");
      panel.setAttribute("aria-hidden", "true");
    };

    trigger.addEventListener("click", (event) => {
      event.preventDefault();
      if (menuRoot.classList.contains("open")) {
        closeMenu();
        return;
      }

      openMenu();
    });

    menuRoot.querySelectorAll("[data-user-menu-link]").forEach((link) => {
      link.addEventListener("click", () => {
        closeMenu();
      });
    });

    document.addEventListener("click", (event) => {
      if (menuRoot.contains(event.target)) {
        return;
      }

      closeMenu();
    });

    document.addEventListener("keydown", (event) => {
      if (event.key === "Escape") {
        closeMenu();
      }
    });

    const logoutLink = menuRoot.querySelector("[data-user-logout]");
    if (logoutLink) {
      logoutLink.addEventListener("click", () => {
        NAVBAR_SESSION_STORAGE_KEYS.forEach((key) => localStorage.removeItem(key));
      });
    }
  });

  function hydrateUserData(menuRoot) {
    const user = readCurrentUser();
    const nameNode = menuRoot.querySelector("[data-user-name]");
    const emailNode = menuRoot.querySelector("[data-user-email]");
    const avatarNode = menuRoot.querySelector("[data-user-avatar]");

    const displayName = resolveDisplayName(user);
    const displayEmail = user?.email || "Sin correo activo";
    const avatarText = displayName.charAt(0).toUpperCase() || "U";

    if (nameNode) {
      nameNode.textContent = displayName;
      nameNode.title = displayName;
    }

    if (emailNode) {
      emailNode.textContent = displayEmail;
      emailNode.title = displayEmail;
    }

    if (avatarNode) {
      avatarNode.textContent = avatarText;
      avatarNode.title = displayName;
    }
  }

  function readCurrentUser() {
    try {
      const raw = localStorage.getItem(NAVBAR_USER_STORAGE_KEY);
      if (!raw) {
        return null;
      }

      const parsed = JSON.parse(raw);
      if (!parsed || typeof parsed.email !== "string") {
        return null;
      }

      const email = parsed.email.trim().toLowerCase();
      const username = typeof parsed.username === "string" ? parsed.username.trim() : "";
      if (!email) {
        return null;
      }

      return { email, username };
    } catch {
      return null;
    }
  }

  function resolveDisplayName(user) {
    if (!user) {
      return "Tu cuenta";
    }

    if (user.username) {
      return user.username;
    }

    const emailName = user.email.split("@")[0]?.trim();
    if (emailName) {
      return emailName;
    }

    return "Tu cuenta";
  }
})();
