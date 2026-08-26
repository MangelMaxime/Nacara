export const dropdowns = () => {
    const closeAll = (except) => {
        for (const dropdown of document.querySelectorAll('.nacara-dropdown[data-open="true"]')) {
            if (dropdown === except) continue;

            dropdown.dataset.open = "false";
            dropdown
                .querySelector("[data-nacara-dropdown]")
                ?.setAttribute("aria-expanded", "false");
        }
    };

    document.addEventListener("click", (event) => {
        const trigger = event.target.closest("[data-nacara-dropdown]");
        const dropdown = trigger?.closest(".nacara-dropdown");

        closeAll(dropdown);

        if (!dropdown) return;

        const open = dropdown.dataset.open !== "true";

        dropdown.dataset.open = String(open);
        trigger.setAttribute("aria-expanded", String(open));
    });

    document.addEventListener("keydown", (event) => {
        if (event.key === "Escape") closeAll();
    });
};
