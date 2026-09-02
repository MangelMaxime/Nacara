import { at } from "./paths.js";
import { config } from "./config.js";

// A relative specifier resolves against the importing module's URL, and these modules are blobs.
const resolvable = (code, names) =>
    code.replace(/(\bfrom\s*)"\.\/([^"]+)"/g, (whole, from, name) =>
        names.has(name) ? `${from}"${name}"` : whole,
    );

const BARE = `<!doctype html><meta charset="utf-8"><body></body>`;

function assemble(template, head, body) {
    const withHead = template.includes("</head>")
        ? template.replace("</head>", `${head}</head>`)
        : head + template;

    return withHead.includes("</body>")
        ? withHead.replace("</body>", `${body}</body>`)
        : withHead + body;
}

// What a specifier resolves to, so an unmapped one can be named.
const specifiers = (code) => [...code.matchAll(/\bfrom\s*"([^"]+)"/g)].map((matched) => matched[1]);

export function sandbox(js, presetModules, { css, template } = {}) {
    // Fable emits bare specifiers, which are import-map keys.
    const imports = { "fable-library-js/": at(`${config().compiler}/fable-library-js/`) };
    const names = new Set(Object.keys(presetModules));

    if (config().verbose) {
        const unmapped = specifiers(js).filter(
            (name) =>
                !names.has(name.replace(/^\.\//, "")) && !name.startsWith("fable-library-js/"),
        );

        if (unmapped.length > 0) {
            console.debug("[nacara-live] specifiers no module answers to:", unmapped);
        }
    }

    for (const [name, code] of Object.entries(presetModules)) {
        imports[name] = URL.createObjectURL(
            new Blob([resolvable(code, names)], { type: "text/javascript" }),
        );
    }

    const head = `<script type="importmap">${JSON.stringify({ imports })}</script>
${css ? `<style>${css}</style>` : ""}
<script>
  for (const level of ["log", "info", "warn", "error"]) {
    const native = console[level];
    console[level] = (...args) => {
      parent.postMessage({ __liveExample: true, level,
        text: args.map((a) => typeof a === "string" ? a : JSON.stringify(a)).join(" ") }, "*");
      native.apply(console, args);
    };
  }
  addEventListener("error", (e) => parent.postMessage({ __liveExample: true, level: "error", text: e.message }, "*"));
  addEventListener("unhandledrejection", (e) => parent.postMessage({ __liveExample: true, level: "error", text: String(e.reason) }, "*"));
</script>`;

    const page = assemble(template || BARE, head, `<script type="module">\n${js}\n</script>`);

    const frame = document.createElement("iframe");
    frame.className = "nacara-live__frame";
    frame.setAttribute("sandbox", "allow-scripts allow-same-origin");
    frame.srcdoc = page;
    return frame;
}
