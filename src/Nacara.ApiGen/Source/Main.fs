module Nacara.ApiGen.Main

open FSharp.Formatting.ApiDocs
open System.IO
open StringBuilder.Extensions
open FSharp.Compiler.Symbols
open System.Text.RegularExpressions

let root = __SOURCE_DIRECTORY__

let file = Path.Combine(root, "../Nacara.Core/bin/Debug/netstandard2.0/publish/Nacara.Core.dll")
let libDir = Path.Combine(root, "../Nacara.Core/bin/Debug/netstandard2.0/publish")
let input = ApiDocInput.FromFile(file)
let xmlDocFile = "/home/maximemangel/Documents/Workspaces/Github/MangelMaxime/Nacara/src/Nacara.Core/bin/Debug/netstandard2.0/publish/Nacara.Core.xml"

let xmlDocContent =
    use reader = new StreamReader(xmlDocFile)

    reader.ReadToEnd()


let minimalApiFileContent =
    """---
title: API
layout: nacara-standard
---

<style>
.api-code pre {
    background-color: transparent;
}

.api-code {
    font-family: monospace;
    margin-bottom: 1rem;
    scroll-margin-top: 5.25rem;
}

.api-code .line {
    white-space: nowrap;
}

.api-code .keyword {
    color: #A626A4;
}

.api-code .type {
    color: #C18401;
}

.api-code .record-field span>a {
    color: #C18401;
}

.docs-summary {
    margin-top: 1rem;
    margin-bottom: 1rem;
}

dl.docs-parameters {
    margin-left: 1rem;
}

dl.docs-parameters dt {
    padding-top: 0.5rem;
}

/* dl.docs-parameters dt:before {
    position: absolute;
    margin-left: -0.5rem;
    display: inline-block;
    content: '-';
} */

dl.docs-parameters dd {
    margin-top: 1em;
    margin-bottom: 1em;
}

dl.docs-parameters dt code {
    color: currentColor;
}

.docs-summary a.type,
dl.docs-parameters dt a.type {
    color: #C18401;
}

.docs-summary a.type:hover,
.api-code .type:hover,
.api-code .record-field span>a:hover,
dl.docs-parameters dt a.type:hover {
    text-decoration: underline;
    cursor: pointer;
}
</style>


"""

let apiDocModel =
    ApiDocs.GenerateModel(
        [ input ],
        "Nacare.Core",
        [],
        qualify = true,
        libDirs = [
            libDir
        ],
        root = "/api/"
    )

let x = 1

// for namespce in apiDocModel.Collection.Namespaces do
//     // if namespce.Name = "global" then
//     //     for entity in namespce.Entities do
//     //         printfn "%A" entity.Name
//     if namespce.Name = "Nacara.Core" then
//         for entity in namespce.Entities do
//             for entity in entity. do
//                 printfn "%A" entity.

let rec findIndentationSize (lines : string list) =
    match lines with
    | head::tail ->
        let lesserThanIndex = head.IndexOf("<")
        if lesserThanIndex <> - 1 then
            lesserThanIndex
        else
            findIndentationSize tail
    | [] -> 0

module String =

    let normalizeEndOfLine (text : string)=
        text.Replace("\r\n", "\n")

    let splitBy (c : char) (text : string) =
        text.Split(c)

    let splitLines (text : string) =
        text
        |> normalizeEndOfLine
        |> splitBy '\n'

let renderRecord (info : ApiDocEntityInfo) =
    let sb = new System.Text.StringBuilder()

    let entity = info.Entity

    sb.Write(minimalApiFileContent)

    sb.WriteLine $"""<div class="api-code">"""
    sb.WriteLine $"""<div><span class="keyword">type</span>&nbsp;<span class="type">%s{entity.Name}</span>&nbsp;<span class="keyword">=</span></div>"""
    sb.Indent 1
    sb.WriteLine """<span class="keyword">{</span>"""

    for field in entity.RecordFields do
        match field.ReturnInfo.ReturnType with
        | Some returnType ->
            let escapedReturnType =
                // Remove the starting <code> and ending </code>
                returnType.HtmlText.[6 .. returnType.HtmlText.Length - 8]

            sb.Write """<div class="record-field">"""
            sb.Indent 2
            sb.Write $"""{field.Name}&nbsp;<span class="keyword">:</span>&nbsp;{escapedReturnType}"""
            sb.WriteLine "</div>"

        | None ->
            ()

    sb.Write "<div>"
    sb.Indent 1
    sb.WriteLine """<span class="keyword">}</span>"""
    sb.WriteLine "</div>"
    sb.WriteLine "</div>"

    let searchedValue =
        Regex.Escape(entity.Symbol.XmlDocSig)

    let pattern =
        $"""<member name="%s{searchedValue}">((?'xml_doc'(?:(?!<member>)(?!<\/member>)[\s\S])*)<\/member\s*>)"""

    let m = Regex.Match(xmlDocContent, pattern, RegexOptions.Singleline)

    if m.Success then
        let xmlDoc = m.Groups.["xml_doc"].Value

        let lines =
            xmlDoc
            |> String.splitLines
            |> Array.toList

        let indentationSize =
            lines
            |> findIndentationSize

        // Remove the non meaning full indentation
        let content =
            let indentationText =
                String.replicate indentationSize " "

            lines
            |> List.map (fun line ->
                // Add a small protection in case the user didn't align all it's tags
                if line.StartsWith(indentationText) then
                    line.Substring(indentationSize)
                else
                    line
            )
            |> String.concat "\n"

        printfn "%A" content

        printfn "Formatted:"

        printfn "%A" (CommentFormatter.format content)

        ()

    else
        ()


    let baseUrl = entity.Url("/api/", "Nacara.Core", true, ".md")
    let docsRoot = "/home/maximemangel/Documents/Workspaces/Github/MangelMaxime/Nacara/docs"

    let filePath = Path.Join(docsRoot, baseUrl)

    Directory.CreateDirectory(Path.GetDirectoryName(filePath))
    |> ignore

    use file = new StreamWriter(filePath)

    file.Write(sb.ToString())

for info in apiDocModel.EntityInfos do
    if not info.Entity.RecordFields.IsEmpty then
        renderRecord info
