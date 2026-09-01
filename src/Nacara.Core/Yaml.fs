namespace Nacara.Core

open System
open System.IO
open YamlDotNet.RepresentationModel

/// <summary>Why a piece of front matter could not be turned into the expected type.</summary>
/// <remarks>
/// Positions are relative to the YAML document that was decoded. Callers that decode front matter
/// embedded in a larger file shift them with <c>Yaml.decodeWithOffset</c> so the reported line is
/// the line of the markdown file, which is the only line the author can act on.
/// </remarks>
type DecodeError =
    {
        /// Path of the offending value, for example <c>title</c> or <c>authors[1].name</c>.
        Path: string
        Message: string
        Line: int
        Column: int
    }

/// <summary>Turns a YAML node into a value of type <c>'T</c>, or explains why it cannot.</summary>
type Decoder<'T> = string -> YamlNode -> Result<'T, DecodeError>

exception private DecodeFailure of DecodeError

[<RequireQualifiedAccess>]
module Decode =

    let private error path (node: YamlNode) message =
        {
            Path = path
            Message = message
            Line = int node.Start.Line
            Column = int node.Start.Column
        }

    let private fail path node message = Error(error path node message)

    let private describe (node: YamlNode) =
        match node with
        | :? YamlScalarNode as scalar -> $"the value '%s{scalar.Value}'"
        | :? YamlSequenceNode -> "a list"
        | :? YamlMappingNode -> "an object"
        | _ -> "an unsupported node"

    let private scalar (typeName: string) (parse: string -> 'T option) : Decoder<'T> =
        fun path node ->
            match node with
            | :? YamlScalarNode as node ->
                match parse node.Value with
                | Some value -> Ok value
                | None -> fail path node $"Expected %s{typeName} but got '%s{node.Value}'"
            | node -> fail path node $"Expected %s{typeName} but got %s{describe node}"

    /// <summary>A decoder that reads nothing and gives back what it was handed.</summary>
    /// <param name="value">What every use of it decodes to. Useful as the default branch of an
    /// <see cref="M:Nacara.Core.Decode.andThen"/>.</param>
    let succeed (value: 'T) : Decoder<'T> = fun _ _ -> Ok value

    /// <summary>A decoder that always fails, saying why.</summary>
    /// <param name="message">What is wrong with the value, said to the author of the front matter:
    /// "expected one of note, tip, warning".</param>
    /// <param name="path">Where in the document the decoder is, for the message when it
    /// fails.</param>
    /// <param name="node">The value being read.</param>
    let error' (message: string) : Decoder<'T> = fun path node -> fail path node message

    /// <summary>Decode, then turn what came out into something else.</summary>
    /// <param name="mapping">Applied to a successful result. It cannot fail; use
    /// <see cref="M:Nacara.Core.Decode.andThen"/> when it can.</param>
    /// <param name="decoder">What reads the value first.</param>
    /// <param name="path">Where in the document the decoder is, for the message when it
    /// fails.</param>
    /// <param name="node">The value being read.</param>
    let map (mapping: 'T -> 'U) (decoder: Decoder<'T>) : Decoder<'U> =
        fun path node -> decoder path node |> Result.map mapping

    /// <summary>Choose how to carry on decoding from what has been read so far.</summary>
    /// <param name="mapping">Given the value read, the decoder to continue with - which is how a
    /// tagged union reads its tag first and its fields after.</param>
    /// <param name="decoder">What reads the value first.</param>
    /// <param name="path">Where in the document the decoder is, for the message when it
    /// fails.</param>
    /// <param name="node">The value being read.</param>
    let andThen (mapping: 'T -> Decoder<'U>) (decoder: Decoder<'T>) : Decoder<'U> =
        fun path node -> decoder path node |> Result.bind (fun value -> mapping value path node)

    /// <summary>The raw node, for plugins that carry their own untyped extras.</summary>
    let node: Decoder<YamlNode> = fun _ node -> Ok node

    let string: Decoder<string> = scalar "a string" Some

    let int: Decoder<int> =
        scalar
            "an integer"
            (fun raw ->
                match
                    Int32.TryParse(
                        raw,
                        Globalization.NumberStyles.Integer,
                        Globalization.CultureInfo.InvariantCulture
                    )
                with
                | true, value -> Some value
                | _ -> None
            )

    let float: Decoder<float> =
        scalar
            "a number"
            (fun raw ->
                match
                    Double.TryParse(
                        raw,
                        Globalization.NumberStyles.Float,
                        Globalization.CultureInfo.InvariantCulture
                    )
                with
                | true, value -> Some value
                | _ -> None
            )

    let bool: Decoder<bool> =
        scalar
            "a boolean"
            (fun raw ->
                match raw.ToLowerInvariant() with
                | "true"
                | "yes"
                | "on" -> Some true
                | "false"
                | "no"
                | "off" -> Some false
                | _ -> None
            )

    let datetime: Decoder<DateTimeOffset> =
        scalar
            "a date"
            (fun raw ->
                match
                    DateTimeOffset.TryParse(
                        raw,
                        Globalization.CultureInfo.InvariantCulture,
                        Globalization.DateTimeStyles.AssumeUniversal
                    )
                with
                | true, value -> Some value
                | _ -> None
            )

    /// <summary>A sequence, every entry read the same way.</summary>
    /// <param name="decoder">Reads one entry. A failure says which index it was at.</param>
    /// <param name="path">Where in the document the decoder is, for the message when it
    /// fails.</param>
    /// <param name="node">The value being read.</param>
    let list (decoder: Decoder<'T>) : Decoder<'T list> =
        fun path node ->
            match node with
            | :? YamlSequenceNode as sequence ->
                let mutable failure = None
                let items = ResizeArray()

                sequence.Children
                |> Seq.iteri (fun index child ->
                    if failure.IsNone then
                        match decoder $"%s{path}[%i{index}]" child with
                        | Ok value -> items.Add value
                        | Error decodeError -> failure <- Some decodeError
                )

                match failure with
                | Some decodeError -> Error decodeError
                | None -> Ok(List.ofSeq items)
            | node -> fail path node $"Expected a list but got %s{describe node}"

    let private childPath (path: string) (name: string) =
        if path = "" then
            name
        else
            $"%s{path}.%s{name}"

    /// <summary>Every pair of an object, whatever its keys are.</summary>
    /// <param name="decoder">Reads one value.</param>
    /// <param name="path">Where in the document the decoder is, for the message when it
    /// fails.</param>
    /// <param name="node">The value being read.</param>
    let keyValuePairs (decoder: Decoder<'T>) : Decoder<(string * 'T) list> =
        fun path node ->
            match node with
            | :? YamlMappingNode as mapping ->
                let mutable failure = None
                let pairs = ResizeArray()

                for entry in mapping.Children do
                    if failure.IsNone then
                        match entry.Key with
                        | :? YamlScalarNode as key ->
                            match decoder (childPath path key.Value) entry.Value with
                            | Ok value -> pairs.Add(key.Value, value)
                            | Error decodeError -> failure <- Some decodeError
                        | key -> failure <- Some(error path key "Expected a name")

                match failure with
                | Some decodeError -> Error decodeError
                | None -> Ok(List.ofSeq pairs)
            | node -> fail path node $"Expected an object but got %s{describe node}"

    /// <summary>A single value or a list of them, which is how humans write YAML.</summary>
    /// <param name="decoder">Reads one entry, whether it was written alone or in a
    /// sequence - <c>tags: draft</c> and <c>tags: [draft]</c> both arrive as a list.</param>
    /// <param name="path">Where in the document the decoder is, for the message when it
    /// fails.</param>
    /// <param name="node">The value being read.</param>
    let listOrSingle (decoder: Decoder<'T>) : Decoder<'T list> =
        fun path node ->
            match node with
            | :? YamlSequenceNode -> list decoder path node
            | node -> decoder path node |> Result.map List.singleton

    let private tryChild (name: string) (mapping: YamlMappingNode) =
        let key = YamlScalarNode name :> YamlNode

        match mapping.Children.TryGetValue key with
        | true, value ->
            // An explicitly empty value ("title:") decodes the same as an absent one.
            match value with
            | :? YamlScalarNode as scalar when
                isNull scalar.Value || scalar.Value = "" || scalar.Value = "~"
                ->
                None
            | value -> Some value
        | _ -> None

    /// Look one field up, saying so when what we are looking in is not an object at all.
    let private child (name: string) (path: string) (node: YamlNode) =
        match node with
        | :? YamlMappingNode as mapping -> Ok(tryChild name mapping)
        | node -> fail path node $"Expected an object but got %s{describe node}"

    /// <summary>A field that must be there and must decode.</summary>
    /// <param name="name">The field to read.</param>
    /// <param name="decoder">How to read its value.</param>
    /// <param name="path">Where in the document the decoder is, for the message when it
    /// fails.</param>
    /// <param name="node">The value being read.</param>
    let field (name: string) (decoder: Decoder<'T>) : Decoder<'T> =
        fun path node ->
            match child name path node with
            | Error decodeError -> Error decodeError
            | Ok None -> fail (childPath path name) node $"Missing required field '%s{name}'"
            | Ok(Some value) -> decoder (childPath path name) value

    /// <summary>A field that may be absent, but must decode when it is there.</summary>
    /// <param name="name">The field to read, if it is there.</param>
    /// <param name="decoder">How to read its value.</param>
    /// <param name="path">Where in the document the decoder is, for the message when it
    /// fails.</param>
    /// <param name="node">The value being read.</param>
    let optional (name: string) (decoder: Decoder<'T>) : Decoder<'T option> =
        fun path node ->
            match child name path node with
            | Error decodeError -> Error decodeError
            | Ok None -> Ok None
            | Ok(Some value) -> decoder (childPath path name) value |> Result.map Some

    /// <summary>A field reached by walking down named fields.</summary>
    /// <param name="names">The path to it, outermost first.</param>
    /// <param name="decoder">How to read what is at the end of it.</param>
    let at (names: string list) (decoder: Decoder<'T>) : Decoder<'T> =
        List.foldBack field names decoder

    /// <summary>Like <see cref="M:Nacara.Core.Decode.at"/>, but a missing step is not a failure.</summary>
    /// <param name="names">The path to it, outermost first. Absent at any step means absent.</param>
    /// <param name="decoder">How to read what is at the end of it.</param>
    /// <param name="path">Where in the document the decoder is, for the message when it
    /// fails.</param>
    /// <param name="node">The value being read.</param>
    let optionalAt (names: string list) (decoder: Decoder<'T>) : Decoder<'T option> =
        fun path node ->
            let rec walk path (node: YamlNode) names =
                match names with
                | [] -> decoder path node |> Result.map Some
                | name :: rest ->
                    match child name path node with
                    | Error decodeError -> Error decodeError
                    | Ok None -> Ok None
                    | Ok(Some value) -> walk (childPath path name) value rest

            walk path node names

    /// <summary>Reads fields that must be there.</summary>
    type IRequiredGetter =
        /// <summary>A field of the object being decoded.</summary>
        abstract Field: string -> Decoder<'T> -> 'T
        /// <summary>A field reached by walking down named fields.</summary>
        abstract At: string list -> Decoder<'T> -> 'T

    /// <summary>Reads fields that may be absent.</summary>
    type IOptionalGetter =
        /// <summary>A field of the object being decoded.</summary>
        abstract Field: string -> Decoder<'T> -> 'T option
        /// <summary>A field reached by walking down named fields.</summary>
        abstract At: string list -> Decoder<'T> -> 'T option

    /// <summary>Reads the fields of a mapping. Used through <c>Decode.object</c>.</summary>
    type IGetters =
        abstract Required: IRequiredGetter
        abstract Optional: IOptionalGetter
        /// <summary>Names of every field present, so a plugin can warn about unknown ones.</summary>
        abstract FieldNames: string list

    let private getters (path: string) (mapping: YamlMappingNode) =
        let run (decoder: Decoder<'T>) =
            match decoder path mapping with
            | Ok value -> value
            | Error decodeError -> raise (DecodeFailure decodeError)

        { new IGetters with
            member _.Required =
                { new IRequiredGetter with
                    member _.Field name decoder = run (field name decoder)
                    member _.At names decoder = run (at names decoder)
                }

            member _.Optional =
                { new IOptionalGetter with
                    member _.Field name decoder = run (optional name decoder)
                    member _.At names decoder = run (optionalAt names decoder)
                }

            member _.FieldNames =
                mapping.Children.Keys
                |> Seq.choose (
                    function
                    | :? YamlScalarNode as scalar -> Some scalar.Value
                    | _ -> None
                )
                |> List.ofSeq
        }

    /// <summary>Decode an object from its fields.</summary>
    /// <param name="build">Given something to read fields with, the value to build. A failure
    /// names the field and its line.</param>
    /// <example>
    /// <code lang="fsharp">
    /// let decoder: Decoder&lt;DocFrontMatter&gt; =
    ///     Decode.object (fun get ->
    ///         {
    ///             Title = get.Required.Field "title" Decode.string
    ///             Order = get.Optional.Field "order" Decode.int
    ///         }
    ///     )
    /// </code>
    /// </example>
    /// <param name="path">Where in the document the decoder is, for the message when it
    /// fails.</param>
    /// <param name="node">The value being read.</param>
    let object (build: IGetters -> 'T) : Decoder<'T> =
        fun path node ->
            match node with
            | :? YamlMappingNode as mapping ->
                try
                    Ok(build (getters path mapping))
                with DecodeFailure decodeError ->
                    Error decodeError
            | node -> fail path node $"Expected an object but got %s{describe node}"

[<RequireQualifiedAccess>]
module Yaml =

    /// <summary>Parse YAML text, returning <c>None</c> for an empty document.</summary>
    /// <param name="text">The document to parse - a page's front matter, usually.</param>
    let parse (text: string) : Result<YamlNode option, DecodeError> =
        try
            let stream = YamlStream()
            stream.Load(new StringReader(text))

            if stream.Documents.Count = 0 then
                Ok None
            else
                Ok(Some stream.Documents[0].RootNode)
        with :? YamlDotNet.Core.YamlException as exn ->
            Error
                {
                    Path = ""
                    Message = exn.Message
                    Line = int exn.Start.Line
                    Column = int exn.Start.Column
                }

    /// <summary>Parse and decode, shifting reported positions by <paramref name="lineOffset" />.</summary>
    /// <param name="lineOffset">How far into the file the text starts, so a position reported
    /// against the front matter points at the right line of the page.</param>
    /// <param name="decoder">What reads the parsed document.</param>
    /// <param name="text">The document to parse.</param>
    /// <remarks>
    /// Front matter sits a few lines down in a markdown file, and the offset is what makes a
    /// decode error point at the right line.
    /// </remarks>
    let decodeWithOffset (lineOffset: int) (decoder: Decoder<'T>) (text: string) =
        let shift (decodeError: DecodeError) =
            { decodeError with
                Line = decodeError.Line + lineOffset
            }

        match parse text with
        | Error decodeError -> Error(shift decodeError)
        | Ok None ->
            Error
                {
                    Path = ""
                    Message = "Expected front matter but the document is empty"
                    Line = 1 + lineOffset
                    Column = 1
                }
        | Ok(Some node) -> decoder "" node |> Result.mapError shift

    /// <summary>Parse and decode a document that stands on its own.</summary>
    /// <param name="decoder">What reads the parsed document.</param>
    /// <param name="text">The document to parse.</param>
    let decode (decoder: Decoder<'T>) (text: string) = decodeWithOffset 0 decoder text

[<RequireQualifiedAccess>]
module DecodeError =

    /// <summary>Report a decode failure as a build diagnostic pointing at the source file.</summary>
    /// <param name="file">The file the value was read from.</param>
    /// <param name="decodeError">What went wrong, and where in the document it was.</param>
    let toDiagnostic (file: AbsolutePath) (decodeError: DecodeError) =
        let path =
            if decodeError.Path = "" then
                ""
            else
                $" (at '%s{decodeError.Path}')"

        Diagnostic.error "nacara/front-matter-invalid" $"%s{decodeError.Message}%s{path}"
        |> Diagnostic.at file decodeError.Line decodeError.Column
