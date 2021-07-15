[<RequireQualifiedAccess>]
module TableOfContentParser

open Fable.Core
open System
open System.Text.RegularExpressions
open Types

// [<Emit("$0.split(/\\r\\n|\\r|\\n/)")>]
// let splitLines (_text : string) : string array = jsNative

type Header =
    {
        Title : string
        Link : string
    }

// Represents an Header2
type Section =
    {
        Header : Header
        SubSections : Header list
    }

type TableOfContent = Section list


let private (|Match|_|) pattern input =
    let m = Regex.Match(input, pattern, RegexOptions.Singleline)
    if m.Success then
        Some m
    else
        None

// let private (|Header2|_|) (input : string) : option<Header> =
//     match input with
//     | Match """<h2[^>]*>(((?!<\/h2>).)*)<a((?!<\/h2>).)*<\/h2>""" m ->
//         {
//             Title = m.Groups.[1].Value
//             Link = m.Groups.[2].Value
//         }
//         |> Some
//     | _ -> None

// let private (|Header3|_|) (input : string) : option<Header> =
//     match input with
//     | Match """<h3[^>]*>(((?!<\/h3>).)*)<a((?!<\/h3>).)*<\/h3>""" m ->
//         {
//             Title = m.Groups.[1].Value
//             Link = m.Groups.[2].Value
//         }
//         |> Some
//     | _ -> None


let private isNotNull (o : 'T) =
   not (isNull o)

let private (|Header2|_|) (m : Match) : option<Header> =
    if isNotNull m.Groups.[1] then
        {
            Title = m.Groups.["h2_text"].Value
            Link = m.Groups.["h2_link"].Value
        }
        |> Some
    else
        None

let private (|Header3|_|) (m : Match) : option<Header> =
    if isNotNull m.Groups.[1] then
        {
            Title = m.Groups.["h3_text"].Value
            Link = m.Groups.["h3_link"].Value
        }
        |> Some
    else
        None

let rec private extractSection (filePath : string) (acc : Section option) (res : TableOfContent) (matches : Match list) =
    match matches with
    | head :: tail ->
        match head with
        | Header2 header ->
            // If we find an header2 and another header2 is already in process
            // Close put the current header2 into the result and start tracking the new header2
            match acc with
            | Some section ->
                let newSection : Section =
                    {
                        Header = header
                        SubSections = []
                    }

                extractSection filePath (Some newSection) (res @ [section]) tail

            // If this is the first header2 start tracking it
            | None ->
                let newSection : Section =
                    {
                        Header = header
                        SubSections = []
                    }

                extractSection filePath (Some newSection) res tail

        | Header3 header ->
            match acc with
            // If an header3 is found inside an header2, add it to the SubSections
            | Some section ->
                let section =
                    { section with
                        SubSections = section.SubSections @ [ header ]
                    }

                extractSection filePath (Some section) res tail

            // If an header3 is found outside of an header2, print an error as this should not be the case
            | None ->
                failwithf "Error in the file: %s\nAn h3 element should be the child of an h2 element" filePath

        // Not a h2 or h3 continue the parsing
        | _ ->
            extractSection filePath acc res tail

    // No more line to handle
    | [ ] ->
        // Close the current section if there is one and return the result
        match acc with
        | Some section ->
            res @ [section]

        | None ->
            res

// Note: TableOfContent parser only extract information from h2 and h3 elements
let parse (pageContent : string) (filePath : string) =
    let pattern = """(<h2[^>]*>(?<h2_text>((?!<\/h2>).)*)<a\s*href="(?<h2_link>[^"]*)"((?!<\/h2>).)*<\/h2>)|(<h3[^>]*>(?<h3_text>((?!<\/h3>).)*)<a\s*href="(?<h3_link>[^"]*)"((?!<\/h3>).)*<\/h3>)"""

    Regex.Matches(pageContent, pattern, RegexOptions.Singleline)
    |> Seq.cast<Match>
    |> Seq.toList
    |> extractSection filePath None [ ]
