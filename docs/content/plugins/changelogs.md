---
title: Changelog
---

Publishes `CHANGELOG.md` files as pages of your site, so releases are documentation rather than a
file people have to find on GitHub.

## Add it

```bash frame=terminal
dotnet add package Nacara.Plugin.Changelog --prerelease
```

```fsharp ins={1-11,16,18}
let changelogs =
    [
        ChangelogSource.create "My library" "CHANGELOG.md"
        ChangelogSource.create "My library.Extras" "src/Extras/CHANGELOG.md"
    ]

let changelog =
    Changelog.collection "changelog" DocFrontMatter.decoder changelogs
    |> Collection.title _.Title
    |> Collection.routePrefix "changelog"
    |> Collection.layout (Theme.layout theme)

let site =
    Site.create "My library"
    |> Markdown.register
    |> Changelog.registerWith "changelog" changelogs
    |> Theme.register theme
    |> Site.collection changelog
```

Two calls, doing two jobs. The collection produces the pages, so you choose their front-matter
type, route and layout, as you would for any other content. `registerWith` adds their styling and
offers the menu for the section, so you do not write the list of packages twice.

Add one source per package. The label becomes the page's title, and the route is a slug of that
label unless you pick one yourself with `ChangelogSource.slug`.

Paths are resolved from the project root, so `"../CHANGELOG.md"` reaches a file above your docs
project.

## Or find them

A list of packages is a list you have to keep in step with your solution. `matching` does it for
you, naming each page after the directory holding the file:

```fsharp
let changelogs =
    [
        ChangelogSource.create "My library" "../CHANGELOG.md"

        ChangelogSource.matching "../src/*/CHANGELOG.md"
        |> ChangelogSource.group "Packages"
    ]
```

Anything you set applies to every match, so the group is written once. Matches come out in a stable
order, and a pattern that finds nothing is silent - a package that has no changelog yet is not a
mistake, while a *named* file that is missing still is.

When the directory is not the name you want, say what is:

```fsharp
ChangelogSource.matching "../packages/*/CHANGELOG.md"
|> ChangelogSource.labelledBy (fun path ->
    "My." + Path.GetFileName(Path.GetDirectoryName path))
```

The callback is given the full path of the file that matched, so you can take whichever part of it
names the thing.

A changelog sitting at the root of a repository - one project, one changelog - has no package
directory to be named after, only whatever the person cloning called the checkout. That one is
called after the file instead, so it reads as **Changelog**.

## Letting the file name itself

`name:` in the changelog's own front matter beats anything worked out from its path:

```markdown title="CHANGELOG.md"
---
name: My Library
---

## 1.0.0
```

[EasyBuild.ShipIt](https://github.com/easybuild-org/EasyBuild.ShipIt#name) reads that same field for
its release notes, and takes the directory name when it is absent - so a changelog it already
manages names its page here without you writing anything twice.

It settles the page's title and its URL together, so a menu and a page can never disagree about
what a changelog is called.

## The menu writes itself

`registerWith` offers the menu for the section: one entry per changelog, in the order you declared
them. When you have more packages than fit in one list, say what each one is:

```fsharp
let changelogs =
    [
        ChangelogSource.create "Nacara.Core" "../src/Nacara.Core/CHANGELOG.md"
        |> ChangelogSource.group "Engine"

        ChangelogSource.create "Nacara.Theme.Default" "../src/Nacara.Theme.Default/CHANGELOG.md"
        |> ChangelogSource.group "Themes"

        ChangelogSource.create "Nacara.Templates" "../templates/CHANGELOG.md"
        |> ChangelogSource.group "Templates"
    ]
```

Changelogs with the same group are listed together under a heading, in the order the groups first
appear. Changelogs without a group are listed on their own, where you declared them.

If you write a menu for the section yourself, yours is used and this one is ignored - what the
plugin offers is a default, never a rule. Use `Changelog.register` instead when you would
rather write it all yourself.

## What ends up on the page

The versions, and only the versions. Everything above the first one - the file's own front matter,
its `# Changelog` title, the sentence about Keep a Changelog - is dropped.

Everything it keeps, it keeps exactly as you wrote it. An entry with a paragraph, two code blocks
and an issue reference under one bullet arrives with all of it:

````markdown
## 0.13.0

### 🐞 Bug Fixes

* Generate a concrete version of interface with constrained type parameters ([24210b7](…))

    ```ts
    declare interface User<T extends Options = Options> {}
    ```

    Fix #211
````

The nested blocks stay nested, and the page goes through the same pipeline as a hand-written one:
highlighting, link checking, heading anchors. That is why the plugin ships no layout of its own -
your layout renders these pages, so they look like the rest of your site.

The table of contents lists the versions only, since their sections are right there under each one.
Set `Collection.toc` on the collection if you want something else.

Each version gets an anchor of its own: `## 2.1.0` becomes `#v2-1-0`, so you can send someone
straight to a release.

## Which headings count as versions

`## 1.2.3`, `## [1.2.3] - 2024-06-01`, `## Unreleased`, `## v1.2.3` - a leading `v` is not part of
the number, brackets are optional, and the separator before a date may be spaced however you like.

That covers everything [EasyBuild.ShipIt](https://github.com/easybuild-org/EasyBuild.ShipIt)
writes, so a changelog it generates needs nothing done to it. The section names inside a version are
your file's own - `Added` in a Keep a Changelog file, `🐞 Bug Fixes` in one ShipIt wrote - and they
are all styled alike.

If no heading in a file looks like a version, the plugin publishes the file whole and the build
warns you. A path that does not exist is an error naming the file.

## Keeping it up to date

Each page says which file it was made from, and watch mode follows those files - including the ones
outside your docs project, which is where a changelog usually lives. Edit
`../src/Extras/CHANGELOG.md` and the page for it rebuilds, the same as editing a page of your own.

The list is rebuilt after every build, so adding a changelog to `Changelog.collection` starts
watching that file too, with no restart.

## Reference

Every function and option of it, signature by signature: [`Changelog`](../reference/nacara-plugin-changelog/nacara-plugins/changelog.md).
