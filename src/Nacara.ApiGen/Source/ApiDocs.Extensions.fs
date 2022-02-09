module FSharp.Formatting.ApiDocs

open FSharp.Formatting.ApiDocs
open System.Text.RegularExpressions
open Helpers

type ApiDocComment with
    member this.TryGetXmlText with get () =
        this.Xml
        |> Option.map (fun comment ->
            let docComment = comment.ToString()

            let pattern =
                $"""<member name=".*">((?'xml_doc'(?:(?!<member>)(?!<\/member>)[\s\S])*)<\/member\s*>)"""

            let m = Regex.Match(docComment, pattern)

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

                content
            else
                docComment
        )

    member this.XmlText with get () =
        this.TryGetXmlText
        |> Option.defaultValue ""
