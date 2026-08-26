---
title: Search
---

A search box in the navbar and a modal behind it, over an index [Pagefind](https://pagefind.app)
builds from your pages. It runs in the reader's browser, so you have no service to pay for, no
account to create and nothing to keep running.

## Add it

```bash frame=terminal
dotnet add package Nacara.Plugin.Search --prerelease
```

```fsharp ins={1-4,9}
let theme =
    Theme.defaults
    |> Theme.navbarEnd [ NavbarDynamicWidget Search.trigger ]

let site =
    Site.create "My library"
    |> Markdown.register
    |> Search.register
    |> Theme.register theme
```

Two lines, because they answer different questions. `Search.register` builds the index and ships
the modal; `Search.trigger` says where the box goes. Put the trigger anywhere the theme takes a
widget.

Then build. The first build fetches the pinned Pagefind release into `~/.cache/nacara` and indexes
the output:

```text frame="terminal"
✓ Search index built
✓ Built 22 pages, 22 written, 512 ms
```

## What a reader gets

<kbd>Ctrl</kbd>+<kbd>K</kbd> (<kbd>⌘</kbd>+<kbd>K</kbd> on a Mac) from anywhere opens the modal, and
so does <kbd>/</kbd> when they are not typing in a field. Results are grouped by page, and by
section within it: a click anywhere on a row follows it, <kbd>↑</kbd> <kbd>↓</kbd> walk them,
<kbd>↵</kbd> opens the one they are on, <kbd>esc</kbd> closes. The modal lists these keys along its
bottom.

The button shows the chord that belongs to their keyboard, decided in their browser rather than at
build time.

Nothing is downloaded until someone searches - not the index, not the modal's own code - so search
costs a page view nothing.

## How it looks

The modal is Pagefind's own, dressed in the theme's tokens: its surfaces, text, borders, focus ring
and shadow are the ones the rest of your site uses, so it follows the reader's light or dark choice
and you have no second design to keep in step.

Set Pagefind's own variables when you want something else - `--pf-background`, `--pf-text`,
`--pf-border`, `--pf-hover`, `--pf-modal-max-width`, and the rest of what
[its documentation lists](https://pagefind.app):

```css
:root:root {
    --pf-modal-max-width: 52rem;
    --pf-border-radius: 0;
}
```

Copy the doubled `:root:root`. Pagefind's stylesheet is loaded the first time a reader opens the
modal, which is after yours, and it declares its defaults on a plain `:root` - so a plain `:root` of
your own loses to it and appears to do nothing.

## Options

| Option | Default | Effect |
|---|---|---|
| `BinaryPath` | `None` | Use your own pagefind instead of the pinned release |
| `RootSelector` | `"main"` | The element Pagefind indexes as the page body |

`RootSelector` keeps the navbar and the sidebar out of every result. Change it if your layout puts
content somewhere else.

## Using your own pagefind

Pagefind is fetched once per machine and kept, so the download happens on your first build and
never again. Point `BinaryPath` at your own copy and nothing is fetched at all:

```fsharp
|> Search.registerWith (fun options ->
    { options with
        BinaryPath = Some "/opt/pagefind/pagefind"
    }
)
```

## Deploying

The index lives in `pagefind/` beside your pages and deploys with them. You need nothing else - no
server-side component, no external service.

A site served from a subdirectory finds its index there too: the trigger is rendered from your site,
so `/project/` looks in `/project/pagefind/`.

## Reference

Every function and option of it, signature by signature: [`Search`](../reference/nacara-plugin-search/nacara-plugins/search.md).
