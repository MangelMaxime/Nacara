---
title: Link validator
---

Checks every link your site published: pages, assets, anchors, and the ones that leave the site
when you ask for it.

## What it adds

The [markdown plugin](../markdown/index.md) already resolves the links you write, and fails the build
when one points at no page. This plugin checks something else: **the site as it was published**. It
takes every `href` and `src` in every rendered page - from markdown, a layout, a web component, a
raw HTML block - and checks it against the files the build actually wrote.

That catches:

- an asset a theme names but never ships
- a link written in HTML, which no markdown pass ever looked at
- an anchor that moved when a heading was reworded
- a relative link, whose meaning depends on the page it is read from
- a link that leaves the site and has since died

## Add it

```bash frame=terminal
dotnet add package Nacara.Plugin.LinkValidator --prerelease
```

```fsharp ins={4}
Site.create "My library"
|> Markdown.register
|> Theme.register theme
|> LinkValidator.register
```

It runs after everything is written, so it reads the output directory. You configure nothing for the
checks that need no network:

```text frame="terminal"
✗ content/guide/deploy.md(1,1): error link-validator/target-missing: '/assets/diagram.png' points at nothing this build wrote
    hint: The build would have to write 'assets/diagram.png'
```

## Links that leave the site

These are checked too, and a failure is a **warning**: a link dies on someone else's schedule, and a
build that stops because a server was down for a minute teaches everyone to ignore the result.

Answers are cached under `~/.cache/nacara` for a week, so most builds ask nobody anything. It sends
`HEAD` first because that costs a server nothing, and retries a refusal as a `GET` before believing
it - plenty of sites answer `HEAD` with 405 while serving the page perfectly.

Turn it off where you have no network, and turn failures into errors where you want the build to
stop:

```fsharp
|> LinkValidator.registerWith (fun options ->
    { options with
        CheckExternal = false   // a build behind a firewall
        FailOnExternal = true   // or: a dead link is a broken site
    }
)
```

To ask only where a failure means something - in a weekly workflow, say - decide from the
environment:

```fsharp
|> LinkValidator.registerWith (fun options ->
    { options with
        CheckExternal = System.Environment.GetEnvironmentVariable "NACARA_CHECK_LINKS" = "1"
    }
)
```

## Options

| Option | Default | Effect |
|---|---|---|
| `CheckExternal` | `true` | Ask servers about links that leave the site |
| `CheckWhileWatching` | `false` | Check during watch builds too |
| `FailOnExternal` | `false` | An unreachable external link fails the build rather than warning |
| `Timeout` | `10` | Seconds to wait for a server |
| `Concurrency` | `8` | How many external links to ask about at once |
| `Ignore` | `[]` | Regular expressions matched against the whole url |
| `AllowStatusCodes` | `[]` | Extra status codes to accept - `403` and `429` are common for bots |
| `CacheHours` | `168` | How long an answer stays good |

Use `Ignore` for sites that refuse anything that is not a browser:

```fsharp
|> LinkValidator.registerWith (fun options ->
    { options with
        CheckExternal = true
        Ignore = [ @"^https://www\.linkedin\.com/"; @"localhost" ]
        AllowStatusCodes = [ 403 ]
    }
)
```

## Versioned sites

Each version is its own build, so each one checks its own output. A link from `/2.0/` into `/3.0/`
is reported as `link-validator/outside-site`, and rightly so: that page belongs to a build this one
cannot see, and nothing here can promise it exists.

## Reference

Every function and option of it, signature by signature: [`LinkValidator`](../reference/nacara-plugin-linkvalidator/nacara-plugins/linkvalidator.md).
