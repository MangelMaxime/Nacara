---
title: Default theme
---

The layout this site is wearing: a navbar with sections and dropdowns, a sidebar built from your
pages, a table of contents, previous and next links, dark mode, and web components for tabs,
callouts and code frames.

## Add it

```bash frame="terminal"
dotnet add package Nacara.Theme.Default --prerelease
```

```fsharp
open Nacara.Theme

let theme = Theme.defaults

let site =
    Site.create "My library"
    |> Site.baseUrl "/"
    |> Markdown.register
    |> Theme.register theme
    |> Site.collection (Theme.docs theme "content")
```

`Theme.register` ships the stylesheet and the components. `Theme.docs` is a ready-made collection
that uses the theme's own front matter and layout - point it at the directory your markdown lives in
and you have a site.

## What a page gets

```text
┌──────────────────────────────────────────────────────────────────┐
│ My library   Guide  Plugins            🔍 Search  ⌘K   3.0 ▾  ☀  │  navbar
├────────────┬────────────────────────────────────┬────────────────┤
│ GETTING    │  Writing content                   │ ON THIS PAGE   │
│  Started   │                                    │  Front matter  │
│  Layout    │  Every collection declares the …   │  Links         │
│ WRITING    │                                    │  Anchors       │
│  Content   │                                    │                │
│  ▸ Themes  │  ← a group a reader folds          │                │
├────────────┴────────────────────────────────────┴────────────────┤
│  ← Project layout                          Code blocks →         │  page nav
└──────────────────────────────────────────────────────────────────┘
```

Nothing above needs configuring. The navbar is empty until you fill it, and every other part is
built from your pages.

## Configuring it

Every option is a setter, and they pipe:

```fsharp
let theme =
    Theme.defaults
    |> Theme.navbar [ NavbarSection("Guide", "guide", "/guide/getting-started/") ]
    |> Theme.navbarEnd [ NavbarDynamicWidget Search.trigger ]
    |> Theme.menu "guide" [ Menu.page "guide/getting-started.md" ]
    |> Theme.editUrl "https://github.com/you/project/edit/main"
    |> Theme.footer (Html.p [ Html.text "© 2026 You" ])
```

| | |
|---|---|
| [`navbar`, `navbarEnd`](navbar.md) | What sits across the top |
| [`menu`, `menus`](menu.md) | The sidebar of a section |
| [`editUrl`](navbar.md#around-the-page) | Adds "Edit this page" |
| [`footer`, `headExtra`, `favIcon`](navbar.md#around-the-page) | The rest of the frame |
| [`css`](customising.md) | A rule or two, without a stylesheet |

## The pages of this section

- [Navbar](navbar.md) - sections, dropdowns, icons, and the frame around the page
- [Menu](menu.md) - where a sidebar comes from, and how to write your own
- [Front matter](front-matter.md) - what a page says about itself
- [Customising](customising.md) - colours, spacing, fonts, your own CSS
- [Components](components.md) - the pieces on their own, and building a layout

## Using another theme

A theme is a package that registers layouts and assets, and nothing about this one is privileged.
To swap it for your own, register yours instead - [Theming](../../../guide/theme.md) in the guide
explains what a theme has to provide.

## Reference

Every function and option of it, signature by signature: [`Theme`](../../../reference/nacara-theme-default/nacara-theme/theme.md).
