module Nacara.ApiGen.Main

open FSharp.Formatting.ApiDocs
open System.IO

open Argu
open Markdown
open Giraffe.ViewEngine
open Nacara.ApiGen.Render

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
.api-code {
    font-family: monospace;
    margin-bottom: 1rem;
}

/* Animate anchor when targetted
This make it easier to spot the anchor
when jumping to it */
@keyframes blink {
    0% {
        background-color: var(--nacara-api-blink-background-color, yellow);
        color: var(--nacara-api-blink-active-color, black);
    }
    100% {
        background-color: transparent;
        color: var(--nacara-api-blink-color, black);
    }
}
.api-code .property[id]:target,
.api-code a[id]:target {
    animation-name: blink;
    animation-direction: normal;
    animation-duration: 0.75s;
    animation-iteration-count: 2;
    animation-timing-function: ease;
    /* Make the background a bit bigger than the actual text */
    margin: -0.25rem;
    padding: 0.25rem;
}

/* Anchor position */
.api-code .property[id],
.api-code a[id] {
    scroll-margin-top: var(--nacara-api-scroll-margin-top);
}

/* .api-code pre {
    background-color: transparent;
}

.api-code .line {
    white-space: nowrap;
} */

/* Synthax highlighting */
.api-code .keyword {
    color: var(--nacara-api-keyword-color, #a626a4);
}

.api-code .property,
.api-code .property:hover {
    color: var(--nacara-api-property-color, #6669d7);
}

.api-code .type,
.api-code .type:hover {
    color: var(--nacara-api-type-color, #c18401);
}

/* Hover instruction */
.api-code .property:hover,
.api-code .type:hover {
    text-decoration: underline;
    cursor: pointer;
}

/*
    Documentation formatting
*/

.api-doc-summary {
    margin-top: 1rem;
    margin-bottom: 1rem;
}

dl.api-doc-record-fields {
    margin-left: 1rem;
}

dl.api-doc-record-fields dt:not(:first-child) {
    padding-top: 1rem;
    border-top: var(--nacara-api-separator-width, 2px) solid var(--nacara-api-separator-color, black);
}

dl.api-doc-record-fields dd {
    margin-top: 1rem;
    margin-bottom: 1rem;
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

let test =
    // The debugger actually stop here and not at `let i = 0`
    [
        "Fable"

        if true then
            let i = 0 // Putting the stop here

            "Maxime"
        else
            ()

        "Elm"
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
                Index.page
                    generateLink
                    apiDocModel.Collection.Namespaces
                |> addBasicCss
                |> addFrontMatter
                |> MarkdownDocument

            File.writeString
                output
                indexPageDestination
                (indexPageDocument.ToMarkdown())

            // Render the modules
            for entity in apiDocModel.EntityInfos do
                let document =
                    Entity.page generateLink entity
                    |> addBasicCss
                    |> addFrontMatter
                    |> MarkdownDocument

                File.writeString
                    output
                    (apiUrl + entity.Entity.UrlBaseName + ".md")
                    (document.ToMarkdown())

            // Render the namespaces
            for ns in apiDocModel.Collection.Namespaces do
                let document =
                    Namespace.page
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
