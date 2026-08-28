---
title: Components
---

Every piece of the theme's layout is a function. Compose them when you want a page shape the theme
does not offer.

## The layout

```fsharp
Theme.layout theme            // a page described by the theme's own front matter
Theme.shell theme docPage     // the same frame, for a front-matter type of your own
```

`shell` renders the whole frame: head, navbar, sidebar, content, table of contents, page navigation
and footer. A `DocPage` says what it holds:

```fsharp
DocPage.create "Releases"
|> DocPage.describedBy (Some "What changed, and when")
|> DocPage.withoutToc
|> DocPage.withoutPageNav
```

| Helper | What it decides |
|---|---|
| `DocPage.description "…"` | The meta description |
| `DocPage.describedBy value` | The same, from a value that may be `None` |
| `DocPage.withoutMenu` | No sidebar |
| `DocPage.withoutToc` | No table of contents |
| `DocPage.withoutPageNav` | No previous and next links |
| `DocPage.withMenuFilter` / `DocPage.withoutMenuFilter` | Shows or hides the menu filter, instead of leaving it to the menu's length |
| `DocPage.withoutMenuMemory` | Every page opens the menu the same way |
| `DocPage.bare` | No chrome at all, the same as `layout: bare` in [front matter](front-matter.md) |

## Your own front matter

`shell` takes a `DocPage`, not the theme's front-matter type. A collection with a type of its own
maps onto it and keeps the theme:

```fsharp
Collection.create "docs" MyFrontMatter.decoder
|> Collection.title _.Heading
|> Collection.layout (fun context ->
    DocPage.create context.FrontMatter.Heading
    |> DocPage.describedBy context.FrontMatter.Summary
    |> fun page -> Theme.shell theme page context
)
```

## The pieces

```fsharp
Components.navbar theme context
Components.sidebar theme docPage context
Components.toc context
Components.pageNav theme context
Components.editLink theme context
```

Each returns markup, so your own layout can keep the navbar and the sidebar and arrange the middle
itself:

```fsharp
Collection.layout (fun context ->
    Html.html
        [
            Html.head [ (* … *) ]
            Html.body
                [
                    Components.navbar theme context
                    // context.Content is already rendered html.
                    Html.main [ prop.dangerouslySetInnerHTML context.Content ]
                    Components.pageNav theme context
                ]
        ]
)
```

`Components.sectionOf`, `Components.sectionPages` and `Components.translationsOf` give you the
section a page is in, the pages that share it, and its translations.

## Web components

The theme emits plain HTML and defines one custom element. The
[versions plugin](../../versions.md) adds another:

| Element | What it does |
|---|---|
| `<nacara-tabs>` / `<nacara-tab>` | The `:::tabs` directive. Tabs with the same `data-sync` follow each other |
| `<nacara-version-switcher>` | The version picker, when that plugin is registered |

To add one of your own, ship the script as an asset and register it:

```fsharp
registry
|> Registry.asset (WriteText(script, RelativePath.create "assets/my-widget.js"))
|> Registry.extra (Script("assets/my-widget.js", true))
```

`CopyFile` ships a file from disk instead. [Writing plugins](../../authoring.md) covers both.

## Code blocks

The theme renders code blocks by implementing `ICodeBlockRenderer`. Register your own to replace its
markup - see [Code blocks](../../../guide/code-blocks.md).
