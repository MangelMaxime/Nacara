#r "paket: groupref netcorebuild //"
#load ".fake/build.fsx/intellisense.fsx"

#if !FAKE
#r "Facades/netstandard"
#r "netstandard"
#endif

open Fake.Core
open Fake.DotNet
open Fake.JavaScript
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open BlackFox.Fake

let clean = BuildTask.create "Clean" [] {
    !! "src/**/bin"
    ++ "src/**/obj"
    ++ "dist"
    |> Shell.cleanDirs
}

let watch = BuildTask.create "Watch" [clean.IfNeeded] {
    [
        async {
            Yarn.exec "fable-splitter -c src/Nacara/splitter.config.js --watch" id
        }

        async {
            Yarn.exec "fable-splitter -c src/Layouts/Standard/splitter.config.js --watch" id
        }

        async {
            CreateProcess.fromRawCommand
                "npx"
                [ "nodemon"; "cli.js"; "--"; "--watch"]
            |> Proc.run
            |> ignore
        }
    ]
    |> Async.Parallel
    |> Async.RunSynchronously
    |> ignore
}

let all = BuildTask.createEmpty "All" [clean; watch]

BuildTask.runOrDefault all
