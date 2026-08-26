namespace Nacara.Plugins.Internal

open System
open System.IO
open System.IO.Compression
open System.Runtime.InteropServices
open System.Text
open System.Text.RegularExpressions
open Nacara.Plugins.Internal.Native

/// <summary>A grammar, loaded and ready to be asked about a piece of source.</summary>
type LoadedGrammar =
    {
        Language: nativeint
        Query: nativeint
    }

/// <summary>
/// Parsing and querying, which is all a highlighter needs from tree-sitter.
/// </summary>
module Grammar =

    let private stride = Marshal.SizeOf<TSQueryCapture>()
    let private stepSize = Marshal.SizeOf<TSQueryPredicateStep>()

    let private engine = lazy wasm_engine_new ()

    /// <summary>
    /// One store serves the whole build, and a store is not thread safe.
    /// </summary>
    let private gate = obj ()

    let private store =
        lazy
            (let mutable error = TSWasmError()
             let store = ts_wasm_store_new (engine.Value, &error)

             if store = 0n then
                 failwith
                     "This tree-sitter was built without the wasm feature, so it has nowhere to load a grammar into"

             store)

    /// <summary>
    /// One parser, bound to the store for as long as the build lasts.
    /// </summary>
    let private parser =
        lazy
            (let parser = ts_parser_new ()
             ts_parser_set_wasm_store (parser, store.Value)
             parser)

    /// <summary>The bytes of a file, gzipped or not.</summary>
    let bytesOf (path: string) =
        if path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase) then
            use file = File.OpenRead path
            use stream = new GZipStream(file, CompressionMode.Decompress)
            use memory = new MemoryStream()
            stream.CopyTo memory
            memory.ToArray()
        else
            File.ReadAllBytes path

    /// <summary>Loads a wasm grammar and compiles the queries that colour it.</summary>
    let loadFrom (language: string) (blob: byte array) (source: byte array) =
        lock
            gate
            (fun () ->
                let mutable error = TSWasmError()

                let loaded =
                    ts_wasm_store_load_language (
                        store.Value,
                        language,
                        blob,
                        uint32 blob.Length,
                        &error
                    )

                if loaded = 0n then
                    let message = Marshal.PtrToStringAnsi error.message
                    failwith $"The grammar '%s{language}' did not load: %s{message}"

                let mutable offset = 0u
                let mutable kind = 0
                let query = ts_query_new (loaded, source, uint32 source.Length, &offset, &kind)

                if query = 0n then
                    failwith $"The queries of '%s{language}' did not compile, at byte %i{offset}"

                {
                    Language = loaded
                    Query = query
                }
            )

    /// <summary>The same, from two files on disk.</summary>
    let load (language: string) (wasm: string) (queries: string) =
        loadFrom language (bytesOf wasm) (bytesOf queries)

    let private capture (found: TSQueryMatch) (index: int) =
        Marshal.PtrToStructure<TSQueryCapture>(found.captures + nativeint (index * stride))

    let private literal (query: nativeint) (id: uint32) =
        let mutable length = 0u
        Marshal.PtrToStringAnsi(ts_query_string_value_for_id (query, id, &length), int length)

    let private named (query: nativeint) (id: uint32) =
        let mutable length = 0u
        Marshal.PtrToStringAnsi(ts_query_capture_name_for_id (query, id, &length), int length)

    /// <summary>
    /// The sentences a pattern asks its matches to satisfy.
    /// </summary>
    let private predicatesOf (query: nativeint) (pattern: uint32) =
        let mutable count = 0u
        let steps = ts_query_predicates_for_pattern (query, pattern, &count)

        [ 0 .. int count - 1 ]
        |> List.map (fun index ->
            Marshal.PtrToStructure<TSQueryPredicateStep>(steps + nativeint (index * stepSize))
        )
        |> List.fold
            (fun (sentences, current) (step: TSQueryPredicateStep) ->
                match step.kind with
                | 0u -> sentences @ [ List.rev current ], []
                | 1u -> sentences, (Choice1Of2 step.valueId :: current)
                | _ -> sentences, (Choice2Of2(literal query step.valueId) :: current)
            )
            ([], [])
        |> fst

    /// <summary>
    /// Whether a match is one its pattern meant to keep.
    /// </summary>
    let private satisfied (query: nativeint) (source: byte array) (found: TSQueryMatch) =
        let text (node: TSNode) =
            // A node says where it is in bytes, not characters.
            let start = int (ts_node_start_byte node)
            let stop = int (ts_node_end_byte node)
            Encoding.UTF8.GetString(source, start, stop - start)

        let captured (id: uint32) =
            [ 0 .. int found.captureCount - 1 ]
            |> List.map (capture found)
            |> List.tryFind (fun candidate -> candidate.index = id)
            |> Option.map (fun candidate -> text candidate.node)

        let value =
            function
            | Choice1Of2 id -> captured id
            | Choice2Of2 word -> Some word

        predicatesOf query (uint32 found.patternIndex)
        |> List.forall (fun sentence ->
            match sentence with
            | Choice2Of2 name :: arguments ->
                match name, arguments |> List.map value with
                | "match?", [ Some subject; Some pattern ] -> Regex.IsMatch(subject, pattern)
                | "not-match?", [ Some subject; Some pattern ] ->
                    not (Regex.IsMatch(subject, pattern))
                | "eq?", [ Some left; Some right ] -> left = right
                | "not-eq?", [ Some left; Some right ] -> left <> right
                | "any-of?", Some subject :: rest -> rest |> List.contains (Some subject)
                | _ -> true
            | _ -> true
        )

    /// <summary>One parse, and what the query made of it.</summary>
    let private read (grammar: LoadedGrammar) (bytes: byte array) =
        if not (ts_parser_set_language (parser.Value, grammar.Language)) then
            failwith "The parser refused the grammar"

        let tree = ts_parser_parse_string (parser.Value, 0n, bytes, uint32 bytes.Length)
        let root = ts_tree_root_node tree
        let guessed = ts_node_has_error root
        let cursor = ts_query_cursor_new ()
        ts_query_cursor_exec (cursor, grammar.Query, root)

        // A capture's pattern number says which of two readings of the same node the query meant to win.
        let found = ResizeArray<int * int * int * string>()
        let mutable matched = TSQueryMatch()

        // Matches rather than captures: a predicate speaks about a whole match.
        while ts_query_cursor_next_match (cursor, &matched) do
            if satisfied grammar.Query bytes matched then
                for index in 0 .. int matched.captureCount - 1 do
                    let capture = capture matched index

                    found.Add(
                        int (ts_node_start_byte capture.node),
                        int (ts_node_end_byte capture.node),
                        int matched.patternIndex,
                        named grammar.Query capture.index
                    )

        ts_query_cursor_delete cursor
        ts_tree_delete tree
        guessed, List.ofSeq found

    /// <summary>
    /// Every capture of the highlights query, in byte order, predicates applied.
    /// </summary>
    /// <param name="grammar">The loaded grammar and its queries.</param>
    /// <param name="continuation">What this language's snippets continue from, when one does not
    /// stand on its own.</param>
    /// <param name="code">The source to read.</param>
    let captures (grammar: LoadedGrammar) (continuation: string option) (code: string) =
        lock
            gate
            (fun () ->
                let bytes = Encoding.UTF8.GetBytes code
                let guessed, found = read grammar bytes

                match continuation with
                | Some continuation when guessed ->
                    let prefix = Encoding.UTF8.GetBytes(continuation + "\n")
                    let both = Array.append prefix bytes
                    let stillGuessed, other = read grammar both

                    if stillGuessed then
                        found
                    else
                        other
                        |> List.choose (fun (start, stop, pattern, capture) ->
                            if start < prefix.Length then
                                None
                            else
                                Some(
                                    start - prefix.Length,
                                    stop - prefix.Length,
                                    pattern,
                                    capture
                                )
                        )
                | _ -> found
            )
