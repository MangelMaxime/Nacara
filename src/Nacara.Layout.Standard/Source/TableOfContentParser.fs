[<RequireQualifiedAccess>]
module TableOfContentParser

open System.Text.RegularExpressions

type HeaderInfo =
    {
        Title : string
        Link : string
    }

type Header =
    | Header2 of HeaderInfo
    | Header3 of HeaderInfo
    | Header4 of HeaderInfo
    | Header5 of HeaderInfo
    | Header6 of HeaderInfo

type TableOfContent = Header list

let private isNotNull (o : 'T) =
   not (isNull o)

let private genHeaderMatcher
    (rank : int)
    (mapper : HeaderInfo -> Header)
    (m : Match) =

    if isNotNull m.Groups.[$"header_%i{rank}"] then
        {
            Title = m.Groups.[$"h%i{rank}_text"].Value
            Link = m.Groups.[$"h%i{rank}_link"].Value
        }
        |> mapper
        |> Some
    else
        None

let private (|Header2|_|) (m : Match) : option<Header> =
    genHeaderMatcher 2 Header.Header2 m

let private (|Header3|_|) (m : Match) : option<Header> =
    genHeaderMatcher 3 Header.Header3 m

let private (|Header4|_|) (m : Match) : option<Header> =
    genHeaderMatcher 4 Header.Header4 m

let private (|Header5|_|) (m : Match) : option<Header> =
    genHeaderMatcher 5 Header.Header5 m

let private (|Header6|_|) (m : Match) : option<Header> =
    genHeaderMatcher 6 Header.Header6 m

let private generateHeaderPattern (rank : int)=
    // $"""(?<header_%i{rank}>(<h%i{rank}[^>]*>(?<h%i{rank}_text>((?!<\/h%i{rank}>).)*)<a[^>]*href="(?<h%i{rank}_link>[^"]*)"((?!<\/h%i{rank}>).)*<\/h%i{rank}>))"""
    $"""(?<header_%i{rank}>(<h%i{rank}[^>]*>(?<h%i{rank}_text>((?!<\/h%i{rank}>).)*)<a[^>]*href="?(?<h%i{rank}_link>[^" >]*)"?((?!<\/h%i{rank}>).)*<\/h%i{rank}>))"""


// Note: TableOfContent parser only extract information from h2 elements
let parse (pageContent : string) =
    let pattern =
        [
            generateHeaderPattern 2
            generateHeaderPattern 3
            generateHeaderPattern 4
            generateHeaderPattern 5
            generateHeaderPattern 6
        ]
        |> String.concat ("|")

    Regex.Matches(pageContent, pattern, RegexOptions.Singleline)
    |> Seq.cast<Match>
    |> Seq.toList
    |> List.choose (fun m ->
        match m with
        | Header2 x -> Some x
        | Header3 x -> Some x
        | Header4 x -> Some x
        | Header5 x -> Some x
        | Header6 x -> Some x
        | _ -> None // Doesn't happen but F# force us to handle it
    )
