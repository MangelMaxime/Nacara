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

let all = testList ("Components", [ failures ])
