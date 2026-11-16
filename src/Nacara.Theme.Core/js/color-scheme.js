const STORAGE_KEY = "nacara-theme";

const SETTINGS = ["light", "dark", "system"];

const dark = matchMedia("(prefers-color-scheme: dark)");

export const chosen = () => {
    try {
        const stored = localStorage.getItem(STORAGE_KEY);
        return SETTINGS.includes(stored) ? stored : "system";
    } catch {
        return "system"; /* private mode */
    }
};

export const settle = (setting) => {
    const root = document.documentElement;

    root.dataset.themeSetting = setting;
    root.dataset.theme = setting === "system" ? (dark.matches ? "dark" : "light") : setting;
};

const showPickers = (setting) => {
    for (const select of document.querySelectorAll("[data-nacara-theme]")) {
        select.value = setting;
    }

    for (const button of document.querySelectorAll("[data-nacara-theme-pick]")) {
        button.setAttribute("aria-pressed", String(button.dataset.nacaraThemePick === setting));
    }
};

const choose = (setting) => {
    if (!SETTINGS.includes(setting)) return;

    settle(setting);
    showPickers(setting);

    try {
        localStorage.setItem(STORAGE_KEY, setting);
    } catch {
        /* private mode */
    }
};

export const themePicker = () => {
    dark.addEventListener("change", () => {
        if (document.documentElement.dataset.themeSetting === "system") settle("system");
    });

    showPickers(chosen());

    for (const select of document.querySelectorAll("[data-nacara-theme]")) {
        select.addEventListener("change", () => choose(select.value));
    }

    document.addEventListener("click", (event) => {
        const button = event.target.closest("[data-nacara-theme-pick]");
        if (button) choose(button.dataset.nacaraThemePick);
    });
};
