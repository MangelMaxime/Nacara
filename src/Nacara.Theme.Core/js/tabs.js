let groups = 0;

export class NacaraTabs extends HTMLElement {
    connectedCallback() {
        // Moving the element around the document connects it again.
        if (this.dataset.upgraded === "true") return;

        const panels = [...this.querySelectorAll(":scope > nacara-tab")];
        if (panels.length === 0) return;

        this.dataset.upgraded = "true";

        const group = ++groups;
        const list = document.createElement("div");
        list.className = "nacara-tabs__list";
        list.setAttribute("role", "tablist");

        const buttons = panels.map((panel, index) => {
            const button = document.createElement("button");
            button.type = "button";
            button.id = `nacara-tab-${group}-${index}`;
            button.className = "nacara-tabs__tab";
            button.textContent = panel.dataset.label || `Tab ${index + 1}`;
            button.setAttribute("role", "tab");

            panel.id ||= `nacara-tabpanel-${group}-${index}`;
            panel.setAttribute("role", "tabpanel");
            panel.setAttribute("aria-labelledby", button.id);
            button.setAttribute("aria-controls", panel.id);

            button.addEventListener("click", () => this.select(index));
            list.append(button);

            return button;
        });

        this.buttons = buttons;
        this.panels = panels;
        this.prepend(list);
        this.select(this.restoreIndex());

        list.addEventListener("keydown", (event) => {
            const delta = event.key === "ArrowRight" ? 1 : event.key === "ArrowLeft" ? -1 : 0;
            if (delta === 0) return;

            event.preventDefault();

            const current = buttons.findIndex((button) => button.tabIndex === 0);
            const next = (current + delta + buttons.length) % buttons.length;

            this.select(next);
            buttons[next].focus();
        });
    }

    restoreIndex() {
        const key = this.dataset.sync;

        if (!key) return 0;

        const stored = sessionStorage.getItem(`nacara-tabs:${key}`);
        const index = this.panels.findIndex((panel) => panel.dataset.label === stored);

        return index === -1 ? 0 : index;
    }

    select(index) {
        this.panels.forEach((panel, current) => {
            panel.hidden = current !== index;
        });

        this.buttons.forEach((button, current) => {
            button.setAttribute("aria-selected", String(current === index));
            button.tabIndex = current === index ? 0 : -1;
        });

        const key = this.dataset.sync;
        const label = this.panels[index]?.dataset.label;
        if (!key || !label) return;

        sessionStorage.setItem(`nacara-tabs:${key}`, label);

        for (const other of document.querySelectorAll(
            `nacara-tabs[data-sync="${CSS.escape(key)}"]`,
        )) {
            if (other === this) continue;

            const target = other.panels?.findIndex((panel) => panel.dataset.label === label);

            if (target > -1) other.select(target);
        }
    }
}
