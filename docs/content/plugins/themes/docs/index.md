---
title: Theme.Docs
description: What a Nacara documentation theme is built from
---

`Nacara.Theme.Docs` is what a documentation theme is built from: the vocabulary a site configures
a theme with, and the models of its navbar, sidebar, table of contents and page navigation. It is
built on [Theme.Core](../core/index.md).

`Nacara.Theme.Default` is built on it. A documentation theme of your own is built on it the same
way.

## Add it

```bash frame="terminal"
dotnet add package Nacara.Theme.Docs --prerelease
```

## What a theme is

A theme is a plugin and a layout. The plugin registers the theme's assets and what Theme.Docs
needs of the site. The layout is a function from `PageContext<'FrontMatter>` to markup, given to
a collection.

```fsharp
namespace My.Theme

open Feliz.ViewEngine
open Nacara.Core
open Nacara.Theme
open Nacara.Theme.Core
open Nacara.Theme.Docs

type ThemeOptions =
    {
        Docs: DocsOptions
        Footer: ReactElement option
    }

[<RequireQualifiedAccess>]
module Theme =

    let defaults =
        {
            Docs = DocsOptions.defaults
            Footer = None
        }

    let private styles = lazy (Styles.bundle "mine" [ "mine.css", Stylesheet.text ])

    let shell (options: ThemeOptions) (doc: DocPage) (context: PageContext<'FrontMatter>) =
        let scope = PageScope.ofContext context
        let _, cssPath = styles.Value
        let navbar = Navbar.resolve options.Docs scope
        let sidebar = Sidebar.resolve options.Docs doc scope

        Html.html
            [
                Html.head
                    [
                        yield! Head.meta (Head.resolve doc.Title doc.Description None scope)
                        Html.link [ prop.rel "stylesheet"; prop.href (context.Site.UrlOfAsset cssPath) ]
                        Behaviours.bootstrapTag ()
                        yield! Shell.stylesheetLinks context.Site
                    ]
                Html.body
                    [
                        // Your markup over the models.
                        Html.main [ prop.dangerouslySetInnerHTML context.Content ]
                        Behaviours.scriptTag context.Site
                        yield! Shell.scripts context.Site
                    ]
            ]

    let layout (options: ThemeOptions) (context: PageContext<DocFrontMatter>) =
        shell options (DocFrontMatter.toDocPage context.FrontMatter) context

    let docs (options: ThemeOptions) (name: string) = Docs.collection name (layout options)

    type private ThemePlugin(options: ThemeOptions) =
        interface IPlugin with
            member _.Name = "theme.mine"

            member _.Configure registry =
                let parts, cssPath = styles.Value

                registry
                |> ThemeDocs.configure
                    {
                        Origin = "theme.mine"
                        Docs = options.Docs
                        NotFound = Some(layout options)
                    }
                |> Registry.asset (Bundle(parts, "mine.css", RelativePath.create cssPath))

    let register (options: ThemeOptions) (site: Site) = Site.plugin (ThemePlugin options) site
```

A site uses it the way it uses the default theme:

```fsharp
Site.create "My library"
|> Theme.register theme
|> Site.collection (Theme.docs theme "content")
```

## Vocabulary

The namespace `Nacara.Theme` holds what a site writes in its options, whichever theme draws it.

| Type | What it is |
|---|---|
| [`NavbarItem`](../../../reference/nacara-theme-docs/nacara-theme/navbaritem.md) | An entry of the navbar: a link, a section, a dropdown, an icon, a widget |
| [`MenuItem`](../../../reference/nacara-theme-docs/nacara-theme/menuitem.md), [`Menu`](../../../reference/nacara-theme-docs/nacara-theme/menu.md) | An entry of a sidebar menu, and the functions that build one |
| [`DocsOptions`](../../../reference/nacara-theme-docs/nacara-theme/docsoptions.md) | The navbar, the menus, the edit url and the menu limits |
| [`DocPage`](../../../reference/nacara-theme-docs/nacara-theme/docpage.md) | What a theme needs to know about the page it renders |
| [`DocFrontMatter`](../../../reference/nacara-theme-docs/nacara-theme/docfrontmatter.md) | The front matter of a documentation page, and its decoder |

### `DocsOptions`

The part of a theme's options every documentation theme shares. A theme holds one in its own
options record and adds its own fields beside it.

| Field | Type | Default |
|---|---|---|
| `Navbar` | `NavbarItem list` | `[]` |
| `NavbarEnd` | `NavbarItem list` | `[]` |
| `Menus` | `Map<string, MenuItem list>` | `Map.empty` |
| `EditUrlBase` | `string option` | `None` |
| `MenuLimit` | `int` | `500` |
| `MenuGroupLimit` | `int` | `150` |

`DocsOptions.navbar`, `navbarEnd`, `menu`, `menus`, `editUrl`, `menuLimit` and `menuGroupLimit`
set them.

## Models and resolvers

A resolver reads the options and the page and returns a model. A model holds every url resolved
and every state settled, and no markup. The theme renders it.

Every resolver takes a [`PageScope`](../core/index.md#pages-and-scope).

| Resolver | Model | What it holds |
|---|---|---|
| `Navbar.resolve options scope` | `NavbarModel` | The brand, the entries at each end of the bar, the current section |
| `Sidebar.resolve options doc scope` | `SidebarModel` | The section's menu as a tree, the trail to the page open, the filter box, the drawer |
| `Toc.resolve doc scope` | `TocModel option` | The page's headings. `None` when the page shows none |
| `PageNav.resolve options scope` | `PageNavModel option` | The previous and next pages of the section |
| `EditLink.resolve options scope` | `EditLinkModel option` | Where the page is edited at its source |

A section's menu comes from `Sidebar.menuOf`: the menu the site declared, else the one a plugin
offered, else `None`. With `None`, `Sidebar.resolve` lists the section's pages in front-matter
order. `SidebarModel.Source` says which it was.

`PageNav.among site ordered page` gives the previous and next of any ordered list of pages.

## Registration

`ThemeDocs.configure` registers what every documentation theme needs of the site: what
[`ThemeCore.configureBase`](../core/index.md#registration) registers, the menus written whole for
the filter box, and the 404 page.

### `ThemeRegistration`

| Field | Type | What it is |
|---|---|---|
| `Origin` | `string` | The theme's name. A page the theme generates says this generated it |
| `Docs` | `DocsOptions` | The menus, read to write the pruned ones whole |
| `NotFound` | `(PageContext<DocFrontMatter> -> ReactElement) option` | The layout the 404 page is rendered with. `None` writes none |

## Front matter and layouts

`Docs.collection name layout` is a collection reading `DocFrontMatter`, with the title, the order
and the table of contents wired.

`DocPage.Layout` names the layout a page asked for: `doc`, `bare`, `splash`, or one the theme
knows. `DocFrontMatter.layouts` maps `bare` and `splash` to what they do to a `DocPage`.

A theme with layouts of its own adds to that map and reads front matter with
`DocFrontMatter.decoderWith`, which fails the build on a layout it was not given:

```fsharp
let layouts = DocFrontMatter.layouts |> Map.add "landing" DocPage.splash

let docs options name =
    Docs.collectionWith (DocFrontMatter.decoderWith (List.ofSeq layouts.Keys)) name (fun context ->
        shell options (DocFrontMatter.toDocPageWith layouts context.FrontMatter) context
    )
```

A collection with a front-matter type of its own maps it onto a `DocPage` and calls the shell.

## The menu script

`nacara-menu.js` is registered by `ThemeDocs.configure` and written by `Shell.scripts`. It reads
nothing but `data-nacara-menu-*` attributes.

| Attribute | On | Behaviour |
|---|---|---|
| `data-nacara-menu-group` | A `<details>` of the menu | Its fold is remembered from page to page |
| `data-nacara-menu-memory="false"` | The sidebar | The folds are not remembered |
| `data-nacara-menu-filter` | An `<input>` | Filters the menu |
| `data-nacara-menu-source` | The same input | Where the section's whole menu is, when the page holds a part of it |
| `data-nacara-menu-list` | The menu's top-level `<ul>` | Where the rest of a pruned menu is grafted |
| `data-nacara-menu-leaf-template`, `data-nacara-menu-group-template` | A `<template>` in the sidebar | The markup grafted entries are cloned from |

The template for a leaf holds one `<li>` with an `<a>`; the one for a group holds an `<li>` with a
`<details>`, a `<summary>` holding an `<a>`, and a `<ul>`. The script sets the link's `href` and
text, the group's `data-nacara-menu-group`, and appends children to the `<ul>`.

`data-nacara-menu-source` names one of the menus `ThemeDocs.configure` writes whole, under
`assets/menu`.

## Reference

[`Nacara.Theme.Docs`](../../../reference/nacara-theme-docs/nacara-theme-docs/index.md), module by module.
