module Render

open FSharp.Formatting.ApiDocs
open System.Text
open StringBuilder.Extensions
open Nacara.ApiGen
open Helpers
open System.Collections.Generic
open FSharp.Compiler.Symbols


[<RequireQualifiedAccess>]
type TextNode =
    | Text of string
    | Anchor of string * string
    | Space
    | Empty
    | Comma
    | Arrow
    | GreaterThan
    | LessThan
    | Tick
    | Node of TextNode list

    member this.Html
        with get () =
            let wrapInKeyword text =
                $"""<span class="keyword">{text}</span>"""

            match this with
            | Text s ->
                s
            | Anchor (url, text) ->
                $"""<a href="{url}">{text}</a>"""
            | Arrow ->
                wrapInKeyword "->"
            | Empty ->
                ""
            | Comma ->
                wrapInKeyword ","
            | Space ->
                wrapInKeyword "&nbsp;"
            | GreaterThan ->
                wrapInKeyword "&gt;"
            | LessThan ->
                wrapInKeyword "&lt;"
            | Tick ->
                "&#x27;"
            | Node node ->
                node
                |> List.map (fun node ->
                    node.Html
                )
                |> String.concat ""

    member this.Length
        with get () =
            match this with
            | Text s ->
                s.Length
            | Anchor (_, text) ->
                text.Length
            | Arrow ->
                2
            | Node node ->
                node
                |> List.map (fun node ->
                    node.Length
                )
                |> List.sum
            | Empty ->
                0
            | Comma
            | Space
            | GreaterThan
            | LessThan
            | Tick ->
                1


let generateDeclaredTypes
    (sb : StringBuilder)
    (docLocator : DocLocator)
    (linkGenerator : string -> string)
    (entities : ApiDocEntity list) =

    let typeDeclarations =
        entities
        |> List.filter (fun entity ->
            entity.IsTypeDefinition
        )

    if not typeDeclarations.IsEmpty then

        sb.WriteLine """<p class="is-size-5"><strong>Declared types</strong></p>"""
        sb.NewLine ()
        sb.WriteLine "<p>"
        sb.WriteLine """<table class="table is-bordered docs-types">"""
        sb.WriteLine "<thead>"
        sb.WriteLine "<tr>"
        sb.WriteLine """<th width="25%">Type</th>"""
        sb.WriteLine """<th width="75%">Description</th>"""
        sb.WriteLine "</tr>"
        sb.WriteLine "</thead>"

        sb.WriteLine "<tbody>"

        typeDeclarations
        |> List.iter (fun typ ->
            let url = linkGenerator typ.UrlBaseName

            sb.WriteLine "<tr>"
            sb.WriteLine $"""<td><a href="{url}">{typ.Name}</a></td>"""

            match docLocator.TryFindComment typ.Symbol.XmlDocSig with
            | Some comment ->
                sb.WriteLine $"""<td>{CommentFormatter.format comment}</td>"""
            | None ->
                sb.WriteLine $"""<td></td>"""

            sb.WriteLine "</tr>"
        )

        sb.WriteLine "</tbody>"
        sb.WriteLine "</table>"
        sb.WriteLine "</p>"

let inline html (s: string) = s

/// <summary>
/// Generate a list of generic parameters
/// <example>
/// 'T, 'T2, 'MyType
/// </example>
/// </summary>
/// <param name="parameters"></param>
/// <returns></returns>
let renderGenericParameters (parameters : IList<FSharpGenericParameter>) : TextNode =
    [
        for index in 0 .. parameters.Count - 1 do
            let param = parameters.[index]

            if index <> 0 then
                TextNode.Comma
                TextNode.Space

            TextNode.Tick
            TextNode.Text param.DisplayName
    ]
    |> TextNode.Node

let renderParameterType (typ : FSharpType) : TextNode =
    // Not a generic type we can display it as it is
    // Example:
    //      - string
    //      - int
    //      - MyObject
    if typ.GenericArguments.Count = 0 then
        TextNode.Text typ.TypeDefinition.DisplayName

    // This is a generic type we need more logic
    else
        // This is a function, we need to generate something like:
        //     - 'T -> string
        //     - 'T -> 'T option
        if typ.IsFunctionType then
            let separator =
                TextNode.Node [
                    TextNode.Space
                    TextNode.Arrow
                    TextNode.Space
                ]

            let result =
                [
                    for index in 0 .. typ.GenericArguments.Count - 1 do
                        let arg = typ.GenericArguments.[index]

                        // Add the separator if this is not the first argument
                        if index <> 0 then
                            separator

                        // This correspond to a generic paramter like: 'T
                        if arg.IsGenericParameter then
                            TextNode.Tick
                            TextNode.Text arg.GenericParameter.DisplayName

                        // This is a type definition like: 'T option or Choice<'T1, 'T2>
                        else if arg.HasTypeDefinition then
                            // This is the name of the type definition
                            // In Choice<'T1, 'T2> this correspond to Choice
                            TextNode.Text arg.TypeDefinition.DisplayName
                            TextNode.LessThan
                            // Render the generic parameters list in the form of 'T1, 'T2
                            renderGenericParameters arg.TypeDefinition.GenericParameters
                            TextNode.GreaterThan

                        else if arg.IsFunctionType then
                            TextNode.Text "function type"

                        else
                            let i = 0
                            TextNode.Text "Unkown syntax please open an issue"
                ]

            TextNode.Node result

        else
            let separator =
                TextNode.Node [
                    TextNode.Space
                    TextNode.Comma
                ]

            let result =
                [
                    for index in 0 .. typ.GenericArguments.Count - 1 do
                        let arg = typ.GenericArguments.[index]

                        // Add the separator if this is not the first argument
                        if index <> 0 then
                            separator

                        if arg.IsGenericParameter then
                            TextNode.Tick
                            TextNode.Text arg.GenericParameter.DisplayName

                        else

                            let url =
                                arg.TypeDefinition.FullName
                                |> String.toLower
                                |> String.replace "." "-"
                                |> String.append ".html"

                            TextNode.Anchor (url, arg.TypeDefinition.DisplayName)
                ]

            TextNode.Node result

type ParamTypesInformation =
    {
        Infos : (string * TextNode) list
        MaxNameLength : int
        MaxReturnTypeLength : int
    }

    static member Empty =
        {
            Infos = []
            MaxNameLength = 0
            MaxReturnTypeLength = 0
        }

let rec extractParamTypesInformation
    (state : ParamTypesInformation)
    (paramTypes : list<Choice<FSharpParameter,FSharpField> * string * ApiDocHtml>) =

        match paramTypes with
        | paramType::tail ->
            match paramType with
            | Choice1Of2 fsharpParameter, name, _apiDoc ->
                let returnType =
                    renderParameterType fsharpParameter.Type

                let newState =
                    { state with
                        Infos = state.Infos @ [ name, returnType ]
                        MaxNameLength = System.Math.Max (state.MaxNameLength, name.Length)
                        MaxReturnTypeLength = System.Math.Max (state.MaxReturnTypeLength, returnType.Length)
                    }

                extractParamTypesInformation newState tail

            | Choice2Of2 _fsharpField, _name, _apiDoc ->
                let newState =
                    { state with
                        Infos = state.Infos @ [ "TODO: extractParamTypesInformation -> fsharpField", TextNode.Empty ]
                    }

                extractParamTypesInformation newState tail

        | [] ->
            state


let generateValueOrFunction
    (sb : StringBuilder)
    (docLocator : DocLocator)
    (linkGenerator : string -> string)
    (entities : ApiDocMember list) =

    if not entities.IsEmpty then

        for entity in entities do
            let (ApiDocMemberDetails(usageHtml, paramTypes, returnType, modifiers, typars, baseType, location, compiledName)) =
                entity.Details

            let returnHtml =
                // Todo: Parse the return type information from
                // let x = entity.Symbol :?> FSharpMemberOrFunctionOrValue
                // x.FullType <-- Here we have access to all the type including the argument for the function that we should ignore... (making the processing complex)
                // For now, we are just using returnType.HtmlText to have something ready as parsing from
                // FSharpMemberOrFunctionOrValue seems to be quite complex
                match returnType with
                | Some returnType ->
                    // Remove the starting <code> and ending </code>
                    returnType.HtmlText.[6 .. returnType.HtmlText.Length - 8]
                | None ->
                    "unit"

            let initial =
                { ParamTypesInformation.Empty with
                    MaxNameLength = entity.Name.Length
                }

            let paramTypesInfo =
                extractParamTypesInformation
                    initial
                    paramTypes

            if paramTypesInfo.Infos.Length = 0 then
                // This is a function/member
                sb.WriteLine """<div class="api-code">"""

                sb.WriteLine $"""<div><span class="keyword">val</span>&nbsp;%s{entity.Name}&nbsp;<span class="keyword">:</span>"""
                sb.Write returnHtml
                sb.Write "</div>"

                sb.Write "</div>"
            else

                // This is a function/member
                sb.WriteLine """<div class="api-code">"""
                sb.WriteLine $"""<div><span class="keyword">val</span>&nbsp;%s{entity.Name}&nbsp;<span class="keyword">:</span></div>"""

                for (name, returnType) in paramTypesInfo.Infos do
                    sb.Write "<div>"
                    sb.Space 4 // Equivalent to 'val '
                    sb.Write name
                    sb.Space (paramTypesInfo.MaxNameLength - name.Length + 1) // Complete with space to align the ':'
                    sb.Write """<span class="keyword">:</span>"""
                    sb.Space 1
                    sb.Write returnType.Html
                    sb.Space (paramTypesInfo.MaxReturnTypeLength - returnType.Length + 1) // Complete with space to align the '->'
                    sb.Write """<span class="keyword">-></span>"""
                    sb.Write "</div>"
                    sb.NewLine ()

                sb.Write "<div>"
                sb.Space (4 + paramTypesInfo.MaxNameLength + 1)
                sb.Write """<span class="keyword">-></span>"""
                sb.Space 1
                sb.Write returnHtml
                sb.Write "</div>"

            sb.WriteLine "</div>"
            sb.NewLine ()

            match docLocator.TryFindComment entity with
            | Some comment ->
                sb.WriteLine (CommentFormatter.extractSummary comment)

            | None ->
                ()

            sb.WriteLine "<hr/>"





let generateDeclaredModules
    (sb : StringBuilder)
    (docLocator : DocLocator)
    (linkGenerator : string -> string)
    (entities : ApiDocEntity list) =

    let moduleDeclarations =
        entities
        |> List.filter (fun ns ->
            ns.Symbol.IsFSharpModule
        )

    if not moduleDeclarations.IsEmpty then

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

        moduleDeclarations
        |> List.iter (fun md ->
            let url = linkGenerator md.UrlBaseName

            sb.WriteLine "<tr>"
            sb.WriteLine $"""<td><a href="{url}">{md.Name}</a></td>"""

            match docLocator.TryFindComment md.Symbol.XmlDocSig with
            | Some comment ->
                sb.WriteLine $"""<td>{CommentFormatter.extractSummary comment}</td>"""
            | None ->
                sb.WriteLine $"""<td></td>"""

            sb.WriteLine "</tr>"
        )

        sb.WriteLine "</tbody>"
        sb.WriteLine "</table>"
        sb.WriteLine "</p>"

let generateNamespace
    (sb : StringBuilder)
    (docLocator : DocLocator)
    (linkGenerator : string -> string)
    (apiDoc : ApiDocNamespace) =

    sb.WriteLine $"""<h2 class="title is-3">{apiDoc.Name}</h2>"""

    generateDeclaredModules
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
        generateDeclaredModules
            sb
            docLocator
            linkGenerator
            globalNamespace.Head.Entities
