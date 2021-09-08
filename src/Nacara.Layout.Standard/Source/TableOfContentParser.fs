[<RequireQualifiedAccess>]
module TableOfContentParser

open System.Text.RegularExpressions

type Header =
    { Title : string
      Link : string }

type TableOfContent = Header list

// Note: TableOfContent parser only extract information from h2 elements
let parse (pageContent : string) =
    let pattern = """(<h2[^>]*>(?<h2_text>((?!<\/h2>).)*)<a\s*href="(?<h2_link>[^"]*)"((?!<\/h2>).)*<\/h2>)"""

    Regex.Matches(pageContent, pattern, RegexOptions.Singleline)
    |> Seq.cast<Match>
    |> Seq.toList
    |> List.choose (fun m ->
        if m.Success then
            {
                Title = m.Groups.["h2_text"].Value
                Link = m.Groups.["h2_link"].Value
            }
            |> Some
        else
            None)
