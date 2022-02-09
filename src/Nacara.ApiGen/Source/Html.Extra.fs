module Html.Extra

open FSlugify.SlugGenerator
open Giraffe.ViewEngine

let private slugify text =
    slugify DefaultSlugGeneratorOptions text

type Html =

    static member space =
        rawText "&nbsp;"

    static member spaces (count : int) =
        String.replicate count "&nbsp;"
        |> rawText

    static member indent =
        rawText "&nbsp;&nbsp;&nbsp;&nbsp;"

    static member keyword (text : string) =
        span [ _class "keyword" ] [ str text ]

    static member property (text : string) =
        span [ _class "property" ] [ str text ]

    static member type' (text : string) =
        span [ _class "type" ] [ str text ]

    static member anchor (text : string, ?cls : string) =
        let slug =
            slugify text

        a
            [
                if cls.IsSome then
                    _class cls.Value

                _href $"#{slug}"
            ]
            [ str text ]

    static member anchorTarget (text : string, ?cls : string) =
        let slug =
            slugify text

        a
            [
                if cls.IsSome then
                    _class cls.Value

                _href $"#{slug}"
                _id slug
            ]
            [ str text ]

    static member ofOption(node : XmlNode option) =
        node
        |> Option.defaultValue (rawText "")
