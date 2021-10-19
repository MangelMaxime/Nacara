module Helpers

module String =

    let normalizeEndOfLine (text : string)=
        text.Replace("\r\n", "\n")

    let splitBy (c : char) (text : string) =
        text.Split(c)

    let splitLines (text : string) =
        text
        |> normalizeEndOfLine
        |> splitBy '\n'

    let toLower (text : string) =
        text.ToLower()

    let replace (oldValue : string) (newValue : string) (text : string) =
        text.Replace(oldValue, newValue)

    let append (value : string) (text : string) =
        text + value

module List =

    let intersperse (element : 'T) (source : 'T list) =
        [
            for item in source do
                item
                element
        ]
