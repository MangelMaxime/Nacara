---
title: Nacara
description: A documentation engine for F#, where the site is an F# program
layout: bare
---

<div class="landing">
<section class="landing-hero">

<h1 class="landing-hero__title">Broken documentation fails the build</h1>

<p class="landing-hero__lede">Nacara turns markdown into a documentation site, and your site is an F# program. Front matter is decoded into types you declare, links are resolved against the route table, and anchors are checked against the headings they name.</p>

<p class="landing-actions">
<a class="landing-button landing-button--primary" href="/Nacara/guide/getting-started/">Get started</a>
<a class="landing-button" href="https://github.com/MangelMaxime/Nacara">View on GitHub</a>
</p>

</section>

```bash frame="terminal"
dotnet new install Nacara.Templates
dotnet new nacara-docs -o docs
cd docs && dotnet run -- watch
```

<div class="landing-intro">

<h2 class="landing-section__title">Your editor already knows it</h2>

<p class="landing-lede">The site is F#, so describing it comes with completion, go to definition
and type errors. A plugin you spelled wrong is a squiggle under your cursor, not a surprise in
CI.</p>

</div>

```fsharp title="docs/Site.fs"
let site =
    Site.create "My library"
    |> Site.baseUrl "/"
    |> Markdown.register
    |> TreeSitter.register
    |> Search.register
    |> Theme.register theme
    |> Site.collection (Theme.docs theme "content")

[<EntryPoint>]
let main argv = Nacara.run site argv
```

<div class="landing-intro">

<h2 class="landing-section__title">Your documentation can be interactive</h2>

<p class="landing-lede">A code block marked <code>live</code> becomes an editor. Fable compiles it
in the browser against your library, so a reader can change the example and run it, and it cannot
drift from the code it documents.</p>

</div>

```fsharp live
open Browser.Dom
open Demo

// Point and distance come from this site's preset, so the type-checker answers for them.
let route =
    [
        { X = 10.0; Y = 60.0 }
        { X = 70.0; Y = 20.0 }
        { X = 130.0; Y = 75.0 }
        { X = 190.0; Y = 30.0 }
    ]
    
let travelled = route |> List.pairwise |> List.sumBy (fun (a, b) -> distance a b)

let line = route |> List.map (fun p -> $"%.0f{p.X},%.0f{p.Y}") |> String.concat " "

let dots =
    route
    |> List.map (fun p -> $"<circle cx='%.0f{p.X}' cy='%.0f{p.Y}' r='6' fill='#6669d7' />")
    |> String.concat ""

document.getElementById("app").innerHTML <-
    $"<h2>%.1f{travelled} units travelled</h2>"
    + "<svg viewBox='0 0 200 90' width='320' height='144'>"
    + $"<polyline points='%s{line}' fill='none' stroke='#6669d7' stroke-width='2' />"
    + dots
    + "</svg>"
```

<section>

<h2 class="landing-section__title">What that buys you</h2>

<div class="landing-grid">

<article class="landing-card">
<h3>Nothing to keep in sync</h3>
<p>The engine is a library your project references, so the tool can never be a version behind the site it builds.</p>
</article>

<article class="landing-card">
<h3>Front matter with types</h3>
<p>A collection declares what it expects. A missing field is a build error with a file, a line and a column - not a blank heading in production.</p>
</article>

<article class="landing-card">
<h3>Links that cannot rot</h3>
<p>Write them the way they work on GitHub, <code>../guide/index.md</code>. The engine resolves them, and a link or anchor pointing nowhere stops the build.</p>
</article>

<article class="landing-card">
<h3>Fast, and it stays fast</h3>
<p>Work is memoised on content hashes, only changed files are written, and orphaned output is pruned. Watching rebuilds what changed and nothing else.</p>
</article>

<article class="landing-card">
<h3>One package per feature</h3>
<p>Search, versions, changelogs, literate F#, an API reference read from your assemblies - each is a plugin on a small core, wired with the pipeline you already write.</p>
</article>

<article class="landing-card">
<h3>Published from the same program</h3>
<p>Build it, then <code>dotnet run -- gh-pages</code> puts it on the branch GitHub Pages serves, leaving the other versions where they are.</p>
</article>

</div>

</section>

<section class="landing-hero landing-hero--closing">

<h2 class="landing-hero__title">Start with a page and a menu</h2>

<p class="landing-hero__lede">The template sets up a site you can deploy, with the plugins a published site needs.</p>

<p class="landing-actions">
<a class="landing-button landing-button--primary" href="/Nacara/guide/getting-started/">Get started</a>
<a class="landing-button" href="/Nacara/plugins/overview/">Browse the plugins</a>
</p>

</section>
</div>
