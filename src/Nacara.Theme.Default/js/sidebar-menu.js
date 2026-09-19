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

/** One entry of a menu, as the markup the sidebar writes for it. */
const entryOf = (node) => {
    const item = document.createElement("li");
    item.dataset.nacaraMenuAdded = "";
    item.hidden = true;

    const link = document.createElement("a");
    link.href = node.url;
    link.textContent = node.label;

    if (!node.children) {
        link.className = "nacara-sidebar__link";
        item.append(link);

        return item;
    }

    link.className = "nacara-sidebar__group-link";

    const group = document.createElement("details");
    group.className = "nacara-sidebar__group";
    group.dataset.nacaraMenuGroup = node.label;

    const summary = document.createElement("summary");
    summary.className = "nacara-sidebar__group-title";
    summary.append(link);

    const list = document.createElement("ul");
    list.className = "nacara-sidebar__list";
    list.append(...node.children.map(entryOf));

    group.append(summary, list);
    item.append(group);

    return item;
};

const linkOf = (item) => item.querySelector(":scope > a, :scope > details > summary > a");

/** The entries a pruned page left out, put back where they belong. */
const graft = (list, nodes) => {
    if (!list) return;

    const present = new Map(
        [...list.children].map((item) => [linkOf(item)?.getAttribute("href"), item]),
    );

    for (const node of nodes) {
        const item = present.get(node.url);

        if (!item) {
            list.append(entryOf(node));
            continue;
        }

        if (node.children) {
            graft(item.querySelector(":scope > details > ul"), node.children);
        }
    }
};

export const menuFilter = (box) => {
    const sidebar = box.closest(".nacara-sidebar");
    if (!sidebar) return;

    const folded = new Map();
    const source = box.dataset.nacaraMenuSource;
    let grafted = null;

    const labelOf = (item) => {
        const own = item.querySelector(":scope > a, :scope > details > summary");

        return (own ? own.textContent : "").trim().toLowerCase();
    };

    // The menu on a page of a large section holds a part of it. The rest is fetched once and put
    // into the tree hidden, so filtering it is filtering an ordinary menu.
    const whole = () => {
        if (!source) return Promise.resolve();

        grafted =
            grafted ||
            fetch(source)
                .then((answer) => answer.json())
                .then((nodes) => graft(sidebar.querySelector(".nacara-sidebar__list"), nodes))
                .catch(() => {});

        return grafted;
    };

    const apply = () => {
        const term = box.value.trim().toLowerCase();
        const items = [...sidebar.querySelectorAll("li")];

        if (term === "") {
            for (const item of items) item.hidden = item.dataset.nacaraMenuAdded !== undefined;

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
    };

    box.addEventListener("focus", whole);

    box.addEventListener("input", async () => {
        await whole();
        apply();
    });

    box.addEventListener("keydown", (event) => {
        if (event.key === "Escape" && box.value !== "") {
            event.stopPropagation();
            box.value = "";
            box.dispatchEvent(new Event("input"));
        }
    });
};
