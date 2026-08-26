// GetToolTipText lays out for a fixed-width grid and returns the XML doc twice, the second time in literal <summary> tags.

const ENTITIES = {
    "&apos;": "'",
    "&quot;": '"',
    "&amp;": "&",
    "&lt;": "<",
    "&gt;": ">",
    "&#39;": "'",
};

// "Cannot find ident for tooltip" is the compiler's sentinel for no identifier at that position.
const NOTHING = "Cannot find ident for tooltip";

const decode = (text) => text.replace(/&(?:apos|quot|amp|lt|gt|#39);/g, (m) => ENTITIES[m] ?? m);
const unpad = (text) => decode(text).replace(/(\S)[ \t]{2,}/g, "$1 ");

// Signatures arrive token-separated - "Library . Point", "< 'T >".
const tidy = (text) =>
    unpad(text)
        .replace(/\s*\.\s*/g, ".")
        .replace(/\s*<\s*/g, "<")
        .replace(/\s+>/g, ">")
        .split("\n")
        .map((line) => line.trimEnd())
        .join("\n")
        .trimEnd();

export function format(lines) {
    const signature = [];
    const doc = [];
    const meta = [];
    const seen = new Set();

    for (const raw of lines ?? []) {
        const text = (raw ?? "").trim();
        if (!text || text === NOTHING) continue;
        if (/^<\/?[a-zA-Z][^>]*>$/.test(text)) continue; // <summary> and its like

        if (/^(Full name|Assembly)\s*:/.test(text)) {
            meta.push(tidy(raw));
            continue;
        }

        if (!signature.length || /^'[A-Za-z0-9_]+\s+is\s/.test(text)) {
            signature.push(tidy(raw));
            continue;
        }

        const key = decode(text).replace(/\s+/g, " "); // the two copies of the doc match here
        if (seen.has(key)) continue;
        seen.add(key);
        doc.push(unpad(raw).trim());
    }

    return { signature, doc, meta };
}
