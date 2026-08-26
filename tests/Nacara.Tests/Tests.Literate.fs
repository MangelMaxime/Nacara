module Nacara.Tests.Literate

open Scriptorium.Nib.Assertion
open type Scriptorium.Quill.Test
open System.IO
open Nacara.Core
open Nacara.Plugins
open Nacara.Tests

let private source =
    """(**
---
title: Getting started
---
*)

(** A literate file is F# that compiles. *)

let answer = 42

(*** hide ***)
let secret = "not for readers"

(** ## Using it *)

(*** title="Program.fs" ***)
printfn "%i" answer
"""

let private blocks = Literate.parse source

let all =
    testList (
        "Literate",
        [
            test (
                "an example that does not compile fails the build",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    File.WriteAllText(
                        Path.Combine(AbsolutePath.value root, "docs/broken.fsx"),
                        "(**\n---\ntitle: Broken\n---\n*)\n\nlet answer : int = \"forty two\"\n"
                    )

                    let site =
                        Fixture.site
                        |> Site.plugin (Literate.create ())
                        |> Site.collection (Fixture.docs |> Collection.source "docs" [ "**/*.fsx" ])

                    let result = Build.run root site

                    let reported =
                        result.Diagnostics
                        |> List.filter (fun item -> item.Code = "literate/does-not-compile")

                    assertThat
                        (List.length reported)
                        (tag "one message, from the compiler" >> isEqualTo 1)

                    let diagnostic = List.head reported

                    assertThat
                        (diagnostic.Message.Contains "FS0001")
                        (tag "with the compiler's own code" >> isTrue)

                    assertThat
                        (diagnostic.Span |> Option.map _.Line)
                        (tag "at the line of the file, not of some intermediate"
                         >> isEqualTo (Some 7))
            )

            test (
                "front matter is read from the comment the file opens with",
                fun _ ->
                    match
                        FrontMatter.extract (Literate.frontMatterFormat Literate.defaults) source
                    with
                    | Error message ->
                        assertThat
                            false
                            (tag $"the front matter should be found: %s{message}" >> isTrue)
                    | Ok block ->
                        assertThat
                            (block.Yaml.Contains "title: Getting started")
                            (tag "the front matter" >> isTrue)

                        assertThat
                            (block.Body.Contains "let answer = 42")
                            (tag "and the code below it" >> isTrue)
            )

            test (
                "a decode error points at the real line of the file",
                fun _ ->
                    let broken = "(**\n---\norder: 1\n---\n*)\n\nlet x = 1\n"

                    match
                        FrontMatter.extract (Literate.frontMatterFormat Literate.defaults) broken
                    with
                    | Error message ->
                        assertThat false (tag $"the block should parse: %s{message}" >> isTrue)
                    | Ok block ->
                        let decoder =
                            Decode.object (fun get -> get.Required.Field "title" Decode.string)

                        match Yaml.decodeWithOffset block.LineOffset decoder block.Yaml with
                        | Ok _ -> assertThat false (tag "the missing field should fail" >> isTrue)
                        | Error error ->
                            assertThat
                                error.Line
                                (tag "the reported line is the line in the file" >> isEqualTo 3)
            )

            test (
                "a fenced block inside prose keeps the shape of its code",
                fun _ ->
                    let source =
                        "(**\nHow it looks:\n\n```fsharp\nlet greet name =\n    if name = \"\" then\n        \"anyone\"\n```\n*)\n"

                    let markdown = Literate.parse source |> Literate.toMarkdown Literate.defaults

                    assertThat
                        (markdown.Contains "    if name")
                        (tag "the indented line is still indented" >> isTrue)

                    assertThat
                        (markdown.Contains "        \"anyone\"")
                        (tag "and so is the one inside it" >> isTrue)

                    assertThat
                        (markdown.Contains "How it looks:")
                        (tag "while the prose stays flush with the margin" >> isTrue)
            )

            test (
                "prose and code alternate",
                fun _ ->
                    let kinds =
                        blocks
                        |> List.map (
                            function
                            | Prose _ -> "prose"
                            | Code _ -> "code"
                            | Hidden _ -> "hidden"
                        )

                    assertThat
                        kinds
                        (tag "the front matter comment is prose too, then the file alternates"
                         >> isEqualTo
                             [
                                 "prose"
                                 "prose"
                                 "code"
                                 "hidden"
                                 "prose"
                                 "code"
                             ])
            )

            test (
                "prose can contain the syntax it is describing",
                fun _ ->
                    let nested =
                        "(**\nHow to open a file:\n\n```fsharp\n(**\n---\ntitle: X\n---\n*)\n```\n\nThat is all.\n*)\n\nlet x = 1\n"

                    match Literate.parse nested with
                    | [ Prose prose; Code(code, _) ] ->
                        assertThat
                            (prose.Contains "title: X")
                            (tag "the inner comment stays inside the prose" >> isTrue)

                        assertThat
                            (prose.Contains "That is all.")
                            (tag "and the prose continues past it" >> isTrue)

                        assertThat
                            code
                            (tag "the code after the block is code" >> isEqualTo "let x = 1")
                    | blocks ->
                        assertThat
                            false
                            (tag
                                $"expected one prose block and one code block, got %i{List.length blocks}"
                             >> isTrue)
            )

            test (
                "hidden code is parsed but not published",
                fun _ ->
                    let markdown = Literate.toMarkdown Literate.defaults blocks

                    assertThat
                        (markdown.Contains "not for readers")
                        (tag "hidden code stays hidden" >> isFalse)

                    assertThat
                        (markdown.Contains "let answer = 42")
                        (tag "the rest is shown" >> isTrue)
            )

            test (
                "a command becomes the fence meta of the block it precedes",
                fun _ ->
                    let markdown = Literate.toMarkdown Literate.defaults blocks

                    assertThat
                        (markdown.Contains "```fsharp title=\"Program.fs\"")
                        (tag "so a literate file can title its code blocks" >> isTrue)
            )

            test (
                "prose is emitted as markdown, not as code",
                fun _ ->
                    let markdown = Literate.toMarkdown Literate.defaults blocks

                    assertThat
                        (markdown.Contains "## Using it")
                        (tag "a heading stays a heading" >> isTrue)
            )

            test (
                "a literate file becomes a page like any other",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    File.WriteAllText(
                        Path.Combine(AbsolutePath.value root, "docs/literate.fsx"),
                        source
                    )

                    let site =
                        Fixture.site
                        |> Site.plugin (Literate.create ())
                        |> Site.collection (Fixture.docs |> Collection.source "docs" [ "**/*.fsx" ])

                    let result = Build.run root site

                    let page =
                        result.Pages |> List.tryFind (fun page -> page.Id.EndsWith "literate.fsx")

                    match page with
                    | None -> assertThat false (tag "the literate file produced a page" >> isTrue)
                    | Some page ->
                        assertThat
                            (page.Html.Contains "<h2 id=\"using-it\">")
                            (tag "its prose went through the markdown pipeline" >> isTrue)

                        assertThat
                            (page.Html.Contains "<pre><code class=\"language-fsharp\"")
                            (tag "and its code blocks through the code block renderer" >> isTrue)

                        assertThat
                            (page.Headings |> List.exists (fun heading -> heading.Text = "Using it"))
                            (tag "so it has a table of contents too" >> isTrue)
            )
        ]
    )
