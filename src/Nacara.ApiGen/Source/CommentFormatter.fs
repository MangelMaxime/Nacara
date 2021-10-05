module Nacara.ApiGen.CommentFormatter

open System
open System.Text.RegularExpressions

let inline nl<'T> = Environment.NewLine

let private tagPattern (tagName : string) =
    sprintf """(?'void_element'<%s(?'void_attributes'\s+[^\/>]+)?\/>)|(?'non_void_element'<%s(?'non_void_attributes'\s+[^>]+)?>(?'non_void_innerText'(?:(?!<%s>)(?!<\/%s>)[\s\S])*)<\/%s\s*>)""" tagName tagName tagName tagName tagName

type private TagInfo =
    | VoidElement of attributes : Map<string, string>
    | NonVoidElement of innerText : string * attributes : Map<string, string>

[<NoEquality;NoComparison>]
type private FormatterInfo =
    {
        TagName : string
        Formatter : TagInfo -> string option
    }

let private extractTextFromQuote (quotedText : string) =
    quotedText.Substring(1, quotedText.Length - 2)

let private extractMemberText (text : string) =
    let pattern = "(?'member_type'[a-z]{1}:)?(?'member_text'.*)"
    let m = Regex.Match(text, pattern, RegexOptions.IgnoreCase)

    if m.Groups.["member_text"].Success then
        m.Groups.["member_text"].Value
    else
        text

let private getAttributes (attributes : Group) =
    if attributes.Success then
        let pattern = """(?'key'\S+)=(?'value''[^']*'|"[^"]*")"""
        Regex.Matches(attributes.Value, pattern, RegexOptions.IgnoreCase)
        |> Seq.cast<Match>
        |> Seq.map (fun m ->
            m.Groups.["key"].Value, extractTextFromQuote m.Groups.["value"].Value
        )
        |> Map.ofSeq
    else
        Map.empty

let rec private applyFormatter (info : FormatterInfo) text =
    let pattern = tagPattern info.TagName
    match Regex.Match(text, pattern, RegexOptions.IgnoreCase) with
    | m when m.Success ->
        if m.Groups.["void_element"].Success then
            let attributes = getAttributes m.Groups.["void_attributes"]

            let replacement =
                VoidElement attributes
                |> info.Formatter

            match replacement with
            | Some replacement ->
                text.Replace(m.Groups.["void_element"].Value, replacement)
                // Re-apply the formatter, because perhaps there is more
                // of the current tag to convert
                |> applyFormatter info

            | None ->
                // The formatter wasn't able to convert the tag
                // Return as it is and don't re-apply the formatter
                // otherwise it will create an infinity loop
                text

        else if m.Groups.["non_void_element"].Success then
            let innerText = m.Groups.["non_void_innerText"].Value
            let attributes = getAttributes m.Groups.["non_void_attributes"]

            let replacement =
                NonVoidElement (innerText, attributes)
                |> info.Formatter

            match replacement with
            | Some replacement ->
                // Re-apply the formatter, because perhaps there is more
                // of the current tag to convert
                text.Replace(m.Groups.["non_void_element"].Value, replacement)
                |> applyFormatter info

            | None ->
                // The formatter wasn't able to convert the tag
                // Return as it is and don't re-apply the formatter
                // otherwise it will create an infinity loop
                text
        else
            // Should not happend but like that we are sure to handle all possible cases
            text
    | _ ->
        text

let private codeBlock =
    {
        TagName = "code"
        Formatter =
            function
            | VoidElement _ ->
                None

            | NonVoidElement (innerText, attributes) ->
                let lang =
                    match Map.tryFind "lang" attributes with
                    | Some lang ->
                        lang

                    | None ->
                        "forceNoHighlight"

                let formattedText =
                    if innerText.Contains("\n") then

                        if innerText.StartsWith("\n") then

                            sprintf "```%s%s\n```" lang innerText

                        else
                            sprintf "```%s\n%s\n```" lang innerText

                    else
                        sprintf "`%s`" innerText

                Some formattedText

    }
    |> applyFormatter

let private codeInline =
    {
        TagName = "c"
        Formatter  =
            function
            | VoidElement _ ->
                None
            | NonVoidElement (innerText, _) ->
                "`" + innerText + "`"
                |> Some
    }
    |> applyFormatter

let private anchor =
    {
        TagName = "a"
        Formatter =
            function
            | VoidElement _ ->
                None

            | NonVoidElement (innerText, attributes) ->
                let href =
                    match Map.tryFind "href" attributes with
                    | Some href ->
                        href

                    | None ->
                        ""

                sprintf "[%s](%s)" innerText href
                |> Some
    }
    |> applyFormatter

let private paragraph =
    {
        TagName = "para"
        Formatter =
            function
            | VoidElement _ ->
                None

            | NonVoidElement (innerText, _) ->
                nl + innerText + nl
                |> Some
    }
    |> applyFormatter

let private block =
    {
        TagName = "block"
        Formatter  =
            function
            | VoidElement _ ->
                None

            | NonVoidElement (innerText, _) ->
                nl + innerText + nl
                |> Some
    }
    |> applyFormatter

let private see =
    let getCRef (attributes : Map<string, string>) = Map.tryFind "cref" attributes
    {
        TagName = "see"
        Formatter =
            function
            | VoidElement attributes ->
                match getCRef attributes with
                | Some cref ->
                    // TODO: Add config to generates command
                    "`" + extractMemberText cref + "`"
                    |> Some

                | None ->
                    None

            | NonVoidElement (innerText, attributes) ->
                if String.IsNullOrWhiteSpace innerText then
                    match getCRef attributes with
                    | Some cref ->
                        // TODO: Add config to generates command
                        "`" + extractMemberText cref + "`"
                        |> Some

                    | None ->
                        None
                else
                    "`" + innerText + "`"
                    |> Some
    }
    |> applyFormatter

let private xref =
    let getHRef (attributes : Map<string, string>) = Map.tryFind "href" attributes
    {
        TagName = "xref"
        Formatter =
            function
            | VoidElement attributes ->
                match getHRef attributes with
                | Some href ->
                    // TODO: Add config to generates command
                    "`" + extractMemberText href + "`"
                    |> Some

                | None ->
                    None

            | NonVoidElement (innerText, attributes) ->
                if String.IsNullOrWhiteSpace innerText then
                    match getHRef attributes with
                    | Some href ->
                        // TODO: Add config to generates command
                        "`" + extractMemberText href + "`"
                        |> Some

                    | None ->
                        None
                else
                    "`" + innerText + "`"
                    |> Some
    }
    |> applyFormatter

let private paramRef =
    let getName (attributes : Map<string, string>) = Map.tryFind "name" attributes

    {
        TagName = "paramref"
        Formatter =
            function
            | VoidElement attributes ->
                match getName attributes with
                | Some name ->
                    "`" + name + "`"
                    |> Some

                | None ->
                    None

            | NonVoidElement (innerText, attributes) ->
                if String.IsNullOrWhiteSpace innerText then
                    match getName attributes with
                    | Some name ->
                        // TODO: Add config to generates command
                        "`" + name + "`"
                        |> Some

                    | None ->
                        None
                else
                    "`" + innerText + "`"
                    |> Some

    }
    |> applyFormatter

let private typeParamRef =
    let getName (attributes : Map<string, string>) = Map.tryFind "name" attributes

    {
        TagName = "typeparamref"
        Formatter =
            function
            | VoidElement attributes ->
                match getName attributes with
                | Some name ->
                    "`" + name + "`"
                    |> Some

                | None ->
                    None

            | NonVoidElement (innerText, attributes) ->
                if String.IsNullOrWhiteSpace innerText then
                    match getName attributes with
                    | Some name ->
                        // TODO: Add config to generates command
                        "`" + name + "`"
                        |> Some

                    | None ->
                        None
                else
                    "`" + innerText + "`"
                    |> Some
    }
    |> applyFormatter

let private convertTable =
    {
        TagName = "table"
        Formatter =
            function
            | VoidElement _ ->
                None

            | NonVoidElement (innerText, _) ->

                let rowCount = Regex.Matches(innerText, "<th\s?>").Count
                let convertedTable =
                    innerText
                        .Replace(nl, "")
                        .Replace("\n", "")
                        .Replace("<table>", "")
                        .Replace("</table>", "")
                        .Replace("<thead>", "")
                        .Replace("</thead>", (String.replicate rowCount "| --- "))
                        .Replace("<tbody>", nl)
                        .Replace("</tbody>", "")
                        .Replace("<tr>", "")
                        .Replace("</tr>", "|" + nl)
                        .Replace("<th>", "|")
                        .Replace("</th>", "")
                        .Replace("<td>", "|")
                        .Replace("</td>", "")

                nl + nl + convertedTable + nl
                |> Some

    }
    |> applyFormatter

type private Term = string
type private Definition = string

type private ListStyle =
    | Bulleted
    | Numbered
    | Tablered

/// ItemList allow a permissive representation of an Item.
/// In theory, TermOnly should not exist but we added it so part of the documentation doesn't disappear
/// TODO: Allow direct text support without <description> and <term> tags
type private ItemList =
    /// A list where the items are just contains in a <description> element
    | DescriptionOnly of string
    /// A list where the items are just contains in a <term> element
    | TermOnly of string
    /// A list where the items are a term followed by a definition (ie in markdown: * <TERM> - <DEFINITION>)
    | Definitions of Term * Definition

let private itemListToStringAsMarkdownList (prefix : string) (item : ItemList) =
    match item with
    | DescriptionOnly description ->
        prefix + " " + description
    | TermOnly term ->
        prefix + " " + "**" + term + "**"
    | Definitions (term, description) ->
        prefix + " " + "**" + term + "** - " + description

let private list =
    let getType (attributes : Map<string, string>) = Map.tryFind "type" attributes

    let tryGetInnerTextOnNonVoidElement (text : string) (tagName : string) =
        match Regex.Match(text, tagPattern tagName, RegexOptions.IgnoreCase) with
        | m when m.Success ->
            if m.Groups.["non_void_element"].Success then
                Some m.Groups.["non_void_innerText"].Value
            else
                None
        | _ ->
            None

    let tryGetNonVoidElement (text : string) (tagName : string) =
        match Regex.Match(text, tagPattern tagName, RegexOptions.IgnoreCase) with
        | m when m.Success ->
            if m.Groups.["non_void_element"].Success then
                Some (m.Groups.["non_void_element"].Value, m.Groups.["non_void_innerText"].Value)
            else
                None
        | _ ->
            None

    let tryGetDescription (text : string) = tryGetInnerTextOnNonVoidElement text "description"

    let tryGetTerm (text : string) = tryGetInnerTextOnNonVoidElement text "term"

    let rec extractItemList (res : ItemList list) (text : string) =
        match Regex.Match(text, tagPattern "item", RegexOptions.IgnoreCase) with
        | m when m.Success ->
            let newText = text.Substring(m.Value.Length)
            if m.Groups.["non_void_element"].Success then
                let innerText = m.Groups.["non_void_innerText"].Value
                let description = tryGetDescription innerText
                let term = tryGetTerm innerText

                let currentItem : ItemList option =
                    match description, term with
                    | Some description, Some term ->
                        Definitions (term, description)
                        |> Some
                    | Some description, None ->
                        DescriptionOnly description
                        |> Some
                    | None, Some term ->
                        TermOnly term
                        |> Some
                    | None, None ->
                        None

                match currentItem with
                | Some currentItem ->
                    extractItemList (res @ [ currentItem ]) newText
                | None ->
                    extractItemList res newText
            else
                extractItemList res newText
        | _ ->
            res

    let rec extractColumnHeader (res : string list) (text : string) =
        match Regex.Match(text, tagPattern "listheader", RegexOptions.IgnoreCase) with
        | m when m.Success ->
            let newText = text.Substring(m.Value.Length)
            if m.Groups.["non_void_element"].Success then
                let innerText = m.Groups.["non_void_innerText"].Value

                let rec extractAllTerms (res : string list) (text : string) =
                    match tryGetNonVoidElement text "term" with
                    | Some (fullString, innerText) ->
                        let escapedRegex = Regex(Regex.Escape(fullString))
                        let newText = escapedRegex.Replace(text, "", 1)
                        extractAllTerms (res @ [ innerText ]) newText
                    | None ->
                        res

                extractColumnHeader (extractAllTerms [] innerText) newText
            else
                extractColumnHeader res newText
        | _ ->
            res


    let rec extractRowsForTable (res : (string list) list) (text : string) =
        match Regex.Match(text, tagPattern "item", RegexOptions.IgnoreCase) with
        | m when m.Success ->
            let newText = text.Substring(m.Value.Length)
            if m.Groups.["non_void_element"].Success then
                let innerText = m.Groups.["non_void_innerText"].Value

                let rec extractAllTerms (res : string list) (text : string) =
                    match tryGetNonVoidElement text "term" with
                    | Some (fullString, innerText) ->
                        let escapedRegex = Regex(Regex.Escape(fullString))
                        let newText = escapedRegex.Replace(text, "", 1)
                        extractAllTerms (res @ [ innerText ]) newText
                    | None ->
                        res

                extractRowsForTable (res @ [extractAllTerms [] innerText]) newText
            else
                extractRowsForTable res newText
        | _ ->
            res

    {
        TagName = "list"
        Formatter =
            function
            | VoidElement _ ->
                None

            | NonVoidElement (innerText, attributes) ->
                let listStyle =
                    match getType attributes with
                    | Some "bullet" -> Bulleted
                    | Some "number" -> Numbered
                    | Some "table" -> Tablered
                    | Some _ | None -> Bulleted

                match listStyle with
                | Bulleted ->
                    let items = extractItemList [] innerText

                    items
                    |> List.map (itemListToStringAsMarkdownList "*")
                    |> String.concat Environment.NewLine

                | Numbered ->
                    let items = extractItemList [] innerText

                    items
                    |> List.map (itemListToStringAsMarkdownList "1.")
                    |> String.concat Environment.NewLine

                | Tablered ->
                    let columnHeaders = extractColumnHeader [] innerText
                    let rows = extractRowsForTable [] innerText

                    let columnHeadersText =
                        columnHeaders
                        |> List.mapi (fun index header ->
                            if index = 0 then
                                "| " + header
                            elif index = columnHeaders.Length - 1 then
                                " | " + header + " |"
                            else
                                " | " + header
                        )
                        |> String.concat ""

                    let seprator =
                        columnHeaders
                        |> List.mapi (fun index _ ->
                            if index = 0 then
                                "| ---"
                            elif index = columnHeaders.Length - 1 then
                                " | --- |"
                            else
                                " | ---"
                        )
                        |> String.concat ""

                    let itemsText =
                        rows
                        |> List.map (fun columns ->
                            columns
                            |> List.mapi (fun index column ->
                                if index = 0 then
                                    "| " + column
                                elif index = columnHeaders.Length - 1 then
                                    " | " + column + " |"
                                else
                                    " | " + column
                            )
                            |> String.concat ""
                        )
                        |> String.concat Environment.NewLine

                    Environment.NewLine
                    + columnHeadersText
                    + Environment.NewLine
                    + seprator
                    + Environment.NewLine
                    + itemsText
                |> Some
    }
    |> applyFormatter

/// <summary>
/// Unescape XML special characters
///
/// For example, this allows to print '>' in the tooltip instead of '&gt;'
/// </summary>
let private unescapeSpecialCharacters (text : string) =
    text.Replace("&lt;", "<")
        .Replace("&gt;", ">")
        .Replace("&quot;", "\"")
        .Replace("&apos;", "'")
        .Replace("&amp;", "&")

let applyAll (text : string) =
    text
    |> paragraph
    |> block
    |> codeInline
    |> codeBlock
    |> see
    |> xref
    |> paramRef
    |> typeParamRef
    |> anchor
    |> list
    |> convertTable
    |> unescapeSpecialCharacters
