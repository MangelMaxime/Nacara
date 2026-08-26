namespace Nacara.Theme

open System.Text
open Nacara.Core

/// <summary>
/// How this theme draws a code block.
/// </summary>
type NacaraCodeBlockRenderer() =

    /// <summary>The copy button, one element per line so its shape can be read.</summary>
    let copyButton =
        [
            """<button class="nacara-code__copy"
                       type="button"
                       aria-label="Copy code">"""
            """<svg class="nacara-code__copy-idle"
                     viewBox="0 0 24 24"
                     fill="none"
                     stroke="currentColor"
                     stroke-width="2"
                     stroke-linecap="round"
                     stroke-linejoin="round"
                     aria-hidden="true">"""
            """<rect x="9" y="9" width="11" height="11" rx="2" />"""
            """<path d="M5 15V5a2 2 0 0 1 2-2h10" />"""
            "</svg>"
            """<svg class="nacara-code__copy-done"
                     viewBox="0 0 24 24"
                     fill="none"
                     stroke="currentColor"
                     stroke-width="2.5"
                     stroke-linecap="round"
                     stroke-linejoin="round"
                     aria-hidden="true">"""
            """<path d="M20 6 9 17l-5-5" />"""
            "</svg>"
            "</button>"
        ]
        |> String.concat ""

    /// <summary>An opening tag, one attribute per line.</summary>
    let openingTag (name: string) (attributes: (string * string) list) =
        let indent = "\n" + String.replicate (name.Length + 2) " "

        attributes
        |> List.map (fun (key, value) -> $"%s{key}=\"%s{value}\"")
        |> String.concat indent
        |> fun written -> $"<%s{name} %s{written}>"

    let renderLine (meta: CodeBlockMeta) (line: CodeLine) =
        let builder = StringBuilder()

        builder.Append(
            openingTag
                "span"
                [
                    "class", "nacara-code__line"

                    match line.Marker with
                    | Some marker -> "data-marker", CodeBlock.escapeAttribute marker.Name
                    | None -> ()

                    if meta.ShowLineNumbers then
                        "data-line", string (line.Number + meta.StartLineNumber - 1)
                ]
        )
        |> ignore

        let mutable marked = None

        for piece in line.Pieces do
            let text = CodeBlock.escapeHtml piece.Text

            if marked <> piece.Marker then
                if Option.isSome marked then
                    builder.Append "</mark>" |> ignore

                match piece.Marker with
                | Some marker ->
                    builder.Append(
                        $"""<mark class="nacara-code__word" data-marker="%s{marker.Name}">"""
                    )
                    |> ignore
                | None -> ()

                marked <- piece.Marker

            match piece.ClassName with
            | None -> builder.Append text |> ignore
            | Some className ->
                builder.Append($"""<span class="%s{className}">%s{text}</span>""") |> ignore

        if Option.isSome marked then
            builder.Append "</mark>" |> ignore

        builder.Append "\n</span>" |> ignore
        builder.ToString()

    /// Consecutive lines that are collapsed together become one disclosure.
    let runs (lines: CodeLine list) =
        lines
        |> List.fold
            (fun runs line ->
                match runs with
                | (collapsed, items) :: rest when collapsed = line.IsCollapsed ->
                    (collapsed, items @ [ line ]) :: rest
                | _ -> (line.IsCollapsed, [ line ]) :: runs
            )
            []
        |> List.rev

    interface ICodeBlockRenderer with
        member _.Name = "theme.default"

        member _.Render(block: PreparedCodeBlock) =
            let builder = StringBuilder()

            let frame =
                match block.Meta.Frame with
                | CodeFrame -> "code"
                | TerminalFrame -> "terminal"
                | NoFrame -> "none"

            let language = block.Language |> Option.defaultValue "text"

            builder.Append(
                openingTag
                    "figure"
                    [
                        "class", "nacara-code"
                        "data-language", CodeBlock.escapeAttribute language
                        "data-frame", frame

                        if block.Meta.Wrap then
                            "data-wrap", "true"

                        if block.Meta.ShowLineNumbers then
                            "data-line-numbers", "true"

                        if not block.Meta.Unknown.IsEmpty then
                            "data-meta",
                            CodeBlock.escapeAttribute (String.concat " " block.Meta.Unknown)

                        "data-source", CodeBlock.escapeAttribute (CodeBlock.source block)
                    ]
            )
            |> ignore

            match block.Meta.Title, frame with
            | Some title, _ ->
                builder.Append(
                    $"""<figcaption class="nacara-code__title">%s{CodeBlock.escapeHtml title}</figcaption>"""
                )
                |> ignore
            | None, "terminal" ->
                builder.Append """<figcaption class="nacara-code__title">Terminal</figcaption>"""
                |> ignore
            | None, _ -> ()

            builder.Append """<div class="nacara-code__body">""" |> ignore

            let blockRuns = runs block.Lines

            if List.isEmpty blockRuns then
                builder.Append "<pre><code></code></pre>" |> ignore

            for isCollapsed, lines in blockRuns do
                if isCollapsed then
                    builder.Append(
                        $"""<details class="nacara-code__collapsed"><summary>%i{List.length lines} collapsed lines</summary><pre><code>"""
                    )
                    |> ignore
                else
                    builder.Append "<pre><code>" |> ignore

                for line in lines do
                    builder.Append(renderLine block.Meta line) |> ignore

                if isCollapsed then
                    builder.Append "</code></pre></details>" |> ignore
                else
                    builder.Append "</code></pre>" |> ignore

            // A glyph in a text font renders at whatever size the font decides.
            builder.Append copyButton |> ignore

            builder.Append "</div></figure>" |> ignore
            builder.ToString()
