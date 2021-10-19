module Generator.Namespace

open FSharp.Formatting.ApiDocs
open System.Text
open StringBuilder.Extensions
open Nacara.ApiGen

let generateModules
    (sb : StringBuilder)
    (docLocator : DocLocator)
    (linkGenerator : string -> string)
    (entities : ApiDocEntity list) =

    sb.WriteLine """<p class="is-size-5"><strong>Declared modules</strong></p>"""
    sb.NewLine ()
    sb.WriteLine "<p>"
    sb.WriteLine """<table class="table is-bordered docs-modules">"""
    sb.WriteLine "<thead>"
    sb.WriteLine "<tr>"
    sb.WriteLine """<th width="25%">Module</th>"""
    sb.WriteLine """<th width="75%">Description</th>"""
    sb.WriteLine "</tr>"
    sb.WriteLine "</thead>"

    sb.WriteLine "<tbody>"

    entities
    |> List.filter (fun ns ->
        ns.Symbol.IsFSharpModule
    )
    |> List.iter (fun md ->
        let url = linkGenerator md.UrlBaseName

        sb.WriteLine "<tr>"
        sb.WriteLine $"""<td><a href="{url}">{md.Name}</a></td>"""

        match docLocator.TryFindComment md.Symbol.XmlDocSig with
        | Some comment ->
            sb.WriteLine $"""<td>{CommentFormatter.format comment}</td>"""
        | None ->
            sb.WriteLine $"""<td></td>"""

        sb.WriteLine "</tr>"
    )

    sb.WriteLine "</tbody>"
    sb.WriteLine "</table>"
    sb.WriteLine "</p>"

let generate
    (sb : StringBuilder)
    (docLocator : DocLocator)
    (linkGenerator : string -> string)
    (apiDoc : ApiDocNamespace) =

    sb.WriteLine $"""<h2 class="title is-3">{apiDoc.Name}</h2>"""

    generateModules
        sb
        docLocator
        linkGenerator
        apiDoc.Entities

let generateIndex
    (sb : StringBuilder)
    (docLocator : DocLocator)
    (linkGenerator : string -> string)
    (namespaces: list<ApiDocNamespace>) =

    let globalNamespace, standardNamespaces  =
        namespaces
        |> List.partition (fun ns ->
            ns.Name = "global"
        )

    sb.WriteLine """<p class="is-size-5"><strong>Declared namespaces</strong></p>"""
    sb.NewLine ()
    sb.WriteLine "<p>"
    sb.WriteLine """<table class="table is-bordered docs-modules">"""
    sb.WriteLine "<thead>"
    sb.WriteLine "<tr>"
    sb.WriteLine """<th width="25%">Namespace</th>"""
    sb.WriteLine """<th width="75%">Description</th>"""
    sb.WriteLine "</tr>"
    sb.WriteLine "</thead>"

    sb.WriteLine "<tbody>"

    for ns in standardNamespaces do

        // TODO: Support <namespacedoc> tag as supported by F# formatting
        // Namespace cannot have documentatin so this is a trick to support it
        let url = linkGenerator ns.UrlBaseName

        sb.WriteLine "<tr>"
        sb.WriteLine $"""<td><a href="{url}">{ns.Name}</a></td>"""
        sb.WriteLine $"""<td></td>"""
        sb.WriteLine "</tr>"

    sb.WriteLine "</tbody>"
    sb.WriteLine "</table>"
    sb.WriteLine "</p>"

    if not globalNamespace.IsEmpty then
        generateModules
            sb
            docLocator
            linkGenerator
            globalNamespace.Head.Entities
