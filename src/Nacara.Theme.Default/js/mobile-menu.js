export const mobileMenu = () => {
    const drawer = document.querySelector(".nacara-sidebar");

    if (!drawer) return;

    const setOpen = (open) => {
        drawer.dataset.open = String(open);

        for (const toggle of document.querySelectorAll("[data-nacara-menu-toggle]")) {
            toggle.setAttribute("aria-expanded", String(open));
        }
    };

    document.addEventListener("click", (event) => {
        const toggle = event.target.closest("[data-nacara-menu-toggle]");

        if (toggle) {
            setOpen(drawer.dataset.open !== "true");
            return;
        }

        // The scrim belongs to the layout, so anything landing on it is outside the drawer.
        if (drawer.dataset.open === "true" && !event.target.closest(".nacara-sidebar")) {
            setOpen(false);
        }
    });

    document.addEventListener("keydown", (event) => {
        if (event.key === "Escape" && drawer.dataset.open === "true") setOpen(false);
    });
};
