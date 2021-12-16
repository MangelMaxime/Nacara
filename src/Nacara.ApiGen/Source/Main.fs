module Nacara.ApiGen.Main

open FSharp.Formatting.ApiDocs
open System.IO
open Nacara.ApiGen.Generate

open Argu
open Markdown
open Giraffe.ViewEngine

type CliArguments =
    | [<AltCommandLine("-p"); Mandatory>] Project of string
    | [<AltCommandLine("-lib"); Mandatory>] Lib_Directory of string
    | [<AltCommandLine("-o"); Mandatory>] Output of string
    | Base_Url of string

    interface IArgParserTemplate with

        member this.Usage =
            match this with
            | Project _ ->
                "Your project name"

            | Lib_Directory _ ->
                "The source directory where your dll and xml file are located"

            | Output _ ->
                "The output directory where the generated files will be written"

            | Base_Url _ ->
                "Base URL for your site. This is the path after the domain."

let parser = ArgumentParser.Create<CliArguments>()

let addFrontMatter (paragraphs : MarkdownParagraphs) =
    [
        YamlFrontmatter [
            "layout: api"
        ]
        yield! paragraphs
    ]

// Function to gain syntax highlighting for CSS thanks to VSCode extension
// https://marketplace.visualstudio.com/items?itemName=alfonsogarciacaro.vscode-template-fsharp-highlight
let inline css text = text

let private basicCss =
    css """
:root {
    --keyword-color: #a626a4;
}

.api-code {
    font-family: monospace;
    margin-bottom: 1rem;
    scroll-margin-top: 5.25rem;
}

.api-code pre {
    background-color: transparent;
}

.api-code .line {
    white-space: nowrap;
}

.api-code .keyword {
    color: var(--keyword-color);
}
"""

let private addBasicCss (paragraphs : MarkdownParagraphs) =
    [
        InlineHtmlBlock (
            style [ ]
                [ str basicCss ]
        )
        yield! paragraphs
    ]


[<EntryPoint>]
let main argv =
    try
        let res = parser.ParseCommandLine(inputs = argv, raiseOnUsage = true)

        match res.TryGetResult Project, res.TryGetResult Lib_Directory, res.TryGetResult Output with
        | Some project, Some libDir, Some output ->
            let cwd = Directory.GetCurrentDirectory()

            let libDir = Path.GetFullPath(libDir, cwd)

            // Compute the output directory path
            let output = Path.GetFullPath(output, cwd)

            // Initialize F# Formatting context
            let dllFile = Path.Combine(libDir, project + ".dll")
            let apiDocInput = ApiDocInput.FromFile(dllFile)

            let baseUrl =
                res.TryGetResult Base_Url
                |> Option.defaultValue "/"

            let apiDocModel =
                ApiDocs.GenerateModel(
                    [ apiDocInput ],
                    project,
                    [],
                    qualify = true,
                    libDirs = [
                        libDir
                    ],
                    root = baseUrl
                )

            // Clean the output folder
            if Directory.Exists (Path.Combine(output, "reference", project)) then
                Directory.Delete(Path.Combine(output, "reference", project), true)

            let apiUrl =
                $"reference/{project}/"

            let generateLink (urlBaseName : string) =
                $"{baseUrl}reference/{project}/{urlBaseName}.html"

            // Render the API index page
            let indexPageDestination =
                Path.Combine(output, "reference", project, "index.md")

            let indexPageDocument =
                Render.renderIndex
                    generateLink
                    apiDocModel.Collection.Namespaces
                |> addBasicCss
                |> addFrontMatter
                |> MarkdownDocument

            File.writeString
                output
                indexPageDestination
                (indexPageDocument.ToMarkdown())

            // generateIndexPage
            //     output
            //     generateLink
            //     apiDocModel.Collection.Namespaces
            //     indexPageDestination

            // // Render all the namespaces pages
            // for ns in apiDocModel.Collection.Namespaces do
            //     generateNamespacePage
            //         output
            //         apiUrl
            //         generateLink
            //         ns

            // // Render all the entities pages
            // for entity in apiDocModel.EntityInfos do
            //     generateEntityPage
            //         output
            //         apiUrl
            //         generateLink
            //         entity

            for entity in apiDocModel.EntityInfos do
                let document =
                    Render.renderEntity
                        generateLink
                        entity
                    |> addBasicCss
                    |> addFrontMatter
                    |> MarkdownDocument

                File.writeString
                    output
                    (apiUrl + entity.Entity.UrlBaseName + ".md")
                    (document.ToMarkdown())

            for ns in apiDocModel.Collection.Namespaces do
                let document =
                    Render.renderNamespace
                        generateLink
                        ns
                    |> addBasicCss
                    |> addFrontMatter
                    |> MarkdownDocument

                File.writeString
                    output
                    (apiUrl + ns.UrlBaseName + ".md")
                    (document.ToMarkdown())

            0

        // Invalid CLI
        | _ ->
            parser.PrintUsage() |> printfn "%s"
            1

    with e ->
        printfn "%s" e.Message
        0
