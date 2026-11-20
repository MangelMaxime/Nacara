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

Each heading inside the block opens a step. The number is written for you.

::::preview

`````markdown
:::steps

### Create the project

Anything a step needs goes under its heading.

````bash frame=terminal
mkdir my-project
````

### Describe the site

One file says what the site is and which plugins it uses.

### Build it

The output lands in `output`.

:::
`````

::::

A step written with no heading keeps its number and renders without a title.

The title is a heading one level under the heading the block sits under, so steps written under an
`##` get `###`. It carries an anchor like any other heading.

A `:::steps` block inside another one is refused. What it holds still renders.

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
paragraph. The hint lists the directives the site adds.

## Your own directives

`Markdown.directive name render` adds a `:::name` directive to a site. `render` returns its
markup:

```fsharp
Site.create "Blog"
|> Markdown.register
|> Markdown.directive "youtube" (fun directive ->
    Html.figure
        [
            Html.iframe [ prop.src $"https://www.youtube-nocookie.com/embed/{directive.Argument.Value}" ]
            Html.figcaption [ directive.Body ]
        ]
)
```

```markdown
:::youtube dQw4w9WgXcQ {.wide start=30}
The launch talk.
:::
```

A directive of your own is used in place of a built-in one of the same name. Names are matched
ignoring case. When two have the same name, the one registered last is used.

`Markdown.directivePlugin name render` gives the same directive as a plugin, for a plugin or a
theme to register.

### `DirectiveContext`

What `render` is given.

### `Name`

**type:** `string`

The name as the page wrote it.

### `Argument`

**type:** `string option`

The text after the name on the opening line, quotes removed. `dQw4w9WgXcQ` above.

### `Attributes`

**type:** `Map<string, string>`

From `{key=value}`, `{.class}` and `{#id}` on the opening line. Classes are under `class`,
separated by spaces, and the id is under `id`. `directive.Attribute "start"` reads one as an
option.

### `Content`

**type:** `string`

The markdown between the opening and closing lines, rendered as HTML. `directive.Body` is the
same as an element, with nothing around it.

### `Page`

**type:** `Page`

The page the directive is on.

### `Site`

**type:** `SiteInfo`

### `Report`

**type:** `Diagnostic -> unit`

Adds a diagnostic at the directive's opening line. `Warn` and `Error` call it.

### Reporting a problem

`directive.Warn(message, ?hint)` reports a warning. `directive.Error(message, ?hint)` reports an
error, which fails the build. Both are reported at the directive's opening line, with the code
`markdown/directive-<name>`. The directive's markup is still written.

```fsharp
|> Markdown.directive "youtube" (fun directive ->
    match directive.Argument with
    | Some id ->
        if directive.Attribute "title" |> Option.isNone then
            directive.Warn("No title given", "Add one: :::youtube <id> {title=\"The launch talk\"}")

        Html.iframe [ prop.src $"https://www.youtube-nocookie.com/embed/{id}" ]
    | None ->
        directive.Error("Name the video: :::youtube <id>")
        Html.none
)
```

### When it throws

An exception that `render` throws fails the build with `markdown/directive-failed`, at the
directive's opening line. The page shows what the directive holds in its place:

```fsharp
|> Markdown.directive "youtube" (fun directive ->
    match directive.Argument with
    | Some id -> Html.iframe [ prop.src $"https://www.youtube-nocookie.com/embed/{id}" ]
    | None -> failwith "Name the video: :::youtube <id>"
)
```

## Markdig extensions

The plugin renders the directives above itself. For anything else - an inline of your own, an
extension from NuGet - write a Markdig extension and contribute it from a plugin, and the pipeline
every page is parsed with picks it up:

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
