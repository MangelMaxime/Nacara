namespace Nacara.Plugins.Internal

open System
open System.Diagnostics
open System.IO
open System.Text.Json

/// <summary>What a project is built from, as MSBuild resolves it.</summary>
module internal ProjectInputs =

    /// Evaluation only - no restore, no build - so this costs a quarter of a second.
    let private evaluate (project: string) =
        let start =
            ProcessStartInfo(
                "dotnet",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WorkingDirectory = Path.GetDirectoryName project
            )

        [
            "msbuild"
            project
            "-getItem:Compile"
            "-getItem:ProjectReference"
            "-getItem:PackageReference"
            "-getItem:PackageVersion"
            "-nologo"
        ]
        |> List.iter start.ArgumentList.Add

        use running = Process.Start start
        let output = running.StandardOutput.ReadToEnd()
        let complaint = running.StandardError.ReadToEnd()
        running.WaitForExit()

        if running.ExitCode = 0 then
            Ok output
        else
            Error(
                if complaint = "" then
                    output
                else
                    complaint
            )

    let private items (document: JsonDocument) (name: string) =
        match document.RootElement.TryGetProperty "Items" with
        | true, all ->
            match all.TryGetProperty name with
            | true, found -> found.EnumerateArray() |> List.ofSeq
            | _ -> []
        | _ -> []

    let private text (element: JsonElement) (name: string) =
        match element.TryGetProperty name with
        | true, value when value.ValueKind = JsonValueKind.String -> value.GetString()
        | _ -> ""

    /// <summary>What every file the project compiles is, by content.</summary>
    let private compiled (paths: string list) =
        [
            for path in List.sort paths do
                let stamp =
                    try
                        use sha = Security.Cryptography.SHA256.Create()
                        use file = File.OpenRead path
                        sha.ComputeHash file |> Convert.ToHexString
                    with _ ->
                        "missing"

                $"%s{path}:%s{stamp}"
        ]

    /// <summary>Everything the project is made of, as one string to key a cache by.</summary>
    /// <param name="project">The <c>.fsproj</c> to describe.</param>
    let read (project: string) =
        let rec walk (visited: Set<string>) (project: string) =
            let project = Path.GetFullPath project

            if Set.contains project visited then
                Ok(visited, [])
            else

                match evaluate project with
                | Error message -> Error message
                | Ok json ->
                    use document = JsonDocument.Parse json

                    let identified (name: string) =
                        items document name
                        |> List.map (fun item ->
                            let identity = text item "Identity"
                            let version = text item "Version"
                            $"%s{identity}@%s{version}"
                        )
                        |> List.sort

                    let described =
                        [
                            $"project:%s{project}"
                            yield!
                                compiled
                                    [ for item in items document "Compile" -> text item "FullPath" ]
                            // Under central package management the version is on PackageVersion, not on the reference.
                            yield! identified "PackageReference"
                            yield! identified "PackageVersion"
                        ]

                    let references =
                        [ for item in items document "ProjectReference" -> text item "FullPath" ]
                        |> List.sort

                    (Ok(Set.add project visited, described), references)
                    ||> List.fold (fun state reference ->
                        match state with
                        | Error message -> Error message
                        | Ok(visited, described) ->
                            walk visited reference
                            |> Result.map (fun (visited, more) -> visited, described @ more)
                    )

        walk Set.empty project |> Result.map (snd >> String.concat ";")
