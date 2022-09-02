namespace Nacara

// Code ported/adapted from:
// https://github.com/fsprojects/FAKE/blob/5267477da251ed621165d31178ab9fba69de6e2f/src/app/Fake.IO.FileSystem/ChangeWatcher.fs#L122-122

// Would it be possible not run the timer as an infinity loop?
// Right now, even if not changes are detected, the timer is still running
// and executing the callback every "tick".

[<RequireQualifiedAccess>]
module Watcher =

    open System
    open System.IO
    open System.Threading
    open Nacara.Core

    type FileStatus =
        // Order is important here.
        // When notifying for the changes we are going to order on the Status
        // Rules:
        // - If a file received both Created and Changed, we want to notify for Created.
        // - If a file received both Changed and Deleted, we want to notify for Deleted.
        // - If a file received both Deleted and Created, we want to notify for Deleted.
        // - If a file received both Created, Changed, Changed, Deleted, we want to notify for Deleted.
        // In the example above, the order of the status doesn't matter.
        | Deleted
        | Created
        | Changed

    type FileChange =
        {
            FullPath: AbsolutePath.AbsolutePath
            Name: string
            Status: FileStatus
        }

    let private handleWatcherEvents
        (status: FileStatus)
        (onChange: FileChange -> unit)
        (e: FileSystemEventArgs)
        =

        onChange (
            {
                FullPath = AbsolutePath.create e.FullPath
                Name = e.Name
                Status = status
            }
        )

    let createWithFilters (dir: string) (filters : string list) onChange =

        // Store the list of queue capture during the current timer "tick"
        let changesQueue = ref List.empty<FileChange>

        let timerCallback =
            fun _ ->
                lock
                    changesQueue
                    (fun () ->
                        if not (Seq.isEmpty changesQueue.Value) then
                            let consolidatedChanges =
                                changesQueue.Value
                                // Group all the changes per files
                                |> Seq.groupBy (fun change -> change.FullPath)
                                // Return the meaningful change for each file
                                // See note on FileStatus for more details.
                                |> Seq.map (fun (_, changes) ->
                                    changes
                                    |> Seq.sortBy (fun change -> change.Status)
                                    |> Seq.head
                                )

                            changesQueue.Value <- []
                            onChange consolidatedChanges
                    )

        let timer =
            Lazy<IDisposable>(
                Func<IDisposable>(fun () ->
                    new Timer(timerCallback, Object(), 0, 200) :> IDisposable
                ),
                LazyThreadSafetyMode.ExecutionAndPublication
            )

        let accumulator (fileChange: FileChange) =
            lock
                changesQueue
                (fun () ->
                    changesQueue.Value <- fileChange :: changesQueue.Value
                    // Start the timer (ignore repeated calls)
                    (timer.Value |> ignore)
                )

        let watcher = new FileSystemWatcher()
        watcher.Path <- dir
        watcher.EnableRaisingEvents <- true
        watcher.IncludeSubdirectories <- true

        for filter in filters do
            watcher.Filters.Add filter

        watcher.NotifyFilter <-
            NotifyFilters.DirectoryName
            ||| NotifyFilters.LastWrite
            ||| NotifyFilters.FileName

        watcher.Created.Add(handleWatcherEvents Created accumulator)
        watcher.Changed.Add(handleWatcherEvents Changed accumulator)
        watcher.Deleted.Add(handleWatcherEvents Deleted accumulator)

        watcher.Renamed.Add(fun event ->
            accumulator
                {
                    FullPath = AbsolutePath.create event.OldFullPath
                    Name = event.OldName
                    Status = Deleted
                }

            accumulator
                {
                    FullPath = AbsolutePath.create event.FullPath
                    Name = event.Name
                    Status = Created
                }
        )

        { new IDisposable with
            member _.Dispose() =
                // Stop receiving events
                watcher.EnableRaisingEvents <- false

                // Free resources
                watcher.Dispose()

                if timer.IsValueCreated then
                    timer.Value.Dispose()
        }

    let create (dir: string) onChange =
        createWithFilters dir [ ] onChange
