module Nacara.ApiGen.Render

open FSharp.Formatting.ApiDocs
open System.Text
open StringBuilder.Extensions
open Nacara.ApiGen
open Helpers
open System.Collections.Generic
open FSharp.Compiler.Symbols
open FSlugify.SlugGenerator

let slugify text =
    slugify DefaultSlugGeneratorOptions text


[<RequireQualifiedAccess>]
type TextNode =
    | Text of string
    | Anchor of url : string * label : string
    | AnchorWithId of url : string * id: string * label : string
    | Space
    | Dot
    | Empty
    | Comma
    | Arrow
    | GreaterThan
    | Colon
    | LessThan
    | LeftParent
    | RightParent
    | Equal
    | Tick
    | Node of TextNode list
    | Keyword of string
    | OpenTag of string
    | OpenTagWithClass of string * string
    | NewLine
    | SelfClosingTag of string
    | CloseTag of string
    | Property of string
    | Spaces of int

    static member ToHtml (node : TextNode) : string =
        node.Html

    member this.Html
        with get () =
            let wrapWithClass cls text =
                $"""<span class="{cls}">{text}</span>"""

            let wrapInKeyword text =
                wrapWithClass "keyword" text

            match this with
            | Text s ->
                s
            | Colon ->
                wrapInKeyword ":"
            | Anchor (url, text) ->
                $"""<a href="{url}">{text}</a>"""
            | AnchorWithId (url, id, text) ->
                $"""<a href="{url}" id="{id}">{text}</a>"""
            | Keyword text ->
                wrapInKeyword text
            | Property text ->
                wrapWithClass "property" text
            | OpenTag tagName ->
                $"""<{tagName}>"""
            | OpenTagWithClass (tagName, cls) ->
                $"""<{tagName} class="{cls}">"""
            | CloseTag tagName ->
                $"""</{tagName}>"""
            | SelfClosingTag tagName ->
                $"""<{tagName}/>"""
            | Spaces n ->
                [
                    for i in 0..n do
                        Space
                ]
                |> Node
                |> TextNode.ToHtml
            | NewLine ->
                "\n"
            | Arrow ->
                wrapInKeyword "->"
            | Dot ->
                wrapInKeyword "."
            | Empty ->
                ""
            | Comma ->
                wrapInKeyword ","
            | Space ->
                "&nbsp;"
            | GreaterThan ->
                wrapInKeyword "&gt;"
            | LessThan ->
                wrapInKeyword "&lt;"
            | Equal ->
                wrapInKeyword "="
            | Tick ->
                "&#x27;"
            | LeftParent ->
                wrapInKeyword "("
            | RightParent ->
                wrapInKeyword ")"
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
            | Anchor (_, text)
            | AnchorWithId (_, _, text)
            | Keyword text
            | Property text ->
                text.Length
            | Arrow ->
                2
            | Node node ->
                node
                |> List.map (fun node ->
                    node.Length
                )
                |> List.sum
            | Empty
            | OpenTag _
            | CloseTag _
            | OpenTagWithClass _
            | SelfClosingTag _
            | NewLine ->
                0
            | Spaces count ->
                count
            | Comma
            | Colon
            | Dot
            | Space
            | GreaterThan
            | LessThan
            | LeftParent
            | RightParent
            | Equal
            | Tick ->
                1


let renderDeclaredTypes
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


let renderValueOrFunction
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
                // This is a value
                sb.WriteLine """<div class="api-code">"""

                sb.WriteLine $"""<div><span class="keyword">val</span>&nbsp;%s{entity.Name}&nbsp;<span class="keyword">:</span>"""
                sb.Write returnHtml
                sb.Write "</div>"

                sb.Write "</div>"
            else
                let slug =
                    slugify entity.Name

                // This is a function/member
                [
                    TextNode.OpenTagWithClass ("div", "api-code")
                    TextNode.NewLine
                    TextNode.OpenTag "div"
                    TextNode.Keyword "val"
                    TextNode.Space
                    TextNode.AnchorWithId ($"#{slug}", slug, entity.Name)
                    TextNode.Space
                    TextNode.Colon
                    TextNode.CloseTag "div"
                ]
                |> TextNode.Node
                |> TextNode.ToHtml
                |> sb.Write
                // sb.WriteLine """<div class="api-code">"""
                // sb.WriteLine $"""<div><span class="keyword">val</span>&nbsp;%s{entity.Name}&nbsp;<span class="keyword">:</span></div>"""

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
                sb.WriteLine (CommentFormatter.formatSummaryOnly comment)

                if paramTypesInfo.Infos.Length <> 0 then
                    sb.WriteLine """<p><strong>Parameters</strong></p>"""

                    for (name, returnType) in paramTypesInfo.Infos do
                        match CommentFormatter.tryFormatParam name comment with
                        | Some paramDoc ->
                            [
                                TextNode.OpenTagWithClass ("div", "doc-parameter")
                                TextNode.NewLine
                                TextNode.OpenTagWithClass ("div", "api-code")
                                TextNode.OpenTagWithClass ("span", "property")
                                TextNode.Text name
                                TextNode.CloseTag "span"
                                TextNode.Space
                                TextNode.Keyword ":"
                                TextNode.Space
                                TextNode.Text returnType.Html
                                TextNode.CloseTag "div"
                                TextNode.NewLine
                                TextNode.Text paramDoc
                                TextNode.CloseTag "div"
                            ]
                            |> TextNode.Node
                            |> TextNode.ToHtml
                            |> sb.Write

                        | None ->
                            [
                                TextNode.OpenTagWithClass ("div", "doc-parameter")
                                TextNode.NewLine
                                TextNode.OpenTagWithClass ("div", "api-code")
                                TextNode.OpenTagWithClass ("span", "property")
                                TextNode.Text name
                                TextNode.CloseTag "span"
                                TextNode.Space
                                TextNode.Keyword ":"
                                TextNode.Space
                                TextNode.Text returnType.Html
                                TextNode.CloseTag "div"
                                TextNode.CloseTag "div"
                            ]
                            |> TextNode.Node
                            |> TextNode.ToHtml
                            |> sb.Write

                        sb.NewLine ()

                match CommentFormatter.tryFormatReturnsOnly comment with
                | Some returnsDoc ->
                    [
                        TextNode.OpenTag "p"
                        TextNode.OpenTag "strong"
                        TextNode.Text "Returns"
                        TextNode.CloseTag "strong"
                        TextNode.CloseTag "p"
                        TextNode.OpenTag "p"
                        TextNode.Text returnsDoc
                        TextNode.CloseTag "p"
                    ]
                    |> TextNode.Node
                    |> TextNode.ToHtml
                    |> sb.WriteLine

                | None ->
                    ()

            | None ->
                ()



            sb.WriteLine "<hr/>"

let renderDeclaredModules
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
                sb.WriteLine $"""<td>{CommentFormatter.formatSummaryOnly comment}</td>"""
            | None ->
                sb.WriteLine $"""<td></td>"""

            sb.WriteLine "</tr>"
        )

        sb.WriteLine "</tbody>"
        sb.WriteLine "</table>"
        sb.WriteLine "</p>"

let renderNamespace
    (sb : StringBuilder)
    (docLocator : DocLocator)
    (linkGenerator : string -> string)
    (apiDoc : ApiDocNamespace) =

    sb.WriteLine $"""<h2 class="title is-3">{apiDoc.Name}</h2>"""

    renderDeclaredModules
        sb
        docLocator
        linkGenerator
        apiDoc.Entities

let renderIndex
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
        renderDeclaredModules
            sb
            docLocator
            linkGenerator
            globalNamespace.Head.Entities

let renderRecordType
    (sb : StringBuilder)
    (docLocator : DocLocator)
    (info : ApiDocEntityInfo) =

    let entity = info.Entity

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
            sb.Write $"""<a class="record-field-name" href="#{slugify field.Name}" >{field.Name}</a>&nbsp;<span class="keyword">:</span>&nbsp;<span class="record-field-type">{escapedReturnType}</span>"""
            sb.WriteLine "</div>"

        | None ->
            ()

    sb.Write "<div>"
    sb.Indent 1
    sb.WriteLine """<span class="keyword">}</span>"""
    sb.WriteLine "</div>"

    if entity.InstanceMembers.Length <> 0 then
        sb.WriteLine "<br />"

    for m in entity.InstanceMembers do
        match m.Symbol with
        | :? FSharpMemberOrFunctionOrValue as symbol ->
            sb.WriteLine "<div>"
            let nodes =
                TextNode.Node
                    [
                        TextNode.Space
                        TextNode.Space
                        TextNode.Space
                        TextNode.Space
                        TextNode.Keyword "member"
                        TextNode.Space
                        TextNode.Text "this"
                        TextNode.Dot
                        TextNode.Property symbol.DisplayName

                        match symbol.HasGetterMethod, symbol.HasSetterMethod with
                        | true, true ->
                            TextNode.Space
                            TextNode.Keyword "with"
                            TextNode.Space
                            TextNode.Text "get"
                            TextNode.Comma
                            TextNode.Space
                            TextNode.Text "set"

                        | true, false ->
                            TextNode.Space
                            TextNode.Keyword "with"
                            TextNode.Space
                            TextNode.Text "get"

                        | false, true ->
                            TextNode.Space
                            TextNode.Keyword "with"
                            TextNode.Space
                            TextNode.Text "set"

                        | false, false ->
                            ()
                    ]

            sb.Write (nodes.Html)
            sb.WriteLine "</div>"

        | _ ->
            ()

    sb.WriteLine "</div>"



    match docLocator.TryFindComment entity.Symbol.XmlDocSig with
    | Some docComment ->

        sb.WriteLine """<div class="docs-summary">"""
        sb.WriteLine "<p><strong>Description</strong></p>"
        sb.WriteLine "<p>"
        sb.WriteLine (CommentFormatter.format docComment)
        sb.WriteLine "</p>"
        sb.WriteLine "</div>"

    | None ->
        ()

    sb.WriteLine "<p><strong>Properties</strong></p>"
    sb.NewLine ()
    sb.WriteLine """<dl class="docs-parameters">"""
    sb.NewLine()

    for field in entity.RecordFields do
        match field.ReturnInfo.ReturnType with
        | Some returnType ->
            let escapedReturnType =
                // Remove the starting <code> and ending </code>
                returnType.HtmlText.[6 .. returnType.HtmlText.Length - 8]

            let slug = slugify field.Name

            sb.WriteLine """<dt class="api-code">"""
            sb.Write $"""<a id={slug} href="#{slug}" class="property">{field.Name}</a>&nbsp;<span class="keyword">:</span>&nbsp;<span class="return-type">{escapedReturnType}</span>"""
            sb.WriteLine "</dt>"

            sb.WriteLine """<dd>"""

            match docLocator.TryFindComment field with
            | Some docComment ->
                sb.WriteLine (CommentFormatter.format docComment)
            | None ->
                ()

            sb.WriteLine "</dd>"

        | None ->
            ()

    sb.WriteLine "</dl>"


let renderUnionType
    (sb : StringBuilder)
    (docLocator : DocLocator)
    (info : ApiDocEntityInfo) =

    let entity = info.Entity

    sb.WriteLine $"""<div class="api-code">"""
    sb.WriteLine $"""<div><span class="keyword">type</span>&nbsp;<span class="type">%s{entity.Name}</span>&nbsp;<span class="keyword">=</span></div>"""

    for case in entity.UnionCases do

        sb.Write """<div class="union-case">"""
        sb.Indent 1
        sb.Write """<span class="keyword">|</span>"""
        sb.Space 1
        sb.Write $"""<a href="#{slugify case.Name}" class="union-case-property">{case.Name}</a>"""
        sb.Space 1
        if case.Parameters.Length <> 0 then
            sb.Write """<span class="keyword">of</span>"""
            sb.Space 1

            for parameter in case.Parameters do
                match parameter.ParameterSymbol with
                | Choice1Of2 fsharpParameter ->
                    ()

                | Choice2Of2 fsharpField ->
                    // Don't use generated names, we try detect it manually because
                    // fsharpField.IsNameGenerated doesn't seems to do what it should...
                    if not (fsharpField.Name.StartsWith("Item")) then
                        sb.Write fsharpField.Name
                        sb.Space 1
                        sb.Write """<span class="keyword">:</span>"""
                        sb.Space 1

                    let returnType =
                        renderParameterType fsharpField.FieldType

                    sb.Write returnType.Html

        sb.WriteLine "</div>"

    sb.WriteLine "</div>"


    match docLocator.TryFindComment entity.Symbol.XmlDocSig with
    | Some docComment ->

        sb.WriteLine """<div class="docs-summary">"""
        sb.WriteLine "<p><strong>Description</strong></p>"
        sb.WriteLine "<p>"
        sb.WriteLine (CommentFormatter.format docComment)
        sb.WriteLine "</p>"
        sb.WriteLine "</div>"

    | None ->
        ()

    sb.WriteLine "<p><strong>Cases</strong></p>"
    sb.NewLine ()
    sb.WriteLine """<dl class="docs-parameters">"""
    sb.NewLine()

    for case in entity.UnionCases do

        sb.WriteLine """<dt>"""
        sb.Write $"""<code id="#{slugify case.Name}">{case.Name}</code>"""
        sb.WriteLine "</dt>"

        sb.WriteLine """<dd>"""

        match docLocator.TryFindComment case with
        | Some docComment ->
            sb.WriteLine (CommentFormatter.format docComment)
        | None ->
            ()

        sb.WriteLine "</dd>"

    sb.WriteLine "</dl>"
