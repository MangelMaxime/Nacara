module Nacara.Tests.Components

open Scriptorium.Nib.Assertion
open type Scriptorium.Quill.Test
open System
open System.IO
open System.Net.Http
open Feliz.ViewEngine
open Nacara.Core
open Nacara.Plugins
open Nacara.Tests

let private write (root: AbsolutePath) (path: string) (text: string) =
    let file = Path.Combine(AbsolutePath.value root, path)
    Directory.CreateDirectory(Path.GetDirectoryName file) |> ignore
    File.WriteAllText(file, text)

let private output (root: AbsolutePath) (path: string) =
    File.ReadAllText(Path.Combine(AbsolutePath.value root, "output", path))

let private youtube (directive: DirectiveContext) =
    Html.figure
        [
            prop.className (directive.Attribute "class" |> Option.defaultValue "")
            prop.custom ("data-start", directive.Attribute "start" |> Option.defaultValue "0")
            prop.children
                [
                    Html.iframe
                        [ prop.src $"https://example.com/embed/%s{directive.Argument.Value}" ]
                    Html.figcaption [ directive.Body ]
                ]
        ]

let private directives =
    testList (
        "directives",
        [
            test (
                "a directive of the site renders what it holds",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    write
                        root
                        "docs/video.md"
                        "---\ntitle: Video\n---\n\n:::youtube abc123 {.wide start=30}\nThe **launch** talk.\n:::\n"

                    let result =
                        Build.run root (Fixture.site |> Markdown.directive "youtube" youtube)

                    assertThat
                        (result.Diagnostics
                         |> List.forall (fun item -> item.Severity <> Severity.Error))
                        (tag "the build is clean" >> isTrue)

                    let html = output root "video/index.html"

                    assertThat
                        (html.Contains "src=\"https://example.com/embed/abc123\"")
                        (tag "the argument is given" >> isTrue)

                    assertThat
                        (html.Contains "class=\"wide\"" && html.Contains "data-start=\"30\"")
                        (tag "and the attributes" >> isTrue)

                    assertThat
                        (html.Contains "<figcaption><p>The <strong>launch</strong> talk.</p>")
                        (tag "and what it holds, rendered" >> isTrue)
            )

            test (
                "a directive of the site replaces a built-in one of the same name",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()
                    write root "docs/note.md" "---\ntitle: Note\n---\n\n:::note\nCareful.\n:::\n"

                    Build.run
                        root
                        (Fixture.site
                         |> Markdown.directive
                             "note"
                             (fun directive ->
                                 Html.div
                                     [
                                         prop.className "my-note"
                                         prop.children [ directive.Body ]
                                     ]
                             ))
                    |> ignore

                    let html = output root "note/index.html"

                    assertThat
                        (html.Contains "<div class=\"my-note\">")
                        (tag "the site's is used" >> isTrue)

                    assertThat
                        (html.Contains "nacara-callout")
                        (tag "not the built-in one" >> isFalse)
            )

            test (
                "a directive that throws fails the build at its line",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    write
                        root
                        "docs/broken.md"
                        "---\ntitle: Broken\n---\n\n:::youtube\nNo id.\n:::\n"

                    let result =
                        Build.run root (Fixture.site |> Markdown.directive "youtube" youtube)

                    let failure =
                        result.Diagnostics
                        |> List.tryFind (fun item -> item.Code = "markdown/directive-failed")

                    assertThat failure.IsSome (tag "it is reported" >> isTrue)

                    assertThat
                        (failure |> Option.bind _.Span |> Option.map _.Line)
                        (tag "on the line that opens it" >> isEqualTo (Some 5))

                    assertThat
                        (failure.Value.Severity)
                        (tag "as an error" >> isEqualTo Severity.Error)
            )

            test (
                "an unknown directive names the ones the site adds",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()
                    write root "docs/odd.md" "---\ntitle: Odd\n---\n\n:::vimeo\nX\n:::\n"

                    let result =
                        Build.run root (Fixture.site |> Markdown.directive "youtube" youtube)

                    assertThat
                        (result.Diagnostics
                         |> List.exists (fun item ->
                             item.Code = "markdown/unknown-directive"
                             && (item.Hint |> Option.defaultValue "").Contains
                                 "this site adds youtube"
                         ))
                        (tag "in the hint" >> isTrue)
            )
        ]
    )

let private inlineMarkdown =
    testList (
        "markdown outside the body",
        [
            test (
                "a layout renders markdown from front matter, its links resolved",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    let card =
                        Collection.create "docs" Fixture.decoder
                        |> Collection.source "docs" [ "**/*.md" ]
                        |> Collection.title _.Title
                        |> Collection.layout (fun context ->
                            Html.div
                                [
                                    Html.p
                                        [
                                            Markdown.renderInline
                                                context
                                                "Read *the* [advanced guide](guide/advanced.md#going-further)."
                                        ]
                                    Html.p [ Markdown.renderInline context "Back [home](/)." ]
                                    Markdown.renderBlock context "One\n\nTwo"
                                ]
                        )

                    let site =
                        Site.create "Inline"
                        |> Site.output "output"
                        |> Site.plugin (Markdown.create ())
                        |> Site.collection card

                    Build.run root site |> ignore
                    let html = output root "index.html"

                    assertThat
                        (html.Contains
                            "<p>Read <em>the</em> <a href=\"/guide/advanced/#going-further\">advanced guide</a>.</p>")
                        (tag "a file beside the page, with no paragraph around it" >> isTrue)

                    assertThat
                        (html.Contains "<p>Back <a href=\"/\">home</a>.</p>")
                        (tag "a path in the site" >> isTrue)

                    assertThat
                        (html.Contains "<p>One</p>" && html.Contains "<p>Two</p>")
                        (tag "and a block keeps its paragraphs" >> isTrue)
            )
        ]
    )

let private failures =
    testList (
        "failures",
        [
            test (
                "a layout that throws fails the build on its page, and the others are written",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    let docs =
                        Fixture.docs
                        |> Collection.layout (fun context ->
                            if context.FrontMatter.Title = "Advanced" then
                                failwith "no sidebar for you"

                            Fixture.layout context
                        )

                    let result =
                        Build.run
                            root
                            (Site.create "Failing"
                             |> Site.output "output"
                             |> Site.plugin (Markdown.create ())
                             |> Site.collection docs)

                    let failure =
                        result.Diagnostics
                        |> List.tryFind (fun item -> item.Code = "nacara/layout-failed")

                    assertThat failure.IsSome (tag "it is reported" >> isTrue)

                    assertThat
                        (failure.Value.Message.Contains "no sidebar for you")
                        (tag "with what was thrown" >> isTrue)

                    assertThat
                        (failure.Value.Span
                         |> Option.exists (fun span ->
                             (AbsolutePath.value span.File).EndsWith "advanced.md"
                         ))
                        (tag "on the page" >> isTrue)

                    assertThat
                        (File.Exists(Path.Combine(AbsolutePath.value root, "output/index.html")))
                        (tag "and the other pages are written" >> isTrue)
            )

            test (
                "the dev server shows the last build's problems",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()
                    let outputDirectory = AbsolutePath.combine root [ "output" ]
                    Directory.CreateDirectory(AbsolutePath.value outputDirectory) |> ignore
                    write root "output/index.html" "<html><body><p>Hi</p></body></html>"

                    let port = 41000 + Random.Shared.Next 2000
                    use server = new DevServer(outputDirectory, "/", "localhost", port)
                    server.Start()

                    server.ShowProblems(
                        root,
                        [
                            Diagnostic.error
                                "markdown/link-target-missing"
                                "This link points at nothing"
                            |> Diagnostic.at
                                (AbsolutePath.combine
                                    root
                                    [
                                        "docs"
                                        "index.md"
                                    ])
                                3
                                5
                        ]
                    )

                    use client = new HttpClient()

                    let get (path: string) =
                        client.GetStringAsync(server.Url.TrimEnd '/' + path).Result

                    let problems = get "/__nacara/problems"

                    assertThat
                        (problems.Contains "\"code\":\"markdown/link-target-missing\"")
                        (tag "as json" >> isTrue)

                    assertThat
                        (problems.Contains "\"location\":\"docs/index.md(3,5)\"")
                        (tag "located relative to the project" >> isTrue)

                    assertThat
                        ((get "/").Contains "/__nacara/problems")
                        (tag "and every page asks for them" >> isTrue)
            )
        ]
    )

let all =
    testList (
        "Components",
        [
            directives
            inlineMarkdown
            failures
        ]
    )
