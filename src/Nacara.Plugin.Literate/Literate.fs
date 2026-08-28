namespace Nacara.Plugins

open System
open System.Diagnostics
open System.Text
open System.Text.RegularExpressions
open System.Threading
open Nacara.Core

/// <summary>A piece of a literate source file.</summary>
type LiterateBlock =
    /// Prose, written in a <c>(** … *)</c> comment.
    | Prose of string
    /// Code, with the fence meta a preceding command asked for.
    | Code of code: string * meta: string
    /// Code the reader should not see: <c>(*** hide ***)</c>.
    | Hidden of string

/// <summary>Options of the literate plugin.</summary>
type LiterateOptions =
    {
        /// Extensions treated as literate source.
        Extensions: string list
        /// Language name used for the generated fences.
        Language: string
        /// Show a "view source" style title on every generated code block.
        DefaultMeta: string
        /// <summary>Check that the sources compile, with <c>dotnet fsi --typecheck-only</c>.</summary>
        /// <remarks>
        /// The file is type-checked, so the example on the page is code that compiles. Nothing
        /// is run.
        /// </remarks>
        TypeCheck: bool
        /// Check while watching too. Off by default: it starts a compiler per file.
        TypeCheckWhileWatching: bool
    }

/// <summary>
/// F# source files as documentation pages.
/// </summary>
/// <remarks>
/// <para>A literate file is ordinary F# that compiles: prose in <c>(** … *)</c> comments,
/// everything else code. The page cannot drift from what it documents, because it <em>is</em> what
/// it documents.</para>
/// <para>It becomes markdown and is handed on, so a literate page gets the same directives, code
/// blocks, highlighting, table of contents and link checking as any other.</para>
/// </remarks>
[<RequireQualifiedAccess>]
module Literate =

    let defaults =
        {
            Extensions =
                [
                    ".fsx"
                    ".fs"
                ]
            Language = "fsharp"
            DefaultMeta = ""
            TypeCheck = true
            TypeCheckWhileWatching = false
        }

    /// <remarks>
    /// A file that starts with <c>---</c> does not compile, so the same block sits inside a
    /// comment. The inner delimiters stay, and are what tell a front matter comment apart from a
    /// prose comment.
    /// </remarks>
    /// <summary>How front matter is carried by a source file rather than a markdown one.</summary>
    /// <param name="options">Which extensions the plugin claims, and what its fences are written
    /// in. An <c>.fsx</c> writes its front matter in the comment it opens with.</param>
    let frontMatterFormat (options: LiterateOptions) =
        {
            Name = "literate"
            Extensions = options.Extensions
            Opening = "---"
            Closing = "---"
            Wrapper = Some("(**", "*)")
        }

    [<Literal>]

    let private CommandStart = "(***"

    [<Literal>]
    let private ProseStart = "(**"

    [<Literal>]
    let private CommentEnd = "*)"

    /// <summary>Read a <c>(*** command ***)</c> line, when the line is one.</summary>
    let private command (line: string) =
        let trimmed = line.Trim()

        if trimmed.StartsWith CommandStart && trimmed.EndsWith "***)" then
            trimmed.Substring(CommandStart.Length, trimmed.Length - CommandStart.Length - 4).Trim()
            |> Some
        else
            None

    /// <summary>How many comments a line opens, minus how many it closes.</summary>
    let private depthChange (text: string) =
        let count (needle: string) =
            let mutable total = 0
            let mutable index = text.IndexOf needle

            while index >= 0 do
                total <- total + 1
                index <- text.IndexOf(needle, index + needle.Length)

            total

        count "(*" - count "*)"

    /// <summary>Content of a line up to the <c>*)</c> that closes the block.</summary>
    let private beforeClose (text: string) =
        match text.LastIndexOf CommentEnd with
        | -1 -> text
        | index -> text.Substring(0, index)

    /// <summary>Split a source file into what it says and what it does.</summary>
    /// <param name="source">The file's text. Lines inside <c>(*** … ***)</c> are prose, everything
    /// else is code, and the order of both is kept.</param>
    let parse (source: string) =
        let lines = source.Replace("\r\n", "\n").Split('\n')
        let blocks = ResizeArray<LiterateBlock>()
        let pending = StringBuilder()
        let mutable meta = ""
        let mutable hide = false
        let mutable index = 0

        let flushCode () =
            // Not about the file: trimming \n off a \r\n leaves the \r inside the snippet.
            let code = pending.ToString().Replace("\r\n", "\n").Trim('\n')
            pending.Clear() |> ignore

            if code.Trim() <> "" then
                if hide then
                    blocks.Add(Hidden code)
                else
                    blocks.Add(Code(code, meta))

            meta <- ""
            hide <- false

        let addProse (prose: StringBuilder) =
            let lines = prose.ToString().Split('\n') |> Array.map _.TrimEnd()

            let common =
                lines
                |> Array.filter (fun line -> line.Trim() <> "")
                |> Array.map (fun line -> line.Length - line.TrimStart().Length)
                |> function
                    | [||] -> 0
                    | indents -> Array.min indents

            blocks.Add(
                Prose(
                    lines
                    |> Array.map (fun line ->
                        if line.Length >= common then
                            line.Substring common
                        else
                            line.TrimStart()
                    )
                    |> String.concat "\n"
                    |> fun text -> text.Trim('\n')
                )
            )

        while index < lines.Length do
            let line = lines[index]

            match command line with
            | Some name ->
                flushCode ()

                match name with
                | "hide" -> hide <- true
                | other -> meta <- other

                index <- index + 1

            | None when line.TrimStart().StartsWith ProseStart ->
                flushCode ()
                let prose = StringBuilder()
                let opening = line.TrimStart().Substring ProseStart.Length
                let mutable depth = 1 + depthChange opening

                if depth <= 0 then
                    prose.Append(beforeClose opening) |> ignore
                    index <- index + 1
                else
                    prose.AppendLine opening |> ignore
                    index <- index + 1

                    while index < lines.Length && depth > 0 do
                        let current = lines[index]
                        depth <- depth + depthChange current

                        if depth <= 0 then
                            prose.AppendLine(beforeClose current) |> ignore
                        else
                            prose.AppendLine current |> ignore

                        index <- index + 1

                addProse prose

            | None ->
                pending.AppendLine line |> ignore
                index <- index + 1

        flushCode ()
        List.ofSeq blocks

    /// <summary>Write the blocks out as the markdown pipeline will read them.</summary>
    /// <param name="options">What language the fences are labelled with, and what annotations they
    /// carry by default.</param>
    /// <param name="blocks">Prose and code, in the order the file wrote them.</param>
    let toMarkdown (options: LiterateOptions) (blocks: LiterateBlock list) =
        let builder = StringBuilder()

        for block in blocks do
            match block with
            | Hidden _ -> ()
            | Prose text -> builder.AppendLine(text).AppendLine "" |> ignore
            | Code(code, meta) ->
                let meta =
                    [
                        options.DefaultMeta
                        meta
                    ]
                    |> List.filter (fun value -> value <> "")
                    |> String.concat " "

                builder
                    .AppendLine($"```%s{options.Language} %s{meta}".TrimEnd())
                    .AppendLine(code)
                    .AppendLine("```")
                    .AppendLine
                    ""
                |> ignore

        builder.ToString()

    /// What the F# compiler says, in the shape it says it: path(line,col): severity FSxxxx: text.
    let private compilerMessage =
        Regex(
            @"^(?<file>.+?)\((?<line>\d+),(?<column>\d+)\):\s*(?<severity>error|warning)\s+(?<code>FS\d+):\s*(?<message>.*)$",
            RegexOptions.Compiled
        )

    /// <summary>Run the type checker over one file and turn what it says into diagnostics.</summary>
    let private typeCheckFile (file: AbsolutePath) =
        let startInfo =
            ProcessStartInfo(
                "dotnet",
                $"fsi --typecheck-only --nologo \"%s{AbsolutePath.value file}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            )

        try
            use fsi = Process.Start startInfo
            let output = fsi.StandardOutput.ReadToEnd()
            let error = fsi.StandardError.ReadToEnd()
            fsi.WaitForExit()

            let reported =
                (output + "\n" + error).Replace("\r\n", "\n").Split('\n')
                |> Array.choose (fun line ->
                    let matched = compilerMessage.Match line

                    if not matched.Success then
                        None
                    else

                        let position =
                            int matched.Groups["line"].Value, int matched.Groups["column"].Value

                        let text =
                            $"""%s{matched.Groups["code"].Value}: %s{matched.Groups["message"].Value}"""

                        Some(matched.Groups["severity"].Value, position, text)
                )
                |> List.ofArray

            if fsi.ExitCode <> 0 && List.isEmpty reported then
                let firstLine =
                    (error + output).Trim().Split('\n')
                    |> Array.tryHead
                    |> Option.defaultValue "no output"

                [ "error", (1, 1), $"The type checker failed: %s{firstLine}" ]
            else
                reported
        with exn ->
            [ "error", (1, 1), $"The type checker could not be started: %s{exn.Message}" ]

    /// <summary>
    /// Check that every literate page compiles.
    /// </summary>
    let private typeCheckAll (options: LiterateOptions) (context: HookContext) =
        if not options.TypeCheck then
            ()
        elif context.IsWatch && not options.TypeCheckWhileWatching then
            Log.debug "Skipping the literate type check while watching"
        else

            let sources =
                context.Pages
                |> List.choose (fun page ->
                    match page.Source with
                    | FromFile file ->
                        let extension =
                            IO.Path.GetExtension(AbsolutePath.value file).ToLowerInvariant()

                        if List.contains extension options.Extensions then
                            Some(page, file)
                        else
                            None
                    | Generated _ -> None
                )
                |> List.distinctBy (fun (_, file) -> AbsolutePath.value file)

            if not (List.isEmpty sources) then
                Log.debug $"Type checking %i{List.length sources} literate files"

                let gate = new SemaphoreSlim(max 1 (Environment.ProcessorCount / 2))

                sources
                |> List.map (fun (page, file) ->
                    async {
                        do! gate.WaitAsync() |> Async.AwaitTask

                        try
                            return file, typeCheckFile file
                        finally
                            gate.Release() |> ignore
                    }
                )
                |> Async.Parallel
                |> Async.RunSynchronously
                |> Array.iter (fun (file, messages) ->
                    for severity, (line, column), text in messages do
                        let diagnostic =
                            if severity = "error" then
                                Diagnostic.error "does-not-compile" text
                            else
                                Diagnostic.warning "compiler-warning" text

                        context.Diagnostics.Add(
                            diagnostic
                            |> Diagnostic.withHint
                                "The page is the file, so what the compiler says about the file is about the page"
                            |> Diagnostic.at file line column
                        )
                )

    type private LiteratePlugin(options: LiterateOptions) =
        interface IPlugin with
            member _.Name = "literate"

            member _.Configure registry =
                registry
                // A source file cannot start with --- and still compile.
                |> Registry.frontMatter (frontMatterFormat options)
                |> Registry.transform
                    {
                        Name = "literate"
                        Extensions = options.Extensions
                        Transform =
                            fun _ page ->
                                { page with
                                    Body = page.Body |> parse |> toMarkdown options
                                    Format = ".md"
                                }
                    }
                |> Registry.onPagesRouted (typeCheckAll options)

    /// <summary>Extensions treated as literate source.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let extensions value (options: LiterateOptions) =
        { options with
            Extensions = value
        }

    /// <summary>Language name used for the generated fences.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let language value (options: LiterateOptions) =
        { options with
            Language = value
        }

    /// <summary>Show a "view source" style title on every generated code block.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let defaultMeta value (options: LiterateOptions) =
        { options with
            DefaultMeta = value
        }

    /// <summary>Check that the sources compile, with <c>dotnet fsi --typecheck-only</c>.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let typeCheck value (options: LiterateOptions) =
        { options with
            TypeCheck = value
        }

    /// <summary>Check while watching too.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let typeCheckWhileWatching value (options: LiterateOptions) =
        { options with
            TypeCheckWhileWatching = value
        }

    /// <summary>F# source files as pages, with the default options.</summary>
    let create () = LiteratePlugin(defaults) :> IPlugin

    /// <summary>F# source files as pages, configured.</summary>
    /// <param name="configure">Given the defaults, the options to use. Anything it leaves alone keeps its default.</param>
    let createWith (configure: LiterateOptions -> LiterateOptions) =
        LiteratePlugin(configure defaults) :> IPlugin

    /// <summary>Add literate F# to a site.</summary>
    let register (site: Site) = Site.plugin (create ()) site

    /// <summary>Add literate F# to a site, configured.</summary>
    /// <param name="configure">Given the defaults, the options to use. Anything it leaves alone keeps its default.</param>
    /// <param name="site">The site being described.</param>
    let registerWith (configure: LiterateOptions -> LiterateOptions) (site: Site) =
        Site.plugin (createWith configure) site
