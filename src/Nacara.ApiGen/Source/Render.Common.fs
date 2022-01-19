namespace Nacara.ApiGen.Render

open FSharp.Formatting.ApiDocs
open Markdown
open Giraffe.ViewEngine
open Nacara.ApiGen.CommentFormatter
open TextNode
open FSharp.Compiler.Symbols
open System.Collections.Generic
open Helpers
open Html.Extra

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

module Common =

    let renderDescriptiveTable
        (entityTypeName : string)
        (tableBody : XmlNode list) =

        InlineHtmlBlock (
            table [ _class "table is-bordered docs-modules" ]
                [
                    thead [ ]
                        [
                            tr [ ]
                                [
                                    th [ _width "25%" ]
                                        [ str entityTypeName ]
                                    th [ _width "75%" ]
                                        [ str "Description" ]
                                ]
                        ]
                    tbody [ ]
                        tableBody
                ]
        )

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


    let renderValueOrFunctionComment
        (comment : ApiDocComment)
        (paramTypesInfo : ParamTypesInformation) =

        let commentContent = comment.XmlText

        let header =
            InlineHtmlBlock (
                str (formatSummaryOnly commentContent)
            )

        let parametersSection =
            InlineHtmlBlock (

                div [ ]
                    [
                        if not paramTypesInfo.Infos.IsEmpty then
                            p [ ]
                                [
                                    strong [ ]
                                        [ str "Parameters" ]
                                ]

                            for (name, returnType) in paramTypesInfo.Infos do
                                div [ _class "doc-parameter" ]
                                    [
                                        div [ _class "api-code" ]
                                            [
                                                Html.property name
                                                Html.space
                                                Html.keyword ":"
                                                Html.space
                                                returnType.Html
                                            ]

                                        tryFormatParam name commentContent
                                        |> Option.map str
                                        |> Html.ofOption
                                    ]
                    ]
            )

        let returnSection =
            [
                match tryFormatReturnsOnly commentContent with
                | Some returnDoc ->
                    Paragraph [
                        Strong [ Literal "Returns" ]
                    ]

                    Paragraph [
                        Literal returnDoc
                    ]

                | None ->
                    ()

            ]


        [
            header

            parametersSection

            yield! returnSection
        ]


    let renderValueOrFunction
        (generateLink : string -> string)
        (entities : ApiDocMember list) =

        [
            if not entities.IsEmpty then

                InlineHtmlBlock (
                    p [ _class "is-size-5" ]
                        [
                            strong [ ]
                                [ str "Value and functions" ]
                        ]
                )

                InlineHtmlBlock (
                    hr [ ]
                )

                for entity in entities do
                    let (ApiDocMemberDetails(usageHtml, paramTypes, returnTypeOpt, modifiers, typars, baseType, location, compiledName)) =
                        entity.Details

                    let returnType =
                        returnTypeOpt
                        // Extract the FSharpType is available
                        |> Option.map fst

                    let initialParamTypesInfo =
                        { ParamTypesInformation.Empty with
                            MaxNameLength = entity.Name.Length
                        }

                    let paramTypesInfo =
                        extractParamTypesInformation
                            initialParamTypesInfo
                            paramTypes

                    if paramTypesInfo.Infos.IsEmpty then
                        Internal.renderValue entity returnType

                        yield! renderValueOrFunctionComment entity.Comment paramTypesInfo
                    else

                        InlineHtmlBlock (
                            div [ _class "api-code" ]
                                [
                                    // Generate the function name row
                                    div [ ]
                                        [
                                            Html.keyword "val"
                                            Html.space
                                            Html.anchor(entity.Name, "property")
                                            Html.spaces (paramTypesInfo.MaxNameLength - entity.Name.Length)
                                            Html.space
                                            Html.keyword ":"
                                        ]

                                    // Generate one row per parameter
                                    for index in 0 .. paramTypesInfo.Infos.Length - 1 do
                                        let (name, returnType) = paramTypesInfo.Infos.[index]

                                        div [ ]
                                            [
                                                Html.indent // Equivalent to 'val '
                                                str name
                                                Html.spaces (paramTypesInfo.MaxNameLength - name.Length + 1) // Complete with space to align the ':'
                                                Html.keyword ":"
                                                Html.space
                                                returnType.Html
                                                Html.spaces (paramTypesInfo.MaxReturnTypeLength - returnType.Length + 1) // Complete with space to align the '->'

                                                // Don't add the arrow after the last parameter
                                                if index <> paramTypesInfo.Infos.Length - 1 then
                                                    Html.keyword "->"
                                            ]

                                    // Generate the return type row
                                    div [ ]
                                        [
                                            Html.spaces (
                                                4 // Equivalent to 'val '
                                                + paramTypesInfo.MaxNameLength
                                                + 1 // Complete with space to align the start of the '->'
                                            )
                                            Html.keyword "->"
                                            Html.space
                                            str (Internal.renderReturnType returnType)
                                        ]
                                ]
                        )

                        yield! renderValueOrFunctionComment entity.Comment paramTypesInfo

                    InlineHtmlBlock (
                        hr [ ]
                    )
            ]
