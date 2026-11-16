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

/** The markup the sidebar writes for an entry, cloned from the templates it left for this. */
const entryOf = (templates, node) => {
    const template = node.children ? templates.group : templates.leaf;
    const item = template.content.firstElementChild.cloneNode(true);
    item.dataset.nacaraMenuAdded = "";
    item.hidden = true;

    const link = item.querySelector("a");
    link.href = node.url;
    (link.querySelector("span") ?? link).textContent = node.label;

    if (node.children) {
        const group = item.querySelector("details");
        group.dataset.nacaraMenuGroup = node.label;

        item.querySelector("details > ul").append(
            ...node.children.map((child) => entryOf(templates, child)),
        );
    }

    return item;
};

const linkOf = (item) => item.querySelector(":scope > a, :scope > details > summary > a");

/** The entries a pruned page left out, put back where they belong. */
const graft = (templates, list, nodes) => {
    if (!list) return;

    const present = new Map(
        [...list.children].map((item) => [linkOf(item)?.getAttribute("href"), item]),
    );

    for (const node of nodes) {
        const item = present.get(node.url);

        if (!item) {
            list.append(entryOf(templates, node));
            continue;
        }

        if (node.children) {
            graft(templates, item.querySelector(":scope > details > ul"), node.children);
        }
    }
};

export const menuFilter = (box) => {
    const sidebar = box.closest("[data-nacara-sidebar]");
    if (!sidebar) return;

    const templates = {
        leaf: sidebar.querySelector("[data-nacara-menu-leaf-template]"),
        group: sidebar.querySelector("[data-nacara-menu-group-template]"),
    };

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
        if (!source || !templates.leaf || !templates.group) return Promise.resolve();

        grafted =
            grafted ||
            fetch(source)
                .then((answer) => answer.json())
                .then((nodes) =>
                    graft(templates, sidebar.querySelector("[data-nacara-menu-list]"), nodes),
                )
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
