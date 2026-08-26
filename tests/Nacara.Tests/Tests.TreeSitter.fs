/// <summary>
/// The tree-sitter highlighter, when the machine has what it needs.
/// </summary>
/// <remarks>
/// It needs a tree-sitter built with its wasm feature, which nobody publishes, so these tests say
/// what they need and step aside when it is not there rather than failing a build that was never
/// going to have it. The grammar they read is the one the plugin ships.
/// </remarks>
module Nacara.Tests.TreeSitter

open System
open System.IO
open Scriptorium.Nib.Assertion
open type Scriptorium.Quill.Test
open Nacara.Core
open Nacara.Plugins

/// <summary>A runtime said outright, for a machine the package ships none for.</summary>
let private runtime =
    Environment.GetEnvironmentVariable "NACARA_TREE_SITTER_RUNTIME"
    |> Option.ofObj
    |> Option.filter Directory.Exists

/// <summary>A highlighter reading the grammars the plugin ships.</summary>
/// <remarks>
/// Nothing is gated: the runtime is packed per platform and copied beside these tests, so what a
/// reader of the docs gets is what runs here.
/// </remarks>
let private configured (grammars: TreeSitterGrammar list) =
    let options =
        { TreeSitter.defaults with
            Grammars = grammars
            RuntimePath = runtime
        }

    TreeSitter.TreeSitterHighlighter(options) :> IHighlighter

let private highlighter () = configured []

let all =
    testList (
        "TreeSitter",
        [
            test (
                "a capture name becomes a class the theme knows",
                fun _ ->
                    for capture, expected in
                        [
                            "keyword", Some "tok-keyword"
                            "type", Some "tok-type"
                            "constructor", Some "tok-constructor"
                            "variable.parameter", Some "tok-parameter"
                            "variable.other.member", Some "tok-property"
                            "punctuation.bracket", Some "tok-punctuation"
                            "keyword.control.conditional", Some "tok-keyword"
                            "nothing-anyone-styles", None
                        ] do
                        assertThat
                            (TreeSitter.className capture)
                            (tag capture >> isEqualTo expected)
            )

            test (
                "TypeScript is coloured by its own query and the one it builds on",
                fun _ ->
                    // tree-sitter-typescript's query says only what TypeScript adds to JavaScript.
                    let highlighter = highlighter ()

                    let code =
                        "import { Record } from \"lib\"\n\nexport class Point extends Record {\n    readonly X: number\n}"

                    match highlighter.Highlight(Some "typescript", code) with
                    | None -> assertThat "" (tag "the grammar answered" >> isNotEqualTo "")
                    | Some lines ->
                        let classOf word =
                            lines
                            |> List.collect id
                            |> List.tryFind (fun token -> token.Text = word)
                            |> Option.bind _.ClassName

                        assertThat
                            (classOf "readonly")
                            (tag "what TypeScript adds" >> isEqualTo (Some "tok-keyword"))

                        assertThat
                            (classOf "class")
                            (tag "and what it shares with JavaScript"
                             >> isEqualTo (Some "tok-keyword"))

                        assertThat
                            (classOf "import")
                            (tag "including the one every generated file opens with"
                             >> isEqualTo (Some "tok-keyword"))
            )

            test (
                "F# is read by the grammar, not guessed at",
                fun _ ->
                    let highlighter = highlighter ()

                    let code =
                        "type Page = { Title: string }\nlet create (name: string) : Page = failwith \"\""

                    match highlighter.Highlight(Some "fsharp", code) with
                    | None -> assertThat "" (tag "the grammar answered" >> isNotEqualTo "")
                    | Some lines ->
                        let classOf word =
                            lines
                            |> List.collect id
                            |> List.tryFind (fun token -> token.Text = word)
                            |> Option.bind _.ClassName

                        assertThat
                            (classOf "type")
                            (tag "a keyword is a keyword" >> isEqualTo (Some "tok-keyword"))

                        assertThat
                            (classOf "Page")
                            (tag "and a type is a type - which TextMate cannot say for F#"
                             >> isEqualTo (Some "tok-type"))

                        assertThat
                            (classOf "create")
                            (tag "a function is a function" >> isEqualTo (Some "tok-function"))

                        assertThat
                            (lines |> List.length)
                            (tag "one entry per line of the source" >> isEqualTo 2)
            )

            test (
                "the package ships the languages a site is written in",
                fun _ ->
                    let shipped = TreeSitter.bundledLanguages.Value

                    for language in
                        [
                            "fsharp"
                            "csharp"
                            "bash"
                            "json"
                            "yaml"
                            "toml"
                            "markdown"
                        ] do
                        assertThat
                            (List.contains language shipped)
                            (tag $"%s{language} needs no building" >> isEqualTo true)
            )

            test (
                "a language a site names is used ahead of the one shipped",
                fun _ ->
                    let highlighter =
                        TreeSitter.TreeSitterHighlighter(
                            { TreeSitter.defaults with
                                Grammars =
                                    [ TreeSitter.grammar "fsharp" "nowhere.wasm" "nowhere.scm" ]
                                RuntimePath = runtime
                            }
                        )

                    assertThat
                        ((highlighter :> IHighlighter).Highlight(Some "fsharp", "let a = 1"))
                        (tag "the block is left uncoloured rather than crashing the build"
                         >> isEqualTo None)

                    let complaint =
                        highlighter.TakeProblems()
                        |> List.map (fun (language, message) -> $"%s{language}: %s{message}")
                        |> String.concat "; "

                    assertThat
                        (complaint.Contains "fsharp" && complaint.Contains "nowhere.wasm")
                        (tag $"and the plugin has something to report: %s{complaint}"
                         >> isEqualTo true)

                    assertThat
                        (highlighter.TakeProblems())
                        (tag "which it reports once" >> isEqualTo [])
            )

            test (
                "a grammar says where it comes from",
                fun _ ->
                    let plain =
                        TreeSitter.fromGitHub
                            "fsharp"
                            "https://github.com/MangelMaxime/tree-sitter-fsharp"
                            "dd0f511f"

                    assertThat
                        plain.Source
                        (tag "a repository and a commit of it"
                         >> isEqualTo (
                             Repository(
                                 "https://github.com/MangelMaxime/tree-sitter-fsharp",
                                 "dd0f511f",
                                 None,
                                 None
                             )
                         ))

                    let placed =
                        plain
                        |> TreeSitter.inDirectory "xml"
                        |> TreeSitter.queriesAt "queries/x.scm"

                    assertThat
                        placed.Source
                        (tag "both said, and neither losing the other"
                         >> isEqualTo (
                             Repository(
                                 "https://github.com/MangelMaxime/tree-sitter-fsharp",
                                 "dd0f511f",
                                 Some "xml",
                                 Some "queries/x.scm"
                             )
                         ))

                    let built =
                        TreeSitter.grammar "fsharp" "grammar.wasm.gz" "highlights.scm"
                        |> TreeSitter.inDirectory "ignored"

                    assertThat
                        built.Source
                        (tag "two files, as given"
                         >> isEqualTo (Files("grammar.wasm.gz", "highlights.scm")))
            )

            test (
                "a grammar answers to every name its fence is written with",
                fun _ ->
                    let fsharp = TreeSitter.grammar "fsharp" "grammar.wasm" "highlights.scm"

                    for name in
                        [
                            "fsharp"
                            "fs"
                            "fsx"
                            "fsi"
                        ] do
                        assertThat
                            ((TreeSitter.namesOf fsharp).Contains name)
                            (tag $"a fence saying '%s{name}' finds it" >> isEqualTo true)

                    assertThat
                        ((TreeSitter.namesOf (TreeSitter.grammar "js" "g" "q")).Contains
                            "javascript")
                        (tag "the family is read from either end" >> isEqualTo true)

                    let dotnet =
                        TreeSitter.grammar "fsharp" "grammar.wasm" "highlights.scm"
                        |> TreeSitter.aliases [ "dotnet" ]

                    assertThat
                        ((TreeSitter.namesOf dotnet).Contains "dotnet")
                        (tag "a name of its own" >> isEqualTo true)

                    assertThat
                        ((TreeSitter.namesOf fsharp).Contains "ocaml")
                        (tag "and no name it was never given" >> isEqualTo false)
            )

            test (
                "source outside ASCII is cut where the grammar says",
                fun _ ->
                    let highlighter = highlighter ()

                    // A node says where it is in bytes, so one accent used to shift every question after it.
                    let code = "let café = \"un café\"\nlet Type = 1"

                    match highlighter.Highlight(Some "fsharp", code) with
                    | None -> assertThat "" (tag "the grammar answered" >> isNotEqualTo "")
                    | Some lines ->
                        assertThat
                            (lines |> List.length)
                            (tag "one entry per line of the source" >> isEqualTo 2)

                        assertThat
                            (lines
                             |> List.collect id
                             |> List.map _.Text
                             |> String.concat ""
                             |> fun rebuilt -> rebuilt.Replace("\n", ""))
                            (tag "and nothing lost on the way back out"
                             >> isEqualTo (code.Replace("\n", "")))
            )

            test (
                "where two patterns claim the same word, the query's last word wins",
                fun _ ->
                    let highlighter = highlighter ()

                    // The F# queries call a dotted name's last segment a member access, then override it inside an open.
                    match highlighter.Highlight(Some "fsharp", "open Nacara.Core") with
                    | None -> assertThat "" (tag "the grammar answered" >> isNotEqualTo "")
                    | Some lines ->
                        let classOf word =
                            lines
                            |> List.collect id
                            |> List.tryFind (fun token -> token.Text = word)
                            |> Option.bind _.ClassName

                        assertThat
                            (classOf "Nacara", classOf "Core")
                            (tag "a namespace is one word all the way along"
                             >> isEqualTo (Some "tok-namespace", Some "tok-namespace"))

                    match highlighter.Highlight(Some "fsharp", "let x = value.Length") with
                    | None -> assertThat "" (tag "the grammar answered" >> isNotEqualTo "")
                    | Some lines ->
                        assertThat
                            (lines
                             |> List.collect id
                             |> List.tryFind (fun token -> token.Text = "Length")
                             |> Option.bind _.ClassName)
                            (tag "what is read after a value is its member"
                             >> isEqualTo (Some "tok-property"))
            )

            test (
                "a snippet that is only part of a file is read as one",
                fun _ ->
                    let highlighter = highlighter ()

                    let fragment = "|> Site.baseUrl \"/x/\"\n|> Site.output \"out\""

                    match highlighter.Highlight(Some "fsharp", fragment) with
                    | None -> assertThat "" (tag "the grammar answered" >> isNotEqualTo "")
                    | Some lines ->
                        let pipes =
                            lines
                            |> List.collect id
                            |> List.filter (fun token -> token.Text.Contains "|")

                        assertThat
                            (pipes |> List.map (fun token -> token.Text, token.ClassName))
                            (tag "both bars belong to the operator they are part of"
                             >> isEqualTo
                                 [
                                     "|>", Some "tok-operator"
                                     "|>", Some "tok-operator"
                                 ])

                    match
                        highlighter.Highlight(Some "fsharp", "match x with\n| [] -> 0\n| _ -> 1")
                    with
                    | None -> assertThat "" (tag "the grammar answered" >> isNotEqualTo "")
                    | Some lines ->
                        assertThat
                            (lines
                             |> List.collect id
                             |> List.filter (fun token -> token.Text = "|")
                             |> List.forall (fun token -> token.ClassName = Some "tok-keyword"))
                            (tag "a case is a case" >> isEqualTo true)
            )

            test (
                "a file written on Windows is read the same way",
                fun _ ->
                    let highlighter = highlighter ()

                    let source = "type Page = { Title: string }\nlet name = \"a\""

                    let both =
                        [
                            source
                            source.Replace("\n", "\r\n")
                        ]
                        |> List.map (fun code -> highlighter.Highlight(Some "fsharp", code))

                    assertThat
                        (both |> List.distinct |> List.length)
                        (tag "a carriage return changes nothing about the colours" >> isEqualTo 1)
            )
        ]
    )
