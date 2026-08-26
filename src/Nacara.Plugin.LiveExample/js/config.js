import { at } from "./paths.js";

let current = null;

export async function load() {
    current = current || (await (await fetch(at("config.json"))).json());
    return current;
}

export const config = () => current;

// The theme puts whatever the fence parser did not recognise into data-meta.
const TABS = ["result", "console", "output"];

const TAB_ALIASES = { javascript: "output" };

export function target(name) {
    const decided = globalThis.__nacaraLiveExample ?? {};
    const targets = decided.targets ?? {};

    if (!name) return targets[decided.target ?? "javascript"];

    const wanted = name.toLowerCase();

    return Object.values(targets).find(
        (item) => item.name === wanted || item.aliases.includes(wanted),
    );
}

export function parseMeta(value) {
    const meta = { live: false, preset: null, tab: null, target: null };

    for (const token of (value || "").split(/\s+/).filter(Boolean)) {
        if (token === "live") meta.live = true;
        else if (token.startsWith("preset=")) meta.preset = token.slice(7);
        else if (token.startsWith("target=")) meta.target = token.slice(7).toLowerCase();
        else if (token.startsWith("tab=")) {
            const asked = token.slice(4).toLowerCase();
            const tab = TAB_ALIASES[asked] ?? asked;

            if (TABS.includes(tab)) {
                meta.tab = tab;
            } else {
                console.warn(
                    `[live-example] no tab called '${asked}', expected one of ${TABS.join(", ")}`,
                );
            }
        }
    }

    return meta;
}

function preset(name) {
    const chosen = name || current.defaultPreset;
    if (!chosen) return null;

    const found = current.presets[chosen];
    if (found) return found;

    console.warn(`[live-example] no preset called '${chosen}'`);
    return null;
}

export function presetFiles(name) {
    return (preset(name)?.files ?? []).map((file) => ({ Name: file.name, Content: file.content }));
}

export function presetShell(name) {
    const found = preset(name);
    return { css: found?.css ?? null, template: found?.template ?? null };
}
