module Nacara.Tests.CodeBlock

open Scriptorium.Nib.Assertion
open type Scriptorium.Quill.Test
open Nacara.Core
open Nacara.Theme
open Nacara.Tests

let private parse = CodeBlockMeta.parse

let private renderer = NacaraCodeBlockRenderer() :> ICodeBlockRenderer

/// Render the way a themed site does: the engine prepares the block, the theme draws it.
let private render meta code =
    CodeBlock.render
        [ renderer ]
        []
        {
            Language = Some "text"
            Code = code
            Meta = meta
        }

/// Render with no theme registered, which is what a bare site gets.
let private renderMinimal meta code =
    CodeBlock.render
        []
        []
        {
            Language = Some "text"
            Code = code
            Meta = meta
        }

/// A highlighter that only ever claims the one language it was made for.
let private only (name: string) (language: string) (className: string) =
    { new IHighlighter with
        member _.Name = name

        member _.Highlight(asked, code) =
            match asked with
            | Some asked when asked = language ->
                Some
                    [
                        [
                            {
                                Text = code
                                ClassName = Some className
                            }
                        ]
                    ]
            | _ -> None
    }

let highlighters =
    testList (
        "highlighters",
        [
            test (
                "the last one registered is asked first",
                fun _ ->
                    let block =
                        {
                            Language = Some "fsharp"
                            Code = "let x = 1"
                            Meta = CodeBlockMeta.empty
                        }

                    let prepared =
                        CodeBlock.prepare
                            [
                                only "brought-by-the-theme" "fsharp" "tok-keyword"
                                only "the-site-s-own" "fsharp" "tok-type"
                            ]
                            block

                    assertThat
                        (prepared.Lines |> List.collect _.Pieces |> List.map _.ClassName)
                        (tag "the site's own wins over the one it was given"
                         >> isEqualTo [ Some "tok-type" ])
            )

            test (
                "an earlier one still covers what the later one declines",
                fun _ ->
                    let block =
                        {
                            Language = Some "python"
                            Code = "x = 1"
                            Meta = CodeBlockMeta.empty
                        }

                    let prepared =
                        CodeBlock.prepare
                            [
                                only "covers-everything" "python" "tok-variable"
                                only "covers-a-few" "fsharp" "tok-type"
                            ]
                            block

                    assertThat
                        (prepared.Lines |> List.collect _.Pieces |> List.map _.ClassName)
                        (tag "so a language the last one does not know is still coloured"
                         >> isEqualTo [ Some "tok-variable" ])
            )
        ]
    )

let meta =
    testList (
        "meta",
        [
            test (
                "an empty meta is the default",
                fun _ ->
                    assertThat
                        (parse "")
                        (tag "nothing asked for, nothing configured"
                         >> isEqualTo CodeBlockMeta.empty)
            )

            test (
                "a title is read and unquoted",
                fun _ ->
                    assertThat
                        (parse "title=\"Program.fs\"").Title
                        (tag "quotes are stripped" >> isEqualTo (Some "Program.fs"))
            )

            test (
                "a title may contain spaces",
                fun _ ->
                    assertThat
                        (parse "title=\"src/My Project/Program.fs\"").Title
                        (tag "quoted values are not split on spaces"
                         >> isEqualTo (Some "src/My Project/Program.fs"))
            )

            test (
                "bare braces mark lines",
                fun _ ->
                    let meta = parse "{1,3-5}"

                    assertThat
                        (meta.LineMarkers |> Map.toList |> List.map fst)
                        (tag "single lines and ranges both expand"
                         >> isEqualTo
                             [
                                 1
                                 3
                                 4
                                 5
                             ])

                    assertThat
                        (meta.LineMarkers |> Map.forall (fun _ marker -> marker = Mark))
                        (tag "and they are plain marks" >> isTrue)
            )

            test (
                "ins and del mark lines as a diff",
                fun _ ->
                    let meta = parse "ins={2} del={3-4}"

                    assertThat
                        (Map.tryFind 2 meta.LineMarkers)
                        (tag "an insertion" >> isEqualTo (Some Insert))

                    assertThat
                        (Map.tryFind 3 meta.LineMarkers)
                        (tag "a deletion" >> isEqualTo (Some Delete))

                    assertThat
                        (Map.tryFind 4 meta.LineMarkers)
                        (tag "over a range" >> isEqualTo (Some Delete))
            )

            test (
                "slashes mark words by regular expression",
                fun _ ->
                    let meta = parse "/Site\\.\\w+/"
                    assertThat (List.length meta.WordMarkers) (tag "one word marker" >> isEqualTo 1)

                    let expression, marker = List.head meta.WordMarkers

                    assertThat
                        (expression.IsMatch "Site.create")
                        (tag "it matches what it should" >> isTrue)

                    assertThat
                        (expression.IsMatch "Sitecreate")
                        (tag "and not what it should not" >> isFalse)

                    assertThat marker (tag "an unqualified marker is a mark" >> isEqualTo Mark)
            )

            test (
                "a regular expression may hold an equals sign",
                fun _ ->
                    let meta = parse "/title=\"Program\\.fs\"/"

                    assertThat
                        (List.length meta.WordMarkers)
                        (tag "the marker survived" >> isEqualTo 1)

                    assertThat (meta.Unknown) (tag "and was not read as an option" >> isEqualTo [])

                    let expression, _ = List.head meta.WordMarkers

                    assertThat
                        (expression.IsMatch "```fsharp title=\"Program.fs\"")
                        (tag "it matches the meta it names" >> isTrue)
            )

            test (
                "a quoted word is matched literally",
                fun _ ->
                    let expression, _ = (parse "\"a.b\"").WordMarkers |> List.head
                    assertThat (expression.IsMatch "a.b") (tag "the literal matches" >> isTrue)

                    assertThat
                        (expression.IsMatch "axb")
                        (tag "and the dot is not a wildcard" >> isFalse)
            )

            test (
                "line numbers can start anywhere",
                fun _ ->
                    let meta = parse "showLineNumbers startLineNumber=42"
                    assertThat meta.ShowLineNumbers (tag "numbers are shown" >> isTrue)

                    assertThat
                        meta.StartLineNumber
                        (tag "starting where the excerpt does" >> isEqualTo 42)
            )

            test (
                "frames are named",
                fun _ ->
                    assertThat
                        (parse "frame=\"terminal\"").Frame
                        (tag "a terminal" >> isEqualTo TerminalFrame)

                    assertThat
                        (parse "frame=\"none\"").Frame
                        (tag "or none at all" >> isEqualTo NoFrame)
            )

            test (
                "collapse takes ranges",
                fun _ ->
                    assertThat
                        (parse "collapse={3-5,8-9}").Collapse
                        (tag "several ranges"
                         >> isEqualTo
                             [
                                 3, 5
                                 8, 9
                             ])
            )

            test (
                "unknown tokens are kept for plugins",
                fun _ ->
                    assertThat
                        (parse "twoslash").Unknown
                        (tag "and not silently dropped" >> isEqualTo [ "twoslash" ])
            )
        ]
    )

let rendering =
    testList (
        "rendering",
        [
            test (
                "every line becomes an element",
                fun _ ->
                    let html = render CodeBlockMeta.empty "one\ntwo\nthree"

                    assertThat
                        (html.Split("nacara-code__line").Length - 1)
                        (tag "three lines, three elements" >> isEqualTo 3)
            )

            test (
                "markup in code is escaped",
                fun _ ->
                    let html = render CodeBlockMeta.empty "<script>alert(1)</script>"

                    assertThat
                        (html.Contains "<script>")
                        (tag "the tag does not survive" >> isFalse)

                    assertThat
                        (html.Contains "&lt;script&gt;")
                        (tag "it is shown as text" >> isTrue)
            )

            test (
                "a title becomes a caption",
                fun _ ->
                    let html = render (parse "title=\"Program.fs\"") "code"
                    assertThat (html.Contains "<figcaption") (tag "there is a caption" >> isTrue)
                    assertThat (html.Contains "Program.fs") (tag "naming the file" >> isTrue)
            )

            test (
                "marked lines carry their marker",
                fun _ ->
                    let html = render (parse "ins={2}") "one\ntwo"

                    assertThat
                        (html.Contains "data-marker=\"ins\"")
                        (tag "the line is marked as an insertion" >> isTrue)
            )

            test (
                "word markers wrap only the match",
                fun _ ->
                    let html = render (parse "\"two\"") "one two three"

                    assertThat
                        (html.Contains "<mark class=\"nacara-code__word\"")
                        (tag "the word is wrapped" >> isTrue)

                    assertThat (html.Contains ">two</mark>") (tag "and only the word" >> isTrue)
            )

            test (
                "a collapsed range becomes a disclosure",
                fun _ ->
                    let html = render (parse "collapse={2-3}") "one\ntwo\nthree\nfour"
                    assertThat (html.Contains "<details") (tag "the range is collapsed" >> isTrue)

                    assertThat
                        (html.Contains "2 collapsed lines")
                        (tag "and says how much is hidden" >> isTrue)
            )

            test (
                "a block collapsed from its first line has no empty block before it",
                fun _ ->
                    let html = render (parse "collapse={1-2}") "one\ntwo\nthree"

                    assertThat
                        (html.Contains "<pre><code></code></pre>")
                        (tag "no empty pre is emitted" >> isFalse)
            )
        ]
    )

let layering =
    testList (
        "layering",
        [
            test (
                "with no theme, code still renders",
                fun _ ->
                    let html = renderMinimal CodeBlockMeta.empty "let x = 1"
                    assertThat (html.StartsWith "<pre><code") (tag "plain markup" >> isTrue)
                    assertThat (html.Contains "let x = 1") (tag "with the code in it" >> isTrue)

                    assertThat
                        (html.Contains "nacara-code")
                        (tag "and none of the theme's class names" >> isFalse)
            )

            test (
                "the engine emits no markup of its own for a themed site",
                fun _ ->
                    let html = render (CodeBlockMeta.parse "title=\"a.fs\"") "let x = 1"

                    assertThat
                        (html.StartsWith "<figure class=\"nacara-code\"")
                        (tag "the theme's markup" >> isTrue)
            )

            test (
                "the last renderer registered wins",
                fun _ ->
                    let mine =
                        { new ICodeBlockRenderer with
                            member _.Name = "mine"
                            member _.Render _ = "<div class=\"mine\"></div>"
                        }

                    let html =
                        CodeBlock.render
                            [
                                renderer
                                mine
                            ]
                            []
                            {
                                Language = None
                                Code = "let x = 1"
                                Meta = CodeBlockMeta.empty
                            }

                    assertThat
                        html
                        (tag "the site's renderer, not the theme's"
                         >> isEqualTo "<div class=\"mine\"></div>")
            )

            test (
                "preparing a block resolves markers and numbers",
                fun _ ->
                    let prepared =
                        CodeBlock.prepare
                            []
                            {
                                Language = None
                                Code = "one\ntwo\nthree"
                                Meta = CodeBlockMeta.parse "ins={2} collapse={3-3}"
                            }

                    assertThat
                        (prepared.Lines |> List.map _.Number)
                        (tag "lines are numbered"
                         >> isEqualTo
                             [
                                 1
                                 2
                                 3
                             ])

                    assertThat
                        (prepared.Lines |> List.map _.Marker)
                        (tag "line markers are resolved before any markup exists"
                         >> isEqualTo
                             [
                                 None
                                 Some Insert
                                 None
                             ])

                    assertThat
                        (prepared.Lines |> List.map _.IsCollapsed)
                        (tag "and so is collapsing"
                         >> isEqualTo
                             [
                                 false
                                 false
                                 true
                             ])
            )
        ]
    )

let diff =
    testList (
        "diff",
        [
            test (
                "a line written with + or - is marked, and loses its marker",
                fun _ ->
                    let block =
                        {
                            Language = Some "diff"
                            Code = "let a = 1\n+let b = 2\n-let c = 3"
                            Meta = CodeBlockMeta.empty
                        }

                    let prepared = CodeBlock.prepare [] block

                    assertThat
                        (prepared.Lines |> List.map (fun line -> line.Marker))
                        (tag "the second is an insert, the third a delete"
                         >> isEqualTo
                             [
                                 None
                                 Some Insert
                                 Some Delete
                             ])

                    assertThat
                        (prepared.Lines
                         |> List.map (fun line ->
                             line.Pieces |> List.map _.Text |> String.concat ""
                         ))
                        (tag "and what is left is the code"
                         >> isEqualTo
                             [
                                 "let a = 1"
                                 "let b = 2"
                                 "let c = 3"
                             ])
            )

            test (
                "the gutter comes off when every line has one",
                fun _ ->
                    let block =
                        {
                            Language = Some "diff"
                            Code = "  let a = 1\n+ let b = 2\n- let c = 3"
                            Meta = CodeBlockMeta.empty
                        }

                    assertThat
                        (CodeBlock.prepare [] block
                         |> _.Lines
                         |> List.map (fun line ->
                             line.Pieces |> List.map _.Text |> String.concat ""
                         ))
                        (tag "one space of gutter, and the indentation left alone"
                         >> isEqualTo
                             [
                                 " let a = 1"
                                 " let b = 2"
                                 " let c = 3"
                             ])
            )

            test (
                "a diff someone pasted keeps every character of it",
                fun _ ->
                    let code =
                        "--- a/Program.fs\n+++ b/Program.fs\n@@ -1,3 +1,3 @@\n-let a = 1\n+let a = 2"

                    let block =
                        {
                            Language = Some "diff"
                            Code = code
                            Meta = CodeBlockMeta.empty
                        }

                    let prepared = CodeBlock.prepare [] block

                    assertThat
                        (prepared.Lines
                         |> List.map (fun line ->
                             line.Pieces |> List.map _.Text |> String.concat ""
                         )
                         |> String.concat "\n")
                        (tag "left exactly as it was pasted" >> isEqualTo code)

                    assertThat
                        (prepared.Lines |> List.forall (fun line -> line.Marker.IsNone))
                        (tag "and nothing in it is marked" >> isTrue)
            )

            test (
                "lang says what the code under a diff is",
                fun _ ->
                    let highlighter =
                        { new IHighlighter with
                            member _.Name = "only-fsharp"

                            member _.Highlight(language, code) =
                                if language = Some "fsharp" then
                                    Some
                                        [
                                            for line in code.Split('\n') ->
                                                [
                                                    {
                                                        Text = line
                                                        ClassName = Some "tok-keyword"
                                                    }
                                                ]
                                        ]
                                else
                                    None
                        }

                    let block =
                        {
                            Language = Some "diff"
                            Code = "let a = 1\n+let b = 2"
                            Meta =
                                { CodeBlockMeta.empty with
                                    HighlightAs = Some "fsharp"
                                }
                        }

                    let prepared = CodeBlock.prepare [ highlighter ] block

                    assertThat
                        (prepared.Lines
                         |> List.forall (fun line ->
                             line.Pieces |> List.forall (fun piece -> piece.ClassName.IsSome)
                         ))
                        (tag "a highlighter that only knows F# coloured it" >> isTrue)

                    assertThat
                        (prepared.Lines |> List.map (fun line -> line.Marker))
                        (tag "and the markers survived the colouring"
                         >> isEqualTo
                             [
                                 None
                                 Some Insert
                             ])
            )

            test (
                "what you copy is what the change leaves behind",
                fun _ ->
                    let block =
                        {
                            Language = Some "diff"
                            Code = "let a = 1\n-let b = 2\n+let b = 3"
                            Meta = CodeBlockMeta.empty
                        }

                    let prepared = CodeBlock.prepare [] block

                    assertThat
                        (CodeBlock.source prepared)
                        (tag "the deleted line is gone" >> isEqualTo "let a = 1\nlet b = 3")

                    assertThat
                        (prepared.Lines |> List.length)
                        (tag "though the page still shows all three" >> isEqualTo 3)
            )

            test (
                "a line del= names is left out of it too",
                fun _ ->
                    let block =
                        {
                            Language = Some "fsharp"
                            Code = "let a = 1\nlet b = 2"
                            Meta =
                                { CodeBlockMeta.empty with
                                    LineMarkers = Map [ 2, Delete ]
                                }
                        }

                    assertThat
                        (CodeBlock.prepare [] block |> CodeBlock.source)
                        (tag "only what survives" >> isEqualTo "let a = 1")
            )

            test (
                "a marker written in the meta wins over one written in the code",
                fun _ ->
                    let block =
                        {
                            Language = Some "diff"
                            Code = "+let a = 1"
                            Meta =
                                { CodeBlockMeta.empty with
                                    LineMarkers = Map [ 1, Mark ]
                                }
                        }

                    assertThat
                        (CodeBlock.prepare [] block |> _.Lines |> List.head |> _.Marker)
                        (tag "the one reached for deliberately" >> isEqualTo (Some Mark))
            )
        ]
    )

let all =
    testList (
        "CodeBlock",
        [
            meta
            rendering
            layering
            highlighters
            diff
        ]
    )
