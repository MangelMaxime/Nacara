---
title: Menu
---

The sidebar of a section. You get one without writing anything; this page is about taking it over
when the automatic one stops being right.

## Where it comes from

A section is the first segment of a route - `guide/getting-started.md` is in `guide` - and each one
gets its menu from the first of these that has something to say:

| | |
|---|---|
| 1. What you declared | `Theme.menu "guide" [ … ]` |
| 2. What a plugin offered | the [API reference](../../fsharp-api.md) offers a group per package, the [changelog](../../changelogs.md) one entry per changelog it publishes |
| 3. The section's own pages | in `order` front-matter order, then by title |

So a new section is listed the moment it has pages, and you only write a menu when that ordering
stops matching how the section should be read.

## Writing one

```fsharp
Theme.defaults
|> Theme.menu
    "guide"
    [
        Menu.section
            "Getting started"
            [
                Menu.page "guide/getting-started.md"
                Menu.page "guide/project-layout.md"
            ]
        Menu.section
            "Writing"
            [
                Menu.page "guide/content.md"
                Menu.page "guide/code-blocks.md"
            ]
    ]
|> Theme.menu "plugins" [ … ]
```

`Theme.menu` takes one section at a time, so a site with four of them calls it four times.

An entry names the **source file**, not the URL. Move a page's route and its menu entry still finds
it; a path that matches no page is a build error rather than a link that quietly goes nowhere.

## The entries

| | |
|---|---|
| `Menu.page "guide/i18n.md"` | A page of the site, by its source file |
| `Menu.link "Changelog" "/changelog/"` | Anything else, by URL |
| `Menu.section "Writing" [ … ]` | A heading over entries. Not a link |
| `Menu.group "plugins/markdown/index.md" [ … ]` | A page that also holds entries |
| `… \|> Menu.badge "New"` | A mark beside an entry |
| `… \|> Menu.badgeOf "beta" "Aperçu"` | The same, with a name you style yourself |

They compose, so an entry carries what you give it and nothing else:

```fsharp
Menu.page "guide/i18n.md" |> Menu.badge "New"
```

### Sections and groups

A **section** is a heading with entries under it. It is not clickable, and it does not fold - it is
there to break a long list into parts a reader can scan.

A **group** is a page that introduces the pages under it. It folds, and it opens itself when the
reader is on a page inside it:

```fsharp
Menu.group
    "plugins/markdown/index.md"
    [
        Menu.page "plugins/markdown/syntax.md"
        Menu.page "plugins/markdown/directives.md"
    ]
```

Nest either inside the other as deep as the section needs.

## Filtering

A menu of more than thirty entries gets a filter box above it. Typing narrows the menu to what
matches and opens whatever holds a match; clearing it puts every fold back where the reader left it.

A shorter menu does not get one, because reading it is quicker than typing. A page can decide for
itself with `menuFilter: true` or `menuFilter: false` in its [front matter](front-matter.md).

## What a reader folded

Folding carries from page to page, so reading through a section does not mean opening the same group
again on every page of it.

For a section a reader arrives at by name rather than by reading through - a reference, where the
folds from the last page mean nothing on this one - turn it off with `menuMemory: false`. Every page
then opens the menu the same way: the trail to itself, and nothing else. The
[API reference](../../fsharp-api.md) sets this on the pages it generates.

## Turning it off

`layout: bare` on a page drops the menu along with the rest of the chrome. For a section that should
never show one, give its collection a layout of your own - see [Components](components.md).
