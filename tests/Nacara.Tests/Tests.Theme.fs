module Nacara.Tests.DefaultTheme

open Scriptorium.Nib.Assertion
open Scriptorium.Nib.Snapshot
open type Scriptorium.Quill.Test
open System.IO
open System.Text.RegularExpressions
open Feliz.ViewEngine
open Nacara.Core
open Nacara.Plugins
open Nacara.Theme
open Nacara.Theme.Core
open Nacara.Theme.Docs
open Nacara.Tests

let private directives =
    """---
title: Directives
description: Every directive the markdown plugin knows
---

:::note Heads up
A callout with a title.
:::

:::tip
A callout without one.
:::

::::tabs install
:::tab dotnet
```bash
dotnet add package Nacara.Core
```
:::
:::tab paket
```bash
paket add Nacara.Core
```
:::
::::

:::steps

### Create the project

Anything a step needs goes under its heading.

### Build it

```bash frame=terminal
dotnet run -- build
```

:::

:::details Show me more
Anything in here starts folded.
:::

:::filetree
- src
  - **Build.fs**
  - Route.fs
- global.json
:::

::::preview
```markdown
:::tip
Written once, shown twice.
:::
```
::::

```fsharp title="Program.fs" showLineNumbers {1} ins={3} del={4}
let one = 1
let two = 2
let three = 3
let four = 4
```

```json collapse={2-3}
{
  "a": 1,
  "b": 2,
  "c": 3
}
```
"""

let private changelog (label: string) =
    $"""# Changelog

## 1.1.0 - 2024-06-01

### Added

- Something new in %s{label}

## 1.0.0 - 2024-01-15

### Added

- The first release of %s{label}
"""

let private sources =
    [
        ChangelogSource.create "One" "CHANGELOG-one.md"
        ChangelogSource.create "Two" "CHANGELOG-two.md"
        |> ChangelogSource.group "Plugins"
    ]

let private theme =
    Theme.defaults
    |> Theme.navbar
        [
            NavbarSection("Guide", "guide", "guide/getting-started.md")
            NavbarSection("Changelog", "changelog", "/changelog/one/")
            NavbarDropdown(
                "More",
                [
                    NavbarDescribed("Advanced", "The deep end", "guide/advanced.md")
                    NavbarDivider
                    NavbarLink("Repository", "https://example.com/repo")
                ]
            )
        ]
    |> Theme.navbarEnd
        [
            NavbarIcon("GitHub", "https://example.com/repo", Icons.github)
            NavbarWidget """<span class="widget">Widget</span>"""
            NavbarDynamicWidget(fun site -> $"<span>%s{site.Title}</span>")
        ]
    |> Theme.menu
        "guide"
        [
            Menu.page "guide/getting-started.md" |> Menu.badge "New"
            Menu.section
                "Section"
                [
                    Menu.group
                        "guide/advanced.md"
                        [ for index in 1..8 -> Menu.page $"guide/member-%i{index}.md" ]
                    Menu.page "guide/directives.md" |> Menu.badgeOf Badge.Deprecated "Old"
                    Menu.link "Outside" "https://example.com"
                ]
        ]
    |> Theme.menuLimit 1
    |> Theme.menuGroupLimit 3
    |> Theme.editUrl "https://example.com/edit/main/docs"
    |> Theme.footer (Html.p "The footer")
    |> Theme.css "body { --site-rule: 1; }"
    |> Theme.headExtra
        [
            Html.meta
                [
                    prop.name "generator"
                    prop.content "tests"
                ]
        ]
    |> Theme.favIcon "favicon.ico"

let private site =
    Site.create "Fixture"
    |> Site.description "A site that exists to be snapshotted"
    |> Site.baseUrl "/"
    |> Site.output "output"
    |> Site.noStaticFiles
    |> Site.plugin (Markdown.create ())
    |> Site.plugin (TextMate.create ())
    |> Changelog.registerWith "changelog" sources
    |> Theme.register theme
    |> Site.collection (Theme.docs theme "docs")
    |> Site.collection (
        Changelog.collection "changelog" DocFrontMatter.decoder sources
        |> Collection.title _.Title
        |> Collection.routePrefix "changelog"
        |> Collection.layout (Theme.layout theme)
    )

/// <summary>The fixture, built once with the theme and every option set.</summary>
let private built =
    lazy
        (let root = Fixture.copyToTemporaryDirectory ()
         let rootPath = AbsolutePath.value root

         for index in 1..8 do
             File.WriteAllText(
                 Path.Combine(rootPath, $"docs/guide/member-%i{index}.md"),
                 $"---\ntitle: Member %i{index}\n---\n\nOne of many.\n"
             )

         File.WriteAllText(Path.Combine(rootPath, "docs/guide/directives.md"), directives)
         File.WriteAllText(Path.Combine(rootPath, "CHANGELOG-one.md"), changelog "One")
         File.WriteAllText(Path.Combine(rootPath, "CHANGELOG-two.md"), changelog "Two")

         Build.run root site |> ignore
         root)

/// Assets carry a content hash in their name, which is not what these snapshots are about.
let private normalise (html: string) =
    Regex.Replace(html, @"\.[0-9a-f]{8}\.(css|js)", ".HASH.$1")

let private output (path: string) =
    File.ReadAllText(Path.Combine(AbsolutePath.value built.Value, "output", path))

let all =
    testList (
        "Theme",
        [
            // One file holds these snapshots, so tests writing it at the same time lose all but one.
            testSequenced (
                "default theme pages",
                [
                    test (
                        "the home page renders as expected",
                        fun context -> context.snapshotWith (normalise, output "index.html")
                    )
                    test (
                        "a page in a declared menu renders as expected",
                        fun context ->
                            context.snapshotWith (normalise, output "guide/advanced/index.html")
                    )
                    test (
                        "a page in a pruned group renders as expected",
                        fun context ->
                            context.snapshotWith (normalise, output "guide/member-3/index.html")
                    )
                    test (
                        "a pruned menu is written out whole",
                        fun context ->
                            context.snapshotWith (normalise, output "assets/menu/guide.json")
                    )
                    test (
                        "every directive renders as expected",
                        fun context ->
                            context.snapshotWith (normalise, output "guide/directives/index.html")
                    )
                    test (
                        "a page in a menu a plugin offered renders as expected",
                        fun context ->
                            context.snapshotWith (normalise, output "changelog/one/index.html")
                    )
                    test (
                        "the theme's 404 page renders as expected",
                        fun context -> context.snapshotWith (normalise, output "404.html")
                    )
                ]
            )

            test (
                "a menu a plugin offers reaches a theme registered before it",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()
                    let rootPath = AbsolutePath.value root
                    File.WriteAllText(Path.Combine(rootPath, "CHANGELOG-one.md"), changelog "One")
                    File.WriteAllText(Path.Combine(rootPath, "CHANGELOG-two.md"), changelog "Two")

                    let site =
                        Site.create "Fixture"
                        |> Site.baseUrl "/"
                        |> Site.output "output"
                        |> Site.noStaticFiles
                        |> Markdown.register
                        |> Theme.register Theme.defaults
                        |> Changelog.registerWith "changelog" sources
                        |> Site.collection (
                            Changelog.collection "changelog" DocFrontMatter.decoder sources
                            |> Collection.title _.Title
                            |> Collection.routePrefix "changelog"
                            |> Collection.layout (Theme.layout Theme.defaults)
                        )

                    Build.run root site |> ignore

                    let html =
                        File.ReadAllText(Path.Combine(rootPath, "output/changelog/one/index.html"))

                    assertThat
                        (html.Contains "<span>Plugins</span>"
                         && html.Contains "href=\"/changelog/two/\"")
                        (tag "the offered menu is drawn, whichever was registered first" >> isTrue)
            )

            test (
                "a part can be replaced, and a default wrapped",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    let theme =
                        Theme.defaults
                        |> Theme.footer (Html.p "Theirs")
                        |> Theme.parts (fun parts ->
                            { parts with
                                Footer =
                                    fun context ->
                                        Html.div
                                            [
                                                prop.className "wrapped"
                                                prop.children [ Parts.footer context ]
                                            ]
                                Navbar =
                                    fun context model ->
                                        Parts.navbar
                                            context
                                            { model with
                                                End =
                                                    model.End
                                                    @ [
                                                        NavbarEntry.Widget
                                                            "<span class=\"sponsor\">Sponsor</span>"
                                                    ]
                                            }
                            }
                        )

                    let site =
                        Site.create "Fixture"
                        |> Site.baseUrl "/"
                        |> Site.output "output"
                        |> Site.noStaticFiles
                        |> Markdown.register
                        |> Theme.register theme
                        |> Site.collection (Theme.docs theme "docs")

                    Build.run root site |> ignore

                    let html =
                        File.ReadAllText(Path.Combine(AbsolutePath.value root, "output/index.html"))

                    assertThat
                        (html.Contains
                            "<div class=\"wrapped\"><footer class=\"nacara-footer\"><p>Theirs</p></footer></div>")
                        (tag "the replacement wraps what the theme would have rendered" >> isTrue)

                    assertThat
                        (html.Contains "<li><span class=\"sponsor\">Sponsor</span></li>")
                        (tag "and a model changed on its way in is what the default draws" >> isTrue)
            )

            test (
                "a layout the theme does not know fails the build",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    File.WriteAllText(
                        Path.Combine(AbsolutePath.value root, "docs/odd.md"),
                        "---\ntitle: Odd\nlayout: poster\n---\n\nNothing draws this.\n"
                    )

                    let site =
                        Site.create "Fixture"
                        |> Site.baseUrl "/"
                        |> Site.output "output"
                        |> Site.noStaticFiles
                        |> Markdown.register
                        |> Theme.register Theme.defaults
                        |> Site.collection (Theme.docs Theme.defaults "docs")

                    let result = Build.run root site

                    let refused =
                        result.Diagnostics
                        |> List.tryFind (fun diagnostic ->
                            diagnostic.Severity = Severity.Error
                            && diagnostic.Message.Contains "'poster' is not a layout"
                        )

                    assertThat
                        refused.IsSome
                        (tag "the page is refused, naming the layout" >> isTrue)

                    assertThat
                        (refused |> Option.bind _.Span).IsSome
                        (tag "and the error points at the file" >> isTrue)
            )

            test (
                "a theme can know a layout of its own",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    File.WriteAllText(
                        Path.Combine(AbsolutePath.value root, "docs/landing.md"),
                        "---\ntitle: Landing\nlayout: landing\n---\n\nWelcome.\n"
                    )

                    let layouts =
                        DocFrontMatter.layouts
                        |> Map.add "landing" (DocPage.splash >> DocPage.withoutMenu)

                    let theme =
                        Theme.defaults
                        |> Theme.parts (fun parts ->
                            { parts with
                                Frame =
                                    fun context slots ->
                                        if context.Doc.Layout = "landing" then
                                            Html.div
                                                [
                                                    prop.className "landing"
                                                    prop.children
                                                        [
                                                            slots.Navbar
                                                            slots.Main
                                                        ]
                                                ]
                                        else
                                            Parts.frame context slots
                            }
                        )

                    let site =
                        Site.create "Fixture"
                        |> Site.baseUrl "/"
                        |> Site.output "output"
                        |> Site.noStaticFiles
                        |> Markdown.register
                        |> Theme.register theme
                        |> Site.collection (
                            Docs.collectionWith
                                (DocFrontMatter.decoderWith (List.ofSeq layouts.Keys))
                                "docs"
                                (fun context ->
                                    Theme.shell
                                        theme
                                        (DocFrontMatter.toDocPageWith layouts context.FrontMatter)
                                        context
                                )
                        )

                    let result = Build.run root site

                    assertThat
                        (result.Diagnostics
                         |> List.forall (fun diagnostic -> diagnostic.Severity <> Severity.Error))
                        (tag "the layout is accepted" >> isTrue)

                    let landing =
                        File.ReadAllText(
                            Path.Combine(AbsolutePath.value root, "output/landing/index.html")
                        )

                    assertThat
                        (landing.Contains "<div class=\"landing\">")
                        (tag "and the frame is told which layout the page asked for" >> isTrue)

                    let home =
                        File.ReadAllText(Path.Combine(AbsolutePath.value root, "output/index.html"))

                    assertThat
                        (home.Contains "<div class=\"landing\">")
                        (tag "while a page that did not ask keeps the theme's frame" >> isFalse)
            )
        ]
    )
