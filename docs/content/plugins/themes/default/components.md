---
title: Parts
toc:
  to: 3
---

The theme renders a page as parts. Replace one with `Theme.parts` and keep the rest, or call the
layout yourself when you want a page shape the theme does not offer.

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
| `DocPage.splash` | No menu, no table of contents, over a wider measure. `Layout` is `splash` |
| `DocPage.bare` | The navbar and the footer, and nothing else. `Layout` is `bare` |

Left alone, the filter box appears when the menu is long enough that reading it is worse than
typing a name. `DocPage.bare` is what [`layout: bare`](front-matter.md#layout) sets from front
matter.

## Parts

`Theme.parts` replaces how a part of the page is rendered:

```fsharp
Theme.defaults
|> Theme.parts (fun parts ->
    { parts with
        Footer = fun context -> Html.div [ Parts.footer context; Html.p "Hosted by us" ]
        Navbar =
            fun context model ->
                Parts.navbar context { model with End = model.End @ [ NavbarEntry.Widget "…" ] }
    })
```

`Parts` holds the theme's own rendering of each one. Call it from your replacement to wrap it, or
change the model on its way in.

| Part | Given | What it renders |
|---|---|---|
| `Head` | `HeadModel` | The tags of the head: title, description, social cards, favicon |
| `Navbar` | `NavbarModel` | The bar across the top |
| `Sidebar` | `SidebarModel` | The section's menu, the filter box, and the drawer the navbar folds into |
| `Toc` | `TocModel` | This page's headings |
| `PageNav` | `PageNavModel` | The previous and next pages of the section |
| `EditLink` | `EditLinkModel` | A link to the page's source |
| `Article` | `ArticleSlots` | The `<main>`: title, content, edit link, previous and next |
| `Footer` | | The footer the site gave, or nothing |
| `Frame` | `FrameSlots` | The navbar, then the sidebar, the content and the table of contents, then the footer |

Every part is given a `PartContext` first: `Scope` holds the page, the site and every page,
`Doc` is the page's [`DocPage`](#docpage), and `Options` the theme's.

The models are resolved by [Theme.Docs](../docs/index.md#models-and-resolvers). `Frame` reads
`context.Doc.Layout` to arrange a layout of the theme's own differently.

## Web components

The theme emits plain HTML. Theme.Core's script defines one custom element, and the
[versions plugin](../../versions.md) adds another:

| Element | What it does |
|---|---|
| `<nacara-tabs>` / `<nacara-tab>` | The `:::tabs` directive. Tabs with the same `data-sync` follow each other |
| `<nacara-version-switcher>` | The version picker, when that plugin is registered |

Everything else is a plain element the script attaches to: the copy button on a code frame, the
colour-scheme picker, the menu filter, the sidebar drawer. [Contracts](../core/contracts.md) lists
the attributes.

To add an element of your own, ship the script as an asset and register it:

```fsharp
registry
|> Registry.asset (WriteText(script, RelativePath.create "assets/my-widget.js"))
|> Registry.extra (Script("assets/my-widget.js", true))
```

`CopyFile` ships a file from disk instead. [Writing plugins](../../authoring.md) covers both.

## Code blocks

Theme.Core renders code blocks with `NacaraCodeBlockRenderer`. Register your own
`ICodeBlockRenderer` to replace its markup - see [Code blocks](../../../guide/code-blocks.md).
