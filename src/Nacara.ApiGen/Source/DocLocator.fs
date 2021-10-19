namespace Nacara.ApiGen

open FSharp.Formatting.ApiDocs
open Helpers
open System.IO
open System.Text.RegularExpressions
open FSharp.Compiler.Symbols

type DocLocator(xmlFilePath : string) =

    let xmlDocContent =
        use reader = new StreamReader(xmlFilePath)

        reader.ReadToEnd()

    let rec findIndentationSize (lines : string list) =
        match lines with
        | head::tail ->
            let lesserThanIndex = head.IndexOf("<")
            if lesserThanIndex <> - 1 then
                lesserThanIndex
            else
                findIndentationSize tail
        | [] -> 0

    member this.TryFindComment(apiDocMember : ApiDocMember) =
        let prefix =
            // Is the translation correct?
            match apiDocMember.Kind with
            | ApiDocMemberKind.ActivePattern
            | ApiDocMemberKind.Constructor
            | ApiDocMemberKind.InstanceMember
            | ApiDocMemberKind.StaticMember ->
                "M:"
            | ApiDocMemberKind.ValueOrFunction
            | ApiDocMemberKind.RecordField ->
                "P:"
            | ApiDocMemberKind.StaticParameter ->
                "F:"
            | ApiDocMemberKind.UnionCase
            | ApiDocMemberKind.TypeExtension ->
                "T:"
            | _ ->
                "!"

        try
            // Try to cast the symbol to get access to the XmlDocSig
            let entity = apiDocMember.Symbol :?> FSharpMemberOrFunctionOrValue

            this.TryFindComment entity.XmlDocSig

        with
            | _ ->
                let xmlDocSig =
                    prefix + apiDocMember.Symbol.FullName

                this.TryFindComment(xmlDocSig)

    member _.TryFindComment(searchedValue : string) =

        let searchedValue =
            Regex.Escape searchedValue

        let pattern =
            $"""<member name="%s{searchedValue}">((?'xml_doc'(?:(?!<member>)(?!<\/member>)[\s\S])*)<\/member\s*>)"""

        let m = Regex.Match(xmlDocContent, pattern, RegexOptions.Singleline)

        if m.Success then
            let xmlDoc = m.Groups.["xml_doc"].Value

            let lines =
                xmlDoc
                |> String.splitLines
                |> Array.toList

            let indentationSize =
                lines
                |> findIndentationSize

            // Remove the non meaning full indentation
            let content =
                let indentationText =
                    String.replicate indentationSize " "

                lines
                |> List.map (fun line ->
                    // Add a small protection in case the user didn't align all it's tags
                    if line.StartsWith(indentationText) then
                        line.Substring(indentationSize)
                    else
                        line
                )
                |> String.concat "\n"

            Some content
        else
            None
