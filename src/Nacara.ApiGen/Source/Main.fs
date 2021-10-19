module Nacara.ApiGen.Main

open FSharp.Formatting.ApiDocs
open System.IO
open StringBuilder.Extensions
open FSharp.Compiler.Symbols
open System.Text
open System.Text.RegularExpressions
open Render

// let libDir = "/home/maximemangel/Documents/Workspaces/Github/MangelMaxime/Nacara/src/Nacara.Core/bin/Debug/netstandard2.0/publish/"
// let file = Path.Combine(libDir, "Nacara.Core.dll")
// let input = ApiDocInput.FromFile(file)
// let xmlDocFile = "/home/maximemangel/Documents/Workspaces/Github/MangelMaxime/Nacara/src/Nacara.Core/bin/Debug/netstandard2.0/publish/Nacara.Core.xml"
// let docsRoot = "/home/maximemangel/Documents/Workspaces/Github/MangelMaxime/Nacara/docs"

// let root = "/Nacara/"

// let apiUrl = "/reference/Nacara.Core/"

// let xmlDocContent =
//     use reader = new StreamReader(xmlDocFile)

//     reader.ReadToEnd()

let minimalApiFileContent =
    """---
layout: api
---

"""

// let apiDocModel =
//     ApiDocs.GenerateModel(
//         [ input ],
//         "Nacara.Core",
//         [],
//         qualify = true,
//         libDirs = [
//             libDir
//         ]
//     )

// let docLocator = new DocLocator(xmlDocFile)

// let renderRecord (sb : StringBuilder) (info : ApiDocEntityInfo) =
//     let entity = info.Entity

//     sb.WriteLine $"""<div class="api-code">"""
//     sb.WriteLine $"""<div><span class="keyword">type</span>&nbsp;<span class="type">%s{entity.Name}</span>&nbsp;<span class="keyword">=</span></div>"""
//     sb.Indent 1
//     sb.WriteLine """<span class="keyword">{</span>"""

//     for field in entity.RecordFields do
//         match field.ReturnInfo.ReturnType with
//         | Some returnType ->
//             let escapedReturnType =
//                 // Remove the starting <code> and ending </code>
//                 returnType.HtmlText.[6 .. returnType.HtmlText.Length - 8]

//             sb.Write """<div class="record-field">"""
//             sb.Indent 2
//             sb.Write $"""<a class="record-field-name" href="#%s{field.UrlBaseName}">{field.Name}</a>&nbsp;<span class="keyword">:</span>&nbsp;<span class="record-field-type">{escapedReturnType}</span>"""
//             sb.WriteLine "</div>"

//         | None ->
//             ()

//     sb.Write "<div>"
//     sb.Indent 1
//     sb.WriteLine """<span class="keyword">}</span>"""
//     sb.WriteLine "</div>"
//     sb.WriteLine "</div>"

//     match docLocator.TryFindComment entity.Symbol.XmlDocSig with
//     | Some docComment ->

//         sb.WriteLine """<div class="docs-summary">"""
//         sb.WriteLine "<p><strong>Description</strong></p>"
//         sb.WriteLine "<p>"
//         sb.WriteLine (CommentFormatter.format docComment)
//         sb.WriteLine "</p>"
//         sb.WriteLine "</div>"

//     | None ->
//         ()

//     sb.WriteLine "<p><strong>Properties</strong></p>"
//     sb.NewLine ()
//     sb.WriteLine """<dl class="docs-parameters">"""
//     sb.NewLine()

//     for field in entity.RecordFields do
//         match field.ReturnInfo.ReturnType with
//         | Some returnType ->
//             let escapedReturnType =
//                 // Remove the starting <code> and ending </code>
//                 returnType.HtmlText.[6 .. returnType.HtmlText.Length - 8]

//             sb.WriteLine """<dt class="api-code">"""
//             sb.Write $"""<span class="property">{field.Name}</span>&nbsp;<span class="keyword">:</span>&nbsp;{escapedReturnType}"""
//             sb.WriteLine "</dt>"

//             sb.WriteLine """<dd>"""

//             match docLocator.TryFindComment field with
//             | Some docComment ->
//                 sb.WriteLine (CommentFormatter.format docComment)
//             | None ->
//                 ()

//             sb.WriteLine "</dd>"

//         | None ->
//             ()

//     sb.WriteLine "</dl>"



// let renderUnion (sb : StringBuilder) (info : ApiDocEntityInfo) =
//     let entity = info.Entity

//     sb.WriteLine $"""<div class="api-code">"""
//     sb.WriteLine $"""<div><span class="keyword">type</span>&nbsp;<span class="type">%s{entity.Name}</span>&nbsp;<span class="keyword">=</span></div>"""

//     for case in entity.UnionCases do
//         sb.Write """<div class="union-case">"""
//         sb.Indent 1
//         sb.Write """<span class="keyword">|</span>"""
//         sb.Space 1
//         sb.Write case.Name
//         sb.Space 1
//         if case.Parameters.Length <> 0 then
//             sb.Write """<span class="keyword">of</span>"""
//             sb.Space 1

//             for parameter in case.Parameters do
//                 match parameter.ParameterSymbol with
//                 | Choice1Of2 fsharpParameter ->
//                     ()

//                 | Choice2Of2 fsharpField ->
//                     // Don't use generated names, we try detect it manually because
//                     // fsharpField.IsNameGenerated doesn't seems to do what it should...
//                     if not (fsharpField.Name.StartsWith("Item")) then
//                         sb.Write fsharpField.Name
//                         sb.Space 1
//                         sb.Write """<span class="keyword">:</span>"""
//                         sb.Space 1

//                     printfn "%A" parameter.ParameterType.HtmlText

//                     let returnType =
//                         Render.renderParameterType fsharpField.FieldType

//                     sb.Write returnType.Html

//         sb.WriteLine "</div>"

//     sb.WriteLine "</div>"


//     match docLocator.TryFindComment entity.Symbol.XmlDocSig with
//     | Some docComment ->

//         sb.WriteLine """<div class="docs-summary">"""
//         sb.WriteLine "<p><strong>Description</strong></p>"
//         sb.WriteLine "<p>"
//         sb.WriteLine (CommentFormatter.format docComment)
//         sb.WriteLine "</p>"
//         sb.WriteLine "</div>"

//     | None ->
//         ()

//     sb.WriteLine "<p><strong>Cases</strong></p>"
//     sb.NewLine ()
//     sb.WriteLine """<dl class="docs-parameters">"""
//     sb.NewLine()

//     for field in entity.UnionCases do

//         sb.WriteLine """<dt>"""
//         sb.Write $"""<code>{field.Name}</code>"""
//         sb.WriteLine "</dt>"

//         sb.WriteLine """<dd>"""

//         match docLocator.TryFindComment field with
//         | Some docComment ->
//             sb.WriteLine (CommentFormatter.format docComment)
//         | None ->
//             ()

//         sb.WriteLine "</dd>"

//     sb.WriteLine "</dl>"

// // Generate namespace index
// let sb = new StringBuilder(minimalApiFileContent)

// let generateLink (urlBaseName : string) =
//     "/Nacara/reference/Nacara.Core/" + urlBaseName + ".html"

// let destination =
//     "/reference/index.md"

// Directory.Delete(docsRoot + "/reference", true)

// generateIndex
//     sb
//     docLocator
//     generateLink
//     apiDocModel.Collection.Namespaces

// File.write docsRoot destination sb

// // Generate all the namespace pages
// for apiDoc in apiDocModel.Collection.Namespaces do
//     let sb = new StringBuilder(minimalApiFileContent)

//     generateNamespace
//         sb
//         docLocator
//         generateLink
//         apiDoc

//     let destination =
//         apiUrl + apiDoc.UrlBaseName + ".md"

//     File.write docsRoot destination sb

// let writeEntityFile (sb : StringBuilder) (entity : ApiDocEntity) =
//     let destination =
//         apiUrl + entity.UrlBaseName + ".md"

//     File.write docsRoot destination sb

// for info in apiDocModel.EntityInfos do
//     let sb = new StringBuilder(minimalApiFileContent)

//     sb.WriteLine $"""<div class="is-size-3">{info.Entity.Name}</div>"""

//     sb.NewLine ()

//     let nsUrl =
//         generateLink info.Namespace.UrlBaseName

//     sb.WriteLine "<p>"

//     sb.WriteLine "<div>"
//     sb.Write "<strong>Namespace:</strong>"
//     sb.Space 1
//     sb.Write $"""<a href="{nsUrl}">{info.Namespace.Name}</a>"""
//     sb.WriteLine "</div>"

//     match info.ParentModule with
//     | Some parentModule ->
//         let url =
//             generateLink parentModule.UrlBaseName

//         sb.WriteLine "<div>"
//         sb.Write "<strong>Parent:</strong>"
//         sb.Space 1
//         sb.Write $"""<a href="{url}">{parentModule.Symbol.FullName}</a>"""
//         sb.WriteLine "</div>"

//     | None ->
//         ()

//     sb.WriteLine "</p>"

//     if info.Entity.Symbol.IsFSharpRecord then
//         renderRecord sb info

//     else if info.Entity.Symbol.IsFSharpModule then

//         generateDeclaredTypes
//             sb
//             docLocator
//             generateLink
//             info.Entity.NestedEntities

//         generateValueOrFunction
//             sb
//             docLocator
//             generateLink
//             info.Entity.ValuesAndFuncs

//         // A module can contains module declarations
//         generateDeclaredModules
//             sb
//             docLocator
//             generateLink
//             info.Entity.NestedEntities

//     else if info.Entity.Symbol.IsNamespace then

//         printfn "TODO: Namespace"

//     else if info.Entity.Symbol.IsFSharpUnion then

//         renderUnion sb info

//     else

//         let msg = sprintf "Type %A not supported yet by Nacare.Api. \n\nPlease open an issue." info.Entity.Name

//         sb.WriteLine msg

//         printfn "%s" msg

//     writeEntityFile sb info.Entity

// let indexPage
//     (docLocator : DocLocator)
//     (generateLink : string -> string)

//     : unit =

//     let sb = new StringBuilder(minimalApiFileContent)

//     generateIndex
//         sb
//         docLocator
//         generateLink
//         apiDocModel.Collection.Namespaces

//     File.write docsRoot destination sb

open Argu

type CliArguments =
    | [<AltCommandLine("-p"); Mandatory>] Project of string
    | [<AltCommandLine("-lib"); Mandatory>] Lib_Directory of string
    | [<AltCommandLine("-o"); Mandatory>] Output of string

    interface IArgParserTemplate with

        member this.Usage =
            match this with
            | Project _ ->
                "Your project name"

            | Lib_Directory _ ->
                "The source directory where your dll and xml file are located"

            | Output _ ->
                "The output directory where the generated files will be written"

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
            let xmlDocContent =
                use reader = new StreamReader(xmlFile)

                reader.ReadToEnd()

            // Render the API index page
            let sb = new StringBuilder(minimalApiFileContent)

            let generateLink (urlBaseName : string) =
                "/Nacara/reference/Nacara.Core/" + urlBaseName + ".html"

            let destination =
                "/reference/index.md"

            // Directory.Delete(Path.Combine(output, "reference"), true)

            printfn "%A" (Path.Combine(output, "reference"))


            // Render all the namespaces pages

            // Render all the entities pages

            0

        // Invalid CLI
        | _ ->
            parser.PrintUsage() |> printfn "%s"
            1

    with e ->
        printfn "%s" e.Message
        0
