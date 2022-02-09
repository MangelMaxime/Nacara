module TextNode

open Giraffe.ViewEngine
open Html.Extra

type TextNode =
    | Text of string
    | Anchor of url : string * label : string
    | AnchorWithId of url : string * id: string * label : string
    | Space
    | Dot
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
    | NewLine
    | Property of string
    | Spaces of int

    static member ToHtml (node : TextNode) : XmlNode =
        node.Html

    member this.Html
        with get () =
            match this with
            | Text s ->
                str s

            | Colon ->
                Html.keyword ":"

            | Anchor (url, text) ->
                a [ _href url ]
                    [ str text ]

            | AnchorWithId (url, id, text) ->
                a   [
                        _href url
                        _id id
                    ]
                    [ str text ]

            | Keyword text ->
                Html.keyword text

            | Property text ->
                Html.property text

            | Spaces n ->
                [
                    for i in 0..n do
                        Space
                ]
                |> Node
                |> TextNode.ToHtml

            | NewLine ->
                br [ ]

            | Arrow ->
                Html.keyword "->"

            | Dot ->
                Html.keyword "."

            | Comma ->
                Html.keyword ","

            | Space ->
                Html.space

            | GreaterThan ->
                Html.keyword "&gt;"

            | LessThan ->
                Html.keyword "&lt;"

            | Equal ->
                Html.keyword "="

            | Tick ->
                Html.keyword "&#x27;"

            | LeftParent ->
                Html.keyword "("

            | RightParent ->
                Html.keyword ")"

            | Node node ->
                node
                |> List.map (fun node ->
                    node.Html
                )
                |> RenderView.AsString.xmlNodes
                |> rawText

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
