---
title: Front matter
toc:
  to: 3
---

The fields a page can set in its `---` block. Only `title` is required; a page that sets nothing
else gets a menu, a table of contents, and links to the pages either side of it.

```yaml
---
title: Writing content
description: Front matter, links and directives
order: 2
---
```

## Fields

### `title`

**type:** `string` (required)

You must provide a title for every page. It is shown at the top of the page, in the browser tab, in
the menu, and in search results.

### `description`

**type:** `string`

The page description, picked up by search engines and in social previews.

### `order`

**type:** `number`

Controls where the page sits in its section, when no [menu](menu.md) is declared for that section.
Pages without one come last, in title order.

### `layout`

**type:** `string`

Set to `bare` for a page with no chrome: no menu, no table of contents, no previous and next links.
Landing pages are the usual case.

```yaml
---
title: My library
layout: bare
---
```

### `pageNav`

**type:** `boolean`

**default:** `true`

Set to `false` to drop the links to the previous and next pages of the section.

### `menuFilter`

**type:** `boolean`

Whether the menu offers a box for filtering it. Left out, the menu's length decides. See
[Menu](menu.md).

### `menuMemory`

**type:** `boolean`

**default:** `true`

Whether the menu keeps what the reader folded from one page to the next. Set to `false` and every
page of the section opens the menu the same way. See [Menu](menu.md).

### `toc`

**type:** `{ from?: number; to?: number }`

**default:** the range the markdown plugin was configured with

The heading levels this page's table of contents holds. Either bound can be left out: `from` is
`2`, `to` is `6`.

```yaml
---
title: Releases
toc:
  from: 2
  to: 2
---
```

## Front matter of your own

`Theme.docs` reads the theme's `DocFrontMatter`, which is these fields and nothing else. To add
fields of your own - an author, a tag, a product - declare your own record and decoder, then map it
onto what the theme needs. [Components](components.md) shows how, along with the `DocPage` setters
that decide the same things from code.
