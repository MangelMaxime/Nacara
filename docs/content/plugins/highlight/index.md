---
title: Highlighting
---

A **highlighter** colours your code blocks, and you have two to choose from. Both emit the same
thing - CSS classes over the theme's tokens, never inline styles - so a page looks the same
whichever one produced it, and switching is one line of your site.

| Plugin | Reads | Good for |
|---|---|---|
| [TextMate](textmate.md) | `.tmLanguage.json` grammars, the ones VS Code ships | Fifty languages in the box, and a rich ecosystem of grammars beyond them |
| [tree-sitter](treesitter.md) | tree-sitter grammars and their queries | Precision: names coloured by what they are, not by what they look like |

## Which one

**TextMate** knows about fifty languages the moment you register it, and most languages that are
not in the box have a `.tmLanguage.json` published somewhere - VS Code extensions are full of them.

**tree-sitter** parses instead of matching patterns, so it tells a type from a function from a
parameter where a pattern sees three identifiers. Twelve languages come inside the package, and you
name any other by its repository. In F#, types, functions, parameters and union cases each get their
own colour.

Both are one line in your site and emit identical classes, so swapping one for the other needs no
change to your theme.

## Or both

Register both: the last one registered is asked first, and the one before it covers whatever the
later one does not know.

```fsharp
|> TextMate.register     // fifty languages
|> TreeSitter.register   // twelve, done properly
```

tree-sitter colours F#, JSON, YAML and the rest of its twelve, and a Python snippet falls through to
TextMate. Swap the two lines for the opposite preference.

## Colours

Highlighted code carries classes - `tok-keyword`, `tok-string` - and the theme decides what they
mean. The default theme ships two schemes, Atom One Light and One Dark Pro, as custom properties.
Override one and every block on the site follows, whichever highlighter drew it:

```css title="static/custom.css"
:root {
    --tok-keyword: #a626a4;
    --tok-string: #50a14f;
}

:root[data-theme="dark"] {
    --tok-keyword: #ff7b72;
    --tok-string: #a5d6ff;
}
```

The vocabulary is small on purpose, and every grammar's captures land somewhere in it:

| | |
|---|---|
| Words | `tok-keyword`, `tok-operator`, `tok-punctuation` |
| Names | `tok-type`, `tok-constructor`, `tok-function`, `tok-namespace`, `tok-variable`, `tok-parameter`, `tok-property` |
| Values | `tok-string`, `tok-escape`, `tok-number`, `tok-constant` |
| Markup | `tok-tag`, `tok-attribute`, `tok-heading`, `tok-bold`, `tok-italic` |
| The rest | `tok-comment`, `tok-preprocessor`, `tok-invalid`, `tok-inserted`, `tok-deleted` |

A capture that matches nothing falls back down its dotted name - `variable.parameter` to
`tok-parameter`, then to `tok-variable` - so a grammar with a vocabulary of its own still ends up
somewhere sensible.
