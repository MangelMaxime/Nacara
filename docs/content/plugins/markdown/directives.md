---
title: Directives
---

The blocks that are more than a paragraph: callouts, tabs, numbered steps and disclosures. You
write them with three or more colons, and they render to semantic HTML plus a web component where
one is needed - no MDX, no JavaScript in your content.

## Callouts

::::preview

```markdown
:::note
Worth knowing, not worth stopping for.
:::

:::warning Careful
A title after the name replaces the default one.
:::
```

::::

You get `note`, `tip`, `info`, `warning`, `danger` and `caution`. They differ in colour and default
title, and the theme decides both.

:::tip
A callout is for something a reader would otherwise miss. Three in a row means the prose needs
rewriting.
:::

## Tabs

:::::preview

````markdown
::::tabs install
:::tab dotnet
```bash
dotnet add package Nacara.Core --prerelease
```
:::
:::tab paket
```bash
paket add Nacara.Core
```
:::
::::
````

:::::

Watch the colon count: **a directive needs more colons than any run of colons inside it**. Four
around, three inside. Getting this wrong is the usual reason a directive renders as text.

The name after `tabs` - `install` above - syncs them: every tab group with that name switches
together, so a reader who picks paket once sees paket everywhere. Leave it out and the group stands
on its own.

## Steps

::::preview

```markdown
:::steps
1. Create the project

    ```bash frame=terminal
    mkdir my-project
    ```

2. Describe the site
3. Build it
:::
```

::::

A numbered sequence, drawn as one - useful when each step carries a code block and the numbers would
otherwise drift apart.

## Disclosures

::::preview

```markdown
:::details Show me the generated html
Anything in here starts folded.
:::
```

::::

## File trees

::::preview

```markdown
:::filetree
- src
  - Nacara.Core
    - **Build.fs**
    - Route.fs
  - Nacara.Plugin.Markdown/
- global.json
:::
```

::::

Write an ordinary nested list and it is drawn as a listing. An entry that holds a list is a
directory, and so is one you write with a trailing `/` - that is how you say a directory is empty,
and the slash is not shown back to the reader. Use emphasis to point at the entry the page is
about.

## Preview

`:::preview` shows the source it contains, and what that source renders as underneath. Every
example on this page is one.

:::::preview

````markdown
::::preview
```markdown
:::tip
Written once, shown twice.
:::
```
::::
````

:::::

It reads what it contains as **markdown**, or as **html** when the fence says so - those are the
two it can show twice. A fence marked `html` is inserted as it is, which is how you document a
component that has no markdown syntax. Anything else goes through the same renderer as the rest of
the page, so what you see under **Preview** is what a reader gets anywhere else.

A fence in any other language already shows what its code looks like, so a preview around one has
nothing to add and warns you:

```text frame="terminal"
! warning markdown/preview-not-markup: ':::preview' has nothing to show below a fsharp block
    hint: It renders the block underneath as markdown, so it is for markdown and html. A code block is already what its code looks like - drop the container.
```

To show a code fence *and* what it renders as, meta line included, put the fence inside a markdown
one and let the preview read that:

``````markdown
:::preview
`````markdown
```fsharp showLineNumbers {1}
let main argv = 0
```
`````
:::
``````

## Unknown directives

```text frame="terminal"
! warning markdown/unknown-directive: Unknown directive ':::tabs'
    hint: A container nested in another one needs fewer colons than its parent: ::::tabs around :::tab
```

An unknown directive is reported, with the position of the line at fault, instead of rendering as a
paragraph.

## Adding your own

The plugin renders the directives above itself. For anything else - a container of your own, an
inline of your own, an extension from NuGet - write a Markdig extension and contribute it from a
plugin, and the pipeline every page is parsed with picks it up:

```fsharp
type MyPlugin() =
    interface IPlugin with
        member _.Name = "my-plugin"

        member _.Configure registry =
            // The type is the contract, so the cast matters: it is read back as IMarkdownExtension.
            registry |> Registry.extra (MyMarkdigExtension() :> IMarkdownExtension)
```

You choose the class names it emits, and the theme styles the ones it knows - see
[writing a plugin](../authoring.md).
