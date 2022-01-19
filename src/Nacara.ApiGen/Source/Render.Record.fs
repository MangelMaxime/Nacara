namespace Nacara.ApiGen.Render

open FSharp.Formatting.ApiDocs
open Markdown
open Giraffe.ViewEngine
open Nacara.ApiGen.CommentFormatter
open Html.Extra
open Helpers
open TextNode

module Record =

    [<NoComparison>]
    type MethodParameters =
        {
            ParameterDocs: option<ApiDocHtml>
            ParameterNameText: string
            ParameterSymbol: Choice<FSharp.Compiler.Symbols.FSharpParameter,FSharp.Compiler.Symbols.FSharpField>
            ParameterType: ApiDocHtml
        }

    let rec prepareMethodParameters
        (state : Common.ParamTypesInformation)
        (parameters : MethodParameters list) =

        match parameters with
        | parameter :: tail ->
            let returnType =
                match parameter.ParameterSymbol with
                | Choice1Of2 fsharpParameter ->
                    Common.renderParameterType true fsharpParameter.Type

                | Choice2Of2 _ ->
                    TextNode.Text "Static member symbol type not implemented"

            let newState =
                { state with
                    Infos = state.Infos @ [ parameter.ParameterNameText, returnType ]
                    MaxNameLength = System.Math.Max (state.MaxNameLength, parameter.ParameterNameText.Length)
                    MaxReturnTypeLength = System.Math.Max (state.MaxReturnTypeLength, returnType.Length)
                }

            prepareMethodParameters newState tail

        | [] ->
            state


    let renderStaticMemberSignature
        (initialIndentation : int)
        (isDeclaration : bool)
        (staticMember : ApiDocMember) =

        let methodParameters =
            staticMember.Parameters
            |> List.map (fun parameter ->
                {
                    ParameterDocs = parameter.ParameterDocs
                    ParameterNameText = parameter.ParameterNameText
                    ParameterSymbol = parameter.ParameterSymbol
                    ParameterType = parameter.ParameterType
                }
            )

        let parametersInfo =
            prepareMethodParameters
                { Common.ParamTypesInformation.Empty with
                    MaxNameLength = staticMember.Name.Length
                }
                methodParameters

        let parameters =
            [
                for index in 0 .. parametersInfo.Infos.Length - 1 do
                    let (name, returnType) = parametersInfo.Infos.[index]

                    div [ ]
                        [
                            Html.spaces (initialIndentation + 4)
                            str name
                            Html.spaces (parametersInfo.MaxNameLength - name.Length)
                            Html.space
                            Html.keyword ":"
                            Html.space
                            returnType.Html
                            Html.spaces (parametersInfo.MaxReturnTypeLength - returnType.Length + 1)

                            // If this is not the last parameter
                            // add "*" to separate the parameters
                            if index <> parametersInfo.Infos.Length - 1 then
                                Html.keyword "*"
                        ]
            ]

        div [ ]
            [
                Html.spaces initialIndentation
                Html.keyword "static member"
                Html.space

                if isDeclaration then
                    Html.anchor (staticMember.Name, "property")
                else
                    Html.anchorTarget (staticMember.Name, "property")

                Html.space
                Html.keyword ":"
                Html.space

                yield! parameters

                div [ ]
                    [
                        Html.spaces (
                            initialIndentation
                            + 4 // Indent by 1 tab
                            + parametersInfo.MaxNameLength
                            + 1 // Complete with space to align the start of the '->'
                        )
                        Html.keyword "->"
                        Html.space

                        Html.ofOption (staticMember.ReturnInfo.ReturnType
                            |> Option.map (fun (fsharpType, _) ->
                                Common.renderParameterType true fsharpType
                                |> TextNode.ToHtml
                            )
                        )
                    ]
            ]


    let typeSnipppet (entity : ApiDocEntity) =

        let fields =
            entity.RecordFields
            |> List.map (fun field ->
                let returnInfo =
                    field.ReturnInfo.ReturnType
                    |> Option.map (fun (fsharpType, _) ->
                        Common.renderParameterType true fsharpType
                        |> TextNode.ToHtml
                    )

                div [ _class "record-field" ]
                    [
                        Html.indent
                        Html.indent
                        Html.anchor (field.Name, "property")
                        Html.space
                        Html.keyword ":"
                        Html.space
                        Html.ofOption returnInfo
                    ]
            )

        let instanceMembers =
            entity.InstanceMembers
            |> List.map (fun instanceMember ->
                match instanceMember.Symbol with
                | :? FSharp.Compiler.Symbols.FSharpMemberOrFunctionOrValue as symbol ->
                    let getterAndSetter =
                        match symbol.HasGetterMethod, symbol.HasSetterMethod with
                        | true, true ->
                            [
                                Html.keyword "with"
                                Html.space
                                Html.keyword "get"
                                Html.keyword ","
                                Html.keyword "set"
                            ]

                        | true, false ->
                            [
                                Html.keyword "with"
                                Html.space
                                Html.keyword "get"
                            ]

                        | false, true ->
                            [
                                Html.keyword "with"
                                Html.space
                                Html.keyword "set"
                            ]

                        | false, false ->
                            [ ]

                    let returnInfo =
                        instanceMember.ReturnInfo.ReturnType
                        |> Option.map (fun (fsharpType, _) ->
                            Common.renderParameterType true fsharpType
                            |> TextNode.ToHtml
                        )
                        // TODO: Member instance with a setter doesn't have
                        // the return info.
                        // Waiting: https://github.com/fsprojects/FSharp.Formatting/issues/734
                        // If it doesn't get fixed, we can always parse the Symbol information

                    div [ ]
                        [
                            Html.indent
                            Html.keyword "member"
                            Html.space
                            Html.anchor (symbol.DisplayName, "property")
                            Html.space
                            Html.keyword ":"
                            Html.space
                            Html.ofOption returnInfo
                            Html.space
                            yield! getterAndSetter
                        ]

                | _ ->
                    str "Record unkown instance members"
            )

        InlineHtmlBlock (
            div [ _class "api-code" ]
                [
                    div [ ]
                        [
                            Html.keyword "type"
                            Html.space
                            Html.type' entity.Name
                            Html.space
                            Html.keyword "="
                        ]

                    div [ ]
                        [
                            Html.indent
                            Html.keyword "{"
                        ]

                    yield! fields

                    div [ ]
                        [
                            Html.indent
                            Html.keyword "}"
                        ]

                    br [ ]

                    yield! instanceMembers

                    br [ ]

                    yield! entity.StaticMembers
                            |> List.map (renderStaticMemberSignature 4 true)
                ]
        )

    let docsSection
        (linkGenerator : string -> string)
        (entity : ApiDocEntityInfo) =

        entity.Entity.Comment.XmlText

    let private summary (apiDocComment : ApiDocComment) =

        apiDocComment.TryGetXmlText
        |> Option.map (fun comment ->
            section [ _class "api-doc-summary" ]
                [
                    p [ ]
                        [ strong [ ]
                            [ str "Description" ] ]

                    p [ ]
                        [
                            str (formatXmlComment comment)
                        ]
                ]
        )
        |> Html.ofOption
        |> InlineHtmlBlock

    let private propertiesDocumentation
        (entity : ApiDocEntity) =

        let fields =
            entity.RecordFields
            |> List.map (fun field ->
                [
                    dt [ _class "api-code" ]
                        [
                            Html.anchorTarget (field.Name, "property")
                            Html.space
                            Html.keyword ":"
                            Html.space

                            field.ReturnInfo.ReturnType
                            |> Option.map (fun (fsharpType, _) ->
                                Common.renderParameterType true fsharpType
                                |> TextNode.ToHtml
                            )
                            |> Html.ofOption
                        ]

                    dd [ ]
                        [
                            field.Comment.TryGetXmlText
                            |> Option.map (formatXmlComment >> str)
                            |> Html.ofOption
                        ]
                ]
            )
            |> List.collect id

        if fields.IsEmpty then
            Paragraph [ ]
        else
            InlineHtmlBlock (
                section [ ]
                    [
                        p [ ]
                            [ strong [ ]
                                [ str "Properties" ] ]

                        dl [ _class "api-doc-record-fields" ]
                            fields
                    ]
            )

    let private instanceMembersDocumentation
        (entity : ApiDocEntity) =

        let instanceMembers =
            entity.InstanceMembers
            |> List.map (fun instanceMember ->
                match instanceMember.Symbol with
                | :? FSharp.Compiler.Symbols.FSharpMemberOrFunctionOrValue as symbol ->
                    let returnInfo =
                        instanceMember.ReturnInfo.ReturnType
                        |> Option.map (fun (fsharpType, _) ->
                            Common.renderParameterType true fsharpType
                            |> TextNode.ToHtml
                        )

                    [
                        dt [ _class "api-code" ]
                            [
                                Html.anchorTarget (symbol.DisplayName, "member")
                                Html.space
                                Html.keyword ":"
                                Html.space
                                Html.ofOption returnInfo
                            ]

                        dd [ ]
                            [
                                instanceMember.Comment.TryGetXmlText
                                |> Option.map (formatXmlComment >> str)
                                |> Html.ofOption
                            ]
                    ]

                | _ ->
                    [ str "Record unkown instance members" ]
            )
            |> List.collect id

        if instanceMembers.IsEmpty then
            Paragraph [ ]
        else
            InlineHtmlBlock (
                section [ ]
                    [
                        p [ ]
                            [ strong [ ]
                                [ str "Instance members" ] ]

                        dl [ _class "api-doc-record-fields" ]
                            instanceMembers
                    ]
            )

    let private staticMemberDocumentation
        (entity : ApiDocEntity) =

        let staticMembers =
            entity.StaticMembers
            |> List.map (fun staticMember ->
                match staticMember.Symbol with
                | :? FSharp.Compiler.Symbols.FSharpMemberOrFunctionOrValue as symbol ->
                    [
                        dt [ _class "api-code" ]
                            [
                                renderStaticMemberSignature 0 false staticMember
                            ]

                        dd [ ]
                            [
                                p [ ]
                                    [
                                        strong [ ]
                                            [ str "Parameters" ]
                                    ]


                                staticMember.Comment.TryGetXmlText
                                |> Option.map (formatXmlComment >> str)
                                |> Html.ofOption


                            ]
                    ]

                | _ ->
                    [ str "Record unkown static members" ]
            )
            |> List.collect id

        if staticMembers.IsEmpty then
            Paragraph [ ]
        else
            InlineHtmlBlock (
                section [ ]
                    [
                        p [ ]
                            [ strong [ ]
                                [ str "Static members" ] ]

                        dl [ _class "api-doc-record-fields" ]
                            staticMembers
                    ]
            )

    let content
        (linkGenerator : string -> string)
        (entity : ApiDocEntity) =

        [
            typeSnipppet entity

            summary entity.Comment

            propertiesDocumentation entity

            instanceMembersDocumentation entity

            staticMemberDocumentation entity
        ]



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
