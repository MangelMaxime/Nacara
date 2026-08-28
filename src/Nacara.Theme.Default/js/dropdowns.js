const OPEN_DELAY = 120;
const CLOSE_DELAY = 250;

export const dropdowns = () => {
    // Touch has no hover, and a hybrid laptop would spend its first tap on one.
    const hoverable = matchMedia("(hover: hover) and (pointer: fine)");

    const setOpen = (dropdown, open) => {
        dropdown.dataset.open = String(open);

        if (!open) delete dropdown.dataset.pinned;

        dropdown
            .querySelector("[data-nacara-dropdown]")
            ?.setAttribute("aria-expanded", String(open));
    };

    const closeAll = (except) => {
        for (const dropdown of document.querySelectorAll('.nacara-dropdown[data-open="true"]')) {
            if (dropdown !== except) setOpen(dropdown, false);
        }
    };

    document.addEventListener("click", (event) => {
        const trigger = event.target.closest("[data-nacara-dropdown]");
        const dropdown = trigger?.closest(".nacara-dropdown");

        closeAll(dropdown);

        if (!dropdown) return;

        // Clicking one the pointer already opened keeps it: that is what a click adds to a hover.
        if (dropdown.dataset.open === "true" && dropdown.dataset.pinned !== "true") {
            dropdown.dataset.pinned = "true";
            return;
        }

        setOpen(dropdown, dropdown.dataset.open !== "true");

        if (dropdown.dataset.open === "true") dropdown.dataset.pinned = "true";
    });

    document.addEventListener("keydown", (event) => {
        if (event.key === "Escape") closeAll();
    });

    for (const dropdown of document.querySelectorAll(".nacara-dropdown")) {
        let timer = null;

        const after = (delay, action) => {
            clearTimeout(timer);
            timer = setTimeout(action, delay);
        };

        dropdown.addEventListener("mouseenter", () => {
            if (!hoverable.matches) return;

            after(OPEN_DELAY, () => {
                closeAll(dropdown);
                setOpen(dropdown, true);
            });
        });

        dropdown.addEventListener("mouseleave", () => {
            if (!hoverable.matches) return;

            clearTimeout(timer);

            if (dropdown.dataset.pinned === "true") return;

            after(CLOSE_DELAY, () => setOpen(dropdown, false));
        });
    }
};
