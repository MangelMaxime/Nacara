export const foldMemory = () => {
    const keyOf = (group) => `nacara-menu:${group.dataset.nacaraMenuGroup}`;

    for (const group of document.querySelectorAll("[data-nacara-menu-group]")) {
        if (group.closest('[data-nacara-menu-memory="false"]')) continue;

        const remembered = sessionStorage.getItem(keyOf(group));

        if (remembered !== null && !group.querySelector('[aria-current="page"]')) {
            group.open = remembered === "true";
        }

        group.addEventListener("toggle", () => {
            sessionStorage.setItem(keyOf(group), String(group.open));
        });
    }
};

export const menuFilter = (box) => {
    const sidebar = box.closest(".nacara-sidebar");
    if (!sidebar) return;

    const items = [...sidebar.querySelectorAll("li")];
    const folded = new Map();

    const labelOf = (item) => {
        const own = item.querySelector(":scope > a, :scope > details > summary");

        return (own ? own.textContent : "").trim().toLowerCase();
    };

    box.addEventListener("input", () => {
        const term = box.value.trim().toLowerCase();

        if (term === "") {
            for (const item of items) item.hidden = false;

            for (const [group, open] of folded) group.open = open;

            folded.clear();
            return;
        }

        // Deepest first, so a group can ask whether anything under it survived.
        for (const item of [...items].reverse()) {
            const kept = [...item.querySelectorAll(":scope li")].some((child) => !child.hidden);

            item.hidden = !labelOf(item).includes(term) && !kept;

            const group = item.querySelector(":scope > details");

            if (group && !item.hidden) {
                if (!folded.has(group)) folded.set(group, group.open);

                group.open = true;
            }
        }
    });

    box.addEventListener("keydown", (event) => {
        if (event.key === "Escape" && box.value !== "") {
            event.stopPropagation();
            box.value = "";
            box.dispatchEvent(new Event("input"));
        }
    });
};
