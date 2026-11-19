---
title: A site with its own look
description: A blog built without a theme, from the engine and Theme.Core
---

This page builds a small blog without a theme: a home page, an about page, posts, a page per tag,
an Atom feed and a 404 page. The layouts and the stylesheet are yours. The engine and
[Theme.Core](../plugins/themes/core/index.md) bring the rest.

## Create the project

```bash frame="terminal"
mkdir blog && cd blog
dotnet new console -lang F# -o .
dotnet add package Nacara.Core --prerelease
dotnet add package Nacara.Plugin.Markdown --prerelease
dotnet add package Nacara.Plugin.Feed --prerelease
dotnet add package Nacara.Theme.Core --prerelease
```

The project holds three F# files, in this order in the `.fsproj`:

:::filetree

- Blog.fsproj
- Posts.fs
- Layouts.fs
- Site.fs
- assets/
  - site.css
- content/
  - pages/
    - index.md
    - about.md
    - 404.md
  - posts/
    - hello.md

:::

## Posts

`Posts.fs` holds the front matter of a post, the collection, the tag pages and the feed.

```fsharp title="Posts.fs"
module Blog.Posts

open System
open Nacara.Core
open Nacara.Plugins

type Post =
    {
        Title: string
        Date: DateTimeOffset
        Tags: string list
        Draft: bool
    }

let decoder: Decoder<Post> =
    Decode.object (fun get ->
        {
            Title = get.Required.Field "title" Decode.string
            Date = get.Required.Field "date" Decode.datetime
            Tags = get.Optional.Field "tags" (Decode.list Decode.string) |> Option.defaultValue []
            Draft = get.Optional.Field "draft" Decode.bool |> Option.defaultValue false
        }
    )

let collection =
    Collection.create "posts" decoder
    |> Collection.title _.Title
    |> Collection.routePrefix "posts"
    |> Collection.draft _.Draft
```

`Collection.create "posts"` reads the files of `content/posts`.

`Collection.draft` names the posts that are drafts. A draft is published by `watch` and left out
of `build`, including from the tag pages and the feed.

The collection has no layout yet: `Site.fs` adds it. The layouts can then read the collection.

### Tag pages

A tag page lists the posts filed under one tag, ten per page.

```fsharp title="Posts.fs"
type TagPage =
    {
        Tag: Term<TypedPage<Post>>
        Paged: Paged<TypedPage<Post>>
    }

let tagPages (posts: TypedPage<Post> list) =
    let tags =
        posts
        |> List.sortByDescending _.FrontMatter.Date
        |> Taxonomy.group _.FrontMatter.Tags

    [
        for tag in tags do
            for paged in Paging.split 10 tag.Items ->
                {
                    Tag = tag
                    Paged = paged
                }
    ]

let tagRoute (locale: Locale) (tag: string) (number: int) =
    let first =
        Route.create
            locale
            [
                "tags"
                Slug.create tag
            ]

    Paging.route first number
```

`TypedPage<Post>` is a page and its decoded front matter.

`Taxonomy.group` groups the posts by tag. It returns one `Term` per tag, with its `Name`, its
`Slug` and its `Items`. A post with several tags is in each of them. Tags with the same slug are
one term. Terms are ordered by name, ignoring case.

`Paging.split` splits a list into `Paged` values. `Paging.route` gives `/tags/fsharp/` for page 1
and `/tags/fsharp/page/2/` for page 2. See [Paging](content.md#paging).

### Feed

```fsharp title="Posts.fs"
let feed =
    Feed.ofCollection
        collection
        (fun post ->
            { Feed.entry post.Title post.Date with
                Categories = post.Tags
            }
        )
```

See the [feed plugin](../plugins/feed.md).

## Layouts

A layout is a function from a `PageContext<'FrontMatter>` to markup. `Layouts.fs` holds a shell
shared by every page, and one layout per kind of page.

### The shell

```fsharp title="Layouts.fs"
module Blog.Layouts

open Feliz.ViewEngine
open Nacara.Core
open Nacara.Plugins
open Nacara.Theme.Core

let private schemes =
    [
        "light"
        "dark"
        "system"
    ]

let shell (context: PageContext<'FrontMatter>) (title: string) (main: ReactElement list) =
    let site = context.Site

    Html.html
        [
            prop.lang context.Page.Locale.Code
            prop.children
                [
                    Html.head
                        [
                            yield! Head.ofContext title None context
                            Behaviours.bootstrapTag ()
                            Feed.headLink site Posts.feed
                            yield! Shell.stylesheetLinks site
                        ]
                    Html.body
                        [
                            Shell.skipLink "skip-link" "#main"
                            Html.header
                                [
                                    Html.a
                                        [
                                            prop.href (context.LinkTo "index.md")
                                            prop.text site.Title
                                        ]
                                    Html.a
                                        [
                                            prop.href (context.LinkTo "about.md")
                                            prop.text "About"
                                        ]
                                    for scheme in schemes do
                                        Html.button
                                            [
                                                prop.type' "button"
                                                prop.custom ("data-nacara-theme-pick", scheme)
                                                prop.text scheme
                                            ]
                                ]
                            Html.main
                                [
                                    prop.id "main"
                                    prop.children main
                                ]
                            Behaviours.scriptTag site
                            yield! Shell.scripts site
                        ]
                ]
        ]
```

| Function | What it renders |
|---|---|
| `Head.ofContext title description context` | Charset, viewport, title, description, canonical url, social cards |
| `Behaviours.bootstrapTag ()` | The script that sets the colour scheme before the first paint |
| `Feed.headLink site feed` | The `<link rel="alternate">` of the feed |
| `Shell.stylesheetLinks site` | A link to every stylesheet: the plugins' and the site's |
| `Shell.skipLink className target` | The link that jumps past the navigation |
| `Behaviours.scriptTag site` | The shared script |
| `Shell.scripts site` | Every script plugins contributed, and the site's |

`context.LinkTo` takes the source path of a page, relative to its collection root, and returns its
url. A path starting with `/` is put under the base url. A path that names no page is reported by
the [link validator](../plugins/checks/link-validator.md).

A button with `data-nacara-theme-pick` picks a colour scheme. The shared script sets
`aria-pressed` on the button picked. See the [attributes](../plugins/themes/core/contracts.md#behaviours)
it reads.

### Posts and tag pages

```fsharp title="Layouts.fs"
let private postLink (context: PageContext<'FrontMatter>) (post: TypedPage<Posts.Post>) =
    Html.li
        [
            Html.a
                [
                    prop.href (context.Site.UrlOf post.Page.Route)
                    prop.text post.FrontMatter.Title
                ]
        ]

let post (context: PageContext<Posts.Post>) =
    let tagUrl tag =
        context.Site.UrlOf(Posts.tagRoute context.Page.Locale tag 1)

    shell
        context
        context.FrontMatter.Title
        [
            Html.h1 context.FrontMatter.Title
            Html.div [ prop.dangerouslySetInnerHTML context.Content ]
            Html.ul
                [
                    for tag in context.FrontMatter.Tags ->
                        Html.li
                            [
                                Html.a
                                    [
                                        prop.href (tagUrl tag)
                                        prop.text tag
                                    ]
                            ]
                ]
        ]

let tag (context: PageContext<Posts.TagPage>) =
    let page = context.FrontMatter

    let href number =
        context.Site.UrlOf(Posts.tagRoute context.Page.Locale page.Tag.Name number)

    shell
        context
        page.Tag.Name
        [
            Html.h1 page.Tag.Name
            Html.ul [ for post in page.Paged.Items -> postLink context post ]
            Html.nav
                [
                    for slot in Paging.slots page.Paged.Number page.Paged.PageCount ->
                        match slot with
                        | PageNumber number ->
                            Html.a
                                [
                                    prop.href (href number)
                                    prop.text number
                                ]
                        | Gap -> Html.span "…"
                ]
        ]
```

`context.Content` is the page's rendered body. `Paging.slots` lists the pages of a pager, with a
`Gap` for the pages left out.

### Pages

The pages that are not posts name their layout in their front matter.

```fsharp title="Layouts.fs"
type Page =
    {
        Title: string
        Layout: string
    }

let pageDecoder: Decoder<Page> =
    Decode.object (fun get ->
        {
            Title = get.Required.Field "title" Decode.string
            Layout = get.Optional.Field "layout" Decode.string |> Option.defaultValue "page"
        }
    )

let page (context: PageContext<Page>) =
    shell
        context
        context.FrontMatter.Title
        [ Html.div [ prop.dangerouslySetInnerHTML context.Content ] ]

let home (context: PageContext<Page>) =
    let latest =
        context.PagesOf Posts.collection
        |> List.sortByDescending _.FrontMatter.Date
        |> List.truncate 5

    shell
        context
        context.FrontMatter.Title
        [
            Html.div [ prop.dangerouslySetInnerHTML context.Content ]
            Html.ul [ for post in latest -> postLink context post ]
        ]
```

`context.PagesOf collection` returns the pages of a collection in the page's locale, as
`TypedPage` values.

## The site

```fsharp title="Site.fs"
module Blog.Site

open Nacara.Core
open Nacara.Plugins
open Nacara.Theme.Core
open Blog

let pages =
    Collection.create "pages" Layouts.pageDecoder
    |> Collection.title _.Title
    |> Collection.layouts
        _.Layout
        [
            "page", Layouts.page
            "home", Layouts.home
        ]

let posts = Posts.collection |> Collection.layout Layouts.post

let tags =
    Collection.fromPages "tags" Posts.collection Posts.tagPages
    |> Collection.title _.Tag.Name
    |> Collection.route (fun page ->
        Posts.tagRoute page.Locale page.FrontMatter.Tag.Name page.FrontMatter.Paged.Number
    )
    |> Collection.layout Layouts.tag

let site =
    Site.create "My blog"
    |> Site.origin "https://example.com"
    |> Site.stylesheet "assets/site.css"
    |> Markdown.register
    |> ThemeCore.register
    |> Feed.register Posts.feed
    |> Site.collection pages
    |> Site.collection posts
    |> Site.collection tags

[<EntryPoint>]
let main argv = Nacara.run site argv
```

`Collection.layouts` renders each page with the layout its front matter names. A page naming
another layout fails the build.

`Collection.fromPages` makes a collection from the pages of another. Each value the function
returns is the front matter of one page. The pages have no source file, so the collection needs a
`Collection.route`.

`content/pages/404.md` is written as `404.html`, where static hosts look for it.

## Styles

`ThemeCore.register` registers the shared script, the default `--nacara-*` tokens and the
stylesheet that draws what plugins emit: code blocks, callouts, tabs. They are in the
`nacara.tokens` and `nacara.content` cascade layers. The site's own stylesheet is outside the
layers, so its rules win over them.

```css title="assets/site.css"
body {
    font-family: var(--nacara-font-sans);
    color: var(--nacara-text);
    background: var(--nacara-bg);
}
```

The tokens are set on `:root`, `:root[data-theme="light"]` and `:root[data-theme="dark"]`. To
change one, set it under the same selector. A site that defines every token itself turns the
default ones off:

```fsharp
|> ThemeCore.registerWith (ThemeCore.defaultTokens false)
```

The [tokens](../plugins/themes/core/contracts.md#tokens) lists them.

## Run it

```bash frame="terminal"
dotnet run -- watch    # served with the drafts
dotnet run -- build    # written to output/, without the drafts
```
