---
title: GitHub Pages
---

Publishes a build to the branch GitHub Pages serves, one version at a time.

## Add it

```bash frame=terminal
dotnet add package Nacara.Plugin.Deploy.GitHubPages --prerelease
```

```fsharp ins={3}
Site.create "My library"
|> Markdown.register
|> GitHubPages.register
```

It adds a `gh-pages` command to your site:

```bash frame=terminal
dotnet run -- build
dotnet run -- gh-pages --dry-run
dotnet run -- gh-pages
```

`--dry-run` prints the files the deploy would add, change and remove, and touches nothing.

## What it does

It publishes what the last build wrote, so build first. Then, from inside your repository:

1. fetches the branch
2. builds the tree it should hold, from the one it already holds
3. commits that tree with the source commit in its message
4. pushes it

Nothing is checked out and nothing is copied: the other versions' directories are carried across as
they were published, which is why publishing one version leaves the rest alone.

The commit is attributed to whoever `git config user.email` says, or to Nacara when nobody is
configured - which is the case on a CI runner.

## Versioned sites

Which version this build is comes from the `versions.json` the [versions](versions.md) plugin
wrote, so the two work together without being told about each other:

```bash frame=terminal
dotnet run -- build                # the current version → the root of the branch
dotnet run -- gh-pages

dotnet run -- build --version 2.0  # an older version → /2.0/ and nothing else
dotnet run -- gh-pages
``` 

Publishing the current version replaces the root and keeps every version directory. Publishing
`2.0` replaces `2.0/` and keeps the root. Neither can lose the other.

## From GitHub Actions

```yaml title=".github/workflows/docs.yml"
permissions:
  contents: write

# Two deploys writing the branch at once would lose one of them.
concurrency:
  group: gh-pages

jobs:
  publish:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v7
      - uses: actions/setup-dotnet@v6
        with:
          global-json-file: global.json
      - run: dotnet run --project docs -- build
      - run: dotnet run --project docs -- gh-pages
```

Point Pages at the branch once, under **Settings → Pages → Build and deployment → Deploy from a
branch**.

:::warning
If you use `actions/deploy-pages` is the other way to publish its artifact becomes the whole site,
so every deploy would erase the versions it does not contain.
:::

## Options

| Option | Default | Effect |
|---|---|---|
| `Branch` | `"gh-pages"` | The branch Pages serves |
| `Remote` | `"origin"` | The remote it is pushed to |

```fsharp
GitHubPages.registerWith (GitHubPages.branch "pages" >> GitHubPages.remote "upstream")
```

## Reference

Every function and option of it, signature by signature:
[`GitHubPages`](../reference/nacara-plugin-deploy-githubpages/nacara-plugins/githubpages.md).
