module Nacara.Tests.Collections

open Scriptorium.Nib.Assertion
open type Scriptorium.Quill.Test
open System.IO
open Feliz.ViewEngine
open Nacara.Core
open Nacara.Tests

type private IndexFrontMatter =
    {
        Paged: Paged<string>
    }

type private PageFrontMatter =
    {
        Title: string
        Layout: string
    }

let private pageDecoder: Decoder<PageFrontMatter> =
    Decode.object (fun get ->
        {
            Title = get.Required.Field "title" Decode.string
            Layout = get.Optional.Field "layout" Decode.string |> Option.defaultValue "plain"
        }
    )

type private DraftFrontMatter =
    {
        Title: string
        Draft: bool
    }

let private draftDecoder: Decoder<DraftFrontMatter> =
    Decode.object (fun get ->
        {
            Title = get.Required.Field "title" Decode.string
            Draft = get.Optional.Field "draft" Decode.bool |> Option.defaultValue false
        }
    )

type private ArchiveFrontMatter =
    {
        Titles: string list
    }

let private outputOf (root: AbsolutePath) (path: string) =
    Path.Combine(AbsolutePath.value root, "output", path)

let private isClean (result: BuildResult) =
    result.Diagnostics
    |> List.forall (fun diagnostic -> diagnostic.Severity <> Severity.Error)

let private paging =
    testList (
        "paging",
        [
            test (
                "a list is split into pages of the size asked for",
                fun _ ->
                    let pages = Paging.split 10 [ 1..23 ]

                    assertThat
                        (pages |> List.map (fun page -> page.Number, page.Items.Length, page.Offset))
                        (tag "three pages, the last one short"
                         >> isEqualTo
                             [
                                 1, 10, 0
                                 2, 10, 10
                                 3, 3, 20
                             ])

                    assertThat
                        (pages |> List.map (fun page -> page.PageCount, page.ItemCount))
                        (tag "each knowing how many pages and items there are"
                         >> isEqualTo
                             [
                                 3, 23
                                 3, 23
                                 3, 23
                             ])

                    assertThat
                        (pages |> List.map (fun page -> page.Previous, page.Next))
                        (tag "and which pages are next to it"
                         >> isEqualTo
                             [
                                 None, Some 2
                                 Some 1, Some 3
                                 Some 2, None
                             ])
            )

            test (
                "an empty list is one empty page",
                fun _ ->
                    let pages = Paging.split 10 ([]: int list)

                    assertThat
                        (pages |> List.map (fun page -> page.Number, page.PageCount, page.Items))
                        (tag "so an index always has a first page" >> isEqualTo [ 1, 1, [] ])
            )

            test (
                "the first page keeps the route, the others are under page/",
                fun _ ->
                    let first = Route.create (Locale.root "en") [ "blog" ]

                    assertThat (string (Paging.route first 1)) (tag "page 1" >> isEqualTo "/blog")

                    assertThat
                        (string (Paging.route first 3))
                        (tag "page 3" >> isEqualTo "/blog/page/3")
            )

            test (
                "a short pager lists every page",
                fun _ ->
                    assertThat
                        (Paging.slots 2 4)
                        (tag "no gaps"
                         >> isEqualTo
                             [
                                 PageNumber 1
                                 PageNumber 2
                                 PageNumber 3
                                 PageNumber 4
                             ])
            )

            test (
                "a long pager keeps seven slots wherever the current page is",
                fun _ ->
                    assertThat
                        (Paging.slots 5 10)
                        (tag "gaps on both sides of the middle"
                         >> isEqualTo
                             [
                                 PageNumber 1
                                 Gap
                                 PageNumber 4
                                 PageNumber 5
                                 PageNumber 6
                                 Gap
                                 PageNumber 10
                             ])

                    assertThat
                        ([ 1..10 ] |> List.map (fun current -> (Paging.slots current 10).Length))
                        (tag "from the first page to the last" >> isEqualTo (List.replicate 10 7))
            )
        ]
    )

let private generated =
    testList (
        "generated pages",
        [
            test (
                "a derived page can carry its front matter as a value",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    let index =
                        Collection.generated<IndexFrontMatter> "docs-index"
                        |> Collection.derive
                            "docs-index"
                            [ "docs" ]
                            (fun context ->
                                let titles = context.Pages |> List.map _.Title |> List.sort

                                [
                                    for paged in Paging.split 2 titles ->
                                        GeneratedContent.ofValue
                                            $"%i{paged.Number}.md"
                                            {
                                                Paged = paged
                                            }
                                            ""
                                ]
                            )
                        |> Collection.route (fun info ->
                            Paging.route
                                (Route.create info.Locale [ "list" ])
                                info.FrontMatter.Paged.Number
                        )
                        |> Collection.layout (fun context ->
                            Html.ul
                                [
                                    for title in context.FrontMatter.Paged.Items ->
                                        Html.li [ prop.text title ]
                                ]
                        )

                    let result = Build.run root (Fixture.site |> Site.collection index)

                    assertThat (isClean result) (tag "the build is clean" >> isTrue)

                    assertThat
                        ((File.ReadAllText(outputOf root "list/index.html")).Contains
                            "<li>Advanced</li><li>Getting started</li>")
                        (tag "the first page lists the first two" >> isTrue)

                    assertThat
                        ((File.ReadAllText(outputOf root "list/page/2/index.html")).Contains
                            "<li>Home</li>")
                        (tag "and the second the rest" >> isTrue)
            )

            test (
                "a value of the wrong type fails the build",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    let index =
                        Collection.generated<IndexFrontMatter> "docs-index"
                        |> Collection.producer
                            "docs-index"
                            (fun _ -> [ GeneratedContent.ofValue "index.md" "not an index" "" ])
                        |> Collection.layout (fun _ -> Html.none)

                    let result = Build.run root (Fixture.site |> Site.collection index)

                    assertThat
                        (result.Diagnostics
                         |> List.exists (fun diagnostic ->
                             diagnostic.Code = "nacara/front-matter-invalid"
                             && diagnostic.Message.Contains "'String'"
                         ))
                        (tag "the type it was given is named" >> isTrue)
            )

            test (
                "a generated collection given text front matter fails the build",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    let index =
                        Collection.generated<IndexFrontMatter> "docs-index"
                        |> Collection.producer
                            "docs-index"
                            (fun _ -> [ GeneratedContent.create "index.md" "---\ntitle: X\n---\n" ])
                        |> Collection.layout (fun _ -> Html.none)

                    let result = Build.run root (Fixture.site |> Site.collection index)

                    assertThat
                        (result.Diagnostics
                         |> List.exists (fun diagnostic ->
                             diagnostic.Code = "nacara/front-matter-invalid"
                             && diagnostic.Message.Contains "GeneratedContent.ofValue"
                         ))
                        (tag "and says how to generate its pages" >> isTrue)
            )
        ]
    )

let private collections =
    testList (
        "collections",
        [
            test (
                "pagesOf gives the pages of a collection with their front matter typed",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()
                    let result = Build.run root Fixture.site

                    assertThat
                        (Collection.pagesOf Fixture.docs result.Pages
                         |> List.map _.FrontMatter.Title
                         |> List.sort)
                        (tag "every page of the docs"
                         >> isEqualTo
                             [
                                 "Advanced"
                                 "Getting started"
                                 "Home"
                             ])
            )

            test (
                "a page is rendered with the layout its front matter names",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    File.WriteAllText(
                        Path.Combine(AbsolutePath.value root, "docs/loud.md"),
                        "---\ntitle: Loud\nlayout: shout\n---\n"
                    )

                    let docs =
                        Collection.create "docs" pageDecoder
                        |> Collection.source "docs" [ "**/*.md" ]
                        |> Collection.layouts
                            _.Layout
                            [
                                "plain", (fun context -> Html.p context.FrontMatter.Title)
                                "shout",
                                (fun context ->
                                    Html.h1 (context.FrontMatter.Title.ToUpperInvariant())
                                )
                            ]

                    let site =
                        Site.create "Layouts"
                        |> Site.output "output"
                        |> Site.plugin (Nacara.Plugins.Markdown.create ())
                        |> Site.collection docs

                    let result = Build.run root site

                    assertThat (isClean result) (tag "the build is clean" >> isTrue)

                    assertThat
                        ((File.ReadAllText(outputOf root "loud/index.html")).Contains
                            "<h1>LOUD</h1>")
                        (tag "the page asking for one gets it" >> isTrue)

                    assertThat
                        ((File.ReadAllText(outputOf root "index.html")).Contains "<p>Home</p>")
                        (tag "and the others the default" >> isTrue)
            )

            test (
                "a page naming a layout the collection does not have fails the build",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    File.WriteAllText(
                        Path.Combine(AbsolutePath.value root, "docs/odd.md"),
                        "---\ntitle: Odd\nlayout: whisper\n---\n"
                    )

                    let docs =
                        Collection.create "docs" pageDecoder
                        |> Collection.source "docs" [ "**/*.md" ]
                        |> Collection.layouts _.Layout [ "plain", (fun _ -> Html.none) ]

                    let result =
                        Build.run
                            root
                            (Site.create "Layouts"
                             |> Site.output "output"
                             |> Site.plugin (Nacara.Plugins.Markdown.create ())
                             |> Site.collection docs)

                    assertThat
                        (result.Diagnostics
                         |> List.exists (fun diagnostic ->
                             diagnostic.Code = "nacara/front-matter-invalid"
                             && diagnostic.Message.Contains "'whisper'"
                             && diagnostic.Message.Contains "'plain'"
                         ))
                        (tag "the layouts it has are listed" >> isTrue)
            )

            test (
                "a collection reads content/<name>, and a site writes to output, unless told otherwise",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()
                    let notes = Path.Combine(AbsolutePath.value root, "content/notes")
                    Directory.CreateDirectory notes |> ignore
                    File.WriteAllText(Path.Combine(notes, "first.md"), "---\ntitle: First\n---\n")

                    let collection =
                        Collection.create "notes" Fixture.decoder
                        |> Collection.layout (fun context -> Html.p context.FrontMatter.Title)

                    let result =
                        Build.run
                            root
                            (Site.create "Defaults"
                             |> Site.plugin (Nacara.Plugins.Markdown.create ())
                             |> Site.collection collection)

                    assertThat (isClean result) (tag "the build is clean" >> isTrue)

                    assertThat
                        (File.Exists(outputOf root "first/index.html"))
                        (tag "the page is read and written" >> isTrue)
            )

            test (
                "a 404 page at the root of a collection is written as 404.html",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    File.WriteAllText(
                        Path.Combine(AbsolutePath.value root, "docs/404.md"),
                        "---\ntitle: Not found\n---\n"
                    )

                    File.WriteAllText(
                        Path.Combine(AbsolutePath.value root, "docs/guide/404.md"),
                        "---\ntitle: Guide 404\n---\n"
                    )

                    let result = Build.run root Fixture.site

                    assertThat (isClean result) (tag "the build is clean" >> isTrue)

                    assertThat
                        (File.Exists(outputOf root "404.html"))
                        (tag "where static hosts look for it" >> isTrue)

                    assertThat
                        (File.Exists(outputOf root "guide/404/index.html"))
                        (tag "while one deeper is an ordinary page" >> isTrue)
            )
        ]
    )

let private siteHelpers =
    testList (
        "site helpers",
        [
            test (
                "items are grouped by the terms they are filed under",
                fun _ ->
                    let terms =
                        Taxonomy.group
                            snd
                            [
                                "a",
                                [
                                    "Web"
                                    "F#"
                                ]
                                "b", [ "web" ]
                                "c", []
                            ]

                    assertThat
                        (terms
                         |> List.map (fun term -> term.Name, term.Slug, List.map fst term.Items))
                        (tag "ordered by name, one term per slug, named as it first appears"
                         >> isEqualTo
                             [
                                 "F#", "fsharp", [ "a" ]
                                 "Web",
                                 "web",
                                 [
                                     "a"
                                     "b"
                                 ]
                             ])
            )

            test (
                "a link names a page by its source, or a path in the site",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()
                    let result = Build.run root Fixture.site
                    let site = Site.toInfo (Fixture.site |> Site.baseUrl "/docs/")
                    let link = PageIndex.link site result.Pages

                    assertThat
                        (link "guide/advanced.md")
                        (tag "a source path" >> isEqualTo "/docs/guide/advanced/")

                    assertThat
                        (link "guide/advanced")
                        (tag "without its extension" >> isEqualTo "/docs/guide/advanced/")

                    assertThat
                        (link "/blog/")
                        (tag "a path keeps its trailing slash" >> isEqualTo "/docs/blog/")

                    assertThat
                        (link "missing.md")
                        (tag "and what names nothing is left for the link validator"
                         >> isEqualTo "missing.md")
            )

            test (
                "a layout links to pages and lists another collection, typed",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    let home =
                        Collection.generated<string> "home"
                        |> Collection.producer
                            "home"
                            (fun _ -> [ GeneratedContent.ofValue "index" "Home" "" ])
                        |> Collection.route (fun info -> Route.create info.Locale [ "home" ])
                        |> Collection.layout (fun context ->
                            Html.div
                                [
                                    yield Html.a [ prop.href (context.LinkTo "guide/advanced.md") ]
                                    for page in context.PagesOf Fixture.docs do
                                        yield Html.p page.FrontMatter.Title
                                ]
                        )

                    Build.run root (Fixture.site |> Site.collection home) |> ignore
                    let html = File.ReadAllText(outputOf root "home/index.html")

                    assertThat
                        (html.Contains "href=\"/guide/advanced/\"")
                        (tag "the link is the page's url" >> isTrue)

                    assertThat
                        (html.Contains "<p>Home</p><p>Advanced</p><p>Getting started</p>")
                        (tag "and the pages come in route order" >> isTrue)
            )

            test (
                "a collection is made from the pages of another",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    let archive =
                        Collection.fromPages
                            "archive"
                            Fixture.docs
                            (fun pages ->
                                [
                                    {
                                        Titles = pages |> List.map _.FrontMatter.Title |> List.sort
                                    }
                                ]
                            )
                        |> Collection.route (fun info -> Route.create info.Locale [ "archive" ])
                        |> Collection.layout (fun context ->
                            Html.p (String.concat ", " context.FrontMatter.Titles)
                        )

                    let result = Build.run root (Fixture.site |> Site.collection archive)

                    assertThat (isClean result) (tag "the build is clean" >> isTrue)

                    assertThat
                        ((File.ReadAllText(outputOf root "archive/index.html")).Contains
                            "Advanced, Getting started, Home")
                        (tag "given every page of the source" >> isTrue)
            )

            test (
                "a draft is published while serving, and left out of a build",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    File.WriteAllText(
                        Path.Combine(AbsolutePath.value root, "docs/soon.md"),
                        "---\ntitle: Soon\ndraft: true\n---\n"
                    )

                    let docs =
                        Collection.create "docs" draftDecoder
                        |> Collection.source "docs" [ "**/*.md" ]
                        |> Collection.title _.Title
                        |> Collection.draft _.Draft
                        |> Collection.layout (fun context -> Html.p context.FrontMatter.Title)

                    let archive =
                        Collection.fromPages
                            "archive"
                            docs
                            (fun pages ->
                                [
                                    {
                                        Titles = pages |> List.map _.FrontMatter.Title
                                    }
                                ]
                            )
                        |> Collection.route (fun info -> Route.create info.Locale [ "archive" ])
                        |> Collection.layout (fun context ->
                            Html.p (String.concat ", " (List.sort context.FrontMatter.Titles))
                        )

                    let site =
                        Site.create "Drafts"
                        |> Site.output "output"
                        |> Site.plugin (Nacara.Plugins.Markdown.create ())
                        |> Site.collection docs
                        |> Site.collection archive

                    Build.run root site |> ignore

                    assertThat
                        (File.Exists(outputOf root "soon/index.html"))
                        (tag "a build does not publish it" >> isFalse)

                    assertThat
                        ((File.ReadAllText(outputOf root "archive/index.html")).Contains "Soon")
                        (tag "nor list it" >> isFalse)

                    Build.runWatch (BuildCache()) root site |> ignore

                    assertThat
                        (File.Exists(outputOf root "soon/index.html"))
                        (tag "serving does" >> isTrue)
            )

            test (
                "a site without a theme gets the default tokens unless it brings its own",
                fun _ ->
                    let tokensWritten (site: Site) =
                        let root = Fixture.copyToTemporaryDirectory ()
                        Build.run root site |> ignore

                        Directory.EnumerateFiles(outputOf root "assets")
                        |> Seq.exists (fun file ->
                            (Path.GetFileName file).StartsWith "nacara-core-tokens."
                        )

                    assertThat
                        (tokensWritten (Fixture.site |> Nacara.Theme.Core.ThemeCore.register))
                        (tag "by default" >> isTrue)

                    assertThat
                        (tokensWritten (
                            Fixture.site
                            |> Nacara.Theme.Core.ThemeCore.registerWith (
                                Nacara.Theme.Core.ThemeCore.defaultTokens false
                            )
                        ))
                        (tag "and not when it says so" >> isFalse)
            )
        ]
    )

let all =
    testList (
        "Collections",
        [
            paging
            generated
            collections
            siteHelpers
        ]
    )
