---
title: Components
---

The theme's layout is a function, and so is every piece of it. When you want a different page shape,
compose those pieces rather than starting from an empty document.

## The layout, and what it is made of

```fsharp
Theme.layout theme            // a page described by the theme's own front matter
Theme.shell theme docPage     // the same frame, for a front-matter type of your own
```

`shell` gives you the whole frame - head, navbar, sidebar, content, table of contents, page
navigation, footer - and a `DocPage` decides what it holds:

```fsharp
DocPage.create "Releases"
|> DocPage.describedBy (Some "What changed, and when")
|> DocPage.withoutToc
|> DocPage.withoutPageNav
```

| Helper | What it decides |
|---|---|
| `DocPage.description "…"` | The meta description |
| `DocPage.describedBy value` | The same, from a front matter that may not carry one |
| `DocPage.withoutMenu` | No sidebar |
| `DocPage.withoutToc` | No table of contents |
| `DocPage.withoutPageNav` | No previous and next |
| `DocPage.withMenuFilter` / `DocPage.withoutMenuFilter` | Decides the filter, rather than leaving it to the menu's length |
| `DocPage.withoutMenuMemory` | Every page opens the menu the same way |
| `DocPage.bare` | No chrome at all - what `layout: bare` does from [front matter](front-matter.md) |

## Your own front matter

The layout takes a `DocPage` rather than the theme's front-matter type, so a collection with a type
of its own keeps the theme by mapping onto it:

```fsharp
Collection.create "docs" MyFrontMatter.decoder
|> Collection.title _.Heading
|> Collection.layout (fun context ->
    DocPage.create context.FrontMatter.Heading
    |> DocPage.describedBy context.FrontMatter.Summary
    |> fun page -> Theme.shell theme page context
)
```

## The pieces on their own

```fsharp
Components.navbar theme context
Components.sidebar theme context
Components.toc context
Components.pageNav theme context
Components.editLink theme context
```

Each one takes the page context and returns markup, so your own layout can use the navbar and the
sidebar while arranging the middle differently - a landing page with a hero, a reference page with
two columns:

```fsharp
Collection.layout (fun context ->
    Html.html
        [
            Html.head [ (* … *) ]
            Html.body
                [
                    Components.navbar theme context
                    // The body is rendered html by this point, so it is inserted rather than escaped.
                    Html.main [ prop.dangerouslySetInnerHTML context.Content ]
                    Components.pageNav theme context
                ]
        ]
)
```

`Components.sectionOf`, `Components.sectionPages` and `Components.translationsOf` answer the
questions those pieces ask: which section a page is in, which pages share it, and which translations
exist.

## Web components

The theme emits plain HTML plus a few custom elements, all vanilla JavaScript with no framework and
no build step:

| Element | What it does |
|---|---|
| `<nacara-tabs>` / `<nacara-tab>` | The `:::tabs` directive; tabs with the same `data-sync` follow each other |
| `<nacara-copy>` | The copy button on a code frame |
| `<nacara-theme-toggle>` | Light, dark, or the system's choice |
| `<nacara-version-switcher>` | Contributed by the [versions plugin](../../versions.md) |

To add an element of your own, ship it as an asset and contribute it with
`Registry.extra (Script(…))`. That is how search and versions arrive, and the theme does not know
they exist.

## Code blocks

The theme decides what a code block looks like by implementing `ICodeBlockRenderer`, while the
engine parses the fence meta and a [highlighter](../../highlight/index.md) colours the tokens.
Register your own renderer to replace the theme's markup without touching either of the other two -
see [Code blocks](../../../guide/code-blocks.md).
