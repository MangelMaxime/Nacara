/// <summary>Where things are, from the root of the repository.</summary>
module EasyBuild.Workspace

open System.IO

[<Literal>]
let private here = __SOURCE_DIRECTORY__

/// <summary>The root of the repository, whatever directory a command is run from.</summary>
let root = Path.GetFullPath(Path.Combine(here, ".."))

let private at ([<System.ParamArray>] parts: string array) = Path.Combine(root, Path.Combine parts)

/// <summary>The tests, run as a program rather than through a test runner.</summary>
let tests =
    at
        [|
            "tests"
            "Nacara.Tests"
        |]

/// <summary>This repository's own site.</summary>
let docs =
    at
        [|
            "docs"
            "Docs.fsproj"
        |]

/// <summary>The tree-sitter plugin, and the two things built into it.</summary>
module TreeSitter =

    let project =
        at
            [|
                "src"
                "Nacara.Plugin.Highlight.TreeSitter"
            |]

    /// <summary>Where the native libraries are put, one directory per platform.</summary>
    let runtimes = Path.Combine(project, "runtimes")

    /// <summary>Where the grammars that ship inside the package are put.</summary>
    let grammars = Path.Combine(project, "grammars")
