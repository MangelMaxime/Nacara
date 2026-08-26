// A browser refuses synchronous WebAssembly instantiation above 8 MB on the main thread.

import { at } from "./paths.js";
import { config } from "./config.js";

const pending = new Map();
let worker = null;
let next = 0;

export function highlight(code, language = "fsharp") {
    if (config()?.highlighting !== "treesitter") return Promise.resolve(null);

    if (!worker) {
        worker = new Worker(at("tree-sitter/highlight-worker.js"), { type: "module" });
        worker.onmessage = (event) => {
            const { id, spans } = event.data;
            const resolve = pending.get(id);

            if (resolve) {
                pending.delete(id);
                resolve(spans);
            }
        };
    }

    const id = ++next;
    return new Promise((resolve) => {
        pending.set(id, resolve);
        worker.postMessage({ id, code, language });
    });
}

// val and let are both three letters, so rewriting a signature as a binding keeps every offset.
const parseable = (text) => (text.startsWith("val ") ? `let ${text.slice(4)} = ()` : text);

export async function colour(text, node, language = "fsharp") {
    node.textContent = text;

    const source = language === "fsharp" ? parseable(text) : text;

    const spans = (await highlight(source, language))
        ?.filter(([from]) => from < text.length)
        ?.map(([from, to, className]) => [from, Math.min(to, text.length), className]);

    if (!spans || !spans.length) return node;

    node.textContent = "";
    let cursor = 0;

    for (const [from, to, className] of spans) {
        if (from > cursor) node.append(text.slice(cursor, from));
        const span = document.createElement("span");
        span.className = className;
        span.textContent = text.slice(from, to);
        node.append(span);
        cursor = to;
    }

    if (cursor < text.length) node.append(text.slice(cursor));
    return node;
}
