namespace Nacara.Core

open System
open System.Text
open System.Text.RegularExpressions

/// <summary>How a line or a piece of text is called out.</summary>
type Marker =
    | Mark
    | Insert
    | Delete

    member this.Name =
        match this with
        | Mark -> "mark"
        | Insert -> "ins"
        | Delete -> "del"

/// <summary>Chrome drawn around a code block.</summary>
type Frame =
    | CodeFrame
    | TerminalFrame
    | NoFrame

/// <summary>
/// Everything the fence meta string asked for.
/// </summary>
/// <remarks>
/// The syntax follows Expressive Code:
/// <c>title="Program.fs" {1,3-5} ins={2} del={3} /word/ showLineNumbers collapse={7-9} wrap</c>.
/// </remarks>
type CodeBlockMeta =
    {
        Title: string option
        Frame: Frame
        ShowLineNumbers: bool
        StartLineNumber: int
        Wrap: bool
        LineMarkers: Map<int, Marker>
        /// Regular expressions marking text inside lines.
        WordMarkers: (Regex * Marker) list
        /// Inclusive line ranges rendered collapsed.
        Collapse: (int * int) list
        /// <summary>Colour it as this language rather than as the one the fence named.</summary>
        /// <remarks>What <c>lang=</c> sets: on a <c>diff</c> fence, what the code underneath is.</remarks>
        HighlightAs: string option
        /// Anything the parser did not recognise, for plugins to consume.
        Unknown: string list
    }

/// <summary>A piece of a line, with the highlighting class it was given.</summary>
type Token =
    {
        Text: string
        ClassName: string option
    }

/// <summary>
/// A syntax highlighter.
/// </summary>
/// <remarks>
/// <para>Return <c>None</c> to say "I do not know this language", and another highlighter gets a
/// turn.</para>
/// <para>The last one registered is asked first, and the earlier ones still cover what the later
/// one declines.</para>
/// </remarks>
type IHighlighter =
    abstract Name: string
    abstract Highlight: language: string option * code: string -> Token list list option

/// <summary>A fenced code block, as it was written.</summary>
type CodeBlock =
    {
        Language: string option
        Code: string
        Meta: CodeBlockMeta
    }

/// <summary>A piece of a line: some text, its highlighting class, and any marker over it.</summary>
type CodePiece =
    {
        Text: string
        ClassName: string option
        /// Set when a word marker covers this piece.
        Marker: Marker option
    }

/// <summary>One line of a code block, numbered as the reader sees it.</summary>
type CodeLine =
    {
        Number: int
        /// Set when the line as a whole is marked, inserted or deleted.
        Marker: Marker option
        /// True when the meta asked for this line to be folded away.
        IsCollapsed: bool
        Pieces: CodePiece list
    }

/// <summary>
/// A code block with everything decided except how it looks.
/// </summary>
type PreparedCodeBlock =
    {
        Language: string option
        Meta: CodeBlockMeta
        Lines: CodeLine list
    }

/// <summary>Turns a prepared code block into HTML.</summary>
/// <remarks>Registered with <c>Registry.extra</c>; the last one registered wins. With none
/// registered the engine falls back to plain <c>&lt;pre&gt;&lt;code&gt;</c>.</remarks>
type ICodeBlockRenderer =
    abstract Name: string
    abstract Render: PreparedCodeBlock -> string

[<RequireQualifiedAccess>]
module CodeBlockMeta =

    let empty =
        {
            Title = None
            Frame = CodeFrame
            ShowLineNumbers = false
            StartLineNumber = 1
            Wrap = false
            LineMarkers = Map.empty
            WordMarkers = []
            Collapse = []
            HighlightAs = None
            Unknown = []
        }

    /// <summary>Split a meta string into tokens, keeping quoted, braced and slashed groups whole.</summary>
    let private tokenize (meta: string) =
        let tokens = ResizeArray()
        let current = StringBuilder()
        let mutable index = 0
        let mutable inQuotes = false
        let mutable inBraces = false
        let mutable inSlashes = false

        while index < meta.Length do
            let char = meta[index]

            match char with
            | '"' ->
                inQuotes <- not inQuotes
                current.Append char |> ignore
            | '{' when not inQuotes ->
                inBraces <- true
                current.Append char |> ignore
            | '}' when not inQuotes ->
                inBraces <- false
                current.Append char |> ignore
            | '/' when
                not inQuotes
                && not inBraces
                && (inSlashes || current.Length = 0 || current.ToString().EndsWith "=")
                ->
                inSlashes <- not inSlashes
                current.Append char |> ignore
            | ' ' when not inQuotes && not inBraces && not inSlashes ->
                if current.Length > 0 then
                    tokens.Add(current.ToString())
                    current.Clear() |> ignore
            | char -> current.Append char |> ignore

            index <- index + 1

        if current.Length > 0 then
            tokens.Add(current.ToString())

        List.ofSeq tokens

    /// <summary>Expand <c>{1,3-5}</c> into the lines it names.</summary>
    let private parseRanges (value: string) =
        value.Trim('{', '}').Split(',', StringSplitOptions.RemoveEmptyEntries)
        |> Array.choose (fun part ->
            match part.Trim().Split('-') with
            | [| single |] ->
                match Int32.TryParse single with
                | true, line -> Some(line, line)
                | _ -> None
            | [| first; last |] ->
                match Int32.TryParse first, Int32.TryParse last with
                | (true, first), (true, last) -> Some(first, last)
                | _ -> None
            | _ -> None
        )
        |> List.ofArray

    let private addLines marker ranges (meta: CodeBlockMeta) =
        let lines =
            ranges
            |> List.collect (fun (first, last) -> [ first..last ])
            |> List.map (fun line -> line, marker)

        { meta with
            LineMarkers =
                lines
                |> List.fold (fun map (line, marker) -> Map.add line marker map) meta.LineMarkers
        }

    let private addWords marker (pattern: string) (meta: CodeBlockMeta) =
        let expression =
            if pattern.StartsWith "/" && pattern.EndsWith "/" && pattern.Length > 1 then
                Regex(pattern.Trim('/'))
            else
                Regex(Regex.Escape(pattern.Trim('"')))

        { meta with
            WordMarkers = meta.WordMarkers @ [ expression, marker ]
        }

    let private unquote (value: string) = value.Trim('"')

    /// <summary>Parse the text that follows the language on a fence line.</summary>
    /// <param name="meta">Everything after the language on the fence -
    /// <c>title="Program.fs" {2,4-6} ins={7} wrap</c>. What it does not recognise is kept in
    /// <c>Unknown</c> rather than dropped, so a plugin can read its own annotations from it.</param>
    /// <returns>The annotations of the block: its frame and title, which lines are marked, which
    /// words, whether it wraps, where its numbering starts.</returns>
    let parse (meta: string) =
        if String.IsNullOrWhiteSpace meta then
            empty
        else

            tokenize meta
            |> List.fold
                (fun state token ->
                    let name, value =
                        // A regular expression may hold an equals sign, so a lone marker is not a name and a value.
                        if
                            token.StartsWith "/" || token.StartsWith "{" || token.StartsWith "\""
                        then
                            token, None
                        else
                            match token.IndexOf '=' with
                            | -1 -> token, None
                            | index -> token.Substring(0, index), Some(token.Substring(index + 1))

                    match name, value with
                    | "title", Some value ->
                        { state with
                            Title = Some(unquote value)
                        }
                    | "frame", Some value ->
                        { state with
                            Frame =
                                match unquote value with
                                | "terminal" -> TerminalFrame
                                | "none" -> NoFrame
                                | _ -> CodeFrame
                        }
                    | "showLineNumbers", None ->
                        { state with
                            ShowLineNumbers = true
                        }
                    | "showLineNumbers", Some value ->
                        { state with
                            ShowLineNumbers = unquote value <> "false"
                        }
                    | "startLineNumber", Some value ->
                        match Int32.TryParse(unquote value) with
                        | true, number ->
                            { state with
                                StartLineNumber = number
                                ShowLineNumbers = true
                            }
                        | _ -> state
                    | "wrap", None ->
                        { state with
                            Wrap = true
                        }
                    | "lang", Some value ->
                        { state with
                            HighlightAs = Some(unquote value)
                        }
                    | "collapse", Some value ->
                        { state with
                            Collapse = state.Collapse @ parseRanges value
                        }
                    | "ins", Some value when value.StartsWith "{" ->
                        addLines Insert (parseRanges value) state
                    | "del", Some value when value.StartsWith "{" ->
                        addLines Delete (parseRanges value) state
                    | "mark", Some value when value.StartsWith "{" ->
                        addLines Mark (parseRanges value) state
                    | "ins", Some value -> addWords Insert value state
                    | "del", Some value -> addWords Delete value state
                    | "mark", Some value -> addWords Mark value state
                    | _, None when token.StartsWith "{" -> addLines Mark (parseRanges token) state
                    | _, None when token.StartsWith "/" && token.EndsWith "/" ->
                        addWords Mark token state
                    | _, None when token.StartsWith "\"" -> addWords Mark token state
                    | _ ->
                        { state with
                            Unknown = state.Unknown @ [ token ]
                        }
                )
                empty

[<RequireQualifiedAccess>]
module CodeBlock =

    /// <summary>Escape text for placing in HTML. Renderers need this too.</summary>
    let escapeHtml (text: string) =
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")

    /// <summary>The same, for text going inside an attribute rather than between tags.</summary>
    /// <remarks>A quote is harmless between tags and ends the value early inside one, so the two are
    /// not the same job.</remarks>
    let escapeAttribute (text: string) =
        (escapeHtml text).Replace("\"", "&quot;").Replace("'", "&#39;")

    /// <summary>Split a line's tokens so that every marked word becomes a piece of its own.</summary>
    let private applyWordMarkers (markers: (Regex * Marker) list) (line: Token list) =
        let toPiece (token: Token) marker =
            {
                Text = token.Text
                ClassName = token.ClassName
                Marker = marker
            }

        if List.isEmpty markers then
            line |> List.map (fun token -> toPiece token None)
        else

            let text = line |> List.map _.Text |> String.concat ""

            let marked =
                markers
                |> List.collect (fun (expression, marker) ->
                    expression.Matches text
                    |> Seq.filter (fun matched -> matched.Length > 0)
                    |> Seq.map (fun matched ->
                        matched.Index, matched.Index + matched.Length, marker
                    )
                    |> List.ofSeq
                )

            if List.isEmpty marked then
                line |> List.map (fun token -> toPiece token None)
            else

                let markerAt position =
                    marked
                    |> List.tryPick (fun (first, last, marker) ->
                        if position >= first && position < last then
                            Some marker
                        else
                            None
                    )

                let result = ResizeArray()
                let mutable offset = 0

                for token in line do
                    let mutable start = 0

                    while start < token.Text.Length do
                        let marker = markerAt (offset + start)
                        let mutable length = 1

                        while start + length < token.Text.Length
                              && markerAt (offset + start + length) = marker do
                            length <- length + 1

                        result.Add(
                            toPiece
                                { token with
                                    Text = token.Text.Substring(start, length)
                                }
                                marker
                        )

                        start <- start + length

                    offset <- offset + token.Text.Length

                List.ofSeq result

    /// <summary>
    /// Fence languages that mean "this is not a language".
    /// </summary>
    /// <remarks>A block labelled <c>text</c> or <c>console</c> is saying it wants no colour, so
    /// nothing is missing when nothing colours it.</remarks>
    let plain =
        set
            [
                "text"
                "plaintext"
                "txt"
                "plain"
                "none"
                "output"
                "console"
                "log"
                "diff"
            ]

    /// <summary>
    /// A block written the way a diff is: <c>+</c> for a line added, <c>-</c> for one taken away.
    /// </summary>
    /// <remarks>The markers are read off the front of each line and the line is handed on without
    /// them. Pair it with <c>lang=</c> and a diff reads as the language it is written in.</remarks>
    module Diff =

        /// A real diff file says what it is about before it says what changed.
        let private headers =
            [
                "***"
                "+++"
                "---"
                "@@"
            ]

        /// The other shape a diff comes in: `2c2`, `0a1,3`, `4,6d5`.
        let private location = Regex(@"^\d+(,\d+)?[acd]\d+(,\d+)?$")

        /// <summary>Whether this is a diff someone pasted rather than one they wrote inline.</summary>
        /// <remarks>Its headers begin with the same characters as its changes, so stripping them
        /// would eat the part that says which files these are.</remarks>
        let isFile (lines: string list) =
            lines
            |> List.exists (fun line ->
                let trimmed = line.Trim()
                headers |> List.exists trimmed.StartsWith || location.IsMatch trimmed
            )

        /// <summary>Read the markers off the front, and give back the code without them.</summary>
        /// <remarks>
        /// The first column is the gutter: a <c>+</c> or <c>-</c> stands there, and an unchanged line
        /// leaves a space where one would have been. Both come off. A block where some line starts at the
        /// margin has no gutter to speak of, so only the markers go.
        /// </remarks>
        /// <param name="lines">The block, as it was written.</param>
        /// <returns>The lines without their markers, and the marker each line carried.</returns>
        let read (lines: string list) =
            let hasGutter =
                lines
                |> List.forall (fun line ->
                    line = ""
                    || [
                        "+"
                        "-"
                        " "
                       ]
                       |> List.exists line.StartsWith
                )

            let read (line: string) =
                if line.StartsWith "+" then
                    Some Insert, line.Substring 1
                elif line.StartsWith "-" then
                    Some Delete, line.Substring 1
                elif hasGutter && line.StartsWith " " then
                    None, line.Substring 1
                else
                    None, line

            let marked = lines |> List.map read

            let markers =
                marked
                |> List.indexed
                |> List.choose (fun (index, (marker, _)) ->
                    marker |> Option.map (fun marker -> index + 1, marker)
                )
                |> Map.ofList

            marked |> List.map snd, markers

    /// <summary>Whether a fence names a language a highlighter was meant to know.</summary>
    /// <param name="language">What the fence wrote after its backticks.</param>
    let claimsALanguage (language: string option) =
        match language with
        | None -> false
        | Some language -> not (plain.Contains(language.Trim().ToLowerInvariant()))

    /// <summary>What to colour a block as: <c>lang=</c> when it says, the fence otherwise.</summary>
    /// <param name="block">The block as the fence was written.</param>
    let language (block: CodeBlock) =
        block.Meta.HighlightAs |> Option.orElse block.Language

    /// <summary>
    /// Highlight a block and resolve its markers, leaving only the markup to decide.
    /// </summary>
    /// <param name="highlighters">What can colour the block. The last one registered is asked first,
    /// and the ones before it cover what it declines; a language nobody claims is left as plain
    /// text.</param>
    /// <param name="block">The block as the fence was written: its language, its annotations and
    /// its lines.</param>
    /// <returns>The block with its tokens coloured and its markers resolved to the lines and words
    /// they name - everything a renderer needs to decide markup and nothing more.</returns>
    let prepare (highlighters: IHighlighter list) (block: CodeBlock) =
        let written =
            block.Code.Replace("\r\n", "\n").TrimEnd('\n').Split('\n') |> List.ofArray

        // Columns are counted against the code, not against the diff markers in front of it.
        let isDiff =
            block.Language
            |> Option.map (fun language -> language.Trim().ToLowerInvariant() = "diff")
            |> Option.defaultValue false

        let lines, diffMarkers =
            if isDiff && not (Diff.isFile written) then
                Diff.read written
            else
                written, Map.empty

        let block =
            { block with
                Code = String.Join("\n", lines)
                Meta =
                    { block.Meta with
                        LineMarkers =
                            diffMarkers
                            |> Map.fold
                                (fun markers number marker ->
                                    if Map.containsKey number markers then
                                        markers
                                    else
                                        Map.add number marker markers
                                )
                                block.Meta.LineMarkers
                    }
            }

        let plainLines () =
            lines
            |> List.map (fun line ->
                [
                    {
                        Text = line
                        ClassName = None
                    }
                ]
            )

        let coloured =
            // Reversed, so the last registered gets first refusal.
            highlighters
            |> List.rev
            |> List.tryPick (fun highlighter -> highlighter.Highlight(language block, block.Code))

        let tokenized = coloured |> Option.defaultWith plainLines

        let collapsed =
            block.Meta.Collapse
            |> List.collect (fun (first, last) ->
                [ max 1 first .. min (List.length tokenized) last ]
            )
            |> Set.ofList

        {
            Language = block.Language
            Meta = block.Meta
            Lines =
                tokenized
                |> List.mapi (fun index line ->
                    let number = index + 1

                    {
                        Number = number
                        Marker = Map.tryFind number block.Meta.LineMarkers
                        IsCollapsed = Set.contains number collapsed
                        Pieces = applyWordMarkers block.Meta.WordMarkers line
                    }
                )
        }

    /// <summary>
    /// The fallback rendering, used when nothing better is registered.
    /// </summary>
    /// <remarks>Highlighting classes and nothing else. A theme registers an
    /// <see cref="T:Nacara.Core.ICodeBlockRenderer" /> to decide frames, line numbers and the rest.</remarks>
    /// <param name="block">A prepared block.</param>
    /// <returns><c>&lt;pre&gt;&lt;code&gt;</c> with the token classes, and nothing else: no
    /// frame, no line numbers, no copy button.</returns>
    let renderMinimal (block: PreparedCodeBlock) =
        let builder = StringBuilder()
        builder.Append "<pre><code" |> ignore

        match block.Language with
        | Some language -> builder.Append($" class=\"language-%s{escapeHtml language}\"") |> ignore
        | None -> ()

        builder.Append ">" |> ignore

        for line in block.Lines do
            for piece in line.Pieces do
                match piece.ClassName with
                | Some className ->
                    builder.Append(
                        $"""<span class="%s{className}">%s{escapeHtml piece.Text}</span>"""
                    )
                    |> ignore
                | None -> builder.Append(escapeHtml piece.Text) |> ignore

            builder.Append "\n" |> ignore

        builder.Append "</code></pre>" |> ignore
        builder.ToString()

    /// <summary>
    /// What the block leaves you with: the code after the change it describes.
    /// </summary>
    /// <remarks>A folded line is kept; a deleted one is not, so what a reader copies or runs is what
    /// is left rather than both sides of the change.</remarks>
    /// <param name="block">The block, prepared.</param>
    let source (block: PreparedCodeBlock) =
        block.Lines
        |> List.filter (fun line -> line.Marker <> Some Delete)
        |> List.map (fun line ->
            line.Pieces |> List.map (fun piece -> piece.Text) |> String.concat ""
        )
        |> String.concat "\n"

    /// <summary>Whether any highlighter claimed this block's language.</summary>
    /// <param name="highlighters">What can colour the block.</param>
    /// <param name="block">The block as the fence was written.</param>
    let isColoured (highlighters: IHighlighter list) (block: CodeBlock) =
        highlighters
        |> List.exists (fun highlighter ->
            (highlighter.Highlight(language block, block.Code)).IsSome
        )

    /// <summary>Render with the first renderer that was registered, or the fallback.</summary>
    /// <param name="renderers">What plugins contributed. The last one registered wins, so a theme
    /// registered after a plugin decides how a block looks.</param>
    /// <param name="highlighters">What can colour the block.</param>
    /// <param name="block">The block as the fence was written.</param>
    let render
        (renderers: ICodeBlockRenderer list)
        (highlighters: IHighlighter list)
        (block: CodeBlock)
        =
        let prepared = prepare highlighters block

        match List.tryLast renderers with
        | Some renderer -> renderer.Render prepared
        | None -> renderMinimal prepared
