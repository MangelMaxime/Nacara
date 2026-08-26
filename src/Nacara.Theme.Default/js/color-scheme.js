const STORAGE_KEY = "nacara-theme";

const dark = matchMedia("(prefers-color-scheme: dark)");

export const chosen = () => {
    try {
        return localStorage.getItem(STORAGE_KEY) || "system";
    } catch {
        return "system"; /* private mode */
    }
};

export const settle = (setting) => {
    const root = document.documentElement;

    root.dataset.themeSetting = setting;
    root.dataset.theme = setting === "system" ? (dark.matches ? "dark" : "light") : setting;
};

export const themePicker = () => {
    dark.addEventListener("change", () => {
        if (document.documentElement.dataset.themeSetting === "system") settle("system");
    });

    for (const select of document.querySelectorAll("[data-nacara-theme]")) {
        select.value = chosen();

        select.addEventListener("change", () => {
            settle(select.value);

            try {
                localStorage.setItem(STORAGE_KEY, select.value);
            } catch {
                /* private mode */
            }
        });
    }
};
