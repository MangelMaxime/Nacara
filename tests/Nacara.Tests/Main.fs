module Nacara.Tests.Main

open Scriptorium.Quill
open type Scriptorium.Quill.Runner

[<EntryPoint>]
let main _ =
    runTestsWith (
        timeout 15_000,
        [
            Core.all
            Tool.all
            Build.all
            Collections.all
            DefaultTheme.all
            Locale.all
            CodeBlock.all
            Changelog.all
            Versions.all
            Literate.all
            Sitemap.all
            Components.all
            LinkValidator.all
            FSharpApi.all
            TreeSitter.all
            Rumdl.all
            Nuglify.all
            LiveExample.all
        ]
    )
