namespace Nacara.Plugins.Internal

open Nacara.Plugins
open Thoth.Json.Core
open Thoth.Json.System.Text.Json

/// <summary>
/// What the build hands the browser: the configuration it fetches, and the preamble that
/// rides along with the script.
/// </summary>
[<RequireQualifiedAccess>]
module internal LiveExampleConfig =

    /// <summary>What the browser is told: the presets, and how to reach the compiler.</summary>
    let configuration
        (options: LiveExampleOptions)
        (layout: Vendor.Layout option)
        (precompiled: Vendor.Precompiled option)
        (presets:
            {|
                Name: string
                Files: (string * string) list
                Css: string option
                Template: string option
            |} list)
        =
        let preset
            (item:
                {|
                    Name: string
                    Files: (string * string) list
                    Css: string option
                    Template: string option
                |})
            =
            Encode.object
                [
                    "files",
                    Encode.list
                        [
                            for name, content in item.Files ->
                                Encode.object
                                    [
                                        "name", Encode.string name
                                        "content", Encode.string content
                                    ]
                        ]
                    "css", Encode.lossyOption Encode.string item.Css
                    "template", Encode.lossyOption Encode.string item.Template
                ]

        let precompiledJson (info: Vendor.Precompiled) =
            Encode.object
                [
                    "compilerVersion", Encode.string info.CompilerVersion
                    "files",
                    Encode.list
                        [
                            for path, rootModule, outPath in info.Files ->
                                Encode.object
                                    [
                                        "path", Encode.string path
                                        "rootModule", Encode.string rootModule
                                        "outPath", Encode.string outPath
                                    ]
                        ]
                    "inlineExprHeaders",
                    Encode.list (info.InlineExprHeaders |> List.map Encode.string)
                    "inlineExprChunks",
                    Encode.list (info.InlineExprChunks |> List.map Encode.string)
                ]

        let highlighting =
            match options.Highlighting with
            | TreeSitterHighlighting -> "treesitter"
            | DefaultHighlighting -> "default"

        let tab =
            options.Tab
            |> Option.map (
                function
                | ResultTab -> "result"
                | ConsoleTab -> "console"
                | OutputTab -> "output"
            )

        let defaultPreset = options.Presets |> List.tryFind _.IsDefault |> Option.map _.Name

        let path (choose: Vendor.Layout -> string) =
            layout |> Option.map choose |> Option.defaultValue "" |> Encode.string

        Encode.object
            [
                "compiler", path _.Compiler
                "refs", path _.Refs
                "precompiledAt", path _.Precompiled
                "assemblySuffix", Encode.string Vendor.AssemblySuffix
                "highlighting", Encode.string highlighting
                "tab", Encode.lossyOption Encode.string tab
                "stats", Encode.bool options.Stats
                "precompiled", Encode.lossyOption precompiledJson precompiled
                "defaultPreset", Encode.lossyOption Encode.string defaultPreset
                "presets", Encode.object [ for item in presets -> item.Name, preset item ]
            ]
        |> Encode.toString 0

    /// <summary>What the script has to know before a reader presses anything.</summary>
    let targets (options: LiveExampleOptions) (coloured: string list) =
        let entries =
            [
                for item in LiveExampleTarget.all do
                    let described = LiveExampleTarget.description item

                    let highlight =
                        if List.contains described.Highlight coloured then
                            Some described.Highlight
                        else
                            None

                    described.Name,
                    Encode.object
                        [
                            "name", Encode.string described.Name
                            "label", Encode.string described.Label
                            "language", Encode.string described.Language
                            "highlight", Encode.lossyOption Encode.string highlight
                            "runs", Encode.bool described.Runs
                            "aliases", Encode.list (described.Aliases |> List.map Encode.string)
                        ]
            ]

        let chosen =
            match options.Target with
            | Some target -> (LiveExampleTarget.description target).Name
            | None -> "javascript"

        let value =
            Encode.object
                [
                    "target", Encode.string chosen
                    "targets", Encode.object entries
                ]
            |> Encode.toString 0

        $"globalThis.__nacaraLiveExample=%s{value};\n"
