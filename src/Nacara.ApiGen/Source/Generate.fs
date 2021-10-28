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
    (docLocator : DocLocator)
    (generateLink : string -> string)
    (namespaces : ApiDocNamespace list)
    (destination : string)
    : unit =

    let sb = new StringBuilder(minimalApiFileContent)

    renderIndex
        sb
        docLocator
        generateLink
        namespaces

    File.write docsRoot destination sb


let generateNamespacePage
    (docsRoot : string)
    (apiUrl : string)
    (docLocator : DocLocator)
    (generateLink : string -> string)
    (ns : ApiDocNamespace) =

    let sb = new StringBuilder(minimalApiFileContent)

    renderNamespace
        sb
        docLocator
        generateLink
        ns

    let destination =
        apiUrl + ns.UrlBaseName + ".md"

    File.write docsRoot destination sb



let generateEntityPage
    (docsRoot : string)
    (apiUrl : string)
    (docLocator : DocLocator)
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
        renderRecordType sb docLocator entityInfo

    else if symbol.IsFSharpModule then

        renderDeclaredTypes
            sb
            docLocator
            generateLink
            entityInfo.Entity.NestedEntities

        // A module can contains module declarations
        renderDeclaredModules
            sb
            docLocator
            generateLink
            entityInfo.Entity.NestedEntities

        renderValueOrFunction
            sb
            docLocator
            generateLink
            entityInfo.Entity.ValuesAndFuncs

    else if symbol.IsNamespace then

        printfn "TODO: Namespace"

    else if symbol.IsFSharpUnion then
        renderUnionType sb docLocator entityInfo

    else

        match docLocator.TryFindComment symbol.XmlDocSig with
        | Some docComment ->

            sb.WriteLine """<div class="docs-summary">"""
            sb.WriteLine "<p><strong>Description</strong></p>"
            sb.WriteLine "<p>"
            sb.WriteLine (CommentFormatter.format docComment)
            sb.WriteLine "</p>"
            sb.WriteLine "</div>"

        | None ->
            ()

        ()

    File.write docsRoot destination sb
