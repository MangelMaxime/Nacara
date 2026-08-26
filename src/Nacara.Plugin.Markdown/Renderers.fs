namespace Nacara.Plugins.Internal

open System.IO
open System.Text.RegularExpressions
open Markdig
open Markdig.Renderers
open Markdig.Renderers.Html
open Markdig.Syntax
open Markdig.Syntax.Inlines
open Markdig.Extensions.CustomContainers
open Nacara.Core

/// <summary>Renders a heading with a link to itself.</summary>
type NacaraHeadingRenderer() =
    inherit HtmlObjectRenderer<HeadingBlock>()

    override _.Write(renderer: HtmlRenderer, heading: HeadingBlock) =
        let tag = "h" + string heading.Level

        let identifier =
            match heading.TryGetAttributes() with
            | null -> None
            | attributes -> Option.ofObj attributes.Id

        if renderer.EnableHtmlForBlock then
            renderer.Write("<").Write(tag).WriteAttributes(heading).Write(">") |> ignore

        renderer.WriteLeafInline heading |> ignore

        match identifier with
        | Some identifier when heading.Level > 1 && renderer.EnableHtmlForBlock ->
            renderer.Write(
                $"""<a class="nacara-heading-anchor"
                       href="#%s{identifier}"
                       aria-label="Link to this section"
                       data-pagefind-ignore></a>"""
            )
            |> ignore
        | _ -> ()

        if renderer.EnableHtmlForBlock then
            renderer.Write("</").Write(tag).Write(">") |> ignore

        renderer.EnsureLine() |> ignore

/// <summary>
/// A fenced block, coloured and drawn.
/// </summary>
type NacaraCodeBlockRenderer
    (
        highlighters: IHighlighter list,
        renderers: ICodeBlockRenderer list,
        report: string -> int -> unit,
        check: CodeBlock -> int -> unit
    )
    =
    inherit HtmlObjectRenderer<Markdig.Syntax.CodeBlock>()

    override _.Write(renderer: HtmlRenderer, block: Markdig.Syntax.CodeBlock) =
        let language, meta =
            match block with
            | :? FencedCodeBlock as fenced ->
                (if System.String.IsNullOrWhiteSpace fenced.Info then
                     None
                 else
                     Some fenced.Info),
                (if isNull fenced.Arguments then
                     ""
                 else
                     fenced.Arguments)
            | _ -> None, ""

        let code =
            seq {
                for index in 0 .. block.Lines.Count - 1 -> block.Lines.Lines[index].Slice.ToString()
            }
            |> String.concat "\n"

        let written =
            {
                Language = language
                Code = code
                Meta = CodeBlockMeta.parse meta
            }

        if
            CodeBlock.claimsALanguage language
            && not (CodeBlock.isColoured highlighters written)
        then
            report (Option.defaultValue "" language) block.Line

        // Markdig only ever hands this a real fence, so a block written inside another never reaches a check.
        check written block.Line

        CodeBlock.render renderers highlighters written |> renderer.Write |> ignore

/// <summary>
/// Inline code, coloured when it says what language it is.
/// </summary>
/// <remarks>
/// <para>Two spellings. <c>`let x = ""{:fsharp}`</c> puts the marker inside the backticks, which is
/// what rehype-pretty-code uses and what survives any markdown processor; <c>`let x = ""`{fsharp}</c>
/// and <c>{lang=fsharp}</c> put it outside, where Markdig's generic attributes already parse it.</para>
/// <para>A bare attribute is only read as a language when a highlighter claims it, so a genuine
/// boolean attribute on a code span is never mistaken for one.</para>
/// </remarks>
type NacaraCodeInlineRenderer(highlighters: IHighlighter list, report: string -> int -> unit) =
    inherit HtmlObjectRenderer<CodeInline>()

    /// The marker as it is written inside the backticks, at the very end and never on its own.
    static let marker = Regex(@"\{:([A-Za-z0-9_+#.-]+)\}$", RegexOptions.Compiled)

    /// The first line of tokens, since inline code is one line by construction.
    let colour (language: string) (text: string) =
        highlighters
        |> List.tryPick (fun highlighter -> highlighter.Highlight(Some language, text))
        |> Option.bind List.tryHead

    override _.Write(renderer: HtmlRenderer, code: CodeInline) =
        let attributes = code.TryGetAttributes()

        /// A property, and the attributes without it: consumed rather than left to reach the HTML.
        let take name =
            match attributes with
            | null -> None
            | attributes ->
                match attributes.Properties with
                | null -> None
                | properties ->
                    properties
                    |> Seq.tryFind (fun property -> property.Key = name)
                    |> Option.map (fun property ->
                        properties.Remove property |> ignore
                        property.Value
                    )

        let written = marker.Match code.Content

        let language, text =
            if written.Success && written.Index > 0 then
                Some written.Groups[1].Value, code.Content.Substring(0, written.Index)
            else

                match take "lang" with
                | Some language when not (System.String.IsNullOrWhiteSpace language) ->
                    Some language, code.Content
                | _ ->
                    let claimed =
                        match attributes with
                        | null -> None
                        | attributes ->
                            match attributes.Properties with
                            | null -> None
                            | properties ->
                                properties
                                |> Seq.filter (fun property ->
                                    System.String.IsNullOrEmpty property.Value
                                )
                                |> Seq.tryFind (fun property ->
                                    (colour property.Key code.Content).IsSome
                                )
                                |> Option.map (fun property ->
                                    properties.Remove property |> ignore
                                    property.Key
                                )

                    claimed, code.Content

        let coloured = language |> Option.bind (fun language -> colour language text)

        match language with
        | Some language when coloured.IsNone && CodeBlock.claimsALanguage (Some language) ->
            report language code.Line
        | _ -> ()

        if renderer.EnableHtmlForInline then
            renderer.Write("<code").WriteAttributes(code).Write(">") |> ignore

        match coloured with
        | Some tokens ->
            for token in tokens do
                match token.ClassName with
                | Some className ->
                    renderer.Write($"""<span class="%s{className}">""") |> ignore
                    renderer.Write(CodeBlock.escapeHtml token.Text) |> ignore
                    renderer.Write "</span>" |> ignore
                | None -> renderer.Write(CodeBlock.escapeHtml token.Text) |> ignore
        | None ->
            if renderer.EnableHtmlEscape then
                renderer.WriteEscape text |> ignore
            else
                renderer.Write text |> ignore

        if renderer.EnableHtmlForInline then
            renderer.Write "</code>" |> ignore

/// <summary>What a <c>:::name</c> directive turns into.</summary>
type DirectiveResult =
    /// Wrap the content in an element, with attributes.
    | Element of tag: string * attributes: (string * string) list
    /// Show the block it contains, then what that block renders as.
    | Preview
    /// Draw the list it contains as a directory listing.
    | FileTree
    /// The directive is not known; the transform reports it.
    | Unknown

[<RequireQualifiedAccess>]
module Directive =

    /// <summary>
    /// Built-in directives.
    /// </summary>
    let builtIn (name: string) (argument: string option) =
        let label = argument |> Option.defaultValue ""

        match name.ToLowerInvariant() with
        | "note"
        | "tip"
        | "info"
        | "warning"
        | "danger"
        | "caution" ->
            Element(
                "aside",
                [
                    "class", "nacara-callout"
                    "data-kind", name.ToLowerInvariant()
                    if label <> "" then
                        "data-title", label
                ]
            )
        | "tabs" ->
            Element(
                "nacara-tabs",
                [
                    if label <> "" then
                        "data-sync", label
                ]
            )
        | "tab" -> Element("nacara-tab", [ "data-label", label ])
        | "steps" -> Element("div", [ "class", "nacara-steps" ])
        | "preview" -> Preview
        | "filetree"
        | "file-tree" -> FileTree
        | "details" ->
            Element(
                "details",
                [
                    "class", "nacara-details"
                    "data-summary", label
                ]
            )
        | _ -> Unknown

/// <summary>Renders <c>:::name</c> containers as semantic HTML and web components.</summary>
type NacaraContainerRenderer
    (
        report: string -> string -> string -> int -> int -> unit,
        pipeline: Lazy<MarkdownPipeline>,
        /// How far the document being rendered is into the page, for a preview inside a preview.
        nested: int ref
    )
    =
    inherit HtmlObjectRenderer<CustomContainer>()

    /// <summary>
    /// The source of an example, and the example itself, from one block.
    /// </summary>
    member private _.WritePreview(renderer: HtmlRenderer, container: CustomContainer) =
        let fence =
            container
            |> Seq.tryPick (
                function
                | :? FencedCodeBlock as fence -> Some fence
                | _ -> None
            )

        renderer.Write """<div class="nacara-preview">""" |> ignore
        renderer.WriteChildren container |> ignore

        match fence with
        | Some fence ->
            let source = fence.Lines.ToString()

            let language =
                (if isNull fence.Info then
                     ""
                 else
                     fence.Info)
                    .ToLowerInvariant()

            match language with
            | "html" ->
                renderer.Write """<div class="nacara-preview__result">""" |> ignore
                renderer.Write source |> ignore
                renderer.Write "</div>" |> ignore
            | "markdown"
            | "md"
            | "" ->
                renderer.Write """<div class="nacara-preview__result">""" |> ignore

                // The snippet starts on the line after the fence that holds it.
                let outer = nested.Value
                nested.Value <- outer + fence.Line + 1
                renderer.Render(Markdown.Parse(source, pipeline.Value)) |> ignore
                nested.Value <- outer

                renderer.Write "</div>" |> ignore
            | other ->
                report
                    "preview-not-markup"
                    $"':::preview' has nothing to show below a %s{other} block"
                    "It renders the block underneath as markdown, so it is for markdown and html. A code block is already what its code looks like - drop the container."
                    container.Line
                    container.Column
        | None -> ()

        renderer.Write "</div>" |> ignore

    /// <summary>
    /// A directory listing, from the list an author wrote.
    /// </summary>
    member private this.WriteFileTree(renderer: HtmlRenderer, container: CustomContainer) =
        let textOf (paragraph: ParagraphBlock) =
            if isNull paragraph.Inline then
                ""
            else
                paragraph.Inline.Descendants()
                |> Seq.cast<MarkdownObject>
                |> Seq.choose (
                    function
                    | :? LiteralInline as literal -> Some(literal.Content.ToString())
                    | :? CodeInline as code -> Some code.Content
                    | _ -> None
                )
                |> String.concat ""
                |> _.Trim()

        let rec writeList (list: ListBlock) =
            renderer.Write """<ul class="nacara-file-tree__list">""" |> ignore

            for child in list do
                match child with
                | :? ListItemBlock as item ->
                    let name =
                        item
                        |> Seq.tryPick (
                            function
                            | :? ParagraphBlock as paragraph -> Some paragraph
                            | _ -> None
                        )

                    let nested =
                        item
                        |> Seq.tryPick (
                            function
                            | :? ListBlock as list -> Some list
                            | _ -> None
                        )

                    let text = name |> Option.map textOf |> Option.defaultValue ""
                    let isDirectory = nested.IsSome || text.EndsWith "/"

                    let foldable = nested.IsSome

                    renderer.Write(
                        if foldable then
                            """<li data-kind="directory"><details class="nacara-file-tree__folder"
                                                                  open><summary class="nacara-file-tree__name">"""
                        elif isDirectory then
                            """<li data-kind="directory"><span class="nacara-file-tree__name">"""
                        else
                            """<li data-kind="file"><span class="nacara-file-tree__name">"""
                    )
                    |> ignore

                    match name with
                    | Some paragraph when nested.IsNone && text.EndsWith "/" ->
                        renderer.WriteEscape(text.TrimEnd '/') |> ignore
                    | Some paragraph -> renderer.WriteLeafInline paragraph |> ignore
                    | None -> ()

                    renderer.Write(
                        if foldable then
                            "</summary>"
                        else
                            "</span>"
                    )
                    |> ignore

                    match nested with
                    | Some nested -> writeList nested
                    | None -> ()

                    renderer.Write(
                        if foldable then
                            "</details></li>"
                        else
                            "</li>"
                    )
                    |> ignore
                | _ -> ()

            renderer.Write "</ul>" |> ignore

        renderer.Write """<div class="nacara-file-tree">""" |> ignore

        match
            container
            |> Seq.tryPick (
                function
                | :? ListBlock as list -> Some list
                | _ -> None
            )
        with
        | Some list -> writeList list
        | None -> renderer.WriteChildren container |> ignore

        renderer.Write "</div>" |> ignore

    override this.Write(renderer: HtmlRenderer, container: CustomContainer) =
        let name =
            if isNull container.Info then
                ""
            else
                container.Info

        let argument =
            if isNull container.Arguments || container.Arguments = "" then
                None
            else
                Some(container.Arguments.Trim('"'))

        match Directive.builtIn name argument with
        | Preview -> this.WritePreview(renderer, container)
        | FileTree -> this.WriteFileTree(renderer, container)
        | Unknown ->
            report
                "unknown-directive"
                $"Unknown directive ':::%s{name}'"
                (if name = "" then
                     "A container nested in another one needs fewer colons than its parent: ::::tabs around :::tab"
                 else
                     "Built-in directives are note, tip, info, warning, danger, caution, tabs, tab, steps, details, preview and filetree")
                container.Line
                container.Column

            renderer.Write("<div class=\"nacara-directive\">") |> ignore
            renderer.WriteChildren container |> ignore
            renderer.Write("</div>") |> ignore
        | Element(tag, attributes) ->
            renderer.Write($"<%s{tag}") |> ignore

            for name, value in attributes do
                renderer.Write($" %s{name}=\"") |> ignore
                renderer.WriteEscape value |> ignore
                renderer.Write "\"" |> ignore

            renderer.Write ">" |> ignore

            match tag, argument with
            | "details", Some summary ->
                renderer.Write "<summary>" |> ignore
                renderer.WriteEscape summary |> ignore
                renderer.Write "</summary>" |> ignore
            | _ -> ()

            renderer.WriteChildren container |> ignore
            renderer.Write($"</%s{tag}>") |> ignore
