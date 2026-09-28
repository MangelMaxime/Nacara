namespace Nacara.Plugins.Internal

open System.IO
open Nacara.Core
open Nacara.Plugins

/// <summary>Reading what a preset names off disk, so a snippet can be handed it.</summary>
[<RequireQualifiedAccess>]
module internal LiveExamplePresets =

    /// <summary>Read every preset's files, reporting the ones that are not there.</summary>
    let read (root: AbsolutePath) (sink: DiagnosticSink) (options: LiveExampleOptions) =
        options.Presets
        |> List.map (fun preset ->
            let contents =
                preset.Files
                |> List.choose (fun file ->
                    let path = AbsolutePath.combine root [ file ]

                    if File.Exists(AbsolutePath.value path) then
                        Some(Path.GetFileName file, File.ReadAllText(AbsolutePath.value path))
                    else
                        sink.Add(
                            Diagnostic.error
                                "preset-source-missing"
                                $"The preset '%s{preset.Name}' names '%s{file}', which does not exist"
                        )

                        None
                )

            let readShell file =
                let path = AbsolutePath.combine root [ file ]

                if File.Exists(AbsolutePath.value path) then
                    Some(File.ReadAllText(AbsolutePath.value path))
                else
                    sink.Add(
                        Diagnostic.error
                            "preset-shell-missing"
                            $"The preset '%s{preset.Name}' names '%s{file}', which does not exist"
                    )

                    None

            let shell name (chosen: string option) =
                chosen |> Option.orElse name |> Option.bind readShell

            let css =
                match preset.Css with
                | [] -> shell options.Css None
                | files ->
                    match List.choose readShell files with
                    | [] -> None
                    | read -> Some(String.concat "\n" read)

            {|
                Name = preset.Name
                Files = contents
                Css = css
                Template = shell options.Template preset.Template
            |}
        )
