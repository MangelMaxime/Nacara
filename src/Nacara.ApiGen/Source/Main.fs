module Nacara.ApiGen.Main

open FSharp.Formatting.ApiDocs
open System.IO
open Nacara.ApiGen.Generate

open Argu

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

            let apiDocModel =
                ApiDocs.GenerateModel(
                    [ apiDocInput ],
                    project,
                    [],
                    qualify = true,
                    libDirs = [
                        libDir
                    ]
                )

            // Load the XML doc file
            let xmlFile = Path.Combine(libDir, project + ".xml")
            let docLocator = new DocLocator(xmlFile)

            // Clean the output folder
            if Directory.Exists (Path.Combine(output, "reference", project)) then
                Directory.Delete(Path.Combine(output, "reference", project), true)

            let baseUrl =
                res.TryGetResult Base_Url
                |> Option.defaultValue "/"

            let apiUrl =
                $"reference/{project}/"

            let generateLink (urlBaseName : string) =
                $"{baseUrl}reference/{project}/{urlBaseName}.html"

            // Render the API index page
            let indexPageDestination =
                Path.Combine(output, "reference", project, "index.md")

            generateIndexPage
                output
                docLocator
                generateLink
                apiDocModel.Collection.Namespaces
                indexPageDestination

            // Render all the namespaces pages
            for ns in apiDocModel.Collection.Namespaces do
                generateNamespacePage
                    output
                    apiUrl
                    docLocator
                    generateLink
                    ns

            // Render all the entities pages
            for entity in apiDocModel.EntityInfos do
                generateEntityPage
                    output
                    apiUrl
                    docLocator
                    generateLink
                    entity

            0

        // Invalid CLI
        | _ ->
            parser.PrintUsage() |> printfn "%s"
            1

    with e ->
        printfn "%s" e.Message
        0
