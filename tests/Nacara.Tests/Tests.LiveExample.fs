module Nacara.Tests.LiveExample

open Scriptorium.Nib.Assertion
open type Scriptorium.Quill.Test
open System.IO
open System.Text.RegularExpressions
open Nacara.Core
open Nacara.Plugins
open Nacara.Plugins.Internal
open Nacara.Theme
open Nacara.Tests

/// These cover what the plugin decides rather than what it fetches: the class map it derives, the
/// configuration it hands the browser, and the markup a live block gets.
let private read (root: AbsolutePath) (path: string) =
    File.ReadAllText(Path.Combine(AbsolutePath.value root, path))

let all =
    testList (
        "LiveExample",
        [
            test (
                "a live fence reaches the markup, so the browser can find it",
                fun _ ->
                    let block =
                        {
                            Language = Some "fsharp"
                            Code = "printfn \"hi\""
                            Meta =
                                { CodeBlockMeta.empty with
                                    Unknown =
                                        [
                                            "live"
                                            "preset=demo"
                                        ]
                                }
                        }

                    let renderer = NacaraCodeBlockRenderer() :> ICodeBlockRenderer
                    let html = renderer.Render(CodeBlock.prepare [] block)

                    assertThat
                        (html.Contains "data-meta=\"live preset=demo\"")
                        (tag "the meta the parser did not recognise is carried through" >> isTrue)
            )

            test (
                "a block with nothing unrecognised carries no meta attribute",
                fun _ ->
                    let block =
                        {
                            Language = Some "fsharp"
                            Code = "printfn \"hi\""
                            Meta = CodeBlockMeta.empty
                        }

                    let renderer = NacaraCodeBlockRenderer() :> ICodeBlockRenderer
                    let html = renderer.Render(CodeBlock.prepare [] block)

                    assertThat
                        (html.Contains "data-meta")
                        (tag "an ordinary block is left as it was" >> isFalse)
            )

            test (
                "a block carries the code it was written with",
                fun _ ->
                    let block =
                        {
                            Language = Some "fsharp"
                            Code = "let a = 1\nlet b = 2\nlet c = 3"
                            Meta =
                                { CodeBlockMeta.empty with
                                    Unknown = [ "live" ]
                                    Collapse = [ (2, 2) ]
                                }
                        }

                    let prepared = CodeBlock.prepare [] block

                    assertThat
                        (CodeBlock.source prepared)
                        (tag "every line, folded or not"
                         >> isEqualTo "let a = 1\nlet b = 2\nlet c = 3")

                    let renderer = NacaraCodeBlockRenderer() :> ICodeBlockRenderer
                    let html = renderer.Render prepared

                    assertThat
                        (html.Contains "data-source=\"let a = 1\nlet b = 2\nlet c = 3\"")
                        (tag "and says so in the markup" >> isTrue)
            )

            test (
                "code that contains a quote does not break the tag it travels in",
                fun _ ->
                    let block =
                        {
                            Language = Some "fsharp"
                            Code = "let greeting = \"hello\""
                            Meta =
                                { CodeBlockMeta.empty with
                                    Unknown = [ "live" ]
                                }
                        }

                    let renderer = NacaraCodeBlockRenderer() :> ICodeBlockRenderer
                    let html = renderer.Render(CodeBlock.prepare [] block)

                    assertThat
                        (html.Contains "data-source=\"let greeting = &quot;hello&quot;\"")
                        (tag "the quotes are escaped for where they are going" >> isTrue)
            )

            test (
                "captures are given the classes the build would give them",
                fun _ ->
                    let map = Vendor.classMap "(a) @keyword (b) @variable.parameter (c) @string"

                    assertThat
                        (map |> List.tryFind (fst >> (=) "keyword") |> Option.map snd)
                        (tag "a keyword" >> isEqualTo (Some "tok-keyword"))

                    assertThat
                        (map |> List.tryFind (fst >> (=) "variable.parameter") |> Option.map snd)
                        (tag "a dotted capture keeps its own class"
                         >> isEqualTo (Some "tok-parameter"))

                    assertThat
                        (map |> List.tryFind (fst >> (=) "string") |> Option.map snd)
                        (tag "a string" >> isEqualTo (Some "tok-string"))
            )

            test (
                "the output is colourable too, from its own grammar",
                fun _ ->
                    let shipped = TreeSitter.bundledLanguages.Value

                    assertThat
                        (LiveExampleTarget.languages
                         |> List.filter (fun language -> not (List.contains language shipped)))
                        (tag "every target's output has a grammar, so none is shown plain"
                         >> isEqualTo [])

                    let map = Vendor.classMap "(a) @keyword (b) @string (c) @punctuation.bracket"

                    assertThat
                        (map |> List.map snd)
                        (tag "and lands on the classes the stylesheet knows"
                         >> isEqualTo
                             [
                                 "tok-keyword"
                                 "tok-punctuation"
                                 "tok-string"
                             ])
            )

            test (
                "a preset is read and handed to the browser",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()
                    let prelude = Path.Combine(AbsolutePath.value root, "preludes")
                    Directory.CreateDirectory prelude |> ignore

                    File.WriteAllText(
                        Path.Combine(prelude, "Demo.fs"),
                        "module Demo\n\nlet answer = 42\n"
                    )

                    let site =
                        Fixture.site
                        |> LiveExample.registerWith (
                            LiveExample.preset (
                                LiveExamplePreset.create "demo"
                                |> LiveExamplePreset.files [ "preludes/Demo.fs" ]
                                |> LiveExamplePreset.asDefault
                            )
                        )

                    Build.run root site |> ignore

                    let config = read root "output/assets/live-example/config.json"

                    assertThat
                        (config.Contains "\"defaultPreset\":\"demo\"")
                        (tag "the default preset is named" >> isTrue)

                    assertThat
                        (config.Contains "let answer = 42")
                        (tag "the preset's source travels with it" >> isTrue)

                    assertThat
                        (config.Contains "\"assemblySuffix\":\".txt\"")
                        (tag "the suffix the worker needs to find assemblies" >> isTrue)
            )

            test (
                "one library is precompiled, so two presets cannot each name a project",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    let site =
                        Fixture.site
                        |> LiveExample.registerWith (
                            LiveExample.preset (
                                LiveExamplePreset.create "one"
                                |> LiveExamplePreset.project "a.fsproj"
                            )
                            >> LiveExample.preset (
                                LiveExamplePreset.create "two"
                                |> LiveExamplePreset.project "b.fsproj"
                            )
                        )

                    let diagnostic =
                        (Build.run root site).Diagnostics
                        |> List.tryFind (fun item ->
                            item.Code = "live-example/duplicate-precompiled-project"
                        )

                    match diagnostic with
                    | None -> assertThat false (tag "the second project is reported" >> isTrue)
                    | Some diagnostic ->
                        assertThat
                            (diagnostic.Message.Contains "one" && diagnostic.Message.Contains "two")
                            (tag "naming both presets" >> isTrue)
            )

            test (
                "two presets cannot both be the default",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    let site =
                        Fixture.site
                        |> LiveExample.registerWith (
                            LiveExample.preset (
                                LiveExamplePreset.create "one" |> LiveExamplePreset.asDefault
                            )
                            >> LiveExample.preset (
                                LiveExamplePreset.create "two" |> LiveExamplePreset.asDefault
                            )
                        )

                    let result = Build.run root site

                    let diagnostic =
                        result.Diagnostics
                        |> List.tryFind (fun item ->
                            item.Code = "live-example/duplicate-default-preset"
                        )

                    match diagnostic with
                    | None -> assertThat false (tag "the clash is reported" >> isTrue)
                    | Some diagnostic ->
                        assertThat
                            (diagnostic.Message.Contains "one" && diagnostic.Message.Contains "two")
                            (tag "naming both of them" >> isTrue)

                        assertThat
                            diagnostic.Severity
                            (tag "and failing the build" >> isEqualTo Severity.Error)
            )

            test (
                "a preset naming a file that is not there says so",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    let site =
                        Fixture.site
                        |> LiveExample.registerWith (
                            LiveExample.preset (
                                LiveExamplePreset.create "demo"
                                |> LiveExamplePreset.files [ "preludes/Missing.fs" ]
                            )
                        )

                    let result = Build.run root site

                    assertThat
                        (result.Diagnostics
                         |> List.exists (fun d -> d.Message.Contains "preludes/Missing.fs"))
                        (tag "the file that is missing is named" >> isTrue)
            )

            test (
                "a site can say which tab a snippet opens on",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    let site = Fixture.site |> LiveExample.registerWith (LiveExample.tab OutputTab)

                    Build.run root site |> ignore

                    assertThat
                        ((read root "output/assets/live-example/config.json").Contains
                            "\"tab\":\"output\"")
                        (tag "the choice reaches the browser" >> isTrue)
            )

            test (
                "a site that says nothing leaves the tab to the snippet",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()
                    Build.run root (Fixture.site |> LiveExample.register) |> ignore

                    assertThat
                        ((read root "output/assets/live-example/config.json").Contains
                            "\"tab\":null")
                        (tag "so the console, or the result when it drew and printed nothing"
                         >> isTrue)
            )

            test (
                "a pinned Fable is used as it was written",
                fun _ ->
                    assertThat
                        (Vendor.resolve (Pinned("9.9.9", "8.8.8")))
                        (tag "the pair is used as it was written" >> isEqualTo ("9.9.9", "8.8.8"))

                    assertThat
                        (Vendor.resolve Vendor.Default)
                        (tag "and the default is the pair this plugin was built against"
                         >> isEqualTo (Vendor.StandaloneVersion, Vendor.MetadataVersion))
            )

            test (
                "a library written by another Fable is not offered to the browser",
                fun _ ->
                    let modules =
                        let directory =
                            Path.Combine(
                                Path.GetTempPath(),
                                "nacara-tests",
                                System.Guid.NewGuid().ToString "N",
                                "out",
                                "fable_modules"
                            )

                        Directory.CreateDirectory directory |> ignore

                        File.WriteAllText(
                            Path.Combine(directory, "precompiled_info.json"),
                            """{"CompilerVersion":"5.15.0","Files":{}}"""
                        )

                        directory

                    assertThat
                        (Vendor.agrees (Some "5.15.0") modules)
                        (tag "the Fable that wrote it is the one that will read it"
                         >> isEqualTo (Ok()))

                    assertThat
                        (Vendor.agrees None modules)
                        (tag "a compiler that does not publish its Fable is taken at its word"
                         >> isEqualTo (Ok()))

                    match Vendor.agrees (Some "5.14.0") modules with
                    | Ok() -> failwith "a mismatch should not have been accepted"
                    | Error message ->
                        assertThat
                            (message.Contains "5.15.0"
                             && message.Contains "5.14.0"
                             && message.Contains "fableTool")
                            (tag "and a mismatch names both, and the way back" >> isTrue)
            )

            test (
                "choosing the editor's own colouring says so to the browser",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    let site =
                        Fixture.site
                        |> LiveExample.registerWith (LiveExample.highlighting DefaultHighlighting)

                    Build.run root site |> ignore

                    let config = read root "output/assets/live-example/config.json"

                    assertThat
                        (config.Contains "\"highlighting\":\"default\"")
                        (tag "the choice reaches the browser" >> isTrue)

                    assertThat
                        LiveExample.defaults.Highlighting
                        (tag "and it is what a site gets without asking"
                         >> isEqualTo DefaultHighlighting)

                    assertThat
                        (File.Exists(
                            Path.Combine(
                                AbsolutePath.value root,
                                "output/assets/live-example/tree-sitter/highlight-worker.js"
                            )
                        ))
                        (tag "and the tree-sitter worker is not shipped" >> isFalse)
            )

            test (
                "what is emitted is grouped by what it is for",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    Build.run
                        root
                        (Fixture.site
                         |> LiveExample.registerWith (
                             LiveExample.highlighting TreeSitterHighlighting
                         ))
                    |> ignore

                    let exists path =
                        File.Exists(
                            Path.Combine(
                                AbsolutePath.value root,
                                "output/assets/live-example",
                                path
                            )
                        )

                    let config = read root "output/assets/live-example/config.json"

                    let published name =
                        Regex.Match(config, $"\"{name}\":\"([^\"]*)\"").Groups[1].Value

                    let compiler = published "compiler"
                    let refs = published "refs"

                    for path in
                        [
                            $"%s{compiler}/worker.min.js"
                            $"%s{compiler}/bundle.min.js"
                            "tree-sitter/highlight-worker.js"
                            "tree-sitter/web-tree-sitter.wasm"
                            "grammars/fsharp/grammar.wasm.gz"
                            "grammars/fsharp/highlights.scm"
                            "grammars/fsharp/captures.json"
                        ] do
                        assertThat (exists path) (tag path >> isTrue)

                    assertThat
                        (Directory.Exists(
                            Path.Combine(
                                AbsolutePath.value root,
                                "output/assets/live-example",
                                refs
                            )
                        ))
                        (tag "the assemblies the compiler checks against sit with it" >> isTrue)
            )

            test (
                "every target says what it is called and whether it runs",
                fun _ ->
                    assertThat
                        (LiveExampleTarget.all
                         |> List.filter (fun target -> (LiveExampleTarget.description target).Runs))
                        (tag "only the one a browser has a runtime for" >> isEqualTo [ JavaScript ])

                    assertThat
                        (LiveExampleTarget.all
                         |> List.map (fun target -> (LiveExampleTarget.description target).Language))
                        (tag "and each is sent the name Fable answers to"
                         >> isEqualTo
                             [
                                 "JavaScript"
                                 "TypeScript"
                                 "Python"
                                 "Rust"
                                 "Dart"
                                 "Php"
                                 "Erlang"
                             ])
            )

            test (
                "a target is found by its short name as well as its long one",
                fun _ ->
                    assertThat
                        (LiveExampleTarget.tryParse "py")
                        (tag "py is python" >> isEqualTo (Some Python))

                    assertThat
                        (LiveExampleTarget.tryParse "RUST")
                        (tag "and case is not something to get right" >> isEqualTo (Some Rust))

                    assertThat
                        (LiveExampleTarget.tryParse "cobol")
                        (tag "something Fable does not compile to is nothing" >> isEqualTo None)
            )

            test (
                "a fence naming a target that does not exist fails the build",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    File.WriteAllText(
                        Path.Combine(AbsolutePath.value root, "docs/wrong.md"),
                        "---\ntitle: Wrong\n---\n\n```fsharp live target=kotlin\nprintfn \"hi\"\n```\n"
                    )

                    let result = Build.run root (Fixture.site |> LiveExample.register)

                    let diagnostic =
                        result.Diagnostics
                        |> List.tryFind (fun item -> item.Code = "live-example/unknown-target")

                    match diagnostic with
                    | None -> assertThat false (tag "the target is reported" >> isTrue)
                    | Some diagnostic ->
                        assertThat
                            (diagnostic.Message.Contains "kotlin")
                            (tag "naming what was written" >> isTrue)

                        assertThat
                            (diagnostic.Span |> Option.map _.Line)
                            (tag "on the line that wrote it" >> isEqualTo (Some 5))
            )

            test (
                "every target is coloured by a grammar the plugin ships",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    Build.run
                        root
                        (Fixture.site
                         |> LiveExample.registerWith (
                             LiveExample.highlighting TreeSitterHighlighting
                         ))
                    |> ignore

                    let script = read root "output/assets/live-example/live-example.js"

                    assertThat
                        (script.Contains "\"typescript\":{\"name\":\"typescript\"")
                        (tag "every target reaches the browser" >> isTrue)

                    assertThat
                        (script.Contains "\"highlight\":\"javascript\""
                         && script.Contains "\"highlight\":\"python\"")
                        (tag "a shipped grammar colours its target" >> isTrue)

                    assertThat
                        (script.Contains "\"highlight\":null")
                        (tag "and none is left plain" >> isFalse)

                    assertThat
                        ((read root "output/assets/live-example/config.json").Contains "targets")
                        (tag "and the config a reader only fetches on Run carries none of it"
                         >> isFalse)
            )

            let scratch (packages: string option) (shared: string) =
                let root =
                    Path.Combine(
                        Path.GetTempPath(),
                        "nacara-tests",
                        System.Guid.NewGuid().ToString "N"
                    )

                Directory.CreateDirectory(Path.Combine(root, "shared")) |> ignore
                Directory.CreateDirectory(Path.Combine(root, "lib")) |> ignore

                File.WriteAllText(Path.Combine(root, "shared", "Helpers.fs"), shared)
                File.WriteAllText(Path.Combine(root, "lib", "Lib.fs"), "module Lib\n")

                let reference =
                    match packages with
                    | Some _ -> """<PackageReference Include="Thoth.Json.Core" />"""
                    | None -> ""

                packages
                |> Option.iter (fun content ->
                    File.WriteAllText(Path.Combine(root, "Directory.Packages.props"), content)
                )

                let project = Path.Combine(root, "lib", "Lib.fsproj")

                File.WriteAllText(
                    project,
                    $"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
  <ItemGroup>
    <Compile Include="../shared/Helpers.fs" />
    <Compile Include="Lib.fs" />
    %s{reference}
  </ItemGroup>
</Project>
"""
                )

                root, project

            test (
                "a file compiled from outside the project counts towards what it is",
                fun _ ->
                    let root, project = scratch None "module Shared.Helpers\nlet answer = 41\n"

                    match ProjectInputs.read project with
                    | Error message ->
                        assertThat message (tag "MSBuild should answer" >> isEqualTo "")
                    | Ok before ->
                        assertThat
                            (before.Contains "Helpers.fs")
                            (tag "a sibling directory's file is part of the project" >> isTrue)

                        File.WriteAllText(
                            Path.Combine(root, "shared", "Helpers.fs"),
                            "module Shared.Helpers\nlet answer = 42\n"
                        )

                        match ProjectInputs.read project with
                        | Error message ->
                            assertThat message (tag "MSBuild should answer" >> isEqualTo "")
                        | Ok after ->
                            assertThat
                                (before <> after)
                                (tag "and editing it makes the project a different one" >> isTrue)

                    Directory.Delete(root, true)
            )

            test (
                "the same sources answer the same, whatever their timestamps say",
                fun _ ->
                    let root, project = scratch None "module Shared.Helpers\nlet answer = 41\n"

                    match ProjectInputs.read project with
                    | Error message ->
                        assertThat message (tag "MSBuild should answer" >> isEqualTo "")
                    | Ok before ->
                        for file in
                            Directory.EnumerateFiles(root, "*.fs", SearchOption.AllDirectories) do
                            File.SetLastWriteTimeUtc(file, System.DateTime.UtcNow.AddMinutes 1.)

                        match ProjectInputs.read project with
                        | Error message ->
                            assertThat message (tag "MSBuild should answer" >> isEqualTo "")
                        | Ok after ->
                            assertThat
                                after
                                (tag "touching a file is not changing it" >> isEqualTo before)

                    Directory.Delete(root, true)
            )

            test (
                "a package version written centrally counts too",
                fun _ ->
                    let packages =
                        """<Project>
  <PropertyGroup><ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally></PropertyGroup>
  <ItemGroup><PackageVersion Include="Thoth.Json.Core" Version="0.9.1" /></ItemGroup>
</Project>
"""

                    let root, project =
                        scratch (Some packages) "module Shared.Helpers\nlet answer = 41\n"

                    match ProjectInputs.read project with
                    | Error message ->
                        assertThat message (tag "MSBuild should answer" >> isEqualTo "")
                    | Ok inputs ->
                        assertThat
                            (inputs.Contains "Thoth.Json.Core@0.9.1")
                            (tag "the version comes from Directory.Packages.props" >> isTrue)

                    Directory.Delete(root, true)
            )
        ]
    )
