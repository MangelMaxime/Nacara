module Nacara.Tests.Collections

open Scriptorium.Nib.Assertion
open type Scriptorium.Quill.Test
open System.IO
open Feliz.ViewEngine
open Nacara.Core
open Nacara.Tests

let private outputOf (root: AbsolutePath) (path: string) =
    Path.Combine(AbsolutePath.value root, "output", path)

let private isClean (result: BuildResult) =
    result.Diagnostics
    |> List.forall (fun diagnostic -> diagnostic.Severity <> Severity.Error)

let private collections =
    testList (
        "collections",
        [
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

let all = testList ("Collections", [ collections ])
