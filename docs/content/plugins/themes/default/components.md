---
title: Components
toc:
  to: 3
---

The theme's layout and every piece of it is a function. Call them yourself when you want a page
shape the theme does not offer.

## Layouts

```fsharp
Theme.layout theme context           // the theme's front matter
Theme.shell theme docPage context    // the same frame, your front matter
```

`layout` is what [`Theme.docs`](index.md) gives its collection. It reads the theme's
`DocFrontMatter` and renders the whole frame: head, navbar, sidebar, content, table of contents,
previous and next links, and footer.

`shell` renders the same frame from a [`DocPage`](#docpage), so a collection with a front-matter
type of its own keeps the theme by mapping onto it:

```fsharp
Collection.create "docs" MyFrontMatter.decoder
|> Collection.title _.Heading
|> Collection.layout (fun context ->
    DocPage.create context.FrontMatter.Heading
    |> DocPage.describedBy context.FrontMatter.Summary
    |> fun page -> Theme.shell theme page context
)
```

## DocPage

What the frame holds. `DocPage.create` starts with everything on - a menu, a table of contents,
previous and next links, and a menu that remembers what the reader folded - and each helper takes
one of them away:

```fsharp
DocPage.create "Releases"
|> DocPage.describedBy (Some "What changed, and when")
|> DocPage.withoutToc
|> DocPage.withoutPageNav
```

| Helper | Effect |
|---|---|
| `DocPage.create "Title"` | The title, used as the heading, the menu entry and the `<title>` |
| `DocPage.description "…"` | The meta description |
| `DocPage.describedBy value` | The same, from a `string option` |
| `DocPage.withoutMenu` | No sidebar; the content takes the width |
| `DocPage.withoutToc` | No table of contents |
| `DocPage.withoutPageNav` | No previous and next links |
| `DocPage.withMenuFilter` | A filter box over the menu, however short it is |
| `DocPage.withoutMenuFilter` | No filter box, however long it is |
| `DocPage.withoutMenuMemory` | Every page of the section opens the menu the same way |
| `DocPage.bare` | No menu, no table of contents, no previous and next links |

Left alone, the filter box appears when the menu is long enough that reading it is worse than
typing a name. `DocPage.bare` is what [`layout: bare`](front-matter.md#layout) sets from front
matter.

## The pieces

Each returns markup for the page it is given, so your own layout can keep the parts you want:

| Function | What it renders |
|---|---|
| `Components.navbar theme context` | The bar across the top, and the drawer it folds into |
| `Components.sidebar theme docPage context` | The section's menu. The `DocPage` decides the filter and the memory |
| `Components.toc context` | This page's headings |
| `Components.pageNav theme context` | The previous and next pages of the section |
| `Components.editLink theme context` | A link to the page's source. Renders nothing unless [`Theme.editUrl`](navbar.md#around-the-page) is set |

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

`Theme.shell` is what links the theme's stylesheet. A layout that does not call it gets no theme
CSS - the file is still built, but nothing points at it - so bring your own.

Three helpers answer the questions those pieces ask:

| Function | What it answers |
|---|---|
| `Components.sectionOf page` | The section a page is in - the first segment of its route |
| `Components.sectionPages context` | The pages of the current section, in menu order |
| `Components.translationsOf site pages page` | Each locale, the url of this page in it, and whether that translation exists |

## Web components

The theme emits plain HTML and defines one custom element. The
[versions plugin](../../versions.md) adds another:

| Element | What it does |
|---|---|
| `<nacara-tabs>` / `<nacara-tab>` | The `:::tabs` directive. Tabs with the same `data-sync` follow each other |
| `<nacara-version-switcher>` | The version picker, when that plugin is registered |

Everything else is a plain element the theme's script attaches to: the copy button on a code frame,
the colour-scheme picker, the menu filter, the sidebar drawer.

To add an element of your own, ship the script as an asset and register it:

```fsharp
registry
|> Registry.asset (WriteText(script, RelativePath.create "assets/my-widget.js"))
|> Registry.extra (Script("assets/my-widget.js", true))
```

`CopyFile` ships a file from disk instead. [Writing plugins](../../authoring.md) covers both.

## Code blocks

The theme renders code blocks by implementing `ICodeBlockRenderer`. Register your own to replace its
markup - see [Code blocks](../../../guide/code-blocks.md).
