module Nacara.ApiGen.Render.Global

open FSharp.Formatting.ApiDocs
open System.Text
open StringBuilder.Extensions
open Nacara.ApiGen
open Helpers
open System.Collections.Generic
open FSharp.Compiler.Symbols
open System.Xml.Linq
open Markdown
open Giraffe.ViewEngine
open Html.Extra
open TextNode


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
                        Infos = state.Infos @ [ "TODO: extractParamTypesInformation -> fsharpField", TextNode.Text "" ]
                    }

                extractParamTypesInformation newState tail

        | [] ->
            state

module Internal =

    let renderReturnType (returnTypeOpt : FSharpType option) =
        match returnTypeOpt with
        | Some returnType ->
            if returnType.IsAbbreviation then
                returnType.TypeDefinition.CompiledName
            else
                "TODO: renderReturnType"

        | None ->
            "unit"

    let renderValue
        (entity : ApiDocMember)
        (returnType : FSharpType option) =

        InlineHtmlBlock (
            div [ _class "api-code" ]
                [
                    div [ ]
                        [
                            Html.keyword "val"
                            Html.space
                            Html.anchor(entity.Name, "property")
                            Html.space
                            Html.keyword ":"
                            Html.space
                            str (renderReturnType returnType)
                        ]
                ]
        )





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

        // if not globalNamespace.IsEmpty then
        //     yield! renderDeclaredModules
        //                 linkGenerator
        //                 globalNamespace.Head.Entities
    ]



// let renderUnionType
//     (sb : StringBuilder)
//     (info : ApiDocEntityInfo) =

//     let entity = info.Entity

//     sb.WriteLine $"""<div class="api-code">"""
//     sb.WriteLine $"""<div><span class="keyword">type</span>&nbsp;<span class="type">%s{entity.Name}</span>&nbsp;<span class="keyword">=</span></div>"""

//     for case in entity.UnionCases do

//         sb.Write """<div class="union-case">"""
//         sb.Indent 1
//         sb.Write """<span class="keyword">|</span>"""
//         sb.Space 1
//         sb.Write $"""<a href="slugify case.Name" class="union-case-property">{case.Name}</a>"""
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

//                     let returnType =
//                         renderParameterType true fsharpField.FieldType

//                     // sb.Write returnType.Html
//                     ()

//         sb.WriteLine "</div>"

//     sb.WriteLine "</div>"


//     match entity.Comment.Xml with
//     | Some _ ->
//         sb.WriteLine """<div class="docs-summary">"""
//         sb.WriteLine "<p><strong>Description</strong></p>"
//         sb.WriteLine "<p>"
//         sb.WriteLine (CommentFormatter.format entity.Comment.XmlText)
//         sb.WriteLine "</p>"
//         sb.WriteLine "</div>"

//     | None ->
//         ()

//     sb.WriteLine "<p><strong>Cases</strong></p>"
//     sb.NewLine ()
//     sb.WriteLine """<dl class="docs-parameters">"""
//     sb.NewLine()

//     for case in entity.UnionCases do

//         sb.WriteLine """<dt>"""
//         sb.Write $"""<code id="slugify case.Name">{case.Name}</code>"""
//         sb.WriteLine "</dt>"

//         sb.WriteLine """<dd>"""

//         match case.Comment.Xml with
//         | Some _ ->
//             sb.WriteLine (CommentFormatter.format entity.Comment.XmlText)

//         | None ->
//             ()

//         sb.WriteLine "</dd>"

//     sb.WriteLine "</dl>"
