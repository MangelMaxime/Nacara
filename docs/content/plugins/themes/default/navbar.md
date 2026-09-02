---
title: Navbar
---

The bar across the top: your sections on the left, and search, versions and the theme toggle on the
right.

## Declaring it

```fsharp
Theme.defaults
|> Theme.navbar
    [
        NavbarSection("Guide", "guide", "guide/getting-started.md")
        NavbarDropdown(
            "Reference",
            [
                NavbarDescribed("API", "Types and functions", "/reference/")
                NavbarDivider
                NavbarLink("Changelog", "/changelog/")
            ]
        )
    ]
|> Theme.navbarEnd
    [
        NavbarDynamicWidget Search.trigger
        NavbarLocalePicker
        NavbarIcon("GitHub", "https://github.com/you/project", Icons.github)
    ]
```

| Item | What it is |
|---|---|
| `NavbarSection(label, section, url)` | A link that lights up while the reader is in that section, and decides which sidebar they see |
| `NavbarLink(label, url)` | An ordinary link |
| `NavbarDropdown(label, items)` | A menu, holding any of the others |
| `NavbarDescribed(label, description, url)` | A link with a line under it, for dropdowns |
| `NavbarDivider` | A rule inside a dropdown |
| `NavbarIcon(label, url, svg)` | Icon only - `Icons.github` and friends are provided |
| `NavbarLocalePicker` | The languages of the site, linking to this page's translation |
| `NavbarWidget html` / `NavbarDynamicWidget render` | Markup from a plugin - search and the version switcher arrive this way |

`Navbar` is the left side, `NavbarEnd` the right.

An item's url is a page's source file, written as the [menu](menu.md) writes it, or a path inside
the site starting with `/`. Either way the theme adds the base url and the version prefix, so write
neither. Anything else is used as written.

## Around the page

| Option | Effect |
|---|---|
| `EditUrlBase` | Adds "Edit this page", resolved against the page's path in your repository |
| `Footer` | Markup under every page |
| `HeadExtra` | Markup in every `<head>` - stylesheets, fonts, analytics |
| `FavIcon` | Path to the icon |

```fsharp
Theme.defaults
|> Theme.editUrl "https://github.com/you/project/edit/main"
|> Theme.footer (Html.p [ Html.text "© 2026 You" ])
|> Theme.favIcon "/favicon.svg"
```
