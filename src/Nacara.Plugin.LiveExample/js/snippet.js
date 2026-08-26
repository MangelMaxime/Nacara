import { config, presetFiles, presetShell, target } from "./config.js";
import { boot, expect, post, uuid } from "./fable.js";
import * as Editor from "./editor.js";
import { colour } from "./highlighting.js";
import { format } from "./tooltip.js";
import { sandbox } from "./sandbox.js";

const FILE = "Snippet.fs";

// One worker holds one project at a time, and every block calls its file Snippet.fs.
let holder = null;

// ParsedCode carries no id, so two parses at once would each take whichever answer arrived first.
let speaking = Promise.resolve();

const ignore = () => {};

const modifier = () => (/Mac|iPhone|iPad|iPod/.test(navigator.platform ?? "") ? "\u2318" : "Ctrl");

const el = (tag, className, text) => {
    const node = document.createElement(tag);
    if (className) node.className = className;
    if (text !== undefined) node.textContent = text;
    return node;
};

// Fable.Standalone lowercases the glyph, so these keys are lowercase.
const GLYPHS = {
    class: "class",
    enum: "enum",
    value: "variable",
    variable: "variable",
    interface: "interface",
    module: "namespace",
    method: "method",
    property: "property",
    field: "property",
    function: "function",
    event: "variable",
    error: "text",
};

export class Snippet {
    constructor(figure, meta) {
        this.figure = figure;
        this.meta = meta;
        // A block that folds some of its lines is drawn as more than one <pre>.
        this.original =
            figure.dataset.source ??
            [...figure.querySelectorAll(".nacara-code__body code")]
                .map((node) => node.textContent)
                .join("");
        this.view = null;
        this.ideReady = false;
        this.parseTimer = null;
        this.frame = null;
        this.output = null;
        this.outputColoured = false;
        this.chosen = null;
        this.presetModules = null;
        this.target = target(meta.target);
        this.expanded = false;
        this.scrolled = 0;
        this.editing = false;

        this.onKeyDown = (event) => {
            if (event.key !== "Escape" || event.defaultPrevented) return;
            if (Editor.completing(this.view)) return;
            if (this.expanded) this.expand(false);
        };
    }

    mount() {
        const bar = el("div", "nacara-live__actions");

        this.runButton = el("button", "nacara-live__run", this.verb(true));
        this.runButton.type = "button";
        this.runButton.addEventListener("click", () => this.run());

        this.resetButton = el("button", "nacara-live__reset", "Reset");
        this.resetButton.type = "button";
        this.resetButton.hidden = true;
        this.resetButton.addEventListener("click", () => this.reset());

        this.expandButton = el("button", "nacara-live__expand", "Expand");
        this.expandButton.type = "button";
        this.expandButton.title = "Give this example the whole window";
        this.expandButton.addEventListener("click", () => this.expand(!this.expanded));

        this.hint = el("span", "nacara-live__hint");
        this.hint.append(el("kbd", null, `${modifier()}+Enter`), ` to ${this.verb()}`);
        this.hint.hidden = true;

        this.status = el("span", "nacara-live__status");

        const said = el("div", "nacara-live__said");
        said.append(this.hint, this.status);

        bar.append(said, this.expandButton, this.resetButton, this.runButton);
        this.figure.append(bar);
        this.figure.dataset.live = "ready";
    }

    expand(wanted) {
        this.expanded = wanted;

        if (wanted) {
            this.scrolled = window.scrollY;
            this.figure.dataset.expanded = "true";
            document.documentElement.dataset.liveExpanded = "true";
            this.expandButton.textContent = "Close";
            addEventListener("keydown", this.onKeyDown);
        } else {
            delete this.figure.dataset.expanded;
            delete document.documentElement.dataset.liveExpanded;
            this.expandButton.textContent = "Expand";
            removeEventListener("keydown", this.onKeyDown);
            window.scrollTo({ top: this.scrolled, behavior: "instant" });
        }

        this.view?.requestMeasure();
    }

    verb(capitalised) {
        const word = this.target.runs ? "run" : "compile";
        return capitalised ? word[0].toUpperCase() + word.slice(1) : word;
    }

    record(stats, wallMs, emitted) {
        const pane = this.views.stats;
        if (!pane) return;

        if (!pane.childElementCount) {
            pane.append(
                this.statsRow(
                    ["run", "parse", "check", "transform", "total", "emitted"],
                    "nacara-live__stats-head",
                ),
            );
            this.runs = 0;
        }

        const ms = (value) => (value === undefined ? "-" : `${Math.round(value)} ms`);

        pane.append(
            this.statsRow([
                String(++this.runs),
                ms(stats?.FCS_parsing),
                ms(stats?.FCS_checker),
                ms(stats?.Fable_transform),
                ms(wallMs),
                emitted.length ? emitted.join(", ") : "all",
            ]),
        );

        pane.scrollTop = pane.scrollHeight;
    }

    statsRow(cells, className) {
        const row = el(
            "div",
            className ? `nacara-live__stats-row ${className}` : "nacara-live__stats-row",
        );

        for (const cell of cells) row.append(el("span", null, cell));

        return row;
    }

    say(text) {
        this.status.textContent = text ?? "";
        this.showHint();
    }

    showHint() {
        this.hint.hidden = !this.editing || this.status.textContent !== "";
    }

    line(text, kind) {
        return el(
            "div",
            kind ? `nacara-live__line nacara-live__line--${kind}` : "nacara-live__line",
            text,
        );
    }

    log(text, kind) {
        this.views.console.append(this.line(text, kind));
    }

    async run() {
        if (this.runButton.disabled) return; // already compiling, from the button or the keyboard

        this.runButton.disabled = true;

        try {
            if (!this.view) {
                this.say("Fetching the compiler...");
                await boot();
                await Editor.load();
                this.becomeEditor();
                this.ideReady = true;

                await this.parse();
            }

            this.say("compiling...");
            await this.compile();
        } catch (error) {
            this.say("failed");
            this.log(Array.isArray(error) ? String(error[1]) : String(error), "error");
        } finally {
            this.runButton.disabled = false;
        }
    }

    becomeEditor() {
        const host = el("div", "nacara-live__editor");
        this.figure.querySelector(".nacara-code__body").replaceChildren(host);

        host.addEventListener("focusin", () => {
            this.editing = true;
            this.showHint();
        });

        host.addEventListener("focusout", () => {
            this.editing = false;
            this.showHint();
        });

        this.view = Editor.create({
            parent: host,
            doc: this.original,
            complete: (ctx) => this.complete(ctx),
            tooltip: (view, pos) => this.tooltip(view, pos),
            onChange: () => this.scheduleParse(),
            onRun: () => this.run(),
        });

        this.buildPanels();
        this.resetButton.hidden = false;
        this.figure.dataset.live = "editor";
    }

    buildPanels() {
        this.panels = el("div", "nacara-live__panels");

        const tabs = el("div", "nacara-live__tabs");
        this.views = {};
        this.buttons = {};

        const wanted = [
            ...(this.target.runs ? [["result", "Result"]] : []),
            ["console", "Console"],
            ["output", this.target.label],
            ...(config().stats ? [["stats", "Stats"]] : []),
        ];

        for (const [key, label] of wanted) {
            const button = el("button", "nacara-live__tab", label);
            button.type = "button";
            button.addEventListener("click", () => this.show(key, true));
            tabs.append(button);
            this.buttons[key] = button;

            const pane = el("div", `nacara-live__pane nacara-live__pane--${key}`);
            pane.hidden = true;
            this.views[key] = pane;
        }

        this.panels.append(this.buildGrip(), tabs, ...Object.values(this.views));
        this.figure.insertBefore(this.panels, this.figure.querySelector(".nacara-live__actions"));
        this.show("console");
    }

    buildGrip() {
        const grip = el("div", "nacara-live__grip");
        grip.setAttribute("role", "separator");
        grip.setAttribute("aria-orientation", "horizontal");
        grip.setAttribute("aria-label", "Resize the output");
        grip.tabIndex = 0;

        const current = () => this.panels.getBoundingClientRect().height;

        const resize = (px) => {
            const capped = Math.min(Math.max(px, 96), window.innerHeight * 0.8);
            this.panels.style.setProperty("--nacara-live-panels-height", `${Math.round(capped)}px`);
            this.panels.dataset.resized = "";
        };

        grip.addEventListener("pointerdown", (event) => {
            event.preventDefault();
            grip.setPointerCapture(event.pointerId);

            const startY = event.clientY;
            const startHeight = current();
            const move = (e) => resize(startHeight - (e.clientY - startY));

            const stop = () => {
                grip.removeEventListener("pointermove", move);
                grip.removeEventListener("pointerup", stop);
                grip.removeEventListener("pointercancel", stop);
            };

            grip.addEventListener("pointermove", move);
            grip.addEventListener("pointerup", stop);
            grip.addEventListener("pointercancel", stop);
        });

        grip.addEventListener("dblclick", () => {
            delete this.panels.dataset.resized;
            this.panels.style.removeProperty("--nacara-live-panels-height");
        });

        grip.addEventListener("keydown", (event) => {
            const step = { ArrowUp: 24, ArrowDown: -24 }[event.key];
            if (step === undefined) return;
            event.preventDefault();
            resize(current() + step);
        });

        return grip;
    }

    show(key, chosen) {
        if (!this.views[key]) key = "output";

        if (chosen) this.chosen = key;

        for (const [name, pane] of Object.entries(this.views)) {
            pane.hidden = name !== key;
            this.buttons[name].setAttribute("aria-selected", String(name === key));
        }

        // highlight is null when the site named no grammar for this language.
        if (key === "output" && this.output && !this.outputColoured && this.target.highlight) {
            this.outputColoured = true;
            const node = this.views.output.querySelector("pre");
            if (node) colour(this.output, node, this.target.highlight);
        }
    }

    reset() {
        if (!this.view) return;

        this.view.dispatch({
            changes: { from: 0, to: this.view.state.doc.length, insert: this.original },
        });

        this.views.console.style.minHeight = "";
        for (const pane of Object.values(this.views)) pane.replaceChildren();
        this.output = null;
        this.outputColoured = false;
        this.say("");
    }

    files() {
        return [
            ...presetFiles(this.meta.preset),
            { Name: FILE, Content: this.view.state.doc.toString() },
        ];
    }

    async compile() {
        const emit = this.presetModules ? [FILE] : [];

        holder = this;
        post(["CompileFiles", this.files(), emit, this.target.language, []]);
        const startedAt = Date.now();
        const [, codes, , errors, stats] = await expect("CompilationsFinished");

        this.record(stats, Date.now() - startedAt, emit);

        const console_ = this.views.console;
        console_.style.minHeight = `${console_.getBoundingClientRect().height}px`;

        console_.replaceChildren(
            ...(errors ?? []).map((error) =>
                this.line(
                    `${error.IsWarning ? "warning" : "error"} ${error.FileName}(${error.StartLine},${error.StartColumn}): ${error.Message}`,
                    error.IsWarning ? "warning" : "error",
                ),
            ),
        );

        this.paintDiagnostics(errors);

        if ((errors ?? []).some((error) => !error.IsWarning)) {
            this.say("did not compile");
            this.show("console");
            return;
        }

        const compiled = codes[codes.length - 1];
        this.output = compiled;
        this.outputColoured = false;
        this.views.output.replaceChildren(el("pre", "nacara-live__js", compiled));

        if (!this.views.output.hidden) this.show("output");

        if (!this.target.runs) {
            this.show(this.chosen ?? this.meta.tab ?? config().tab ?? "output");
            this.say("");
            this.parse().catch(() => {});
            return;
        }

        // CompileFiles answers one module per emitted file, in order, keyed by the F# file name Fable emits as its specifier.
        if (!this.presetModules) {
            const files = this.files();
            this.presetModules = {};

            codes.slice(0, -1).forEach((code, index) => {
                this.presetModules[files[index].Name] = code;
            });
        }

        this.frame = sandbox(compiled, this.presetModules, presetShell(this.meta.preset));
        this.views.result.replaceChildren(this.frame);
        const asked = this.meta.tab ?? config().tab;
        this.show(this.chosen ?? asked ?? "console");

        if (!asked) {
            this.frame.addEventListener("load", () => {
                setTimeout(() => {
                    if (this.chosen) return;
                    const printed = this.views.console.childElementCount > 0;
                    const drew = (this.frame.contentDocument?.body?.childElementCount ?? 0) > 0;
                    if (!printed && drew) this.show("result");
                }, 50);
            });
        }

        this.say("");
        this.parse().catch(() => {}); // keep what the checker knows in step with what compiled
    }

    scheduleParse() {
        if (!this.ideReady) return;

        clearTimeout(this.parseTimer);
        this.parseTimer = setTimeout(() => this.parse().catch(() => {}), 400);
    }

    async parse() {
        const ask = async () => {
            holder = this;
            post(["ParseFile", FILE, this.files(), []]);
            const [, errors] = await expect("ParsedCode");
            this.paintDiagnostics(errors);
        };

        speaking = speaking.catch(ignore).then(ask);
        return speaking;
    }

    async held() {
        await speaking.catch(ignore);
        if (holder !== this) await this.parse().catch(ignore);
    }

    paintDiagnostics(errors) {
        const CM = Editor.editor();
        if (!this.view || !CM) return;

        const doc = this.view.state.doc;

        const diagnostics = (errors ?? [])
            .filter((error) => !error.FileName || error.FileName === FILE)
            .map((error) => {
                const from = Editor.offsetOf(doc, error.StartLine, error.StartColumn);
                return {
                    from,
                    to: Math.max(Editor.offsetOf(doc, error.EndLine, error.EndColumn), from + 1),
                    severity: error.IsWarning ? "warning" : "error",
                    message: error.Message,
                };
            });

        this.view.dispatch(CM.setDiagnostics(this.view.state, diagnostics));
    }

    async tooltip(view, pos) {
        if (!this.ideReady) return null;

        await this.held();

        const { line, column, lineText } = Editor.at1(view, pos);
        const id = uuid();
        post(["GetTooltipForFile", id, FILE, line, column, lineText]);
        const [, , lines] = await expect("FoundTooltip", id);

        const { signature, doc, meta } = format(lines);
        if (!signature.length && !doc.length) return null;

        const word = view.state.wordAt(pos);

        return {
            pos: word ? word.from : pos,
            end: word ? word.to : pos,
            above: true,
            create: () => {
                const dom = el("div", "nacara-live__tt");

                if (signature.length) {
                    const node = el("pre", "nacara-live__tt-signature");
                    dom.append(node);
                    colour(signature.join("\n"), node); // fills in when the worker answers
                }

                if (doc.length) {
                    dom.append(
                        el("div", "nacara-live__tt-label", "Description"),
                        el("div", "nacara-live__tt-doc", doc.join("\n")),
                    );
                }

                if (meta.length) dom.append(el("div", "nacara-live__tt-meta", meta.join("\n")));

                return { dom };
            },
        };
    }

    // Fable.Standalone answers with a name and a glyph and nothing else.
    async complete(ctx) {
        if (!this.ideReady) return null;

        const before = ctx.matchBefore(/[\w'.]+/);
        if (!before && !ctx.explicit) return null;

        await this.held();

        const { line, column, lineText } = Editor.at1(ctx.view, ctx.pos);
        const id = uuid();
        post(["GetCompletionsForFile", id, FILE, line, column, lineText]);
        const [, , items] = await expect("FoundCompletions", id);

        if (!items?.length) return null;

        const segment = /[A-Za-z0-9_']*$/.exec(before?.text ?? "")[0];

        return {
            from: ctx.pos - segment.length,
            options: items.map((item) => {
                const glyph = String(Array.isArray(item.Glyph) ? item.Glyph[0] : item.Glyph);
                return { label: item.Name, type: GLYPHS[glyph.toLowerCase()] ?? "text" };
            }),
        };
    }
}
