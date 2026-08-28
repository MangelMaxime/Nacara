---
title: Search
---

A search box in the navbar and a modal behind it, over an index [Pagefind](https://pagefind.app)
builds from your pages. It runs in the reader's browser: no service, no account, nothing to keep
running.

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

Two lines: `Search.register` builds the index and ships the modal, `Search.trigger` says where the
box goes. Put the trigger anywhere the theme takes a widget.

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

The button shows the chord for the reader's keyboard, decided in their browser.

Nothing is downloaded until someone searches: not the index, not the modal's own code.

## How it looks

The modal is Pagefind's own, drawn with the theme's tokens: the same surfaces, text, borders, focus
ring and shadow as the rest of your site, so it follows the reader's light or dark choice.

Set Pagefind's own variables when you want something else - `--pf-background`, `--pf-text`,
`--pf-border`, `--pf-hover`, `--pf-modal-max-width`, and the rest of what
[its documentation lists](https://pagefind.app):

```css
:root:root {
    --pf-modal-max-width: 52rem;
    --pf-border-radius: 0;
}
```

Copy the doubled `:root:root`. Pagefind's stylesheet loads the first time a reader opens the modal,
after yours, and declares its defaults on a plain `:root`, which a plain `:root` of your own would
lose to.

## Options

| Option | Default | Effect |
|---|---|---|
| `BinaryPath` | `None` | Use your own pagefind instead of the pinned release |
| `RootSelector` | `"main"` | The element Pagefind indexes as the page body |

`RootSelector` keeps the navbar and the sidebar out of every result. Change it if your layout puts
content somewhere else.

## Using your own pagefind

Pagefind is fetched once per machine and kept, so only the first build downloads it. Point
`BinaryPath` at your own copy and nothing is fetched:

```fsharp
|> Search.registerWith (fun options ->
    { options with
        BinaryPath = Some "/opt/pagefind/pagefind"
    }
)
```

## Deploying

The index lives in `pagefind/` beside your pages and deploys with them. Nothing else is needed: no
server-side component, no external service.

A site served from a subdirectory finds its index there too: the trigger is rendered from your site,
so `/project/` looks in `/project/pagefind/`.

## Reference

Every function and option of it, signature by signature: [`Search`](../reference/nacara-plugin-search/nacara-plugins/search.md).
