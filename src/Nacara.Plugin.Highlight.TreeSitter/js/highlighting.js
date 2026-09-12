import { at } from "./paths.js";

const pending = new Map();
let worker = null;
let next = 0;

function ask(code, language) {
    if (!worker) {
        worker = new Worker(at("highlight-worker.js"), { type: "module" });
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

export async function paint(text, node, language) {
    node.textContent = text;

    const spans = (await ask(text, language))
        ?.filter(([from]) => from < text.length)
        ?.map(([from, to, className]) => [from, Math.min(to, text.length), className]);

    if (!spans || !spans.length) return false;

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
    return true;
}
