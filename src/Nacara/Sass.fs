namespace Nacara

open System
open System.Reflection
open System.Runtime.InteropServices
open System.Text
open System.IO
open Nacara.Core
open System.Diagnostics

module Sass =

    type SassBuildResult =
        {
            Success: bool
            Error: string
            Output: string
        }

    let getExecutable () =

        let assemblyLocation = Assembly.GetExecutingAssembly().Location
        let assemblyDirectory = Path.GetDirectoryName assemblyLocation

        let dartExecutableInfo =
            if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
                if RuntimeInformation.OSArchitecture = Architecture.X64 then
                    Some("windows-x64", "sass.bat")
                else
                    Log.error
                        $"Unsupported architecture: %s{Enum.GetName RuntimeInformation.OSArchitecture}"

                    None
            else if RuntimeInformation.IsOSPlatform(OSPlatform.Linux) then
                if RuntimeInformation.OSArchitecture = Architecture.X64 then
                    Some("linux-x64", "sass")
                else if
                    RuntimeInformation.OSArchitecture = Architecture.Arm64
                then
                    Some("linux-arm64", "sass")
                else
                    Log.error
                        $"Unsupported architecture: %s{Enum.GetName RuntimeInformation.OSArchitecture}"

                    None
            else if RuntimeInformation.IsOSPlatform(OSPlatform.OSX) then
                if RuntimeInformation.OSArchitecture = Architecture.X64 then
                    Some("macos-x64", "sass")
                else if
                    RuntimeInformation.OSArchitecture = Architecture.Arm64
                then
                    Some("macos-arm64", "sass")
                else
                    Log.error
                        $"Unsupported architecture: %s{Enum.GetName RuntimeInformation.OSArchitecture}"

                    None
            else
                Log.error
                    $"Unsupported OS: %s{RuntimeInformation.OSDescription}"

                None

        match dartExecutableInfo with
        | Some (sassRuntimeFolder, sassExecutable) ->
            Path.Combine(
                assemblyDirectory,
                "sass-runtimes",
                sassRuntimeFolder,
                sassExecutable
            )

        | None ->
            Log.error "Could not find Sass executable"
            exit 1

    let private createCompilerProcess
        (projectRoot: ProjectRoot.ProjectRoot)
        (arguments: string)
        =

        let psi = ProcessStartInfo()
        psi.FileName <- getExecutable ()
        psi.WorkingDirectory <- ProjectRoot.value projectRoot
        psi.Arguments <- arguments
        psi.CreateNoWindow <- true
        psi.UseShellExecute <- false
        psi.RedirectStandardOutput <- true
        psi.RedirectStandardError <- true

        let compiler = new Process()
        compiler.StartInfo <- psi

        compiler

    let compile (projectRoot: ProjectRoot.ProjectRoot) (sassArgs: SassArg list) =

        let arguments = sassArgs |> SassArgs.sort |> SassArgs.toString
        let compiler = createCompilerProcess projectRoot arguments

        let errorBuilder = StringBuilder()

        compiler.ErrorDataReceived.Add(fun event ->
            errorBuilder.AppendLine event.Data |> ignore
        )

        compiler.Start() |> ignore

        compiler.BeginErrorReadLine()

        let ouput = compiler.StandardOutput.ReadToEnd()

        compiler.WaitForExit()

        let success = compiler.ExitCode = 0

        {
            Success = success
            Error = errorBuilder.ToString()
            Output = ouput
        }

    let watch (projectRoot: ProjectRoot.ProjectRoot) (sassArgs: SassArg list) =

        let arguments =
            // Add watch mode to sass process
            SassArg.Watch :: sassArgs |> SassArgs.sort |> SassArgs.toString

        let compiler = createCompilerProcess projectRoot arguments

        compiler.ErrorDataReceived.Add(fun event ->
            Log.error event.Data
        )

        compiler.OutputDataReceived.Add(fun event ->
            Log.info event.Data
        )

        compiler.Start() |> ignore

        compiler.BeginErrorReadLine()
        compiler.BeginOutputReadLine()

        compiler.Exited.Add(fun _ ->
            Log.info "Sass process exited"
        )

        compiler
