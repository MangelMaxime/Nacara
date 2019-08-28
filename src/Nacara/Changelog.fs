[<RequireQualifiedAccess>]
module Changelog

open Fable.Core
open System
open System.Text.RegularExpressions

[<Emit("return $0.split(/\\r\\n|\\r|\\n/)")>]
let splitLines (_text : string) : string array = jsNative

[<AutoOpen>]
module Types =

    type CategoryBody =
        | ListItem of string
        | Text of string

    [<RequireQualifiedAccess>]
    type CategoryType =
        | Added
        | Changed
        | Deprecated
        | Removed
        | Fixed
        | Security
        | Unkown of string

        member this.Text
            with get () =
                match this with
                | Added -> "Added"
                | Changed -> "Changed"
                | Deprecated -> "Deprecated"
                | Removed -> "Removed"
                | Fixed -> "Fixed"
                | Security -> "Security"
                | Unkown tag -> tag

    type Version =
        { Version : string option
          Title : string
          Date : DateTime option
          Categories : Map<CategoryType, CategoryBody list> }

    type Changelog =
        { Title : string
          Description : string
          Versions : Version list }

        static member Empty =
            { Title = ""
              Description = ""
              Versions = [] }

    type Symbols =
        | Title of title : string
        | RawText of body : string
        | SectionHeader of title : string * version : string option * date : string option
        | SubSection of tag : string
        | ListItem of content : string


[<RequireQualifiedAccess>]
module Lexer =

    let private (|Match|_|) pattern input =
        let m = Regex.Match(input, pattern)
        if m.Success then
            Some m
        else
            None

    let private (|Title|_|) (input : string) =
        match input with
        | Match "^# ?[^#]" _ ->
            input.Substring(1).Trim()
            |> Some
        | _ -> None

    let private (|Semver|_|) (input : string) =
        match input with
        | Match "\\[?v?([\\w\\d.-]+\\.[\\w\\d.-]+[a-zA-Z0-9])\\]?" m ->
            Some m.Groups.[1].Value
        | _ ->
            None

    let private (|Date|_|) (input : string) =
        match input with
        | Match "(\\d{4}-\\d{2}-\\d{2})" m ->
            Some m.Groups.[0].Value
        | _ -> None

    let private (|Version|_|) (input : string) =
        match input with
        | Match "^##? ?[^#]" _ ->
            let version =
                match input with
                | Semver version -> Some version
                | _ -> None

            let date =
                match input with
                | Date date -> Some date
                | _ -> None

            let title = input.Substring(2).Trim()

            Some (title, version, date)

        | _ -> None

    let private (|SubSection|_|) (input : string) =
        match input with
        | Match "^###" _ ->
            input.Substring(3).Trim()
            |> Some
        | _ -> None

    let private (|ListItem|_|) (input : string) =
        match input with
        | Match "^[*-]" _ ->
            input.Substring(1).Trim()
            |> Some
        | _ -> None

    let toSymbols (lines : string list) =
        lines
        |> List.map (function
            | Title title -> Symbols.Title title
            | Version (title, version, date) -> Symbols.SectionHeader (title, version, date)
            | SubSection tag -> Symbols.SubSection tag
            | ListItem content -> Symbols.ListItem content
            | rawText -> Symbols.RawText (rawText.TrimEnd())
        )

[<RequireQualifiedAccess>]
module Transform =

    let rec private parseCategoryBody (symbols : Symbols list) (sectionContent : CategoryBody list) =
        match symbols with
        | Symbols.ListItem item::tail ->
            parseCategoryBody tail (sectionContent @ [ CategoryBody.ListItem item ])
        // If this is the beginning of a text block
        | Symbols.RawText _::_ ->
            // Capture all the lines of the text block
            let textLines =
                symbols
                |> List.takeWhile (function
                    | Symbols.RawText _ -> true
                    | _ -> false
                )

            // Regroupe everything into a single string
            let content =
                textLines
                |> List.map (function
                    | Symbols.RawText text -> text
                    | _ -> failwith "Should not happen the list has been filtered"
                )
                |> String.concat "\n"

            // Remove already handle symbols
            let rest =
                symbols
                |> List.skip textLines.Length

            parseCategoryBody rest (sectionContent @ [ CategoryBody.Text content ])
        // End of the Section, return the built content
        | _ -> symbols, sectionContent

    let rec private parse (symbols : Symbols list) (changelog : Changelog) =
        match symbols with
        | Symbols.Title title::tail ->
            if String.IsNullOrEmpty changelog.Title then
                { changelog with Title = title }
            else
                Log.warnFn "Title has already been filled."
                Log.warnFn "Discarding: %s" title
                changelog
            |> parse tail

        | Symbols.SectionHeader (title, version, date)::tail ->
            let version =
                { Version = version
                  Title = title
                  Date = date |> Option.map DateTime.Parse
                  Categories = Map.empty }

            parse tail { changelog with Versions = version :: changelog.Versions }

        | Symbols.SubSection tag::tail ->
            let (unparsedSymbols, categoryBody) = parseCategoryBody tail []

            let categoryType =
                match tag.ToLower() with
                | "added" -> CategoryType.Added
                | "changed" -> CategoryType.Changed
                | "deprecated" -> CategoryType.Deprecated
                | "removed" -> CategoryType.Removed
                | "fixed" -> CategoryType.Fixed
                | "security" -> CategoryType.Security
                | unkown -> CategoryType.Unkown unkown

            match changelog.Versions with
            | currentVersion::otherVersions ->
                let categoryBody =
                    match Map.tryFind categoryType currentVersion.Categories with
                    | Some existingBody ->
                        existingBody @ categoryBody
                    | None -> categoryBody

                let updatedCategories = currentVersion.Categories.Add(categoryType, categoryBody)
                let versions = { currentVersion with Categories = updatedCategories } :: otherVersions
                parse unparsedSymbols { changelog with Versions = versions }
            | _ ->
                Error "A category should always be under a version"

        | Symbols.RawText _::_ ->
            // Capture all the lines of the text block
            let textLines =
                symbols
                |> List.takeWhile (function
                    | Symbols.RawText _ -> true
                    | _ -> false
                )

            // Regroupe everything into a single string
            let content =
                textLines
                |> List.map (function
                    | Symbols.RawText text -> text
                    | _ -> failwith "Should not happen the list has been filtered"
                )
                |> String.concat "\n"

            // Remove already handle symbols
            let rest =
                symbols
                |> List.skip textLines.Length

            parse rest { changelog with Description = content }

        | Symbols.ListItem text ::_ ->
            sprintf "A list item should always be under a category. The next list item made the parser failed:\n\n%s\n" text
            |> Error

        | [] -> Ok { changelog with Versions = changelog.Versions |> List.rev }

    let fromSymbols (symbols : Symbols list) =
        parse symbols Changelog.Empty

let parse (changelogContent : string) =
    splitLines changelogContent
    |> Array.toList
    |> Lexer.toSymbols
    |> Transform.fromSymbols

// let test () =
//     promise {
//         let! changelogContent = File.read "CHANGELOG.md"

//         match parse changelogContent with
//         | Ok changelog ->

//             printfn "Title: %s" changelog.Title
//             printfn "Description: %s" changelog.Description

//             changelog.Versions
//             |> List.iter (fun version ->
//                 printfn "%A" version.Version
//                 printfn "%A" version.Categories
//             )
//         | Error msg ->
//             Log.error "%s" msg
//             // Crash the program
//             Fable.Import.Node.Globals.``process``.exit(1)
//             |> unbox
//     }
