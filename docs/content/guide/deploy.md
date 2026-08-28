---
title: Deploying
---

A built site is static files. Any host will do.

## Where the site lives

Two settings say where, and several things need them:

```fsharp
Site.create "Nacara"
|> Site.baseUrl "/Nacara/"                        // the path it is served from
|> Site.origin "https://mangelmaxime.github.io"   // where that path lives
```

The base URL goes through every URL the engine emits: pages, assets, menus. The origin is used by
[canonical links and the sitemap](../plugins/sitemap.md), which are left out when it is not
declared.

## The page for everything else

Every static host - GitHub Pages, GitLab Pages, Netlify, Cloudflare Pages, Vercel - serves
`404.html` from the root of your output when a URL matches nothing. The default theme writes one for
you, in every locale.

Write your own when you want it to say something specific. Add `404.md` to a collection and route
it as a file, since a host looks for `404.html` and not `404/index.html`:

```fsharp
Theme.docs theme "content"
|> Collection.route (fun page ->
    if RelativePath.value page.RelativePath = "404.md" then
        Route.file page.Locale "404.html"
    else
        Collection.defaultRoute page
)
```

The theme writes its own only where no page of yours claims that path. Use `Route.file` for
anything else a host expects at a literal path.

Either way the page is ordinary content: your theme, your navbar, your search box, and links
resolved through the route table - which matters here, since a reader hits this page at an
arbitrary deep URL. The development server serves it too.

## GitHub Pages

```yaml title=".github/workflows/docs.yml"
name: Docs
on:
  push:
    branches: [ main ]

# deploy-pages trades this run's OIDC token for a deployment, so the job needs both
# permissions and the github-pages environment. Without them it fails at the last step.
permissions:
  contents: read
  pages: write
  id-token: write

jobs:
  build:
    runs-on: ubuntu-latest
    environment:
      name: github-pages
      url: ${{ steps.deployment.outputs.page_url }}
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          global-json-file: global.json
      # build, not check: check deliberately writes nothing, so there would be
      # nothing to upload. Both fail on errors.
      - run: dotnet run --project docs -- build
      - uses: actions/upload-pages-artifact@v3
        with:
          path: docs/output
      - uses: actions/deploy-pages@v4
        id: deployment
```

Pages also has to be told to expect this: **Settings → Pages → Build and deployment → Source**, set
to *GitHub Actions*. A workflow that is right in every other way still fails while that says *Deploy
from a branch*.

## Versions

A version is a **build**, not a dimension of your content. Your repository holds the site your
sources describe now; older versions are earlier builds sitting in their own directories.

`--version` tells a build where it will be served from, so every URL it writes points there:

```bash frame="terminal"
dotnet run -- build --version 1.0
```

That build's links, assets, canonical URLs and sitemap all sit under `/1.0/`. The output directory
itself does not change - there is no `1.0/` inside it. Leave the flag out and the build addresses
the root.

### Building one

A version's pages come from that version's sources, so building one is a checkout and a build:

```bash frame="terminal"
git checkout v1.0
dotnet run -- build --version 1.0        # → copy output/ to <host>/1.0/
git checkout main
dotnet run -- build                      # → copy output/ to <host>/
```

Each build is self-contained: the 1.0 tag pins its own `Site.fs`, its own plugins and its own
Nacara, so rebuilding it a year later gives the same site.

### Rebuilding an old version

A version is a directory of files, and you replace it the way you made it: check out the tag, build
it, copy `output/` over `<host>/1.0/`. To fix a typo in the 1.0 docs, commit to a `1.0` maintenance
branch and rebuild from it. The other versions are untouched, since that build addresses nothing
outside `/1.0/`.

### Deploying them side by side

Deploying is a copy into a directory. Versions never overwrite each other, since their prefixes
differ:

```bash frame="terminal"
dotnet run -- build && cp -r output/. public/
dotnet run -- build --version 1.0 && cp -r output/. public/1.0/
```

One caveat on hosts that publish a whole directory as *the* site - GitHub Pages with
`deploy-pages` is one: uploading only the current version's output **replaces** everything,
including the version directories already online. Either assemble the versions you want served in
one job:

```yaml title=".github/workflows/docs.yml"
      - name: Build the current version
        run: dotnet run --project docs -- build

      - name: Stage it at the root
        run: mkdir -p public && cp -r docs/output/. public/

      - name: Build 1.0 from its tag
        run: |
          git checkout v1.0
          dotnet run --project docs -- build --version 1.0
          cp -r docs/output/. public/1.0/
          git checkout -

      - uses: actions/upload-pages-artifact@v3
        with:
          path: public

      - uses: actions/deploy-pages@v4
        id: deployment
```

...or publish to a branch that keeps what is already there, and write only the directory you
rebuilt.

The [versions plugin](../plugins/versions.md) adds the switcher and the `versions.json` it reads.
That manifest belongs at the **deployment root**, above the version directories - each build writes
one into its own output, and publishing copies the current version's copy up.
