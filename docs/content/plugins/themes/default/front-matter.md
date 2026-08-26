---
title: Front matter
---

What a page can say about itself. Every field but `title` is optional, and a page that writes only a
title gets the whole frame.

```yaml
---
title: Writing content
description: Front matter, links and directives
order: 2
---
```

## Every field

| Field | Effect |
|---|---|
| `title` | The page's heading, its entry in the menu, its `<title>`, and what search shows. **Required** |
| `description` | One sentence, used as the meta description and in search results |
| `order` | Where it sits in a sidebar the theme built itself. Ignored once you [declare the menu](menu.md) |
| `layout` | `bare` for a page with no chrome |
| `pageNav` | `false` drops the previous and next links |
| `menuFilter` | Whether the menu offers a filter box, when its length should not decide |
| `menuMemory` | `false` opens the menu the same way on every page of the section |
| `toc` | Which heading levels the table of contents holds |

## The ones worth explaining

**`layout: bare`** drops the menu, the table of contents and the previous and next links, leaving
the navbar, your content and the footer. A landing page is the usual reason.

```yaml
---
title: My library
layout: bare
---
```

**`pageNav: false`** is for a page that is not part of a sequence. Previous and next make sense when
a section is read through; on a reference page or a landing page they offer whatever happens to sort
next, which helps nobody.

**`toc`** takes the levels this page wants, when the collection's default does not fit:

```yaml
---
title: Releases
toc:
  from: 2
  to: 2
---
```

`from: 2, to: 3` is the usual default: `##` and `###`, since `#` is the title you already read.

**`menuFilter` and `menuMemory`** are explained where they act, on the [menu](menu.md) page.

## Front matter of your own

`Theme.docs` uses the theme's `DocFrontMatter`, which is these fields and nothing else. A site that
needs more - a page that knows its author, a tag, a product - declares its own type and maps it onto
what the theme needs. [Components](components.md) covers that, and the `DocPage` setters that decide
the same things from code rather than from front matter.
