module Nacara.Tests.Main

open type Scriptorium.Quill.Runner

[<EntryPoint>]
let main _ =
    runTests
        [
            Core.all
            Tool.all
            Build.all
            Locale.all
            CodeBlock.all
            Changelog.all
            Versions.all
            Literate.all
            Sitemap.all
            LinkValidator.all
            FSharpApi.all
            TreeSitter.all
            Rumdl.all
            Nuglify.all
            LiveExample.all
        ]
