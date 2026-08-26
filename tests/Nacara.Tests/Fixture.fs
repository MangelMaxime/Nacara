module Nacara.Tests.Fixture

open System.IO
open Feliz.ViewEngine
open Nacara.Core
open Nacara.Plugins

/// <summary>Front matter of the fixture's documentation pages.</summary>
type DocFrontMatter =
    {
        Title: string
        Description: string option
        Order: int option
        Toc: TocRange option
    }

let decoder: Decoder<DocFrontMatter> =
    Decode.object (fun get ->
        {
            Title = get.Required.Field "title" Decode.string
            Description = get.Optional.Field "description" Decode.string
            Order = get.Optional.Field "order" Decode.int
            Toc =
                get.Optional.Field
                    "toc"
                    (Decode.object (fun toc ->
                        {
                            From = toc.Optional.Field "from" Decode.int |> Option.defaultValue 2
                            To = toc.Optional.Field "to" Decode.int |> Option.defaultValue 6
                        }
                    ))
        }
    )

/// <summary>A layout small enough to read in a snapshot, real enough to exercise the pipeline.</summary>
let layout (context: PageContext<DocFrontMatter>) =
    Html.html
        [
            prop.lang context.Page.Locale.Code
            prop.children
                [
                    Html.head
                        [
                            Html.meta [ prop.charset.utf8 ]
                            Html.title $"{context.FrontMatter.Title} · {context.Site.Title}"
                            match context.FrontMatter.Description with
                            | Some description ->
                                Html.meta
                                    [
                                        prop.name "description"
                                        prop.content description
                                    ]
                            | None -> Html.none
                        ]
                    Html.body
                        [
                            Html.nav
                                [
                                    Html.ul
                                        [
                                            for page in context.PagesOf "docs" do
                                                Html.li
                                                    [
                                                        Html.a
                                                            [
                                                                prop.href (
                                                                    context.Site.UrlOf page.Route
                                                                )
                                                                prop.text page.Title
                                                            ]
                                                    ]
                                        ]
                                ]
                            Html.main
                                [
                                    Html.h1 context.FrontMatter.Title
                                    Html.div
                                        [
                                            prop.className "content"
                                            prop.dangerouslySetInnerHTML context.Content
                                        ]
                                ]
                            Html.aside
                                [
                                    prop.className "toc"
                                    prop.children
                                        [
                                            for heading in context.Page.Headings do
                                                Html.a
                                                    [
                                                        prop.href $"#{heading.Anchor}"
                                                        prop.text heading.Text
                                                    ]
                                        ]
                                ]
                        ]
                ]
        ]

let docs =
    Collection.create "docs" decoder
    |> Collection.source "docs" [ "**/*.md" ]
    |> Collection.title _.Title
    |> Collection.toc _.Toc
    |> Collection.layout layout

let site =
    Site.create "Fixture"
    |> Site.description "A site that exists to be snapshotted"
    |> Site.baseUrl "/"
    |> Site.output "output"
    |> Site.staticFiles "static"
    |> Site.plugin (Markdown.create ())
    |> Site.plugin (TextMate.create ())
    |> Site.collection docs

/// <summary>Root of the fixture site checked into the repository.</summary>
let root =
    Path.Combine(__SOURCE_DIRECTORY__, "..", "fixture")
    |> Path.GetFullPath
    |> AbsolutePath.create

/// <summary>A throwaway copy of the fixture, so tests can add and remove files freely.</summary>
let copyToTemporaryDirectory () =
    let target =
        Path.Combine(Path.GetTempPath(), "nacara-tests", System.Guid.NewGuid().ToString "N")

    for source in
        Directory.EnumerateFiles(AbsolutePath.value root, "*", SearchOption.AllDirectories) do
        let relative = Path.GetRelativePath(AbsolutePath.value root, source)
        let destination = Path.Combine(target, relative)
        Directory.CreateDirectory(Path.GetDirectoryName destination) |> ignore
        File.Copy(source, destination)

    AbsolutePath.create target
