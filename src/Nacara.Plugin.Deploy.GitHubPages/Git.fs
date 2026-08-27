namespace Nacara.Plugins.Internal

open System
open System.Diagnostics
open System.IO

/// <summary>Enough of git to write a tree and push it.</summary>
[<RequireQualifiedAccess>]
module Git =

    /// <summary>What a git invocation answered.</summary>
    type Result =
        {
            ExitCode: int
            Output: string
            Error: string
        }

        member this.Succeeded = this.ExitCode = 0

    /// <summary>Runs git in a directory, with an index of its own when one is given.</summary>
    /// <param name="workingDirectory">Where to run it.</param>
    /// <param name="index">The index file to use, for the plumbing that builds a tree.</param>
    /// <param name="arguments">What to pass to git.</param>
    let run (workingDirectory: string) (index: string option) (arguments: string list) =
        let start = ProcessStartInfo "git"
        start.WorkingDirectory <- workingDirectory
        start.RedirectStandardOutput <- true
        start.RedirectStandardError <- true

        for argument in arguments do
            start.ArgumentList.Add argument

        match index with
        | Some path -> start.EnvironmentVariables["GIT_INDEX_FILE"] <- path
        | None -> ()

        use git = Process.Start start
        let output = git.StandardOutput.ReadToEnd()
        let error = git.StandardError.ReadToEnd()
        git.WaitForExit()

        {
            ExitCode = git.ExitCode
            Output = output.Trim()
            Error = error.Trim()
        }

    /// <summary>Runs git, and says what went wrong rather than what came out.</summary>
    let read (workingDirectory: string) (index: string option) (arguments: string list) =
        let result = run workingDirectory index arguments

        if result.Succeeded then
            Ok result.Output
        else
            let command = String.concat " " ("git" :: arguments)
            Error $"%s{command} failed: %s{result.Error}"

    /// <summary>Runs git for what it does rather than for what it says.</summary>
    /// <param name="workingDirectory">Where to run it.</param>
    /// <param name="index">The index file to use, for the plumbing that builds a tree.</param>
    /// <param name="arguments">What to pass to git.</param>
    let exec (workingDirectory: string) (index: string option) (arguments: string list) =
        read workingDirectory index arguments |> Result.map ignore

    /// <summary>A path no index is at yet, for plumbing that wants an index of its own.</summary>
    let temporaryIndex () =
        let name = Guid.NewGuid().ToString "N"
        Path.Combine(Path.GetTempPath(), $"nacara-deploy-%s{name}.index")
