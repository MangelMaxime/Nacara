// A browser refuses synchronous WebAssembly instantiation above 8 MB on the main thread, and the F# grammar is 10.1 MB inflated.

import { Parser, Language, Query } from "./web-tree-sitter.js";

// This worker sits in tree-sitter/ beside the runtime it imports, so the grammars are one level up.
const base = new URL(".", import.meta.url);
const at = (name) => new URL(name, base).href;
const grammarAt = (language, name) => at(`../grammars/${language}/${name}`);

let parser = null;
let started = null;

const grammars = new Map();

function start() {
    started =
        started ||
        Parser.init({ locateFile: () => at("web-tree-sitter.wasm") }).then(() => {
            parser = new Parser();
        });

    return started;
}

async function grammarFor(name) {
    if (grammars.has(name)) return grammars.get(name);

    const loading = (async () => {
        await start();

        // Shipped gzipped, so the transfer does not depend on what the host does about Content-Encoding.
        const packed = await fetch(grammarAt(name, "grammar.wasm.gz"));
        const wasm = new Uint8Array(
            await new Response(
                packed.body.pipeThrough(new DecompressionStream("gzip")),
            ).arrayBuffer(),
        );

        const language = await Language.load(wasm);
        const query = new Query(
            language,
            await (await fetch(grammarAt(name, "highlights.scm"))).text(),
        );
        const classes = await (await fetch(grammarAt(name, "captures.json"))).json();

        return { language, query, classes };
    })();

    grammars.set(name, loading);
    return loading;
}

function classOf(classes, capture) {
    let name = capture;

    while (name) {
        if (classes[name]) return classes[name];
        const cut = name.lastIndexOf(".");
        if (cut < 0) return null;
        name = name.slice(0, cut);
    }

    return null;
}

// The narrowest capture wins, and among equals the one written later in the query file.
function spans(grammar, code) {
    parser.setLanguage(grammar.language);

    const tree = parser.parse(code);
    const found = grammar.query.captures(tree.rootNode);

    found.sort((a, b) => {
        const width = (c) => c.node.endIndex - c.node.startIndex;
        return width(b) - width(a) || a.patternIndex - b.patternIndex;
    });

    const painted = new Array(code.length).fill(null);

    for (const capture of found) {
        const className = classOf(grammar.classes, capture.name);
        if (!className) continue;

        for (let i = capture.node.startIndex; i < capture.node.endIndex; i++) {
            painted[i] = className;
        }
    }

    tree.delete();

    const out = [];
    let start = 0;

    for (let i = 1; i <= painted.length; i++) {
        if (i === painted.length || painted[i] !== painted[start]) {
            if (painted[start]) out.push([start, i, painted[start]]);
            start = i;
        }
    }

    return out;
}

onmessage = async (event) => {
    const { id, code, language } = event.data;

    try {
        const grammar = await grammarFor(language);
        postMessage({ id, spans: spans(grammar, code) });
    } catch {
        postMessage({ id, spans: [] });
    }
};
