export const mobileMenu = () => {
    document.addEventListener("click", (event) => {
        const toggle = event.target.closest("[data-nacara-menu-toggle]");
        if (!toggle) return;

        const sidebar = document.querySelector(".nacara-sidebar");
        if (!sidebar) return;

        const open = sidebar.dataset.open !== "true";

        sidebar.dataset.open = String(open);
        toggle.setAttribute("aria-expanded", String(open));
    });
};
