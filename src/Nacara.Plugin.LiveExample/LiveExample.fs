namespace Nacara.Plugins

open System
open System.IO
open System.Reflection
open Nacara.Core
open Nacara.Plugins.Internal

/// <summary>
/// F# snippets a reader can run and edit, compiled in their browser.
/// </summary>
/// <remarks>
/// <para>A live block is an ordinary code block until someone presses Run, so a reader who never
/// does - or who has JavaScript off - loses nothing. Run fetches the compiler once for the page and
/// the result runs in a frame of its own.</para>
/// <para>Mark a block with <c>live</c>, and name a preset when it needs one:</para>
/// <code lang="markdown">
/// ```fsharp live
/// printfn "hello"
/// ```
///
/// ```fsharp live preset=elmish
/// open Elmish
/// ```
/// </code>
/// </remarks>
[<RequireQualifiedAccess>]
module LiveExample =

    let private readResource = Resource.text (Assembly.GetExecutingAssembly())

    /// <summary>A script emitted as bytes, so no asset transform reads it.</summary>
    let private asBytes (content: string) (path: string) =
        WriteBytes(Text.Encoding.UTF8.GetBytes content, RelativePath.create path)

    let private styles = lazy readResource "live-example.css"
    let private script = lazy readResource "live-example.js"
    let private highlightWorker = lazy readResource "highlight-worker.js"
    let private editor = lazy readResource "codemirror.js"

    /// <summary>The options a site starts from.</summary>
    let defaults =
        {
            Presets = []
            Highlighting = DefaultHighlighting
            Fable = Vendor.Default
            Tab = None
            Target = None
            Css = None
            Template = None
            Stats = false
            FableTool = None
            OutputGrammars = []
        }

    /// <summary>Add a preset a fence can name.</summary>
    /// <remarks>Built with <see cref="T:Nacara.Plugins.LiveExamplePreset" /> and handed over
    /// whole.</remarks>
    /// <param name="value">The preset, as
    /// <see cref="M:Nacara.Plugins.LiveExamplePreset.create" /> and its setters made it.</param>
    /// <param name="options">The options so far.</param>
    let preset (value: LiveExamplePreset) (options: LiveExampleOptions) =
        { options with
            Presets = options.Presets @ [ value ]
        }

    /// <summary>The stylesheet used by a preset that names none of its own.</summary>
    /// <param name="value">The CSS file, relative to the project root.</param>
    /// <param name="options">The options so far.</param>
    let defaultCss value (options: LiveExampleOptions) =
        { options with
            Css = Some value
        }

    /// <summary>The page used by a preset that names none of its own.</summary>
    /// <param name="value">The HTML file, relative to the project root.</param>
    /// <param name="options">The options so far.</param>
    let defaultTemplate value (options: LiveExampleOptions) =
        { options with
            Template = Some value
        }

    /// <summary>Set how an edited snippet colours itself.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let highlighting value (options: LiveExampleOptions) =
        { options with
            Highlighting = value
        }

    /// <summary>Set which tab a snippet opens on once it has run.</summary>
    /// <remarks>Left alone it opens on the console, or on the result when the snippet drew
    /// something and printed nothing.</remarks>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let tab value (options: LiveExampleOptions) =
        { options with
            Tab = Some value
        }

    /// <summary>Set what a snippet is compiled to when its fence does not say.</summary>
    /// <remarks>Only <c>JavaScript</c> runs in a browser. Any other target compiles and shows its
    /// code.</remarks>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let target value (options: LiveExampleOptions) =
        { options with
            Target = Some value
        }

    /// <summary>Show what each compile cost, in a tab of its own.</summary>
    /// <remarks>Off by default: it is for whoever is tuning the site, not for whoever is reading
    /// it. Rows accumulate rather than replacing each other, so an edit can be compared with the
    /// run before it.</remarks>
    /// <param name="value">Whether to show it.</param>
    /// <param name="options">The options so far.</param>
    let stats value (options: LiveExampleOptions) =
        { options with
            Stats = value
        }

    /// <summary>Precompile with a Fable of your own instead of the one that matches.</summary>
    /// <remarks>
    /// <para>Left alone, the build fetches the Fable the browser's compiler was built from and
    /// precompiles with that, so the two agree whatever is installed on the machine.</para>
    /// <para>Set it when working on Fable itself: point it at a local build and the snippets are
    /// precompiled with that one. Keeping the two in step is then yours, and the build says when
    /// they have drifted.</para>
    /// </remarks>
    /// <param name="value">The <c>fable</c> executable to run.</param>
    /// <param name="options">The options so far.</param>
    let fableTool value (options: LiveExampleOptions) =
        { options with
            FableTool = Some value
        }

    /// <summary>Add a grammar for colouring the output of a target.</summary>
    /// <remarks>Every language Fable targets is coloured already, so name a grammar here only to
    /// override one, or to colour a language Fable does not target. Declared the same way the
    /// highlighting plugin declares one, so a site that already colours Gleam blocks can hand over
    /// the grammar it has.</remarks>
    /// <param name="value">The grammar, named after the language it colours.</param>
    /// <param name="options">The options so far.</param>
    let outputGrammar value (options: LiveExampleOptions) =
        { options with
            OutputGrammars = options.OutputGrammars @ [ value ]
        }

    /// <summary>Set which build of the Fable compiler snippets are compiled by.</summary>
    /// <remarks>Pinned by default, to the pair this plugin was built against. <c>Latest</c> follows
    /// Fable without waiting for a release of this plugin, so two builds of the same commit can
    /// differ; the log says which Fable it found.</remarks>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let fable value (options: LiveExampleOptions) =
        { options with
            Fable = value
        }

    type private LiveExamplePlugin(options: LiveExampleOptions) =
        interface IPlugin with
            member _.Name = "live-example"

            member _.Configure registry =
                let treeSitter = options.Highlighting = TreeSitterHighlighting

                let projects =
                    options.Presets
                    |> List.choose (fun preset ->
                        preset.Project |> Option.map (fun path -> preset.Name, path)
                    )

                let clash =
                    match projects with
                    | (first, _) :: (second, _) :: _ ->
                        Some(
                            "duplicate-precompiled-project",
                            $"The presets '%s{first}' and '%s{second}' each name a project",
                            "One library is precompiled for the whole site, so only one preset can name a project"
                        )
                    | _ -> None

                let precompiled =
                    let project = projects |> List.tryHead |> Option.map snd

                    let cli =
                        let expected = Vendor.fableVersion options.Fable

                        match options.FableTool, expected with
                        | Some tool, _ -> Some(NamedFable(tool, expected))
                        | None, Some version -> Some(FetchedFable version)
                        | None, None -> None

                    let built =
                        match cli, project with
                        | _ when clash.IsSome -> None
                        | Some cli, Some project ->
                            let root = Nacara.defaultProjectRoot ()

                            let path =
                                if Path.IsPathRooted project then
                                    project
                                else
                                    Path.Combine(root, project)

                            Some(Vendor.precompileProject (AbsolutePath.create root) cli path)
                        | _ -> None

                    match built with
                    | None -> None
                    | Some(Ok modules) -> Some modules
                    | Some(Error message) ->
                        Log.warn
                            $"A snippet carries its library rather than referencing it: %s{message}"

                        None

                let tag = precompiled |> Option.map Vendor.precompileTag

                let registry, coloured, layout, browserFable =
                    match
                        Vendor.assets
                            treeSitter
                            options.OutputGrammars
                            LiveExampleTarget.languages
                            options.Fable
                            tag
                    with
                    | Ok(assets, coloured, layout, fable) ->
                        assets |> List.fold (fun acc asset -> Registry.asset asset acc) registry,
                        coloured,
                        Some layout,
                        fable
                    | Error message ->
                        Log.warn $"Live examples are disabled: %s{message}"
                        registry, [], None, None

                let precompiled =
                    match precompiled with
                    | None -> None
                    | Some modules ->
                        match Vendor.agrees browserFable modules with
                        | Ok() -> Some modules
                        | Error message ->
                            Log.warn
                                $"A snippet carries its library rather than referencing it: %s{message}"

                            None

                let registry =
                    match layout, precompiled with
                    | Some layout, Some modules ->
                        Vendor.precompiledAssets layout modules
                        |> List.fold (fun acc asset -> Registry.asset asset acc) registry
                    | _ -> registry

                let registry =
                    registry
                    |> Registry.asset (
                        WriteText(
                            styles.Value,
                            RelativePath.create $"%s{Vendor.Directory}/live-example.css"
                        )
                    )
                    |> Registry.extra (Stylesheet $"%s{Vendor.Directory}/live-example.css")
                    |> Registry.asset (
                        WriteText(
                            LiveExampleConfig.targets options coloured + script.Value,
                            RelativePath.create $"%s{Vendor.Directory}/live-example.js"
                        )
                    )
                    |> Registry.extra (Script($"%s{Vendor.Directory}/live-example.js", true))
                    |> Registry.asset (asBytes editor.Value $"%s{Vendor.Directory}/codemirror.js")

                let registry =
                    if treeSitter then
                        registry
                        |> Registry.asset (
                            asBytes
                                highlightWorker.Value
                                $"%s{Vendor.Directory}/tree-sitter/highlight-worker.js"
                        )
                    else
                        registry

                registry
                |> Registry.codeBlockCheck LiveExampleFences.check
                |> Registry.onPagesRouted (fun context ->
                    match clash with
                    | Some(code, message, hint) ->
                        context.Diagnostics.Add(
                            Diagnostic.error code message |> Diagnostic.withHint hint
                        )
                    | None -> ()

                    match options.Presets |> List.filter _.IsDefault with
                    | _ :: _ :: _ as many ->
                        let named = many |> List.map _.Name |> String.concat ", "

                        context.Diagnostics.Add(
                            Diagnostic.error
                                "duplicate-default-preset"
                                $"More than one preset is the default: %s{named}"
                            |> Diagnostic.withHint
                                "A fence that names no preset gets the one marked with LiveExamplePreset.asDefault - so only one can be"
                        )
                    | _ -> ()

                    let presets =
                        LiveExamplePresets.read context.ProjectRoot context.Diagnostics options

                    context.Write
                        $"%s{Vendor.Directory}/config.json"
                        (LiveExampleConfig.configuration
                            options
                            layout
                            (precompiled |> Option.map Vendor.precompiledInfo)
                            presets)
                    |> ignore
                )

    /// <summary>The plugin, with its options as they are.</summary>
    let create () = LiveExamplePlugin(defaults) :> IPlugin

    /// <summary>The plugin, with its options changed.</summary>
    /// <param name="configure">What to change about them.</param>
    let createWith (configure: LiveExampleOptions -> LiveExampleOptions) =
        LiveExamplePlugin(configure defaults) :> IPlugin

    /// <summary>Let a site run its F# snippets in the browser.</summary>
    /// <param name="site">The site being described.</param>
    let register (site: Site) = Site.plugin (create ()) site

    /// <summary>Let a site run its F# snippets in the browser, configured.</summary>
    /// <param name="configure">Given the defaults, the options to use. Anything it leaves alone keeps its default.</param>
    /// <param name="site">The site being described.</param>
    let registerWith (configure: LiveExampleOptions -> LiveExampleOptions) (site: Site) =
        Site.plugin (createWith configure) site
