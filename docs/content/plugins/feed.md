---
title: Feed
---

Writes an Atom feed of the pages of a collection.

## Add it

```bash frame=terminal
dotnet add package Nacara.Plugin.Feed --prerelease
```

```fsharp
let posts =
    Collection.create "blog" PostFrontMatter.decoder
    |> Collection.title _.Title

let feed =
    Feed.ofCollection posts (fun post ->
        { Feed.entry post.Title post.Date with
            Summary = Some post.Excerpt
            Categories = [ post.Tag ]
        }
    )
    |> Feed.author "Ada Lovelace"

Site.create "My blog"
|> Site.origin "https://example.com"
|> Markdown.register
|> Feed.register feed
|> Site.collection (posts |> Collection.layout Layouts.post)
```

`Feed.ofCollection` takes the collection and a function from a page's front matter to its entry.

`Site.origin` is required. Without it, no feed is written and the build reports
`feed/origin-missing`.

The feed lists the pages of the root locale, newest first. It is written once the pages are
rendered, so the [link validator](checks/link-validator.md) sees it.

## Link to it

`Feed.headLink` renders the `<link rel="alternate">` for a page's head:

```fsharp
Html.head
    [
        yield! Head.meta model
        Feed.headLink context.Site feed
    ]
```

## Entries

`Feed.entry title published` creates an entry with a title and a publication date.

| Field | Default | Effect |
|---|---|---|
| `Title` | | The entry's title |
| `Published` | | When the page was published |
| `Updated` | `None` | When the page last changed. `Published` is used when it is `None` |
| `Summary` | `None` | A short description of the page |
| `Categories` | `[]` | The categories the entry is filed under |

## Options

| Option | Default | Effect |
|---|---|---|
| `Path` | `"feed.xml"` | Where the feed is written, relative to the output |
| `Title` | `None` | The feed's title. The site's title is used when it is `None` |
| `Authors` | `[]` | The feed's authors |
| `Limit` | `Some 20` | How many entries the feed holds. `None` holds every page |
| `IncludeContent` | `true` | Put each page's rendered body in its entry |

`Feed.path`, `Feed.title`, `Feed.author`, `Feed.limit` and `Feed.includeContent` set them:

```fsharp
Feed.ofCollection posts describe
|> Feed.title "Release notes"
|> Feed.author "Ada Lovelace"
|> Feed.limit None
```

## Several feeds

Register the plugin once per feed, each with its own `Path`:

```fsharp
|> Feed.register (Feed.ofCollection posts describe)
|> Feed.register (Feed.ofCollection notes describe |> Feed.path "notes.xml")
```

## Reference

Every function and option of it, signature by signature: [`Feed`](../reference/nacara-plugin-feed/nacara-plugins/feed.md).
