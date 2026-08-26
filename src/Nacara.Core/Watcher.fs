namespace Nacara.Core

open System
open System.IO
open System.Threading

/// <summary>
/// Watches the project for changes and coalesces them into rebuilds.
/// </summary>
/// <remarks>
/// <para>Editors write files in bursts, so changes are debounced. Output, <c>.nacara</c>,
/// <c>obj</c> and <c>bin</c> are ignored, or the build would trigger itself forever.</para>
/// <para>The project is what is watched by default, and <see cref="M:Nacara.Core.Watcher.Follow" />
/// adds the files a build said it read from somewhere else - a changelog in a sibling directory,
/// an assembly an API reference is generated from.</para>
/// </remarks>
type Watcher
    (
        projectRoot: AbsolutePath,
        ignoredDirectories: string list,
        debounce: TimeSpan,
        onChange: string list -> unit
    )
    =
    let pending = System.Collections.Generic.HashSet<string>()
    let gate = obj ()
    let mutable timer: Timer = null

    /// Files outside the project a build said it depends on, and the directory watchers reaching
    /// them. One watcher per directory, since watching a file means watching what contains it.
    let mutable followedFiles = Set.empty<string>
    let mutable followedDirectories: Map<string, FileSystemWatcher> = Map.empty

    let isIgnored (path: string) =
        let normalized = path.Replace('\\', '/')

        ignoredDirectories
        |> List.exists (fun ignored ->
            let ignored = ignored.Replace('\\', '/').TrimEnd('/')
            normalized.StartsWith(ignored + "/") || normalized = ignored
        )
        || normalized.Contains "/.git/"
        || normalized.Contains $"/%s{ProjectCache.PROJECT_CACHE_DIR_NAME}/"
        || normalized.Contains "/obj/"
        || normalized.Contains "/bin/"
        || normalized.Contains "/node_modules/"
        || normalized.EndsWith "~"
        || Path.GetFileName(normalized).StartsWith "."

    /// Hand over what has piled up, and start again empty.
    let fire () =
        let changes =
            lock
                gate
                (fun () ->
                    let changes = List.ofSeq pending
                    pending.Clear()
                    changes
                )

        if not (List.isEmpty changes) then
            onChange changes

    /// Queue a path whatever the ignore rules say - for the files a build asked to follow.
    let forceQueue (path: string) =
        lock gate (fun () -> pending.Add path |> ignore)

        // One-shot: no period, only a due time, pushed back by every event.
        if isNull timer then
            timer <- new Timer((fun _ -> fire ()), null, debounce, Timeout.InfiniteTimeSpan)
        else
            timer.Change(debounce, Timeout.InfiniteTimeSpan) |> ignore

    /// Queue a path from the project, unless it is one of the many nobody means to watch.
    let queue (path: string) =
        if not (isIgnored path) then
            forceQueue path

    let watcher =
        new FileSystemWatcher(
            AbsolutePath.value projectRoot,
            IncludeSubdirectories = true,
            NotifyFilter =
                (NotifyFilters.FileName
                 ||| NotifyFilters.DirectoryName
                 ||| NotifyFilters.LastWrite
                 ||| NotifyFilters.Size)
        )

    do
        watcher.Changed.Add(fun args -> queue args.FullPath)
        watcher.Created.Add(fun args -> queue args.FullPath)
        watcher.Deleted.Add(fun args -> queue args.FullPath)
        watcher.Renamed.Add(fun args -> queue args.FullPath)

    member _.Start() = watcher.EnableRaisingEvents <- true

    /// <summary>Also watch these files, wherever they are.</summary>
    /// <remarks>
    /// Call it after every build: what a site depends on is only known once it has been built, and
    /// it changes when a page does. Paths already under the project are dropped, being watched
    /// already, and calling it again with the same files does nothing.
    /// </remarks>
    /// <param name="paths">Files a build read from outside the project.</param>
    member _.Follow(paths: AbsolutePath list) =
        let root = (AbsolutePath.value projectRoot).Replace('\\', '/').TrimEnd('/') + "/"

        let wanted =
            paths
            |> List.map (fun path -> (AbsolutePath.value path).Replace('\\', '/'))
            |> List.filter (fun path -> not (path.StartsWith root))
            |> Set.ofList

        if wanted <> followedFiles then
            followedFiles <- wanted

            let directories =
                wanted
                |> Set.map (fun path -> Path.GetDirectoryName path)
                |> Set.filter Directory.Exists

            for directory in followedDirectories do
                if not (Set.contains directory.Key directories) then
                    directory.Value.Dispose()

            followedDirectories <-
                directories
                |> Seq.map (fun directory ->
                    match Map.tryFind directory followedDirectories with
                    | Some existing -> directory, existing
                    | None ->
                        let follower =
                            new FileSystemWatcher(
                                directory,
                                IncludeSubdirectories = false,
                                NotifyFilter =
                                    (NotifyFilters.FileName
                                     ||| NotifyFilters.LastWrite
                                     ||| NotifyFilters.Size)
                            )

                        // The ignore rules do not apply here: a file a build asked for is worth reacting to.
                        let queueFollowed (path: string) =
                            if Set.contains (path.Replace('\\', '/')) followedFiles then
                                forceQueue path

                        follower.Changed.Add(fun args -> queueFollowed args.FullPath)
                        follower.Created.Add(fun args -> queueFollowed args.FullPath)
                        follower.Deleted.Add(fun args -> queueFollowed args.FullPath)
                        follower.Renamed.Add(fun args -> queueFollowed args.FullPath)
                        follower.EnableRaisingEvents <- watcher.EnableRaisingEvents
                        directory, follower
                )
                |> Map.ofSeq

    interface IDisposable with
        member _.Dispose() =
            watcher.Dispose()

            for directory in followedDirectories do
                directory.Value.Dispose()

            if not (isNull timer) then
                timer.Dispose()
