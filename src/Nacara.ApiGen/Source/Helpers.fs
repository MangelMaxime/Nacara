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

    let trimEnd (value : string) =
        value.TrimEnd()

module List =

    let intersperse (separator : 'T) (source : 'T list) =
        [
            let mutable notFirst = false
            for element in source do
                if notFirst then
                    separator
                element
                notFirst <- true
        ]

    let intercalate (separator : 'T list) (source : 'T list) =
        [
            let mutable notFirst = false
            for element in source do
                if notFirst then
                    yield! separator

                element
                notFirst <- true
        ]
