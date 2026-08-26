---
title: TextMate
---

Colours code blocks with TextMate grammars, through
[TextMateSharp](https://github.com/danipen/TextMateSharp), which is where
[the languages come from](https://github.com/danipen/TextMateSharp#languages) too. F#, C#, JSON,
YAML, HTML, CSS, JavaScript, TypeScript, shell, XML and markdown are all in it, and you configure
nothing.

## Add it

```bash frame=terminal
dotnet add package Nacara.Plugin.Highlight.TextMate --prerelease
```

```fsharp ins={3}
Site.create "My library"
|> Markdown.register
|> TextMate.register
```

That is the whole setup. Any fenced block with a language now gets colour:

:::preview

````markdown
```fsharp
let greet name = printfn $"Hello, %s{name}"
```
````

:::

## Colours

A grammar's scopes become the theme's [token classes](index.md#colours) - `tok-keyword`,
`tok-string`, and the rest of a small vocabulary. Recolour one there and it changes everywhere.

## When a language is unknown

Your code is rendered as you wrote it, without colour, and the build tells you where:

```text frame="terminal"
! content/guide/deploy.md(48,1): warning markdown/unknown-language: No highlighter knows 'gleam', so this block is rendered without colour
    hint: Check the spelling, register a highlighter that covers it, or label the fence 'text' when it is not code
```

It is a warning, never an error, and it comes from the [markdown plugin](../markdown/index.md),
which knows the page and the line - a highlighter only ever sees a language. Turn it off there with
`WarnOnUnknownLanguage = false`.

A fence labelled `text`, `console`, `diff` or the like is asking for no colour, so it is never
reported. Neither is a fence with no language at all.

## Reference

Every function and option of it, signature by signature: [`TextMate`](../../reference/nacara-plugin-highlight-textmate/nacara-plugins/textmate.md).
