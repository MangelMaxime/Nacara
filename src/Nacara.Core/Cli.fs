namespace Nacara.Core

open System
open System.IO
open System.Threading

/// <summary>Command line of a site.</summary>
[<RequireQualifiedAccess>]
module Nacara =

    type private Options =
        {
            Command: string
            ProjectRoot: AbsolutePath
            Port: int
            Host: string
            Verbose: bool
            Strict: bool
            Global: bool
            Version: string option
            /// Everything after a command the engine does not know, kept for whichever plugin does.
            Rest: string list
        }

    /// The commands the engine answers itself. Anything else belongs to a plugin, arguments
    /// included.
    let private builtIn =
        set
            [
                "help"
                "version"
                "clean"
                "build"
                "check"
                "watch"
                "serve"
            ]

    let private help =
        """nacara - build a documentation site

USAGE
    dotnet run -- <command> [options]

COMMANDS
    build           Build the site once (default)
    watch, serve    Build, serve on localhost and rebuild on change
    check           Build without writing anything, for CI
    clean           Delete the output directory and the build cache beside it
                    --global also empties the shared cache of downloaded tools
    version         Print the engine version

OPTIONS
    --root <dir>    Project root (default: the directory of this site's project)
    --port <n>      Port used by watch (default: 8080)
    --host [name]   Address watch listens on (default: localhost; bare '--host' listens on all)
    --version <v>   Deploy this build under a version prefix
    --strict        Treat warnings as errors, for build and check alike
    --global        With clean, also empty ~/.cache/nacara
    --verbose       Log what the build is doing
    --help          Show this help

NOTE
    Editing content is picked up by watch. Editing layouts or plugins is F# code, so run
    `dotnet watch run -- watch` to have it recompiled and restarted for you."""

    /// <summary>
    /// Where the site's files are, when <c>--root</c> does not say.
    /// </summary>
    /// <remarks>Found by walking up from the assembly to the first project file. A published binary
    /// has none, and falls back to the current directory.</remarks>
    let defaultProjectRoot () =
        let hasProject (directory: DirectoryInfo) =
            directory.EnumerateFiles "*.?sproj" |> Seq.isEmpty |> not

        let rec search (directory: DirectoryInfo) depth =
            if isNull (box directory) || depth > 6 then
                None
            elif hasProject directory then
                Some directory.FullName
            else
                search directory.Parent (depth + 1)

        match search (DirectoryInfo AppContext.BaseDirectory) 0 with
        | Some path -> path
        | None -> Directory.GetCurrentDirectory()

    /// What each option that takes a value wants after it, for when one arrives without.
    let private valued =
        Map
            [
                "--root", "a directory"
                "--port", "a number"
                "--version", "a version"
            ]

    let private parse (argv: string array) =
        let rec loop (options: Options) (remaining: string list) =
            match remaining with
            | [] -> Ok options
            | "--root" :: value :: rest ->
                loop
                    { options with
                        ProjectRoot = AbsolutePath.create (Path.GetFullPath value)
                    }
                    rest
            | "--port" :: value :: rest ->
                match Int32.TryParse value with
                | true, port ->
                    loop
                        { options with
                            Port = port
                        }
                        rest
                | _ -> Error $"'--port' expects a number but got '%s{value}'"
            | "--host" :: value :: rest when not (value.StartsWith "-") ->
                loop
                    { options with
                        Host = value
                    }
                    rest
            | "--host" :: rest ->
                loop
                    { options with
                        Host = "+"
                    }
                    rest
            | "--version" :: value :: rest ->
                loop
                    { options with
                        Version = Some value
                    }
                    rest
            | "--strict" :: rest ->
                loop
                    { options with
                        Strict = true
                    }
                    rest
            | "--global" :: rest ->
                loop
                    { options with
                        Global = true
                    }
                    rest
            | "--verbose" :: rest ->
                loop
                    { options with
                        Verbose = true
                    }
                    rest
            | ("--help" | "-h") :: _ ->
                Ok
                    { options with
                        Command = "help"
                    }
            | flag :: _ when Map.containsKey flag valued ->
                Error $"'%s{flag}' expects %s{valued[flag]} after it"
            | flag :: _ when flag.StartsWith "-" -> Error $"Unknown option '%s{flag}'"
            | command :: rest when builtIn.Contains command ->
                loop
                    { options with
                        Command = command
                    }
                    rest
            | command :: rest ->
                Ok
                    { options with
                        Command = command
                        Rest = rest
                    }

        loop
            {
                Command = "build"
                ProjectRoot = AbsolutePath.create (defaultProjectRoot ())
                Port = 8080
                Host = "localhost"
                Verbose = false
                Strict = false
                Global = false
                Version = None
                Rest = []
            }
            (List.ofArray argv)

    /// <summary>The help, with the commands this site's plugins added.</summary>
    /// <param name="commands">What the plugins registered.</param>
    let private helpWith (commands: PluginCommand list) =
        if List.isEmpty commands then
            help
        else

            let widest =
                commands |> List.map (fun command -> command.Name.Length) |> List.max |> max 15

            let lines =
                commands
                |> List.map (fun command ->
                    let name = command.Name.PadRight widest
                    $"    %s{name} %s{command.Summary} (%s{command.Source})"
                )

            let listed =
                help.Replace(
                    "    version         Print the engine version\n",
                    "    version         Print the engine version\n"
                    + String.concat "\n" lines
                    + "\n"
                )

            listed.Replace(
                "    --help          Show this help\n",
                "    --help          Show this help, or a command's own with '<command> --help'\n"
            )

    let private report (options: Options) (result: BuildResult) =
        result.Diagnostics |> List.iter Log.diagnostic

        let errors =
            result.Diagnostics
            |> List.filter (fun item -> item.Severity = Severity.Error)
            |> List.length

        let warnings =
            result.Diagnostics
            |> List.filter (fun item -> item.Severity = Severity.Warning)
            |> List.length

        let summary =
            [
                $"%i{List.length result.Pages} pages"
                $"%i{result.WrittenFiles} written"
                if result.UnchangedFiles > 0 then
                    $"%i{result.UnchangedFiles} unchanged"
                if result.PrunedFiles > 0 then
                    $"%i{result.PrunedFiles} pruned"
                $"%i{int result.Elapsed.TotalMilliseconds} ms"
            ]
            |> String.concat ", "

        if errors > 0 then
            Log.error $"Build failed: %i{errors} errors, %i{warnings} warnings (%s{summary})"
            1
        elif warnings > 0 && options.Strict then
            Log.error $"Build failed: %i{warnings} warnings, and --strict was given (%s{summary})"
            1
        else
            Log.success $"Built %s{summary}"
            0

    let private emptyOutput (options: Options) (site: Site) =
        let output = AbsolutePath.combine options.ProjectRoot [ site.OutputDirectory ]
        let path = AbsolutePath.value output

        // Somewhere above the project is not this site's to empty.
        if
            path = AbsolutePath.value options.ProjectRoot
            || not (path.StartsWith(AbsolutePath.value options.ProjectRoot + "/"))
        then
            Log.warn
                $"'%s{site.OutputDirectory}' is not inside the project, so it was left as it was"
        elif Directory.Exists path then
            Directory.Delete(path, true)
            Log.debug $"Emptied %s{site.OutputDirectory}"

    let private watch (options: Options) (site: Site) =
        let cache = BuildCache()

        let outputDirectory =
            AbsolutePath.combine options.ProjectRoot [ site.OutputDirectory ]

        let follow = ref (fun (_: BuildResult) -> ())

        let build () =
            let result = Build.runWatch cache options.ProjectRoot site
            report options result |> ignore
            follow.Value result
            result

        let first = build ()

        use server =
            new DevServer(outputDirectory, site.BaseUrl, options.Host, options.Port)

        try
            server.Start()
        with :? System.Net.HttpListenerException ->
            Log.error $"Port %i{options.Port} is already in use"
            Log.info "Serve on another port with --port, or stop whatever is listening"
            exit 1

        Log.success $"Serving %s{server.Url}"
        Log.info "Press Ctrl+C to stop"

        use watcher =
            let watcher =
                new Watcher(
                    options.ProjectRoot,
                    [ AbsolutePath.value outputDirectory ],
                    TimeSpan.FromMilliseconds 80.,
                    fun changes ->
                        let names =
                            changes
                            |> List.truncate 3
                            |> List.map (fun change -> Path.GetFileName change)
                            |> String.concat ", "

                        let extra =
                            if List.length changes > 3 then
                                $" and %i{List.length changes - 3} more"
                            else
                                ""

                        Log.info $"Changed: %s{names}%s{extra}"
                        build () |> ignore
                        server.NotifyReload()
                )

            watcher.Start()
            watcher

        follow.Value <- fun result -> result.Pages |> List.collect _.Dependencies |> watcher.Follow

        follow.Value first

        let quit = new ManualResetEventSlim(false)

        Console.CancelKeyPress.Add(fun args ->
            args.Cancel <- true
            quit.Set()
        )

        quit.Wait()
        Log.info "Stopped"
        0

    /// <summary>Run the site's command line. Call this from your site's <c>main</c> function.</summary>
    /// <param name="site">The site you described.</param>
    /// <param name="argv">The arguments <c>main</c> was given.</param>
    let run (site: Site) (argv: string array) =
        match parse argv with
        | Error message ->
            Log.error message
            printfn ""
            printfn "%s" help
            1
        | Ok options ->
            Log.setVerbose options.Verbose
            Log.debug $"Project root: %s{AbsolutePath.value options.ProjectRoot}"

            let site =
                match options.Version with
                | Some version -> Site.version version site
                | None -> site

            let registry = lazy Registry.ofPlugins site.Plugins

            match options.Command with
            | "help" ->
                printfn "%s" (helpWith registry.Value.Commands)
                0
            | "version" ->
                let version = Reflection.Assembly.GetExecutingAssembly().GetName().Version |> string

                printfn $"nacara %s{version}"
                0
            | "clean" ->
                Build.clean options.ProjectRoot site

                Log.success
                    $"Removed %s{site.OutputDirectory} and %s{ProjectCache.PROJECT_CACHE_DIR_NAME}"

                if options.Global then
                    match Tool.clearCache () with
                    | 0L -> Log.info $"Nothing cached in %s{Tool.cache}"
                    | freed ->
                        let megabytes = float freed / 1_048_576.0
                        Log.success $"Removed %s{Tool.cache} (%.1f{megabytes} MB)"

                0
            | "build" ->
                emptyOutput options site
                report options (Build.run options.ProjectRoot site)
            | "check" -> report options (Build.check options.ProjectRoot site)
            | "watch"
            | "serve" -> watch options site
            | name ->
                match registry.Value.Commands |> List.filter (fun item -> item.Name = name) with
                | first :: _ :: _ as clashing ->
                    let plugins = clashing |> List.map _.Source |> String.concat " and "

                    Log.error $"'%s{first.Name}' is claimed by %s{plugins}"

                    Log.info
                        "Remove one of them, or ask its author to name the command after itself"

                    1
                | [ command ] ->
                    match options.Rest with
                    | "--help" :: _
                    | "-h" :: _ ->
                        match command.Help with
                        | Some help -> printfn "%s" help
                        | None ->
                            printfn $"%s{command.Name} - %s{command.Summary}"
                            printfn ""
                            printfn $"From %s{command.Source}, which documents no arguments."

                        0
                    | rest ->
                        command.Run
                            {
                                Site =
                                    { Site.toInfo site with
                                        PageAssets = Registry.extras<PageAsset> registry.Value
                                    }
                                ProjectRoot = options.ProjectRoot
                                OutputDirectory =
                                    AbsolutePath.combine
                                        options.ProjectRoot
                                        [ site.OutputDirectory ]
                                Arguments = rest
                            }
                | [] ->
                    Log.error $"Unknown command '%s{name}'"
                    printfn ""
                    printfn "%s" (helpWith registry.Value.Commands)
                    1
