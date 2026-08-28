---
title: Syntax
---

Everything you can write in a page, beyond ordinary markdown.

## Front matter

Every page opens with front matter, and its fields are whatever your collection declared. `title` is
required by the default theme:

```markdown
---
title: Getting started
order: 1
---

Text, `code`, [the link validator](../link-validator.md), and everything GitHub-flavoured markdown gives you.
```

A page that does not match its collection stops the build, naming the field and the line in your
file. See [content and collections](../../guide/content.md) for the type behind it.

## The markdown you can write

Tables, task lists, footnotes, definition lists, strikethrough and auto links all work out of the
box:

```markdown
| Option | Default |
|---|---|
| `Toc` | `{ From = 2; To = 3 }` |

- [x] Written
- [ ] Reviewed

A statement worth a source.[^1]

[^1]: The source.
```

Code fences carry a language and, optionally, [everything a code block can say about
itself](../../guide/code-blocks.md) - a title, marked lines, a folded range:

````markdown
```fsharp title="Program.fs" {2}
let greet name =
    printfn $"Hello, %s{name}"
```
````

## Attributes

Any element takes attributes in braces, so you can name something yourself:

```markdown
## Options {#reader-options}

A paragraph that needs a class. {.lead}
```

On a code span, an attribute naming a language colours it - see
[inline code](../../guide/code-blocks.md#inline-code).

## Headings and anchors

Every heading gets an id made of its own letters, so `## Créer un site` is `#créer-un-site` and a
heading written in Chinese keeps its characters. Two headings with the same text are numbered in
order of appearance - `#options`, `#options-1` - so every link has exactly one target.

Write `{#anchor}` yourself when you want an anchor to survive a rewording. Links are checked
against these ids - see [the link validator](../checks/link-validator.md).

Every heading below the title carries a link to itself in the margin, shown on hover. The search
index ignores it, and a screen reader reads it as "Link to this section".

## Table of contents

Headings are collected as the page renders, and the theme puts them in the right-hand column. You
get `##` and `###` by default, and a page can ask for something else:

```yaml
---
title: Releases
toc:
  from: 2
  to: 2
---
```

That page then lists its `##` headings and nothing under them, which is the shape a changelog
wants. You can leave either bound out: `from` is 2, `to` is 6.
