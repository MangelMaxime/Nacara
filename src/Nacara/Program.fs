module Nacara.Main

open Argu
open System
open System.IO
open Nacara.Core
open Nacara.Evaluator
open Spectre.Console

/// Minor optimization for Argu
/// See: https://fsprojects.github.io/Argu/perf.html
let inline private checkStructure<'T> =
#if DEBUG
    true
#else
    false
#endif

let private loadLoaders (context: Context) =

    let loaders =
        Directory.GetFiles(
            Path.Combine(ProjectRoot.value context.ProjectRoot, "loaders"),
            "*.fsx"
        )
        |> Seq.map AbsolutePath.create

    // Stop on first error because if a loader fails to load,
    // the website will not be able to be generated.
    for loader in loaders do
        match LoaderEvaluator.tryEvaluate loader context with
        | Ok () -> ()
        | Error error -> Log.error error

[<EntryPoint>]
let main argv =
    let errorHandler =
        ProcessExiter(
            colorizer =
                function
                | ErrorCode.HelpText -> None
                | _ -> Some ConsoleColor.Red
        )

    let parser =
        ArgumentParser.Create<CliArguments>(
            programName = "nacara",
            errorHandler = errorHandler,
            checkStructure = checkStructure
        )

    let results = parser.ParseCommandLine(inputs = argv).GetAllResults()

    if List.isEmpty results then
        printfn "No arguments provided."
        printfn $"%s{parser.PrintUsage()}"
        1
    else if List.length results > 1 then
        printfn "More than one command was provided"
        printfn $"%s{parser.PrintUsage()}"
        1
    else

        match List.tryHead results with
        | Some Version -> Commands.Version.execute ()
        | Some Build -> Commands.Build.execute ()
        | Some Watch -> Commands.Watch.execute ()
        | Some Serve -> 0
        | Some Clean -> Commands.Clean.execute ()
        | None ->
            printfn $"%s{parser.PrintUsage()}"
            1
