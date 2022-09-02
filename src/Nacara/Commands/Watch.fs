module Nacara.Commands.Watch

open Nacara
open Nacara.Core
open Nacara.Evaluator
open System
open System.IO
open Spectre.Console
open System.Diagnostics
open FSharp.Compiler.Interactive.Shell
open Saturn
open Nacara.Server
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Builder
open AspNetCore.NacaraLoggerExtensions

let private keepAlive () =

    // Keep the program alive until the user presses Ctrl+C
    Console.CancelKeyPress.AddHandler(fun _ ea ->
        ea.Cancel <- true
        Log.info "Received Ctrl+C, shutting down..."
        exit 0
    )

    while true do
        Console.ReadKey(true) |> ignore

type OnSourceFiledChanged = OnSourceFiledChanged of (AbsolutePath.AbsolutePath -> unit)
type OnSourceFiledCreated = OnSourceFiledCreated of (AbsolutePath.AbsolutePath -> unit)
type OnSourceFiledDeleted = OnSourceFiledDeleted of (AbsolutePath.AbsolutePath -> unit)
type OnRendererChanged = OnRendererChanged of (AbsolutePath.AbsolutePath -> unit)

let private createSourceWatcher
    (OnSourceFiledChanged onChanged: OnSourceFiledChanged)
    (OnSourceFiledCreated onCreated: OnSourceFiledCreated)
    (OnSourceFiledDeleted onDeleted: OnSourceFiledDeleted)
    (context: Context)
    =
    Watcher.create
        (AbsolutePath.value context.SourcePath)
        (fun changes ->
            changes
            |> Seq.iter (fun fileChange ->

                [
                    // Adding some spaces for easier readability
                    ""
                    ""
                    "Change detected, rebuilding site."
                ]
                |> String.concat "\n"
                |> Log.info

                Log.info
                    $"Source changed %s{AbsolutePath.toLog fileChange.FullPath}: %A{fileChange.Status}"

                match fileChange.Status with
                | Watcher.Changed ->
                    // Don't handle directory changes
                    if not (AbsolutePath.isDirectory fileChange.FullPath) then
                        onChanged fileChange.FullPath

                | Watcher.Created ->
                    // Don't handle directory creations
                    if not (AbsolutePath.isDirectory fileChange.FullPath) then
                        onCreated fileChange.FullPath

                | Watcher.Deleted ->
                    onDeleted fileChange.FullPath
            )
        )

let private createRendererDepencyWatcher
    (info: DependencyWatchInfo)
    (onChange: AbsolutePath.AbsolutePath -> unit)
    =

    Watcher.createWithFilters
        (AbsolutePath.getDirectoryName info.DependencyPath)
        [
            AbsolutePath.getFileName info.DependencyPath
        ]
        (fun changes ->
            changes
            |> Seq.iter (fun fileChange ->
                [
                    // Adding some spaces for easier readability
                    ""
                    ""
                    "Change detected, rebuilding site."
                ]
                |> String.concat "\n"
                |> Log.info
                // signalUpdate.raise()
                match fileChange.Status with
                | Watcher.Changed ->
                    [
                        $"Dependency of a renderer changed"
                        $"    Dependency: \"%s{AbsolutePath.value info.DependencyPath}\""
                        $"    Renderer: \"%s{AbsolutePath.value info.RendererPath}\""
                    ]
                    |> String.concat "\n"
                    |> Log.info

                    onChange info.RendererPath
                | _ ->
                    Log.debug
                        $"File status not supported yet: %A{fileChange.Status}"
            )
        )

let private createRendererWatcher
    (OnRendererChanged onChange: OnRendererChanged)
    (absolutePath: AbsolutePath.AbsolutePath)
    =

    Watcher.createWithFilters
        (AbsolutePath.getDirectoryName absolutePath)
        [
            AbsolutePath.getFileName absolutePath
        ]
        (fun changes ->
            changes
            |> Seq.iter (fun fileChange ->
                [
                    // Adding some spaces for easier readability
                    ""
                    ""
                    "Change detected, rebuilding site."
                ]
                |> String.concat "\n"
                |> Log.info
                // signalUpdate.raise()
                match fileChange.Status with
                | Watcher.Changed ->
                    Log.info
                        $"Renderer changed %s{AbsolutePath.toLog fileChange.FullPath}: %A{fileChange.Status}"

                    onChange fileChange.FullPath

                | _ ->
                    Log.debug
                        $"File status not supported yet: %A{fileChange.Status}"
            )
        )

let private runSetup
    (fsi: FsiEvaluationSession)
    (context: Context)
    (registerDependencyForWatch: DependencyWatchInfo -> unit)
    =

    let sw = Stopwatch.StartNew()
    // Clean artifacts from previous builds
    Directory.Delete(AbsolutePath.value context.OutputPath, true)

    // Ensure that the output directory exists.
    context.OutputPath
    |> AbsolutePath.value
    |> Directory.CreateDirectory
    |> ignore

    Log.info ""
    Log.info "Generating site..."

    let (validPages, erroredPages) = Shared.extractFiles context

    // Store the valid pages in the context
    // Like that the pages can be accessed when during the rendering process
    // Example: To generate some navigation
    validPages |> Seq.iter context.Add

    // Report the errors and continue because we are in watch mode
    erroredPages |> Array.iter Log.error

    // Render the pages
    validPages
    |> Array.iter (fun pageContext ->
        Shared.renderPage
            fsi
            context
            pageContext
            (Some registerDependencyForWatch)
        |> ignore
    )

    sw.Stop()
    Log.success $"Site generated in [bold]%i{sw.ElapsedMilliseconds}[/] ms"

let private createLocalServer (context: Context) =
    application {
        url $"http://localhost:%i{context.Config.Port}"
        no_router
        use_static (AbsolutePath.value context.OutputPath)

        logging (fun builder ->
            builder.ClearProviders().AddNacaraLogger() |> ignore

            builder.SetMinimumLevel LogLevel.Warning |> ignore
        )

        app_config (fun builder ->
            builder
                .UseWebSockets()
                .UseMiddleware<LiveReloadWebSockets.LiveReloadWebSocketMiddleware>()
        )
    }

let private cleanState
    (localServer: byref<IHost>)
    (sassCompilerOpt: byref<Process option>)
    (context: Context)
    (watchers: ResizeArray<IDisposable>)
    =

    // Dispose the previous watchers
    watchers |> Seq.iter (fun watcher -> watcher.Dispose())
    watchers.Clear()

    // Clear the renderer cache
    // Is this really needed? For now, I will leave it here
    // because it is not hurting the performance too much
    RendererEvaluator.clearCache ()

    match sassCompilerOpt with
    | Some sassCompiler ->
        sassCompiler.Kill()
        sassCompilerOpt <- None
    | None -> ()

    // Dispose the previous server
    localServer.StopAsync() |> Async.AwaitTask |> Async.RunSynchronously

    // Start the new server
    localServer <- (createLocalServer context).Build()

let private runServer (context: Context) (server: IHost) =
    server.RunAsync() |> Async.AwaitTask |> Async.StartImmediate

    [
        ""
        $"Server started at: http://localhost:%i{context.Config.Port}"
    ]
    |> String.concat "\n"
    |> Log.info

let private onConfigChange
    (fsi: FsiEvaluationSession)
    (context: byref<Context>)
    (sassCompiler: byref<Process option>)
    (localServer: byref<IHost>)
    (watchers: ResizeArray<IDisposable>)
    (onSourceFileChanged: OnSourceFiledChanged)
    (onSourceFileCreated: OnSourceFiledCreated)
    (onSourceFileDeleted: OnSourceFiledDeleted)
    (onRenderedChanged: OnRendererChanged)
    (registerDependencyForWatch: DependencyWatchInfo -> unit)
    =
    AnsiConsole.Clear()
    Log.info "Configuration changed - Restarting..."
    // 1. Create a new context
    let newContext = Shared.createContext ()
    context <- newContext

    // 2. Load the new configuration
    Shared.loadConfigOrExit fsi newContext

    // 3. Clean the different memorized states
    cleanState &localServer &sassCompiler newContext watchers

    // 4. Re-run the setup phase because the config has changed
    runSetup fsi newContext registerDependencyForWatch

    // 5. Start the new server, because now the new files are available
    runServer newContext localServer

    // 6. Start the new sass compiler
    match newContext.Config.Sass with
    | Some sassArgs ->
        sassCompiler <- Some(Sass.watch context.ProjectRoot sassArgs)
    | None -> ()

    // 7. Notify the client that the site has been updated
    LiveReloadWebSockets.notifyClientsToReload ()

    // 8. Register the watchers
    watchers.Add(
        createSourceWatcher
            onSourceFileChanged
            onSourceFileCreated
            onSourceFileDeleted
            context
    )

    newContext.Config.Render
    |> List.iter (fun renderer ->
        let absolutePath =
            Path.Combine(
                ProjectRoot.value newContext.ProjectRoot,
                renderer.Script
            )
            |> AbsolutePath.create

        watchers.Add(createRendererWatcher onRenderedChanged absolutePath)
    )

let private onSourceFileEventSharedLogic
    (fsi: FsiEvaluationSession)
    (context: Context)
    (registerDependencyForWatch: DependencyWatchInfo -> unit)
    (newPagesInMemory: PageContext ResizeArray)
    (sw: Stopwatch)
    =

    context.Replace newPagesInMemory

    // Re-render all the pages
    newPagesInMemory
    |> Seq.iter (fun pageContext ->
        Shared.renderPage
            fsi
            context
            pageContext
            (Some registerDependencyForWatch)
        |> ignore
    )

    sw.Stop()
    Log.success $"Site generated in [bold]%i{sw.ElapsedMilliseconds}[/] ms"

    LiveReloadWebSockets.notifyClientsToReload ()

let private onSourceFileDeleted
    (fsi: FsiEvaluationSession)
    (context: Context)
    (registerDependencyForWatch: DependencyWatchInfo -> unit)
    (pathOfChangedFile: AbsolutePath.AbsolutePath)
    =
    Log.info ""
    Log.info "Generating site..."
    let sw = Stopwatch.StartNew()

    // Try to delete the previously rendered file
    context.TryGetValues<PageContext>()
    |> Option.map (fun items ->
        items
        |> Seq.tryFind (fun pageContext ->
            pageContext.AbsolutePath = pathOfChangedFile
        )
    )
    |> Option.flatten
    |> Option.iter (fun pageContext -> Shared.deletePage context pageContext)

    // Update the page context list
    let newPagesInMemory =
        match context.TryGetValues<PageContext>() with
        | Some knownPages ->
            knownPages
            |> Seq.filter (fun currentPageContext ->
                currentPageContext.AbsolutePath <> pathOfChangedFile
            )
            |> ResizeArray

        | None -> ResizeArray []

    onSourceFileEventSharedLogic
        fsi
        context
        registerDependencyForWatch
        newPagesInMemory
        sw

let private onSourceFileCreated
    (fsi: FsiEvaluationSession)
    (context: Context)
    (registerDependencyForWatch: DependencyWatchInfo -> unit)
    (pathOfChangedFile: AbsolutePath.AbsolutePath)
    =

    Log.info ""
    Log.info "Generating site..."
    let sw = Stopwatch.StartNew()

    match Shared.extractFile context pathOfChangedFile with
    | Ok pageContext ->

        let newPagesInMemory =
            match context.TryGetValues<PageContext>() with
            | Some knownPages -> ResizeArray knownPages

            | None -> ResizeArray Seq.empty

        newPagesInMemory.Add pageContext

        onSourceFileEventSharedLogic
            fsi
            context
            registerDependencyForWatch
            newPagesInMemory
            sw

    | Error errorMessage ->
        sw.Stop()
        Log.error errorMessage

let private onSourceFileChanged
    (fsi: FsiEvaluationSession)
    (context: Context)
    (registerDependencyForWatch: DependencyWatchInfo -> unit)
    (pathOfChangedFile: AbsolutePath.AbsolutePath)
    =
    Log.info ""
    Log.info "Generating site..."
    let sw = Stopwatch.StartNew()

    match Shared.extractFile context pathOfChangedFile with
    | Ok pageContext ->

        let newPagesInMemory =
            match context.TryGetValues<PageContext>() with
            | Some knownPages ->
                knownPages
                |> Seq.map (fun currentPageContext ->
                    // If the page context is the same as the one we want to update
                    if
                        currentPageContext.AbsolutePath = pageContext.AbsolutePath
                    then
                        pageContext
                    else
                        currentPageContext
                )
                |> ResizeArray

            | None ->
                ResizeArray
                    [
                        pageContext
                    ]

        onSourceFileEventSharedLogic
            fsi
            context
            registerDependencyForWatch
            newPagesInMemory
            sw

    | Error errorMessage ->
        sw.Stop()
        Log.error errorMessage

let execute () =
    let mutable context = Shared.createContext ()
    use fsi = EvaluatorHelpers.fsi context
    Shared.loadConfigOrExit fsi context

    let mutable server = (createLocalServer context).Build()
    let mutable sassCompiler = None

    let watchers = ResizeArray()
    let registeredDependencyForWatchCache = ResizeArray()

    let rec registerDependencyForWatch (info: DependencyWatchInfo) =
        if not (registeredDependencyForWatchCache.Contains info) then
            let onChange (pathOfChangedFile: AbsolutePath.AbsolutePath) =
                RendererEvaluator.removeItemFromCache pathOfChangedFile
                runSetup fsi context registerDependencyForWatch

            registeredDependencyForWatchCache.Add info
            watchers.Add(createRendererDepencyWatcher info onChange)

    let onSourceFileChanged =
        onSourceFileChanged fsi context registerDependencyForWatch
        |> OnSourceFiledChanged

    let onSourceFileCreated =
        onSourceFileCreated fsi context registerDependencyForWatch
        |> OnSourceFiledCreated

    let onSourceFileDeleted =
        onSourceFileDeleted fsi context registerDependencyForWatch
        |> OnSourceFiledDeleted

    let onRenderedChanged =
        OnRendererChanged(fun (pathOfChangedFile: AbsolutePath.AbsolutePath) ->
            RendererEvaluator.removeItemFromCache pathOfChangedFile
            runSetup fsi context registerDependencyForWatch
        )

    // Watch nacara.fsx file for changes
    use _ =
        Watcher.createWithFilters
            (ProjectRoot.value context.ProjectRoot)
            [
                "nacara.fsx"
            ]
            (fun _ ->
                onConfigChange
                    fsi
                    &context
                    &sassCompiler
                    &server
                    watchers
                    onSourceFileChanged
                    onSourceFileCreated
                    onSourceFileDeleted
                    onRenderedChanged
                    registerDependencyForWatch
            )

    // 1. Run the setup phase
    runSetup fsi context registerDependencyForWatch
    // 2. Start the server
    runServer context server
    // 3. Notify clients to reload, it can happen that the user
    // has a browser tab open and he killed then restarted the server manually
    LiveReloadWebSockets.notifyClientsToReload ()
    // 4. Register the watchers
    watchers.Add(
        createSourceWatcher
            onSourceFileChanged
            onSourceFileCreated
            onSourceFileDeleted
            context
    )

    context.Config.Render
    |> List.iter (fun renderer ->
        let absolutePath =
            Path.Combine(
                ProjectRoot.value context.ProjectRoot,
                renderer.Script
            )
            |> AbsolutePath.create

        watchers.Add(createRendererWatcher onRenderedChanged absolutePath)
    )

    keepAlive ()
    0
