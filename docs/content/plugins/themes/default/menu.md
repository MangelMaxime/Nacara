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
| `Menu.page "guide/i18n.md"` | A page of the site, by its source file. `"reference:index.md"` when several collections hold that file |
| `Menu.link "Changelog" "/changelog/"` | Anything else, by URL |
| `Menu.section "Writing" [ … ]` | A heading over entries. Not a link |
| `Menu.group "plugins/markdown/index.md" [ … ]` | A page that also holds entries |
| `… |> Menu.badge "New"` | A mark beside an entry |
| `… |> Menu.badgeOf Badge.Beta "Aperçu"` | The same, saying which kind it is |

They compose, so an entry carries what you give it and nothing else:

```fsharp
Menu.page "guide/i18n.md" |> Menu.badge "New"
```

### Badges

A badge is two things: the word a reader sees, and the kind it is drawn as. The kind reaches the
markup as `data-kind`, and that is what carries the colour.

`Menu.badge` works the kind out from the label, so `New` is drawn as `new`. The theme has colours
for five of them:

| Constant | Kind | Drawn in |
|---|---|---|
| `Badge.New` | `new` | the tip colour |
| `Badge.Updated` | `updated` | the tip colour |
| `Badge.Experimental` | `experimental` | the warning colour |
| `Badge.Beta` | `beta` | the warning colour |
| `Badge.Deprecated` | `deprecated` | the danger colour |

Any other kind is drawn neutrally rather than refused, so a site styling
`.nacara-badge[data-kind="internal"]` of its own passes `"internal"` and it works.

`Menu.badgeOf` is for when the label cannot decide the kind - another language, or a word that says
more than a status does:

```fsharp
Menu.page "guide/i18n.md" |> Menu.badgeOf Badge.New "Nouveau"
Menu.page "guide/deploy.md" |> Menu.badgeOf Badge.Deprecated "Removed in 4.0"
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

A [pruned menu](#large-menus) always gets one, and it searches every page of the section rather than
the part the page holds.

## Large menus

Every page of a section contains the menu of that section. In a section of thousands of pages, such
as a generated API reference, this makes every page large.

When a section's menu has more than 500 entries, the theme prunes it. In a pruned menu, a group of
more than 150 pages shows only:

- the entries that lead to the page being read;
- a "_N_ more" link to the page of the group.

A menu of 500 entries or fewer is written whole.

| Option | Default | Effect |
|---|---|---|
| `Theme.menuLimit` | `500` | Number of entries above which a section's menu is pruned. `0` never prunes. |
| `Theme.menuGroupLimit` | `150` | Number of pages above which a group of a pruned menu is cut. `0` cuts no group. |

```fsharp
|> Theme.menuLimit 1000
|> Theme.menuGroupLimit 40
```

The [filter box](#filtering) searches every page of the section, also when the menu is pruned. For
each pruned section, the theme writes the full menu to `assets/menu/<section>.json`, and the filter
box reads it from there.

## Menu memory

When the reader opens or closes a group of the menu, the next page shows the group the same way.
The theme keeps this state until the browser tab is closed. A group that holds the page being read
is always open.

To turn the memory off on a page, set `menuMemory: false` in its [front matter](front-matter.md):

```yaml
---
title: Nacara.Core.Site
menuMemory: false
---
```

On that page, the menu opens only the groups that lead to the page, and the reader's changes are
not kept. The [API reference](../../fsharp-api.md) sets `menuMemory: false` on the pages it
generates.
