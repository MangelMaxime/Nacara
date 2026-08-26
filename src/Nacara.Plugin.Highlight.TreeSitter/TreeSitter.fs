namespace Nacara.Plugins

open System
open System.Collections.Concurrent
open System.IO
open System.IO.Compression
open System.Reflection
open System.Text
open Nacara.Core
open Nacara.Plugins.Internal

/// <summary>Where a grammar comes from.</summary>
type TreeSitterGrammarSource =
    /// <summary>Two files already built: the wasm, gzipped or not, and its queries.</summary>
    | Files of wasm: string * queries: string
    /// <summary>A repository and a commit of it, built once and kept.</summary>
    | Repository of
        repository: string *
        reference: string *
        subdirectory: string option *
        queries: string option
    /// <summary>One of the grammars this package ships, read out of it.</summary>
    | Bundled

/// <summary>A grammar this site can colour with: what it is called, and where it comes from.</summary>
type TreeSitterGrammar =
    {
        /// What a fence writes after its backticks.
        Language: string
        /// Other names the same fence is written with, on top of the ones already known.
        Aliases: string list
        /// <summary>What a snippet of this language continues from, when it is a fragment.</summary>
        /// <remarks>Used only when a snippet does not parse on its own, and dropped from what
        /// comes out.</remarks>
        Continuation: string option
        /// Files to read, or a repository to build them from.
        Source: TreeSitterGrammarSource
    }

/// <summary>Options of the tree-sitter highlighter.</summary>
type TreeSitterOptions =
    {
        /// The grammars to load, one per language.
        Grammars: TreeSitterGrammar list
        /// <summary>Where the two native libraries are: tree-sitter and wasmtime.</summary>
        /// <remarks>Left unset, they are fetched once into a cache under the user's profile - the
        /// pair for this platform, and no other. Set it to run against a build of your own, and
        /// nothing is downloaded.</remarks>
        RuntimePath: string option
        /// <summary>Where that pair is published, with <c>{version}</c> and <c>{rid}</c> in it.</summary>
        /// <remarks>Point it at a mirror for a build that cannot reach the npm registry.</remarks>
        RuntimeSource: string
        /// <summary>Fall back to the grammars this package ships.</summary>
        /// <remarks>A language named in <c>Grammars</c> is used ahead of a shipped one, so a site
        /// overrides F# without giving up the rest.</remarks>
        UseBundledGrammars: bool
        /// <summary>Build a grammar named by repository when it is not already built.</summary>
        /// <remarks>Turned off, a grammar missing from the cache is an error saying where it was
        /// looked for, which is what an offline or locked-down build wants.</remarks>
        AutoBuild: bool
        /// <summary>Where the tree-sitter CLI is published, with <c>{version}</c> and
        /// <c>{platform}</c> in it.</summary>
        CliSource: string
        /// <summary>Where the wasi-sdk is published, with <c>{major}</c>, <c>{version}</c> and
        /// <c>{platform}</c> in it.</summary>
        WasiSdkSource: string
    }

[<RequireQualifiedAccess>]
module TreeSitter =

    let defaults =
        {
            Grammars = []
            RuntimePath = None
            RuntimeSource = Runtime.Source
            UseBundledGrammars = true
            AutoBuild = true
            CliSource = Toolchain.CliSource
            WasiSdkSource = Toolchain.WasiSdkSource
        }

    /// <summary>
    /// What a fragment of a language hangs on, for the languages written about in fragments.
    /// </summary>
    let private continuations = [ "fsharp", "()" ]

    let private continuationOf (language: string) =
        continuations
        |> List.tryFind (fun (name, _) -> name = language.ToLowerInvariant())
        |> Option.map snd

    /// <summary>A grammar, from the two files it takes.</summary>
    /// <param name="language">The name a fence writes to ask for it.</param>
    /// <param name="wasm">The compiled grammar.</param>
    /// <param name="queries">The highlights query saying what its nodes mean.</param>
    let grammar (language: string) (wasm: string) (queries: string) =
        {
            Language = language
            Aliases = []
            Continuation = continuationOf language
            Source = Files(wasm, queries)
        }

    /// <summary>
    /// A grammar, from the repository it lives in.
    /// </summary>
    /// <remarks>Fetched and compiled to wasm the first time, then kept. Nothing has to be installed,
    /// and nothing installed is used.</remarks>
    /// <param name="language">What a fence writes after its backticks.</param>
    /// <param name="repository">The repository, as its web address.</param>
    /// <param name="reference">A branch, a tag or a commit. A commit is the one that cannot move
    /// under a site.</param>
    let fromGitHub (language: string) (repository: string) (reference: string) =
        {
            Language = language
            Aliases = []
            Continuation = continuationOf language
            Source = Repository(repository, reference, None, None)
        }

    /// <summary>
    /// The languages this package ships a grammar for.
    /// </summary>
    /// <remarks>Inside the assembly, so the first site built on a machine colours its F#, its shell
    /// commands and its configuration files without compiling anything.</remarks>
    let bundledLanguages =
        lazy
            (Assembly.GetExecutingAssembly().GetManifestResourceNames()
             |> Array.choose (fun name ->
                 // grammars/<language>/grammar.wasm.gz
                 let parts = name.Split '/'

                 if parts.Length = 3 && parts[2] = "grammar.wasm.gz" then
                     Some parts[1]
                 else
                     None
             )
             |> Array.sort
             |> List.ofArray)

    /// <summary>A file of a shipped grammar, as it sits inside the assembly.</summary>
    let private shipped (language: string) (name: string) =
        let assembly = Assembly.GetExecutingAssembly()

        match assembly.GetManifestResourceStream $"grammars/%s{language}/%s{name}" with
        | null -> failwith $"This build of the plugin ships no '%s{name}' for %s{language}"
        | stream ->
            use stream = stream
            use memory = new MemoryStream()
            stream.CopyTo memory
            memory.ToArray()

    /// <summary>Grammars whose queries describe only what they add to another language.</summary>
    let private inherited = [ "typescript", "javascript" ]

    /// <summary>The queries a shipped language is coloured by, with any it builds on.</summary>
    /// <remarks>What the language adds comes last: a later pattern wins the bytes it covers, so a
    /// node both queries claim is coloured the way the more specific one asked.</remarks>
    /// <param name="language">The language whose queries are wanted.</param>
    let bundledQueries (language: string) =
        let own = shipped language "highlights.scm"

        match inherited |> List.tryFind (fst >> (=) language) with
        | Some(_, parent) ->
            Array.concat
                [
                    shipped parent "highlights.scm"
                    "\n"B
                    own
                ]
        | None -> own

    /// <summary>The parse tables of a shipped language, gzipped as they are stored.</summary>
    /// <param name="language">The language whose grammar is wanted.</param>
    let bundledGrammar (language: string) = shipped language "grammar.wasm.gz"

    /// <summary>A shipped grammar is kept gzipped, the way one in a repository would be.</summary>
    let private unzipped (blob: byte array) =
        use packed = new MemoryStream(blob)
        use stream = new GZipStream(packed, CompressionMode.Decompress)
        use memory = new MemoryStream()
        stream.CopyTo memory
        memory.ToArray()

    /// <summary>One of the grammars this package ships.</summary>
    let bundled (language: string) =
        {
            Language = language
            Aliases = []
            Continuation = continuationOf language
            Source = Bundled
        }

    /// <summary>What a snippet of this language continues from, when it is a fragment.</summary>
    /// <param name="text">What to parse ahead of the snippet, so a fragment stands alone.</param>
    /// <param name="grammar">The grammar so far.</param>
    let continuedFrom (text: string) (grammar: TreeSitterGrammar) =
        { grammar with
            Continuation = Some text
        }

    /// <summary>Which directory of the repository holds the grammar, when it is not the top one.</summary>
    /// <param name="subdirectory">Where in the repository the grammar lives.</param>
    /// <param name="grammar">The grammar so far.</param>
    let inDirectory (subdirectory: string) (grammar: TreeSitterGrammar) =
        match grammar.Source with
        | Bundled
        | Files _ -> grammar
        | Repository(repository, reference, _, queries) ->
            { grammar with
                Source = Repository(repository, reference, Some subdirectory, queries)
            }

    /// <summary>Which file of the repository says what the nodes mean, when it is not where they
    /// usually are.</summary>
    /// <param name="path">Where the highlights query is, inside the repository.</param>
    /// <param name="grammar">The grammar so far.</param>
    let queriesAt (path: string) (grammar: TreeSitterGrammar) =
        match grammar.Source with
        | Bundled -> grammar
        | Files(wasm, _) ->
            { grammar with
                Source = Files(wasm, path)
            }
        | Repository(repository, reference, subdirectory, _) ->
            { grammar with
                Source = Repository(repository, reference, subdirectory, Some path)
            }

    /// <summary>Names this grammar answers to besides its own.</summary>
    /// <param name="names">The other names a fence may write.</param>
    /// <param name="grammar">The grammar so far.</param>
    let aliases (names: string list) (grammar: TreeSitterGrammar) =
        { grammar with
            Aliases = names
        }

    /// <summary>Fence names that mean the same language.</summary>
    let private families =
        [
            [
                "fsharp"
                "fs"
                "fsx"
                "fsi"
            ]
            [
                "csharp"
                "cs"
            ]
            [
                "javascript"
                "js"
                "jsx"
                "mjs"
                "cjs"
            ]
            [
                "typescript"
                "ts"
                "mts"
                "cts"
            ]
            [
                "python"
                "py"
            ]
            [
                "ruby"
                "rb"
            ]
            [
                "rust"
                "rs"
            ]
            [
                "bash"
                "sh"
                "shell"
                "zsh"
            ]
            [
                "yaml"
                "yml"
            ]
            [
                "markdown"
                "md"
            ]
            [
                "haskell"
                "hs"
            ]
            [
                "kotlin"
                "kt"
            ]
            [
                "powershell"
                "ps1"
                "pwsh"
            ]
            [
                "html"
                "htm"
            ]
            [
                "cpp"
                "c++"
                "cc"
                "cxx"
                "hpp"
            ]
            [
                "dockerfile"
                "docker"
            ]
            [
                "makefile"
                "make"
            ]
        ]

    /// <summary>Every name a grammar answers to: its own, the ones it lists, and their family.</summary>
    let namesOf (grammar: TreeSitterGrammar) =
        let own =
            grammar.Language :: grammar.Aliases
            |> List.map (fun name -> name.ToLowerInvariant())

        families
        |> List.filter (List.exists (fun name -> List.contains name own))
        |> List.concat
        |> List.append own
        |> Set.ofList

    /// <summary>
    /// What a capture name means to the theme.
    /// </summary>
    /// <remarks>
    /// Capture names are a convention rather than a standard, and they nest: <c>variable.parameter</c>
    /// falls back to <c>variable</c> when nothing claims the whole of it. What comes out is the
    /// vocabulary the stylesheet is written against.
    /// </remarks>
    let className (capture: string) =
        let known =
            [
                "keyword", "tok-keyword"
                "type", "tok-type"
                "constructor", "tok-constructor"
                "function", "tok-function"
                "method", "tok-function"
                "string", "tok-string"
                "character", "tok-string"
                "comment", "tok-comment"
                "number", "tok-number"
                "float", "tok-number"
                "boolean", "tok-constant"
                "constant", "tok-constant"
                "escape", "tok-escape"
                "operator", "tok-operator"
                "punctuation", "tok-punctuation"
                "namespace", "tok-namespace"
                "module", "tok-namespace"
                "attribute", "tok-attribute"
                "property", "tok-property"
                "label", "tok-attribute"
                "tag", "tok-tag"
                "variable.parameter", "tok-parameter"
                "variable.other.member", "tok-property"
                "variable.builtin", "tok-variable"
                "variable", "tok-variable"
            ]

        let rec shorten (name: string) =
            match known |> List.tryFind (fun (key, _) -> key = name) with
            | Some(_, cssClass) -> Some cssClass
            | None ->
                match name.LastIndexOf '.' with
                | -1 -> None
                | cut -> shorten (name.Substring(0, cut))

        shorten capture

    /// <summary>
    /// The pieces of one line, coloured.
    /// </summary>
    let private colour (code: string) (captures: (int * int * int * string) list) =
        let bytes = Encoding.UTF8.GetBytes code
        let owner: string option array = Array.create bytes.Length None

        for start, stop, _, capture in
            captures |> List.sortBy (fun (start, stop, pattern, _) -> start - stop, pattern) do
            match className capture with
            | None -> ()
            | Some cssClass ->
                for index in start .. (min stop bytes.Length) - 1 do
                    owner[index] <- Some cssClass

        let lines = ResizeArray<Token list>()
        let current = ResizeArray<Token>()
        let piece = StringBuilder()
        let mutable pieceClass = None
        let mutable index = 0

        let flush () =
            if piece.Length > 0 then
                current.Add
                    {
                        Text = piece.ToString()
                        ClassName = pieceClass
                    }

                piece.Clear() |> ignore

        while index < bytes.Length do
            let width =
                // One character may be several bytes, and a capture never splits one.
                let head = bytes[index]

                if head < 0x80uy then
                    1
                elif head < 0xE0uy then
                    2
                elif head < 0xF0uy then
                    3
                else
                    4

            let character = Encoding.UTF8.GetString(bytes, index, width)

            if character = "\n" then
                flush ()
                lines.Add(List.ofSeq current)
                current.Clear()
                pieceClass <- None
            else
                if owner[index] <> pieceClass then
                    flush ()
                    pieceClass <- owner[index]

                piece.Append character |> ignore

            index <- index + width

        flush ()

        if current.Count > 0 || lines.Count = 0 then
            lines.Add(List.ofSeq current)

        List.ofSeq lines

    /// <summary>A highlighter backed by tree-sitter grammars compiled to wasm.</summary>
    type TreeSitterHighlighter(options: TreeSitterOptions) =
        let loaded =
            ConcurrentDictionary<string, Lazy<(LoadedGrammar * string option) option>>()

        let problems = ConcurrentDictionary<string, string>()

        let runtime =
            lazy
                (let resolved =
                    match options.RuntimePath with
                    | Some where -> Ok where
                    | None -> Runtime.resolve options.RuntimeSource

                 match resolved with
                 | Ok where ->
                     Native.lookIn where
                     true
                 | Error message ->
                     problems.TryAdd("runtime", message) |> ignore
                     false)

        let grammarFor (language: string) =
            // ConcurrentDictionary is free to run its factory once per caller for the same key.
            loaded
                .GetOrAdd(
                    language.ToLowerInvariant(),
                    fun key ->
                        lazy
                            (options.Grammars
                             |> List.tryFind (fun candidate -> (namesOf candidate).Contains key)
                             |> Option.orElseWith (fun () ->
                                 if options.UseBundledGrammars then
                                     bundledLanguages.Value
                                     |> List.map bundled
                                     |> List.tryFind (fun candidate ->
                                         (namesOf candidate).Contains key
                                     )
                                 else
                                     None
                             )
                             |> Option.map (fun candidate ->
                                 let loaded =
                                     match candidate.Source with
                                     | Bundled ->
                                         Grammar.loadFrom
                                             candidate.Language
                                             (shipped candidate.Language "grammar.wasm.gz"
                                              |> unzipped)
                                             (bundledQueries candidate.Language)
                                     | Files(wasm, queries) ->
                                         Grammar.load candidate.Language wasm queries
                                     | Repository(repository, reference, subdirectory, queries) ->
                                         match
                                             Toolchain.ensure
                                                 {
                                                     Language = candidate.Language
                                                     Repository = repository
                                                     Reference = reference
                                                     Subdirectory = subdirectory
                                                     Queries = queries
                                                 }
                                                 options.AutoBuild
                                                 options.CliSource
                                                 options.WasiSdkSource
                                         with
                                         | Error message -> failwith message
                                         | Ok(wasm, queries) ->
                                             Grammar.load candidate.Language wasm queries

                                 loaded, candidate.Continuation
                             ))
                )
                .Value

        /// A grammar that will not load is this language's problem, not the build's to crash on.
        let tryGrammarFor (language: string) =
            try
                grammarFor language
            with exn ->
                problems.TryAdd(language.ToLowerInvariant(), exn.Message) |> ignore
                None

        /// <summary>Resolves the runtime, before any page asks for a colour.</summary>
        member _.Warm() = runtime.Value |> ignore

        /// <summary>What could not be done. Taking clears it, so each is said once.</summary>
        /// <remarks>The runtime comes first, since nothing works without it, then a line per
        /// language whose grammar could not be had.</remarks>
        member _.TakeProblems() =
            problems.Keys
            |> Seq.sortBy (fun key ->
                if key = "runtime" then
                    ""
                else
                    key
            )
            |> Seq.toList
            |> List.choose (fun key ->
                match problems.TryRemove key with
                | true, message -> Some(key, message)
                | _ -> None
            )

        interface IHighlighter with
            member _.Name = "tree-sitter"

            member _.Highlight(language, code) =
                match language with
                | None -> None
                | Some _ when not runtime.Value -> None
                | Some language ->
                    match tryGrammarFor language with
                    | None -> None
                    | Some grammar ->
                        // A capture is a byte range, so a carriage return left in the source ends up inside a token.
                        let code = code.Replace("\r\n", "\n").TrimEnd('\n')

                        // What is not thread safe is the store, and Grammar owns that lock.
                        let grammar, continuation = grammar
                        Grammar.captures grammar continuation code |> colour code |> Some

    type private TreeSitterPlugin(options: TreeSitterOptions) =
        let highlighter = lazy (TreeSitterHighlighter(options))

        /// <summary>Says what could not be had, as a diagnostic rather than as an exception.</summary>
        let report (context: HookContext) =
            for language, message in highlighter.Value.TakeProblems() do
                let diagnostic =
                    if language = "runtime" then
                        Diagnostic.error "tree-sitter/runtime-missing" message
                        |> Diagnostic.withHint
                            "Set RuntimePath to a directory holding the two libraries, or allow the build to fetch them"
                    else
                        Diagnostic.error
                            "tree-sitter/grammar-failed"
                            $"The '%s{language}' grammar could not be loaded: %s{message}"
                        |> Diagnostic.withHint
                            "A grammar and its queries have to come from the same commit; naming one by commit is what keeps them together"

                context.Diagnostics.Add diagnostic

        interface IPlugin with
            member _.Name = "highlight-tree-sitter"

            member _.Configure registry =
                registry
                |> Registry.extra (highlighter.Value :> IHighlighter)
                |> Registry.onPagesRouted (fun context ->
                    highlighter.Value.Warm()
                    report context
                )
                |> Registry.onBuildComplete report

    /// <summary>The grammars to load, one per language.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let grammars value (options: TreeSitterOptions) =
        { options with
            Grammars = value
        }

    /// <summary>Where the two native libraries are: tree-sitter and wasmtime.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let runtimePath value (options: TreeSitterOptions) =
        { options with
            RuntimePath = value
        }

    /// <summary>Where that pair is published, with <c>{version}</c> and <c>{rid}</c> in it.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let runtimeSource value (options: TreeSitterOptions) =
        { options with
            RuntimeSource = value
        }

    /// <summary>Fall back to the grammars this package ships.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let useBundledGrammars value (options: TreeSitterOptions) =
        { options with
            UseBundledGrammars = value
        }

    /// <summary>Build a grammar named by repository when it is not already built.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let autoBuild value (options: TreeSitterOptions) =
        { options with
            AutoBuild = value
        }

    /// <summary>Where the tree-sitter CLI is published, with <c>{version}</c> and <c>{platform}</c> in it.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let cliSource value (options: TreeSitterOptions) =
        { options with
            CliSource = value
        }

    /// <summary>Where the wasi-sdk is published, with <c>{major}</c>, <c>{version}</c> and <c>{platform}</c> in it.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let wasiSdkSource value (options: TreeSitterOptions) =
        { options with
            WasiSdkSource = value
        }

    /// <summary>The plugin, with the grammars this package ships.</summary>
    let create () = TreeSitterPlugin(defaults) :> IPlugin

    /// <summary>The plugin, configured.</summary>
    let createWith (configure: TreeSitterOptions -> TreeSitterOptions) =
        TreeSitterPlugin(configure defaults) :> IPlugin

    /// <summary>Colour code blocks with the grammars this package ships.</summary>
    let register (site: Site) = Site.plugin (create ()) site

    /// <summary>Colour code blocks with tree-sitter, with languages of your own.</summary>
    /// <param name="configure">Given the defaults, the options to use.</param>
    /// <param name="site">The site you are describing.</param>
    let registerWith (configure: TreeSitterOptions -> TreeSitterOptions) (site: Site) =
        Site.plugin (createWith configure) site
