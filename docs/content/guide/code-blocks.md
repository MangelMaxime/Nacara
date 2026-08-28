---
title: Code blocks
---

Nacara treats a code block as a list of lines, each a list of tokens, with annotations on top, so
highlighting, markers and line numbers compose.

The meta syntax follows [Expressive Code](https://expressive-code.com).

## Title

You can give a code block a title:

:::preview

````markdown /title="Program.fs"/
```fsharp title="Program.fs"
let main argv = 0
```
````

:::

## Frames

`frame=terminal` draws it as a terminal instead:

:::preview

````markdown /frame=terminal/
```bash frame=terminal
dotnet run -- build
```
````

:::

`frame=none` removes the chrome:

:::preview

````markdown /frame=none/
```bash frame=none
dotnet run -- build
```
````

:::

## Marking lines

`{2,4-5}` marks lines, `ins={3}` and `del={4}` mark insertions and deletions:

:::preview

````markdown "showLineNumbers {1} ins={3} del={4}"
```fsharp showLineNumbers {1} ins={3} del={4}
let site =
    Site.create "Docs"
    |> Markdown.register
    |> Legacy.register
```
````

:::

## Writing a diff

If you would rather write the changes than count the lines, fence the block as `diff` and put `+`
or `-` in front of each line. The markers are read off and never reach the page, so what you copy
is code you can paste:

:::preview

````markdown
```diff
Site.create "Docs"
-|> Legacy.register
+|> Markdown.register
```
````

:::

That colours it as a diff, though - the code underneath is F# and nothing knows it. Say so with
`lang=`:

:::preview

````markdown /lang="fsharp"/
```diff lang="fsharp"
Site.create "Docs"
-|> Legacy.register
+|> Markdown.register
```
````

:::

Write the markers the same way down the block: `-printfn` and `+printfn`, or a space after every
one of them. Only the marker comes off, so a space after `-` on one line and none after `+` on the
next leaves the two a column apart.

A diff pasted from `git diff` is left exactly as it is: the `---` and `+++` headers are kept,
filenames and all.

A deleted line is drawn but left out of what the block gives you, so copying returns the code after
the change. The same goes for a line `del=` names, and for
[live examples](../plugins/live-example.md): Run compiles what survives.

## Marking words

A quoted string or a `/regular expression/` marks text inside the lines.

`ins=` and `del=` take the same, and colour what they mark:

:::preview

````markdown /Site...w[+]/ /ins="Markdown"/ /del="register"/
```fsharp /Site\.\w+/ ins="Markdown" del="register"
let site = Site.create "Docs" |> Site.baseUrl "/" |> Markdown.register
```
````

:::

## Line numbers and collapsing

`showLineNumbers`, with `startLineNumber=` when the excerpt starts further down, and
`collapse={3-5}` to fold a range behind a summary:

:::preview

````markdown "showLineNumbers collapse={3-5}"
```json showLineNumbers collapse={3-5}
{
  "name": "example",
  "hidden": "one",
  "hidden": "two",
  "hidden": "three",
  "visible": true
}
```
````

:::

## Highlighting

Colours come from CSS classes, never inline styles, so one rendering serves light and dark and
switching theme costs no rebuild. See [the highlight plugin](../plugins/highlight/index.md).

## Inline code

Code in a sentence is coloured too, once it says what language it is:

:::preview

```markdown /\{:fsharp\}/
Call `Site.baseUrl "/"{:fsharp}` before anything else.
```

:::

The marker goes *inside* the backticks, where it is part of the snippet and gets stripped before
the page is written - nothing to copy by accident. This is the spelling
[rehype-pretty-code](https://rehype-pretty.pages.dev/) uses, so it may already be familiar.

The [attribute form](../plugins/markdown/syntax.md#attributes) works too, and reads better when the
snippet itself ends in a brace:

:::preview

```markdown /\{fsharp\}/ /\{lang=fsharp\}/
Call `Site.baseUrl "/"`{fsharp} before anything else.

Call `Site.baseUrl "/"`{lang=fsharp} before anything else.
```

:::

:::warning
Keep the attribute on the same line as the text that follows it. A `{…}` at the end of a line eats
the line break, so the next word runs into the snippet.
:::

A bare `{word}` is only read as a language when a highlighter claims it, so `` `x`{disabled} ``
stays the attribute you wrote. A language nobody covers is reported the same way a fence naming one
is, and the snippet renders without colour.

## Who does what

Three things meet in a code block. Knowing which is which tells you where to go when one of them
is wrong:

| Part | Owner | Change it by |
|---|---|---|
| The meta after the language - titles, frames, `{1,3}`, `ins=`, `collapse=` | The **engine** | Nothing to configure; it is the same in markdown and in [literate F#](../plugins/literate/index.md) |
| The colours of the tokens | A **highlighter plugin** | [Highlighting](../plugins/highlight/index.md), or a plugin taking one language |
| The markup around it - the frame, the copy button, the fold | The **theme** | Registering your own `ICodeBlockRenderer`; the last one wins |

The meta is the engine's, not the markdown plugin's: literate F# writes the same annotations as
`(*** title="Greeting.fs" {2} ***)`. So a fence with no highlighter still gets its title, its
markers and its copy button.
