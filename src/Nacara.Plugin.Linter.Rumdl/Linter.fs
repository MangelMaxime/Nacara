namespace Nacara.Plugins

open System
open System.Diagnostics
open System.IO
open System.Security.Cryptography
open System.Text
open System.Text.Json
open Nacara.Core
open Nacara.Plugins.Internal

/// <summary>When a lint finding stops a build.</summary>
type LintSeverity =
    /// <summary>Findings are warnings, whatever the build is.</summary>
    | AlwaysWarning
    /// <summary>Findings are errors, whatever the build is.</summary>
    | AlwaysError
    /// <summary>A warning while watching, an error when building - the default.</summary>
    | WarningWhileWatching

/// <summary>Options of the rumdl linter.</summary>
type RumdlOptions =
    {
        /// Path of an existing rumdl binary. Downloaded and cached when not set.
        BinaryPath: string option
        /// <summary>Where that release is published, with <c>{version}</c> and <c>{target}</c> in it.</summary>
        Source: string
        /// <summary>Ship the defaults this plugin was written with.</summary>
        /// <remarks>They are what makes rumdl agree with the engine: the rules it turns off are
        /// the ones Nacara already answers, and better. Turn them off to start from rumdl's own.</remarks>
        UseDefaults: bool
        /// <summary>A <c>rumdl.toml</c> to read, when it is not the one rumdl would find itself.</summary>
        ConfigPath: string option
        /// <summary>Ignore any configuration file, so only what is said here applies.</summary>
        Isolated: bool
        /// <summary>Rule settings, as inline TOML: <c>"MD013.line-length = 100"</c>.</summary>
        /// <remarks>Applied above everything else, so this is where a site settles an argument.</remarks>
        Settings: string list
        /// <summary>Rules to switch off by name - <c>[ "MD033" ]</c>.</summary>
        Disable: string list
        /// Whether a finding is a warning or an error.
        Severity: LintSeverity
        /// <summary>Lint while watching too.</summary>
        /// <remarks>On: it is milliseconds, and a warning is worth having while you write rather
        /// than when you deploy.</remarks>
        LintWhileWatching: bool
    }

/// <summary>
/// Lints the site's markdown with rumdl.
/// </summary>
/// <remarks>
/// <para>The pages of the build are what it reads - the files a collection actually publishes -
/// so a note kept beside your content is not linted, and a generated page has nothing to lint.</para>
/// <para>What it finds becomes ordinary diagnostics, with a position an editor can jump to. A
/// linter that cannot run is a warning rather than a failure - markdown style is never worth being
/// the reason a site does not build.</para>
/// </remarks>
[<RequireQualifiedAccess>]
module Rumdl =

    /// <summary>
    /// What this plugin asks of rumdl before you say anything.
    /// </summary>
    /// <remarks>
    /// Two of these turn a rule off because the engine answers the same question and knows more
    /// while doing it - which pages exist, and what a directive is. The others are the settings a
    /// prose file wants rather than a source file.
    /// </remarks>
    let Defaults =
        [
            // MD057 checks the file system, which calls every link to a generated page broken.
            "MD057.enabled = false"
            // A page says <kbd> with raw HTML, and a documentation theme expects it.
            "MD033.enabled = false"
            "MD013.line-length = 100"
            "MD013.code-blocks = false"
            "MD013.tables = false"
        ]

    let defaults =
        {
            BinaryPath = None
            Source = RumdlBinary.Source
            UseDefaults = true
            ConfigPath = None
            Isolated = false
            Settings = []
            Disable = []
            Severity = WarningWhileWatching
            LintWhileWatching = true
        }

    /// Resolved once for the life of the plugin, not once per build.
    let private binary (options: RumdlOptions) =
        lazy
            (match options.BinaryPath with
             | Some path when File.Exists path -> Ok path
             | Some path -> Error $"no such file: '%s{path}'"
             | None -> RumdlBinary.resolve options.Source)

    /// <summary>What a finding is: rumdl's own JSON, which says everything a diagnostic needs.</summary>
    type Finding =
        {
            File: string
            Line: int
            Column: int
            Rule: string
            Message: string
            Fixable: bool
        }

    /// <summary>Reads what rumdl said, which is a JSON array of findings.</summary>
    /// <param name="json">The output of <c>rumdl check --output-format json</c>.</param>
    /// <returns>One finding per element, with what a diagnostic needs from it.</returns>
    let readFindings (json: string) =
        use document = JsonDocument.Parse json

        [
            for element in document.RootElement.EnumerateArray() do
                let text (name: string) =
                    match element.TryGetProperty name with
                    | true, value when value.ValueKind = JsonValueKind.String -> value.GetString()
                    | _ -> ""

                let number (name: string) =
                    match element.TryGetProperty name with
                    | true, value when value.ValueKind = JsonValueKind.Number -> value.GetInt32()
                    | _ -> 1

                {
                    File = text "file"
                    Line = number "line"
                    Column = number "column"
                    Rule = text "rule"
                    Message = text "message"
                    Fixable =
                        match element.TryGetProperty "fixable" with
                        | true, value -> value.ValueKind = JsonValueKind.True
                        | _ -> false
                }
        ]

    /// <summary>Where rumdl keeps its index of what it has already read.</summary>
    /// <remarks>
    /// Out of the site, where rumdl would put it: a <c>.rumdl_cache</c> beside the content is a
    /// file a watch build writes and then rebuilds on, forever. One directory per project, so two
    /// sites do not share an index.
    /// </remarks>
    let private cacheDirectory (root: AbsolutePath) =
        let key =
            (AbsolutePath.value root).Replace('\\', '/')
            |> Encoding.UTF8.GetBytes
            |> SHA256.HashData
            |> Convert.ToHexString
            |> _.Substring(0, 16)
            |> _.ToLowerInvariant()

        Path.Combine(Tool.cache, "rumdl-cache", key)

    /// <summary>Runs rumdl over a batch of files and reads what it says.</summary>
    /// <summary>The rules, added to a command line.</summary>
    let private configure
        (options: RumdlOptions)
        (root: AbsolutePath)
        (arguments: Collections.ObjectModel.Collection<string>)
        =
        arguments.Add "--cache-dir"
        arguments.Add(cacheDirectory root)

        if options.Isolated then
            arguments.Add "--no-config"

        match options.ConfigPath with
        | Some path ->
            arguments.Add "--config"
            arguments.Add path
        | None -> ()

        let settings =
            [
                if options.UseDefaults then
                    yield! Defaults
                yield! options.Settings
                for rule in options.Disable -> $"%s{rule}.enabled = false"
            ]

        for setting in settings do
            arguments.Add "--config"
            arguments.Add setting

    let private run
        (binary: string)
        (options: RumdlOptions)
        (root: AbsolutePath)
        (files: string list)
        =
        let arguments =
            ProcessStartInfo(binary, RedirectStandardOutput = true, RedirectStandardError = true)

        arguments.ArgumentList.Add "check"
        arguments.ArgumentList.Add "--output-format"
        arguments.ArgumentList.Add "json"
        arguments.ArgumentList.Add "--color"
        arguments.ArgumentList.Add "never"
        arguments.ArgumentList.Add "--quiet"

        configure options root arguments.ArgumentList

        for file in files do
            arguments.ArgumentList.Add file

        use linter = Process.Start arguments
        let output = linter.StandardOutput.ReadToEnd()
        let complaint = linter.StandardError.ReadToEnd()
        linter.WaitForExit()

        // 0 is clean and 1 is "found something"; anything else is rumdl itself failing.
        if linter.ExitCode > 1 then
            Error(
                if String.IsNullOrWhiteSpace complaint then
                    $"rumdl exited with %i{linter.ExitCode}"
                else
                    complaint.Trim()
            )
        else

            try
                Ok(readFindings output)
            with exn ->
                Error $"rumdl said something this plugin could not read: %s{exn.Message}"

    /// <summary>Let rumdl fix what it can, in place.</summary>
    /// <param name="options">What the site configured the plugin with.</param>
    /// <param name="context">The site it was run in, and the paths to fix.</param>
    let private fix (options: RumdlOptions) (context: CommandContext) =
        let root = context.ProjectRoot
        let arguments = context.Arguments

        match (binary options).Value with
        | Error message ->
            Log.error $"rumdl is not available: %s{message}"
            1
        | Ok path ->
            let start = ProcessStartInfo(path)
            start.ArgumentList.Add "fmt"
            configure options root start.ArgumentList

            for target in
                (if List.isEmpty arguments then
                     [ AbsolutePath.value root ]
                 else
                     arguments) do
                start.ArgumentList.Add target

            use rumdl = Process.Start start
            rumdl.WaitForExit()

            if rumdl.ExitCode = 0 then
                Log.success "Markdown formatted"

            rumdl.ExitCode

    type private RumdlPlugin(options: RumdlOptions) =
        let binary = binary options

        interface IPlugin with
            member _.Name = "linter-rumdl"

            member _.Configure registry =
                registry
                |> Registry.command (
                    PluginCommand.create
                        "fmt"
                        "Fix the markdown rumdl can fix, in place"
                        (fix options)
                    |> PluginCommand.help
                        """fmt - fix the markdown rumdl can fix, in place

USAGE
    fmt [path...]

Rewrites your files. With no path it starts from the project root; name files or
directories to narrow it:

    dotnet run --project docs -- fmt docs/content/guide
    dotnet run --project docs -- fmt docs/content/index.md

It uses the rumdl the plugin fetched, with the rules your build lints by, so what
it writes is what the build then accepts."""
                )
                |> Registry.onPagesRouted (fun context ->
                    if context.IsWatch && not options.LintWhileWatching then
                        ()
                    else

                        let files =
                            context.Pages
                            |> List.choose _.SourceFile
                            |> List.map AbsolutePath.value
                            |> List.filter (fun file ->
                                file.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                            )
                            |> List.distinct
                            |> List.sort

                        if not (List.isEmpty files) then
                            match binary.Value with
                            | Error message ->
                                context.Diagnostics.Add(
                                    Diagnostic.warning
                                        "not-linted"
                                        $"Markdown is not linted: %s{message}"
                                    |> Diagnostic.withHint
                                        "The site is built and correct, only unchecked. Set BinaryPath to use your own rumdl."
                                )
                            | Ok binary ->
                                let severity =
                                    match options.Severity with
                                    | AlwaysWarning -> Diagnostic.warning
                                    | AlwaysError -> Diagnostic.error
                                    | WarningWhileWatching ->
                                        if context.IsWatch then
                                            Diagnostic.warning
                                        else
                                            Diagnostic.error

                                let batches = files |> List.chunkBySize 100

                                for batch in batches do
                                    match run binary options context.ProjectRoot batch with
                                    | Error message ->
                                        context.Diagnostics.Add(
                                            Diagnostic.warning
                                                "not-linted"
                                                $"Markdown is not linted: %s{message}"
                                        )
                                    | Ok findings ->
                                        for finding in findings do
                                            let diagnostic =
                                                severity
                                                    $"%s{finding.Rule.ToLowerInvariant()}"
                                                    finding.Message
                                                |> Diagnostic.at
                                                    (AbsolutePath.create (
                                                        Path.GetFullPath finding.File
                                                    ))
                                                    finding.Line
                                                    finding.Column

                                            context.Diagnostics.Add(
                                                if finding.Fixable then
                                                    diagnostic
                                                    |> Diagnostic.withHint
                                                        "rumdl fixes this one itself: run 'rumdl fmt' over the file"
                                                else
                                                    diagnostic
                                            )
                )

    /// <summary>Path of an existing rumdl binary.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let binaryPath value (options: RumdlOptions) =
        { options with
            BinaryPath = value
        }

    /// <summary>Where that release is published, with <c>{version}</c> and <c>{target}</c> in it.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let source value (options: RumdlOptions) =
        { options with
            Source = value
        }

    /// <summary>Ship the defaults this plugin was written with.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let useDefaults value (options: RumdlOptions) =
        { options with
            UseDefaults = value
        }

    /// <summary>A <c>rumdl.toml</c> to read, when it is not the one rumdl would find itself.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let configPath value (options: RumdlOptions) =
        { options with
            ConfigPath = value
        }

    /// <summary>Ignore any configuration file, so only what is said here applies.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let isolated value (options: RumdlOptions) =
        { options with
            Isolated = value
        }

    /// <summary>Rule settings, as inline TOML: <c>"MD013.line-length = 100"</c>.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let settings value (options: RumdlOptions) =
        { options with
            Settings = value
        }

    /// <summary>Rules to switch off by name - <c>[ "MD033" ]</c>.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let disable value (options: RumdlOptions) =
        { options with
            Disable = value
        }

    /// <summary>Whether a finding is a warning or an error.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let severity value (options: RumdlOptions) =
        { options with
            Severity = value
        }

    /// <summary>Lint while watching too.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let lintWhileWatching value (options: RumdlOptions) =
        { options with
            LintWhileWatching = value
        }

    /// <summary>Lint the site's markdown, with the default options.</summary>
    let create () = RumdlPlugin(defaults) :> IPlugin

    /// <summary>Lint the site's markdown, configured.</summary>
    /// <param name="configure">Given the defaults, the options to use. Anything it leaves alone
    /// keeps its default.</param>
    let createWith (configure: RumdlOptions -> RumdlOptions) =
        RumdlPlugin(configure defaults) :> IPlugin

    /// <summary>Lint the site's markdown.</summary>
    /// <param name="site">The site being described.</param>
    let register (site: Site) = Site.plugin (create ()) site

    /// <summary>Lint the site's markdown, configured.</summary>
    /// <param name="configure">Given the defaults, the options to use.</param>
    /// <param name="site">The site being described.</param>
    let registerWith (configure: RumdlOptions -> RumdlOptions) (site: Site) =
        Site.plugin (createWith configure) site
