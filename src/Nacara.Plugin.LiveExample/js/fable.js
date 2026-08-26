// The protocol is an F# union serialised by Thoth: every message is a JSON array carried as a string.

import { at } from "./paths.js";
import { config, load } from "./config.js";

// crypto.randomUUID exists only in a secure context; the worker still decodes this field with Guid.TryParse.
export function uuid() {
    if (crypto.randomUUID) return crypto.randomUUID();

    const bytes = crypto.getRandomValues(new Uint8Array(16));
    bytes[6] = (bytes[6] & 0x0f) | 0x40;
    bytes[8] = (bytes[8] & 0x3f) | 0x80;
    const hex = [...bytes].map((b) => b.toString(16).padStart(2, "0")).join("");
    return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
}

let worker = null;
let booting = null;
const waiters = [];

export const post = (message) => worker.postMessage(JSON.stringify(message));

// Requests that can be in flight more than once at a time carry a Guid.
export const expect = (name, id) =>
    new Promise((resolve, reject) => waiters.push({ name, id, resolve, reject }));

function receive(event) {
    if (typeof event.data !== "string" || !event.data) return;

    let message;
    try {
        message = JSON.parse(event.data);
    } catch {
        return;
    }

    const [name] = message;
    const failed = name === "CompilerCrashed" || name === "LoadFailed";
    const index = waiters.findIndex(
        (waiter) =>
            failed ||
            (waiter.name === name && (waiter.id === undefined || waiter.id === message[1])),
    );

    if (index < 0) return;

    const waiter = waiters.splice(index, 1)[0];
    (failed ? waiter.reject : waiter.resolve)(message);
}

// Path is what Fable matches a source file on, not somewhere to fetch from.
async function precompiled() {
    const info = config().precompiled;
    if (!info) return null;

    // F# keeps an inline member's body out of the assembly.
    const chunks = await Promise.all(
        (info.inlineExprChunks ?? []).map((chunk) =>
            fetch(at(`${config().precompiledAt}/${chunk}`)).then((response) => {
                if (!response.ok) throw new Error(`${chunk}: ${response.status}`);
                return response.text();
            }),
        ),
    );

    return {
        CompilerVersion: info.compilerVersion,
        Files: info.files.map((file) => ({
            Path: file.path,
            RootModule: file.rootModule,
            OutPath: at(`${config().precompiledAt}/${file.outPath}`),
        })),
        InlineExprHeaders: info.inlineExprHeaders ?? [],
        InlineExprChunks: chunks,
    };
}

export function boot() {
    if (booting) return booting;

    booting = (async () => {
        await load();

        worker = new Worker(at(`${config().compiler}/worker.min.js`)); // classic: it importScripts the compiler
        worker.onmessage = receive;

        const extraRefs = ["Browser.Dom", "Browser.Blob", "Browser.Event", "Browser.WebStorage"];

        if (config().precompiled) extraRefs.push("Fable.Precompiled");

        post([
            "CreateChecker",
            at(config().refs),
            extraRefs,
            config().assemblySuffix,
            [],
            await precompiled(),
        ]);
        await expect("Loaded");
    })();

    return booting;
}
