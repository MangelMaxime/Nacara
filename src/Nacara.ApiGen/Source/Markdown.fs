module rec Markdown

open System
open System.Collections.Generic
open Giraffe.ViewEngine

let startMarkdownBlock = str "\n\n"

// Code adapted from FSharp.Formatting
// To make it easier to write Markdown AST in F# and then transform it into a string
// It mostly remove the Range information etc. as we are not parsing markdown but
// creating the Markdown AST ourself

// See later if we can merge back with F# Formatting
// As discussed quickly with Don Sym, it would be nice if Nacara way of structuring
// the documentation could go back to F# Formatting
// https://github.com/MangelMaxime/Nacara/issues/123#issuecomment-965648281

/// <summary>
///   A list kind can be Ordered or Unordered corresponding to <c>&lt;ol&gt;</c> and <c>&lt;ul&gt;</c> elements
/// </summary>
type MarkdownListKind =
    | Ordered
    | Unordered

/// Column in a table can be aligned to left, right, center or using the default alignment
type MarkdownColumnAlignment =
    | AlignLeft
    | AlignRight
    | AlignCenter
    | AlignDefault

[<NoComparison>]
type MarkdownSpan =
    | Literal of text: string
    | InlineCode of code: string
    | Strong of body: MarkdownSpans
    | Emphasis of body: MarkdownSpans
    | DirectLink of body: MarkdownSpans * link: string * title: string option
    | IndirectLink of body: MarkdownSpans * original: string * key: string
    | DirectImage of body: string * link: string * title: string option
    | IndirectImage of body: string * link: string * key: string
    | HardLineBreak

/// A type alias for a list of MarkdownSpan values
type MarkdownSpans = MarkdownSpan list

/// A paragraph represents a (possibly) multi-line element of a Markdown document.
/// Paragraphs are headings, inline paragraphs, code blocks, lists, quotations, tables and
/// also embedded LaTeX blocks.
[<NoComparison>]
type MarkdownParagraph =
    | Heading of size: int * body: MarkdownSpans
    | Paragraph of body: MarkdownSpans

    /// A code block, whether fenced or via indentation
    | CodeBlock of
        code: string *
        fence: string option *
        language: string option

    /// A HTML block
    | InlineHtmlBlock of node : XmlNode

    /// A Markdown List block
    | ListBlock of kind: MarkdownListKind * items: list<MarkdownParagraphs>

    /// A Markdown Quote block
    | QuotedBlock of paragraphs: MarkdownParagraphs

    /// A Markdown Span block
    | Span of body: MarkdownSpans

    /// A Markdown Horizontal rule
    | HorizontalRule

    /// A Markdown Table
    | TableBlock of
        headers: option<MarkdownTableRow> *
        alignments: list<MarkdownColumnAlignment> *
        rows: list<MarkdownTableRow>

    // /// Represents a block of markdown produced when parsing of code or tables or quoted blocks is suppressed
    // | OtherBlock of lines: (string * MarkdownRange) list * range: MarkdownRange option

    // /// A special addition for computing paragraphs
    // | EmbedParagraphs of customParagraphs: MarkdownEmbedParagraphs * range: MarkdownRange option

    /// A special addition for YAML-style frontmatter
    | YamlFrontmatter of yaml: string list

/// A type alias for a list of paragraphs
type MarkdownParagraphs = list<MarkdownParagraph>

/// A type alias representing table row as a list of paragraphs
type MarkdownTableRow = list<MarkdownParagraphs>

let rec formatSpan (span : MarkdownSpan) =
    match span with
    | InlineCode code ->
        "`" + code + "`"

    | Literal text ->
        text

    | Strong body ->
        "**" + formatSpans body + "**"

    | Emphasis(body) ->
        "*" + formatSpans body + "*"

    | DirectLink(body, link, title) ->
        match title with
        | Some title ->
            "[" + formatSpans body + "](" + link + " \"" + title + "\")"
        | None ->
            "[" + formatSpans body + "](" + link + ")"

    | IndirectLink(body, original, key) ->
        failwith "Not Implemented"

    | DirectImage(body, link, title) ->
        match title with
        | Some title ->
            "![" + body + "](" + link + " \"" + title + "\")"
        | None ->
            "![" + body + "](" + link + ")"

    | IndirectImage(body, link, key) ->
        failwith "Not Implemented"

    | HardLineBreak ->
        "\n"

and formatSpans spans =
    spans |> List.map formatSpan |> String.concat ""

let inline emptyLine<'a> = ""

let rec formatParagraph (paragraph : MarkdownParagraph) =
    [
        match paragraph with
        | Heading (size, body) ->
            String.replicate size "#" + " " + formatSpans body

        | Paragraph body ->
            formatSpans body
            emptyLine

        | CodeBlock(code, fence, language) ->
            let fence = defaultArg fence "```"
            let language = defaultArg language ""

            fence + language
            + "\n" + code
            + "\n" + fence

            emptyLine

        | InlineHtmlBlock html ->
            let htmlString =
                html
                |> RenderView.AsString.htmlNode

            yield! htmlString.Replace("\r\n", "\n").Split("\n")
            emptyLine

        | ListBlock(kind, items) -> failwith "Not Implemented"
        | QuotedBlock(paragraphs) -> failwith "Not Implemented"
        | Span(body) -> failwith "Not Implemented"
        | HorizontalRule ->
            "---"
            emptyLine

        | TableBlock(headers, alignments, rows) -> failwith "Not Implemented"
        | YamlFrontmatter(yaml) ->
            "---"
            yield! yaml
            "---"
            emptyLine
    ]

let rec formatParagraphs (paragraphs: MarkdownParagraph list) =
    paragraphs
    |> List.collect formatParagraph

/// <summary>
/// Representation of a Markdown document - the representation of Paragraphs
/// uses an F# discriminated union type and so is best used from F#.
/// </summary>
/// <namespacedoc>
///   <summary>Functionality for processing markdown documents, converting to HTML, LaTeX, ipynb and scripts</summary>
/// </namespacedoc>
type MarkdownDocument(paragraphs) =
    /// Returns a list of paragraphs in the document
    member __.Paragraphs: MarkdownParagraphs = paragraphs

    // /// Returns a dictionary containing explicitly defined links
    // member __.DefinedLinks: IDictionary<string, string * option<string>> = links

    /// Transform the MarkdownDocument into Markdown and return the result as a string.
    member this.ToMarkdown() =
        let newline = "\n"

        formatParagraphs paragraphs
        |> String.concat newline
