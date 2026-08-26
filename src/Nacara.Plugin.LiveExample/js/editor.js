import { at } from "./paths.js";
import { config } from "./config.js";
import { highlight } from "./highlighting.js";

let module_ = null;

export async function load() {
    module_ = module_ || (await import(at("codemirror.js")));
    return module_;
}

export const editor = () => module_;

// Into the worker, line and column are both 1-based
export const at1 = (view, pos) => {
    const line = view.state.doc.lineAt(pos);
    return { line: line.number, column: pos - line.from + 1, lineText: line.text };
};

// Coming back out, lines are 1-based but columns are 0-based.
export const offsetOf = (doc, line, column) =>
    Math.min(doc.line(Math.max(1, Math.min(line, doc.lines))).from + column, doc.length);

function treeSitterHighlighting(CM) {
    const effect = CM.StateEffect.define();

    const field = CM.StateField.define({
        create: () => CM.Decoration.none,
        update(value, transaction) {
            value = value.map(transaction.changes);

            for (const applied of transaction.effects) {
                if (!applied.is(effect)) continue;

                const builder = new CM.RangeSetBuilder();
                const length = transaction.state.doc.length;

                for (const [from, to, className] of applied.value) {
                    if (from >= to || to > length) continue;
                    builder.add(from, to, CM.Decoration.mark({ class: className }));
                }

                value = builder.finish();
            }

            return value;
        },
        provide: (f) => CM.EditorView.decorations.from(f),
    });

    const repaint = async (view) => {
        const code = view.state.doc.toString();
        const spans = await highlight(code);

        if (!spans || view.state.doc.toString() !== code) return;
        view.dispatch({ effects: effect.of(spans) });
    };

    const plugin = CM.ViewPlugin.fromClass(
        class {
            constructor(view) {
                repaint(view);
            }

            update(update) {
                if (update.docChanged) repaint(update.view);
            }
        },
    );

    return [field, plugin];
}

export function completing(view) {
    return view ? module_.completionStatus(view.state) !== null : false;
}

export function create({ parent, doc, complete, tooltip, onChange, onRun }) {
    const CM = module_;

    const extensions = [
        // Before the default keymap, so this binding takes the key.
        CM.keymap.of([{ key: "Mod-Enter", run: () => (onRun(), true) }]),
        CM.lineNumbers(),
        CM.highlightActiveLine(),
        CM.lintGutter(),
        CM.history(),
        // The default set less the single quote: in F# that opens a generic parameter.
        CM.EditorState.languageData.of(() => [
            { closeBrackets: { brackets: ["(", "[", "{", '"'] } },
        ]),
        CM.closeBrackets(),
        // acceptCompletion reports that it did nothing when no completion is open.
        CM.keymap.of([{ key: "Tab", run: CM.acceptCompletion }, CM.indentWithTab]),
        // closeBracketsKeymap first: its Backspace takes an empty pair out together.
        CM.keymap.of([...CM.closeBracketsKeymap, ...CM.defaultKeymap, ...CM.historyKeymap]),
        CM.indentUnit.of("    "),
        CM.autocompletion({ override: [complete] }),
        CM.hoverTooltip(tooltip, { hoverTime: 250 }),
        CM.EditorView.updateListener.of((update) => {
            if (update.docChanged) onChange();
        }),
    ];

    if (config().highlighting === "treesitter") {
        extensions.push(...treeSitterHighlighting(CM));
    } else {
        extensions.push(
            CM.StreamLanguage.define(CM.fSharp),
            CM.syntaxHighlighting(CM.defaultHighlightStyle, { fallback: true }),
        );
    }

    return new CM.EditorView({ parent, state: CM.EditorState.create({ doc, extensions }) });
}
