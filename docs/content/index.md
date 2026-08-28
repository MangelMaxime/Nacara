---
title: Nacara
description: A documentation engine for F#, where the site is an F# program
layout: bare
---

<div class="landing">
<section class="landing-hero">

<p class="landing-hero__eyebrow">Documentation engine for F#</p>

<h1 class="landing-hero__title">Documentation the compiler checks</h1>

<p class="landing-hero__lede">Nacara turns markdown into a documentation site, and your site is an F# program. Front matter is decoded into types you declare, links are resolved against the route table, and a page that would ship broken fails the build instead.</p>

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
<p>Build it, then <code>dotnet run -- gh-pages</code> puts it on the branch GitHub Pages serves, one version at a time.</p>
</article>

</div>

</section>

<section class="landing-hero landing-hero--closing">

<h2 class="landing-hero__title">Start with a page and a menu</h2>

<p class="landing-hero__lede">The template sets up a site you can deploy, with the plugins a published one wants.</p>

<p class="landing-actions">
<a class="landing-button landing-button--primary" href="/Nacara/guide/getting-started/">Get started</a>
<a class="landing-button" href="/Nacara/plugins/overview/">Browse the plugins</a>
</p>

</section>
</div>
