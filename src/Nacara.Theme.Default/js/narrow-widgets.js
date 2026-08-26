// The breakpoint is the one responsive.css hides the navbar's links at.
export const narrowWidgets = () => {
    const end = document.querySelector(".nacara-navbar__items--end");
    const sidebar = document.querySelector(".nacara-sidebar");

    if (!end || !sidebar) return;

    const movable = [...end.children].filter((item) => !item.querySelector("[data-nacara-search]"));

    if (movable.length === 0) return;

    const shelf = document.createElement("ul");
    shelf.className = "nacara-sidebar__widgets";

    const narrow = matchMedia("(max-width: 860px)");
    let placed = null;

    const place = () => {
        if (placed === narrow.matches) return;

        placed = narrow.matches;

        if (narrow.matches) {
            sidebar.append(shelf);

            for (const item of movable) shelf.append(item);
        } else {
            for (const item of movable) end.append(item);

            shelf.remove();
        }
    };

    place();
    narrow.addEventListener("change", place);
};
