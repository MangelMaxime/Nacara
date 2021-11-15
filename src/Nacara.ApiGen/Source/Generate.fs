module Nacara.ApiGen.Generate

open FSharp.Formatting.ApiDocs
open System.IO
open StringBuilder.Extensions
open FSharp.Compiler.Symbols
open System.Text
open System.Text.RegularExpressions
open Render


let minimalApiFileContent =
    """---
layout: api
---

"""

let generateIndexPage
    (docsRoot : string)
    (generateLink : string -> string)
    (namespaces : ApiDocNamespace list)
    (destination : string)
    : unit =

    let sb = new StringBuilder(minimalApiFileContent)

    renderIndex
        sb
        generateLink
        namespaces

    File.write docsRoot destination sb


let generateNamespacePage
    (docsRoot : string)
    (apiUrl : string)
    (generateLink : string -> string)
    (ns : ApiDocNamespace) =

    let sb = new StringBuilder(minimalApiFileContent)

    renderNamespace
        sb
        generateLink
        ns

    let destination =
        apiUrl + ns.UrlBaseName + ".md"

    File.write docsRoot destination sb



let generateEntityPage
    (docsRoot : string)
    (apiUrl : string)
    (generateLink : string -> string)
    (entityInfo : ApiDocEntityInfo) =

    let sb = new StringBuilder(minimalApiFileContent)

    let destination =
        apiUrl + entityInfo.Entity.UrlBaseName + ".md"

    sb.WriteLine $"""<div class="is-size-3">{entityInfo.Entity.Name}</div>"""

    sb.NewLine ()

    let nsUrl =
        generateLink entityInfo.Namespace.UrlBaseName

    sb.WriteLine "<p>"

    sb.WriteLine "<div>"
    sb.Write "<strong>Namespace:</strong>"
    sb.Space 1
    sb.Write $"""<a href="{nsUrl}">{entityInfo.Namespace.Name}</a>"""
    sb.WriteLine "</div>"

    match entityInfo.ParentModule with
    | Some parentModule ->
        let url =
            generateLink parentModule.UrlBaseName

        sb.WriteLine "<div>"
        sb.Write "<strong>Parent:</strong>"
        sb.Space 1
        sb.Write $"""<a href="{url}">{parentModule.Symbol.FullName}</a>"""
        sb.WriteLine "</div>"

    | None ->
        ()

    sb.WriteLine "</p>"

    let symbol = entityInfo.Entity.Symbol

    if symbol.IsFSharpRecord then
        renderRecordType sb entityInfo

    else if symbol.IsFSharpModule then

        renderDeclaredTypes
            sb
            generateLink
            entityInfo.Entity.NestedEntities

        // A module can contains module declarations
        renderDeclaredModules
            sb
            generateLink
            entityInfo.Entity.NestedEntities

        renderValueOrFunction
            sb
            generateLink
            entityInfo.Entity.ValuesAndFuncs

    else if symbol.IsNamespace then

        printfn "TODO: Namespace"

    else if symbol.IsFSharpUnion then
        renderUnionType sb entityInfo

    else

        match entityInfo.Entity.Comment.Xml with
        | Some _ ->
            sb.WriteLine """<div class="docs-summary">"""
            sb.WriteLine "<p><strong>Description</strong></p>"
            sb.WriteLine "<p>"
            sb.WriteLine (formatXmlComment entityInfo.Entity.Comment.Xml)
            sb.WriteLine "</p>"
            sb.WriteLine "</div>"

        | None ->
            ()

    File.write docsRoot destination sb
