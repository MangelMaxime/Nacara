import { parseMeta } from "./config.js";
import { Snippet } from "./snippet.js";

const snippets = [];

addEventListener("message", (event) => {
    const data = event.data;
    if (!data || !data.__liveExample) return;

    const snippet = snippets.find((s) => s.frame && s.frame.contentWindow === event.source);
    if (!snippet) return;

    const kind = data.level === "error" ? "error" : data.level === "warn" ? "warning" : null;
    snippet.log(data.text, kind);
});

function start() {
    for (const figure of document.querySelectorAll(".nacara-code[data-meta]")) {
        const meta = parseMeta(figure.dataset.meta);
        if (!meta.live) continue;

        const snippet = new Snippet(figure, meta);
        snippet.mount();
        snippets.push(snippet);
    }
}

if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", start);
} else {
    start();
}
