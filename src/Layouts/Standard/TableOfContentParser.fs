[<RequireQualifiedAccess>]
module TableOfContentParser

open Fable.Core
open System
open System.Text.RegularExpressions
open Types

[<Emit("$0.split(/\\r\\n|\\r|\\n/)")>]
let splitLines (_text : string) : string array = jsNative

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
    let m = Regex.Match(input, pattern)
    if m.Success then
        Some m
    else
        None

let private (|Header2|_|) (input : string) : option<Header> =
    match input with
    | Match """<h2>(.*)<a href="([^"]*)".*<\/h2>""" m ->
        {
            Title = m.Groups.[1].Value
            Link = m.Groups.[2].Value
        }
        |> Some
    | _ -> None

let private (|Header3|_|) (input : string) : option<Header> =
    match input with
    | Match """<h3>(.*)<a href="([^"]*)".*<\/h3>""" m ->
        {
            Title = m.Groups.[1].Value
            Link = m.Groups.[2].Value
        }
        |> Some
    | _ -> None

let rec private extractSection (pageContext : PageContext) (acc : Section option) (res : TableOfContent) (lines : string list) =
    match lines with
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

                extractSection pageContext (Some newSection) (res @ [section]) tail

            // If this is the first header2 start tracking it
            | None ->
                let newSection : Section =
                    {
                        Header = header
                        SubSections = []
                    }

                extractSection pageContext (Some newSection) res tail

        | Header3 header ->
            match acc with
            // If an header3 is found inside an header2, add it to the SubSections
            | Some section ->
                let section =
                    { section with
                        SubSections = section.SubSections @ [ header ]
                    }

                extractSection pageContext (Some section) res tail

            // If an header3 is found outside of an header2, print an error as this should not be the case
            | None ->
                failwithf "Error in the file: %s\nAn h3 element should be the child of an h2 element" pageContext.Path

        // Not a h2 or h3 continue the parsing
        | _ ->
            extractSection pageContext acc res tail

    // No more line to handle
    | [ ] ->
        // Close the current section if there is one and return the result
        match acc with
        | Some section ->
            res @ [section]

        | None ->
            res

// Note: TableOfContent parser only extract information from h2 and h3 elements
let parse (pageContext : PageContext) =
    splitLines pageContext.Content
    |> Array.toList
    |> extractSection pageContext None [ ]
