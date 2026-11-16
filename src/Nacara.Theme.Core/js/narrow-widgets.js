// The breakpoint is read from --nacara-narrow, which the theme sets beside its media query.
export const narrowWidgets = () => {
    const end = document.querySelector("[data-nacara-widgets-end]");
    const shelf = document.querySelector("[data-nacara-widgets-shelf]");

    if (!end || !shelf) return;

    const movable = [...end.children].filter((item) => !item.querySelector("[data-nacara-search]"));

    if (movable.length === 0) return;

    const width =
        getComputedStyle(document.documentElement).getPropertyValue("--nacara-narrow").trim() ||
        "860px";
    const narrow = matchMedia(`(max-width: ${width})`);
    let placed = null;

    const place = () => {
        if (placed === narrow.matches) return;

        placed = narrow.matches;

        if (narrow.matches) {
            for (const item of movable) shelf.append(item);

            shelf.hidden = false;
        } else {
            for (const item of movable) end.append(item);

            shelf.hidden = true;
        }
    };

    place();
    narrow.addEventListener("change", place);
};
