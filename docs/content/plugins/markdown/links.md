---
title: Links
---

You write links between **files**, the way you do on GitHub, and the engine resolves them against
the built site. Nothing in your content spells out a URL, so a rename cannot leave a link behind.

## Writing them

```markdown
See [writing content](../../guide/content.md) and [its front matter](../../guide/content.md#front-matter).
```

The path is relative to the file you are writing, and the engine turns it into whatever URL that
page ends up at - including the site's base URL, its locale prefix and its version prefix. Move the
page, change `Collection.routePrefix`, deploy under `/docs/`: the link follows.

Images and assets work the same way.

## What gets checked

**Every internal link, while the site builds.** A link that points at no page fails:

```text frame="terminal"
✗ content/guide/deploy.md(12,3): error markdown/link-target-missing: This link points at an unknown page 'nowhere.md'
    hint: A link names a file: one beside this page, or one from the project root with a leading '/'.
```

**Every anchor, once every page is rendered** - the earliest point at which the target's ids are
known:

```text frame="terminal"
✗ content/index.md(1,1): error markdown/anchor-missing: 'guide/theme.md#nope' points at an anchor that does not exist on the target page
```

Anchors are checked against *every id on the page*, not only the headings the table of contents
kept, so an `{#anchor}` you wrote yourself counts too.

## Strictness

Both are errors by default. If you are migrating a lot of content, turn them into warnings while
you work through it:

```fsharp
|> Markdown.registerWith (fun options -> { options with StrictLinks = false })
```

## Links that leave the site

This plugin leaves them alone and fetches nothing during a build. To check those - and the links in
the *published* HTML, including ones no markdown pass ever saw - use the
[link validator](../checks/link-validator.md).

## References to issues and commits

With a repository declared, `#12` and a commit hash become links to it:

```fsharp
|> Markdown.registerWith (fun options ->
    { options with
        GithubRepo = Some "MangelMaxime/Nacara"
    }
)
```

That is what makes a [changelog page](../changelogs.md) generated from commits readable: its hashes
lead somewhere.
