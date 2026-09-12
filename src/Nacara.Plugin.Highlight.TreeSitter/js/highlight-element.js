import { paint } from "./highlighting.js";

const config = () => globalThis.__nacaraTreeSitter ?? {};

const warned = new Set();

const languageOf = (name) => {
    const wanted = (name ?? "").trim().toLowerCase();
    if (!wanted) return null;

    const found = (config().languages ?? {})[wanted];

    if (!found && !warned.has(wanted)) {
        warned.add(wanted);
        console.warn(
            `[tree-sitter] no grammar is shipped for '${wanted}', so it is shown as plain text. Name it in TreeSitter.browser.`,
        );
    }

    return found ?? null;
};

// Code written inside an element is indented with the markup around it.
const dedent = (text) => {
    const lines = text.split("\n");

    while (lines.length && !lines[0].trim()) lines.shift();
    while (lines.length && !lines[lines.length - 1].trim()) lines.pop();

    const indent = lines
        .filter((line) => line.trim())
        .reduce((least, line) => Math.min(least, line.length - line.trimStart().length), Infinity);

    if (!Number.isFinite(indent)) return lines.join("\n");

    return lines.map((line) => line.slice(indent)).join("\n");
};

let observer = null;

const whenVisible = (element, then) => {
    if (!("IntersectionObserver" in globalThis)) return then();

    observer =
        observer ||
        new IntersectionObserver((entries) => {
            for (const entry of entries) {
                if (!entry.isIntersecting) continue;
                observer.unobserve(entry.target);
                entry.target.waiting?.();
            }
        });

    element.waiting = then;
    observer.observe(element);
};

export class NacaraHighlight extends HTMLElement {
    static observedAttributes = ["language", "code"];

    connectedCallback() {
        this.given = this.given ?? this.read();
        this.render();
    }

    // An attribute already on the tag is announced before the element is connected, which is
    // before it has read the code it was written with.
    attributeChangedCallback(name, before, after) {
        if (before === after || this.given === undefined || !this.isConnected) return;
        if (name === "code") this.given = this.getAttribute("code") ?? "";
        this.render();
    }

    get code() {
        return this.given ?? "";
    }

    set code(value) {
        this.given = String(value ?? "");
        if (this.isConnected) this.render();
    }

    read() {
        const script = this.querySelector(':scope > script[type="text/plain"]');
        if (script) return dedent(script.textContent);

        const attribute = this.getAttribute("code");
        if (attribute !== null) return attribute;

        return dedent(this.textContent);
    }

    target() {
        const existing = this.querySelector("code");
        if (existing) return existing;

        const pre = document.createElement("pre");
        const code = document.createElement("code");
        pre.append(code);
        this.replaceChildren(pre);

        return code;
    }

    render() {
        const node = this.target();
        const language = languageOf(this.getAttribute("language"));

        node.textContent = this.code;
        this.dataset.state = "plain";

        if (!language) return;

        if (this.getAttribute("loading") === "lazy") {
            whenVisible(this, () => this.colour(node, language));
        } else {
            this.colour(node, language);
        }
    }

    async colour(node, language) {
        const text = this.code;
        this.dataset.state = "loading";

        const coloured = await paint(text, node, language);

        // Another render may have answered while the grammar was being fetched.
        if (text !== this.code) return;

        this.dataset.state = coloured ? "coloured" : "plain";
        this.dispatchEvent(new CustomEvent("nacara-highlighted", { bubbles: true }));
    }
}

// A custom element is inline until something says otherwise, and it holds a block.
if (globalThis.CSSStyleSheet?.prototype.replaceSync && document.adoptedStyleSheets) {
    const sheet = new CSSStyleSheet();
    sheet.replaceSync("nacara-highlight{display:block}");
    document.adoptedStyleSheets = [...document.adoptedStyleSheets, sheet];
}

customElements.define("nacara-highlight", NacaraHighlight);
