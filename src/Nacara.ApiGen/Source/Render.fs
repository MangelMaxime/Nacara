module Nacara.ApiGen.Render

open FSharp.Formatting.ApiDocs
open System.Text
open StringBuilder.Extensions
open Nacara.ApiGen
open Helpers
open System.Collections.Generic
open FSharp.Compiler.Symbols
open FSlugify.SlugGenerator
open System.Text.RegularExpressions
open System.Xml.Linq
open Markdown
open Giraffe.ViewEngine


let slugify text =
    slugify DefaultSlugGeneratorOptions text

let wrapWithClass cls text =
    $"""<span class="{cls}">{text}</span>"""

let wrapInKeyword text =
    wrapWithClass "keyword" text

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

let formatXmlComment
    (commentOpt : XElement option) : string =

    match commentOpt with
    | Some comment ->
        let docComment = comment.ToString()

        let pattern =
            $"""<member name=".*">((?'xml_doc'(?:(?!<member>)(?!<\/member>)[\s\S])*)<\/member\s*>)"""

        let m = Regex.Match(docComment, pattern, RegexOptions.Singleline)

        // Remove the <member> and </member> tags
        if m.Success then
            let xmlDoc = m.Groups.["xml_doc"].Value

            let lines =
                xmlDoc
                |> String.splitLines
                |> Array.toList

            // Remove the non meaning full indentation
            let content =
                lines
                |> List.map (fun line ->
                    // Add a small protection in case the user didn't align all it's tags
                    if line.StartsWith(" ") then
                        line.Substring(1)
                    else
                        line
                )
                // Trim end of the line as the trailing whitespaces are not significant
                |> List.map String.trimEnd
                |> String.concat "\n"

            CommentFormatter.format content
        else
            CommentFormatter.format docComment

    | None ->
        ""

let renderDeclaredTypes
    (sb : StringBuilder)
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

            sb.WriteLine $"""<td>{formatXmlComment typ.Comment.Xml}</td>"""

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

let rec renderParameterType (isTopLevel : bool) (typ : FSharpType) : TextNode =
    // This correspond to a generic paramter like: 'T
    if typ.IsGenericParameter then
        TextNode.Node [
            TextNode.Tick
            TextNode.Text typ.GenericParameter.DisplayName
        ]
    // Not a generic type we can display it as it is
    // Example:
    //      - string
    //      - int
    //      - MyObject
    else if typ.GenericArguments.Count = 0 then
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
                            // For some generic types definition we don't add the generic arguments
                            if arg.TypeDefinition.DisplayName = "exn"
                                || arg.TypeDefinition.DisplayName = "unit" then

                                TextNode.Text arg.TypeDefinition.DisplayName

                            else
                                // This is the name of the type definition
                                // In Choice<'T1, 'T2> this correspond to Choice
                                TextNode.Text arg.TypeDefinition.DisplayName
                                TextNode.LessThan
                                // Render the generic parameters list in the form of 'T1, 'T2
                                renderGenericParameters arg.TypeDefinition.GenericParameters
                                TextNode.GreaterThan

                        else if arg.IsFunctionType then

                            let res =
                                [
                                    for index in 0 .. arg.GenericArguments.Count - 1 do
                                        let arg = arg.GenericArguments.[index]

                                        if index <> 0 then
                                            TextNode.Space
                                            TextNode.Arrow
                                            TextNode.Space

                                        renderParameterType false arg
                                ]

                            // Try to detect curried case
                            // Like in:
                            // let create (f: ('T -> unit) -> (exn -> unit) -> unit): JS.Promise<'T> = jsNative
                            // FCS gives back an equivalent of :
                            // let create (f: ('T -> unit) -> ((exn -> unit) -> unit)): JS.Promise<'T> = jsNative
                            // So we try to detect it to avoid the extract Parents
                            match res with
                            | (TextNode.Node (TextNode.LeftParent :: _ ) :: _ ) ->
                                TextNode.Node res

                            | _ ->
                                TextNode.Node [
                                    TextNode.LeftParent

                                    yield! res

                                    TextNode.RightParent
                                ]

                        else
                            TextNode.Text "Unkown syntax please open an issue"
                ]

            // If this is a top level function we don't neeed to add the parenthesis
            TextNode.Node [
                if not isTopLevel then
                    TextNode.LeftParent

                TextNode.Node result

                if not isTopLevel then
                    TextNode.RightParent
            ]

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
                        else if arg.IsAbbreviation then
                            TextNode.Text arg.TypeDefinition.DisplayName
                        else

                            let url =
                                arg.TypeDefinition.FullName
                                |> String.toLower
                                |> String.replace "." "-"
                                |> String.append ".html"

                            let subType =
                                renderParameterType false arg

                            TextNode.Anchor (url, arg.TypeDefinition.DisplayName)
                            TextNode.LessThan

                            subType

                            TextNode.GreaterThan
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
                    renderParameterType true fsharpParameter.Type

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

// let renderValue
//     (linkGenerator : string -> string)
//     (entities : ApiDocMember list) =

let renderValueOrFunction
    (sb : StringBuilder)
    (linkGenerator : string -> string)
    (entities : ApiDocMember list) =

    if not entities.IsEmpty then

        sb.WriteLine """<p class="is-size-5"><strong>Values and functions</strong></p>"""
        sb.NewLine ()
        sb.WriteLine "<hr/>"

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
                | Some (_, returnType) ->
                    // Remove the starting <code> and ending </code>
                    returnType.HtmlText.[6 .. returnType.HtmlText.Length - 8]
                    // Adapt the text to have basic syntax highlighting
                    |> fun text ->
                        text.Replace("&lt;", wrapInKeyword "&lt;")
                    |> fun text ->
                        text.Replace("&gt;", wrapInKeyword "&gt;")
                    |> fun text ->
                        text.Replace(",", wrapInKeyword ",")

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

                for index in 0 .. paramTypesInfo.Infos.Length - 1 do
                    let (name, returnType) = paramTypesInfo.Infos.[index]

                    sb.Write "<div>"
                    sb.Space 4 // Equivalent to 'val '
                    sb.Write name
                    sb.Space (paramTypesInfo.MaxNameLength - name.Length + 1) // Complete with space to align the ':'
                    sb.Write """<span class="keyword">:</span>"""
                    sb.Space 1
                    sb.Write returnType.Html
                    sb.Space (paramTypesInfo.MaxReturnTypeLength - returnType.Length + 1) // Complete with space to align the '->'

                    // Don't add the arrow after the last parameter
                    if index <> paramTypesInfo.Infos.Length - 1 then
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

            // match docLocator.TryFindComment entity with
            match entity.Comment.Xml with
            | Some xmlComment ->
                let comment = xmlComment.ToString()
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
    (linkGenerator : string -> string)
    (entities : ApiDocEntity list) =

    let moduleDeclarations =
        entities
        |> List.filter (fun ns ->
            ns.Symbol.IsFSharpModule
        )

    if not moduleDeclarations.IsEmpty then
        [
            InlineHtmlBlock (
                p [ _class "is-size-5" ]
                    [
                        strong [ ]
                            [ str "Declared modules" ]
                    ]
            )

            Paragraph [ HardLineBreak ]

            InlineHtmlBlock (
                table [ _class "table is-bordered docs-modules" ]
                    [
                        thead [ ]
                            [
                                tr [ ]
                                    [
                                        th [ _width "25%" ]
                                            [ str "Module" ]
                                        th [ _width "75%" ]
                                            [ str "Description" ]
                                    ]
                            ]
                        tbody [ ]
                            [
                                for md in moduleDeclarations do
                                    let url = linkGenerator md.UrlBaseName

                                    tr [ ]
                                        [
                                            td [ ]
                                                [
                                                    a [ _href url ]
                                                        [ str md.Name ]
                                                ]
                                            td [ ]
                                                [
                                                    str (formatXmlComment md.Comment.Xml)
                                                ]
                                        ]
                            ]
                    ]
            )

        ]

    else
        [ ]


let renderNamespace
    (linkGenerator : string -> string)
    (apiDoc : ApiDocNamespace) =

    [
        InlineHtmlBlock (
            h2
                [ _class "title is-3" ]
                [ str apiDoc.Name ]
        )

        yield! renderDeclaredModules
                linkGenerator
                apiDoc.Entities

        // TODO: Render declared types
    ]


let renderIndex
    (linkGenerator : string -> string)
    (namespaces: list<ApiDocNamespace>) =

    let globalNamespace, standardNamespaces  =
        namespaces
        |> List.partition (fun ns ->
            ns.Name = "global"
        )

    [
        if not standardNamespaces.IsEmpty then

            InlineHtmlBlock (
                p [ _class "is-size-5" ]
                    [
                        strong [ ]
                            [ str "Declared namespaces"]
                    ]
            )

            Paragraph [ HardLineBreak ]

            InlineHtmlBlock (
                table [ _class "table is-bordered docs-modules" ]
                    [
                        thead [ ]
                            [
                                tr [ ]
                                    [
                                        th [ _width "25%" ]
                                            [ str "Namespace" ]
                                        th [ _width "75%" ]
                                            [ str "Description" ]
                                    ]
                            ]
                        tbody [ ]
                            [
                                for ns in standardNamespaces do
                                    let url = linkGenerator ns.UrlBaseName

                                    tr [ ]
                                        [
                                            td [ ]
                                                [
                                                    a [ _href url ]
                                                        [ str ns.Name ]
                                                ]
                                            // TODO: Support <namespacedoc> tag as supported by F# formatting
                                            // Namespace cannot have documentatin so this is a trick to support it
                                            td [ ] [ ]
                                        ]

                            ]
                    ]
            )

        if not globalNamespace.IsEmpty then
            yield! renderDeclaredModules
                        linkGenerator
                        globalNamespace.Head.Entities
    ]

let space =
    rawText "&nbsp;"

let indent =
    rawText "&nbsp;&nbsp;&nbsp;&nbsp;"

let keyword (text : string) =
    span [ _class "keyword" ] [ str text ]

let renderRecordType
    (info : ApiDocEntityInfo) =

    let entity = info.Entity

    div [ _class "api-code" ]
        [
            // Render type defintion
            div [ ]
                [
                    keyword "type"
                    space
                    span [ _class "type" ]
                        [ str entity.Name ]
                    space
                    keyword "="
                ]

            div [ ]
                [
                    indent
                    keyword "{"
                ]

            for field in entity.RecordFields do
                match field.ReturnInfo.ReturnType with
                | Some (fsharpType, _) ->
                    div [ _class "record-field" ]
                        [
                            indent
                            indent
                            a
                                [
                                    _class "record-field-name"
                                    _href ("#" + slugify field.Name)
                                ]
                                [
                                    str field.Name
                                ]

                            space
                            keyword ":"
                            space

                            if fsharpType.HasTypeDefinition then
                                span [ _class "record-field-type" ]
                                    [
                                        str fsharpType.TypeDefinition.CompiledName
                                    ]
                        ]

                | None ->
                    ()

            div [ ]
                [
                    indent
                    keyword "}"
                ]
        ]

    // let entity = info.Entity

    // sb.WriteLine $"""<div class="api-code">"""
    // sb.WriteLine $"""<div><span class="keyword">type</span>&nbsp;<span class="type">%s{entity.Name}</span>&nbsp;<span class="keyword">=</span></div>"""
    // sb.Indent 1
    // sb.WriteLine """<span class="keyword">{</span>"""

    // for field in entity.RecordFields do
    //     match field.ReturnInfo.ReturnType with
    //     | Some (_, returnType) ->
    //         let escapedReturnType =
    //             // Remove the starting <code> and ending </code>
    //             returnType.HtmlText.[6 .. returnType.HtmlText.Length - 8]

    //         sb.Write """<div class="record-field">"""
    //         sb.Indent 2
    //         sb.Write $"""<a class="record-field-name" href="#{slugify field.Name}" >{field.Name}</a>&nbsp;<span class="keyword">:</span>&nbsp;<span class="record-field-type">{escapedReturnType}</span>"""
    //         sb.WriteLine "</div>"

    //     | None ->
    //         ()

    // sb.Write "<div>"
    // sb.Indent 1
    // sb.WriteLine """<span class="keyword">}</span>"""
    // sb.WriteLine "</div>"

    // if entity.InstanceMembers.Length <> 0 then
    //     sb.WriteLine "<br />"

    // for m in entity.InstanceMembers do
    //     match m.Symbol with
    //     | :? FSharpMemberOrFunctionOrValue as symbol ->
    //         sb.WriteLine "<div>"
    //         let nodes =
    //             TextNode.Node
    //                 [
    //                     TextNode.Space
    //                     TextNode.Space
    //                     TextNode.Space
    //                     TextNode.Space
    //                     TextNode.Keyword "member"
    //                     TextNode.Space
    //                     TextNode.Text "this"
    //                     TextNode.Dot
    //                     TextNode.Property symbol.DisplayName

    //                     match symbol.HasGetterMethod, symbol.HasSetterMethod with
    //                     | true, true ->
    //                         TextNode.Space
    //                         TextNode.Keyword "with"
    //                         TextNode.Space
    //                         TextNode.Text "get"
    //                         TextNode.Comma
    //                         TextNode.Space
    //                         TextNode.Text "set"

    //                     | true, false ->
    //                         TextNode.Space
    //                         TextNode.Keyword "with"
    //                         TextNode.Space
    //                         TextNode.Text "get"

    //                     | false, true ->
    //                         TextNode.Space
    //                         TextNode.Keyword "with"
    //                         TextNode.Space
    //                         TextNode.Text "set"

    //                     | false, false ->
    //                         ()
    //                 ]

    //         sb.Write (nodes.Html)
    //         sb.WriteLine "</div>"

    //     | _ ->
    //         ()

    // sb.WriteLine "</div>"

    // match entity.Comment.Xml with
    // | Some _ ->
    //     sb.WriteLine """<div class="docs-summary">"""
    //     sb.WriteLine "<p><strong>Description</strong></p>"
    //     sb.WriteLine "<p>"
    //     sb.WriteLine (formatXmlComment entity.Comment.Xml)
    //     sb.WriteLine "</p>"
    //     sb.WriteLine "</div>"

    // | None ->
    //     ()

    // sb.WriteLine "<p><strong>Properties</strong></p>"
    // sb.NewLine ()
    // sb.WriteLine """<dl class="docs-parameters">"""
    // sb.NewLine()

    // for field in entity.RecordFields do
    //     match field.ReturnInfo.ReturnType with
    //     | Some (_, returnType) ->
    //         let escapedReturnType =
    //             // Remove the starting <code> and ending </code>
    //             returnType.HtmlText.[6 .. returnType.HtmlText.Length - 8]

    //         let slug = slugify field.Name

    //         sb.WriteLine """<dt class="api-code">"""
    //         sb.Write $"""<a id={slug} href="#{slug}" class="property">{field.Name}</a>&nbsp;<span class="keyword">:</span>&nbsp;<span class="return-type">{escapedReturnType}</span>"""
    //         sb.WriteLine "</dt>"

    //         sb.WriteLine """<dd>"""

    //         match field.Comment.Xml with
    //         | Some _ ->
    //             sb.WriteLine (formatXmlComment entity.Comment.Xml)

    //         | None ->
    //             ()

    //         sb.WriteLine "</dd>"

    //     | None ->
    //         ()

    // sb.WriteLine "</dl>"


let renderUnionType
    (sb : StringBuilder)
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
                        renderParameterType true fsharpField.FieldType

                    sb.Write returnType.Html

        sb.WriteLine "</div>"

    sb.WriteLine "</div>"


    match entity.Comment.Xml with
    | Some _ ->
        sb.WriteLine """<div class="docs-summary">"""
        sb.WriteLine "<p><strong>Description</strong></p>"
        sb.WriteLine "<p>"
        sb.WriteLine (formatXmlComment entity.Comment.Xml)
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

        match case.Comment.Xml with
        | Some _ ->
            sb.WriteLine (formatXmlComment entity.Comment.Xml)

        | None ->
            ()

        sb.WriteLine "</dd>"

    sb.WriteLine "</dl>"

let renderEntity
    (linkGenerator : string -> string)
    (entity : ApiDocEntityInfo) =

    let symbol = entity.Entity.Symbol

    [
        InlineHtmlBlock (
            div [ _class "is-size-3" ]
                [ str entity.Entity.Name ]
        )

        Paragraph [ HardLineBreak ]

        InlineHtmlBlock (
            p [ ]
                [
                    div [ ]
                        [
                            strong [ ]
                                [ str "Namespace:" ]
                            str " "
                            a [ _href (linkGenerator entity.Namespace.UrlBaseName) ]
                                [ str entity.Namespace.Name ]
                        ]

                    match entity.ParentModule with
                    | Some parentModule ->
                        let parentModuleUrl =
                            linkGenerator parentModule.UrlBaseName

                        div [ ]
                            [
                                strong [ ]
                                    [ str "Parent:"]
                                str " "
                                a [ _href parentModuleUrl ]
                                    [ str parentModule.Symbol.FullName ]
                            ]

                        if symbol.IsFSharpRecord then
                            renderRecordType entity
                        else
                            ()

                    | None ->
                        ()
                ]
        )
    ]
