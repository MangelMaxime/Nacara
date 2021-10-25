module StringBuilder.Extensions

open System.Text

type StringBuilder with
    member this.Write(text : string) =
        this.Append(text)
        |> ignore

    member this.WriteLine(text : string) =
        this.AppendLine(text)
        |> ignore

    member this.NewLine() =
        this.AppendLine()
        |> ignore

    member this.Indent(?factor : int) =
        let factor = defaultArg factor 1

        this.Space(factor * 4)
        |> ignore

    member this.Space(?factor : int) =
        let factor = defaultArg factor 1

        let text = String.replicate factor "&nbsp;"

        this.Append(text)
        |> ignore
