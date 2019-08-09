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
    Yarn.exec "fable-splitter --watch -c src/splitter.config.js" id
}

let all = BuildTask.createEmpty "All" [clean; watch]

BuildTask.runOrDefault all
