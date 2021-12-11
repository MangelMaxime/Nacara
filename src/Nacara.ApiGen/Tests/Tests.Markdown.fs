module Tests.Markdown

open Expecto
open FSharp.Formatting.ApiDocs
open Utils
open Markdown

let tests =
    ftestList "Nacara.ApiGen.Markdown" [

        testList "formatSpan" [

            test "supports InlineCode" {
                let expected =
                    "`let x = 12`"

                let actual =
                    InlineCode "let x = 12"
                    |> formatSpan

                Expect.equal actual expected
            }

            test "supports Literal text" {
                let expected =
                    """<div class="strong">Hello world</div>"""

                let actual =
                    Literal """<div class="strong">Hello world</div>"""
                    |> formatSpan

                Expect.equal actual expected
            }

            test "supports Strong" {
                let expected =
                    "**Hello world**"

                let actual =
                    Strong [ Literal "Hello world" ]
                    |> formatSpan

                Expect.equal actual expected
            }

            test "supports Strong with Emphasis text in it" {
                let expected =
                    "**Hello *world***"

                let actual =
                    Strong [ Literal "Hello "; Emphasis [ Literal "world" ] ]
                    |> formatSpan

                Expect.equal actual expected
            }

            test "supports Emphasis" {
                let expected =
                    "*Hello world*"

                let actual =
                    Emphasis [ Literal "Hello world" ]
                    |> formatSpan

                Expect.equal actual expected
            }

            test "supports DirectLink without title" {
                let expected =
                    "[Hello world](https://nacara.org)"

                let actual =
                    DirectLink ([ Literal "Hello world" ], "https://nacara.org", None)
                    |> formatSpan

                Expect.equal actual expected
            }

            test "supports DirectLink with title" {
                let expected =
                    "[Hello world](https://nacara.org \"Nacara title\")"

                let actual =
                    DirectLink ([ Literal "Hello world" ], "https://nacara.org", Some "Nacara title")
                    |> formatSpan

                Expect.equal actual expected
            }

            test "supports DirectImage without title" {
                let expected =
                    "![Description of the image](https://dummyimage.com/600x400/000/fff)"

                let actual =
                    DirectImage ("Description of the image", "https://dummyimage.com/600x400/000/fff", None)
                    |> formatSpan

                Expect.equal actual expected
            }

            test "support DirectImage with title" {
                let expected =
                    "![Description of the image](https://dummyimage.com/600x400/000/fff \"Nacara title\")"

                let actual =
                    DirectImage ("Description of the image", "https://dummyimage.com/600x400/000/fff", Some "Nacara title")
                    |> formatSpan

                Expect.equal actual expected
            }

            test "support HardLineBreak" {
                let expected =
                    "\n"

                let actual =
                    HardLineBreak
                    |> formatSpan

                Expect.equal actual expected
            }

        ]

        testList "formatSpans" [

            test "works" {
                let expected =
                    "**Hello *world***\n[This is a link](https://nacara.org)\n![This is an image](https://dummyimage.com/600x400/000/fff)\nand here is some `code`"

                let actual =
                    [
                        Strong [ Literal "Hello "; Emphasis [ Literal "world" ] ]
                        HardLineBreak
                        DirectLink ([ Literal "This is a link" ], "https://nacara.org", None)
                        HardLineBreak
                        DirectImage ("This is an image", "https://dummyimage.com/600x400/000/fff", None)
                        HardLineBreak
                        Literal "and here is some "
                        InlineCode "code"
                    ]
                    |> formatSpans

                Expect.equal actual expected
            }

        ]

        testList "formatParagraph" [

            test "supports Heading" {
                let expected =
                    [
                        "## Hello world"
                    ]

                let actual =
                    Heading (2, [ Literal "Hello world" ])
                    |> formatParagraph

                Expect.equal actual expected
            }

            test "supports Paragraph" {
                let expected =
                    [
                        "**Hello *world***`let x = 42`"
                        ""
                    ]

                let actual =
                    Paragraph [
                        Strong [ Literal "Hello "; Emphasis [ Literal "world" ] ]
                        InlineCode "let x = 42"
                    ]
                    |> formatParagraph

                Expect.equal actual expected
            }

            test "supports CodeBlock with no fence provided and no language" {
                let expected =
                    [
                        "```\nlet x = 42\n```"
                        ""
                    ]

                let actual =
                    CodeBlock ("let x = 42", None, None)
                    |> formatParagraph

                Expect.equal actual expected
            }

            test "supports CodeBlock with no fence provided and language" {
                let expected =
                    [
                        "```fsharp\nlet x = 42\n```"
                        ""
                    ]

                let actual =
                    CodeBlock ("let x = 42", None, Some "fsharp")
                    |> formatParagraph

                Expect.equal actual expected
            }

            test "supports CodeBlock with fence provided and no language" {
                let expected =
                    [
                        "````\nlet x = 42\n````"
                        ""
                    ]

                let actual =
                    CodeBlock ("let x = 42", Some "````", None)
                    |> formatParagraph

                Expect.equal actual expected
            }

            test "supports CodeBlock with fence provided and language" {
                let expected =
                    [
                        "````fsharp\nlet x = 42\n````"
                        ""
                    ]

                let actual =
                    CodeBlock ("let x = 42", Some "````", Some "fsharp")
                    |> formatParagraph

                Expect.equal actual expected
            }

            test "support InlineHtmlBlock" {
                let expected =
                    [
                        "<div class=\"strong\">Hello world</div>"
                    ]

                let actual =
                    InlineHtmlBlock "<div class=\"strong\">Hello world</div>"
                    |> formatParagraph

                Expect.equal actual expected
            }

            test "support HorizontalRule" {
                let expected =
                    [
                        "---"
                        ""
                    ]

                let actual =
                    HorizontalRule
                    |> formatParagraph

                Expect.equal actual expected
            }

            test "support YamlFrontmatter" {
                let expected =
                    [
                        "---"
                        "title: Hello world"
                        "---"
                        ""
                    ]

                let actual =
                    YamlFrontmatter [ "title: Hello world" ]
                    |> formatParagraph

                Expect.equal actual expected
            }

        ]

        testList "formatParagraphs" [

            test "works" {
                let expected =
                    [
                        "---"
                        "title: Hello world"
                        "---"
                        ""
                        "## Hello **world**"
                        "<div class=\"strong\">Hello world</div>"
                        "This is a *Paragraph* with some `code` and an image: ![This is an image](https://dummyimage.com/600x400/000/fff)"
                        ""
                    ]

                let actual =
                    [
                        YamlFrontmatter [ "title: Hello world" ]
                        Heading (2, [ Literal "Hello "; Strong [ Literal "world" ] ])
                        InlineHtmlBlock "<div class=\"strong\">Hello world</div>"
                        Paragraph [
                            Literal "This is a "
                            Emphasis [ Literal "Paragraph" ]
                            Literal " with some "
                            InlineCode "code"
                            Literal " and an image: "
                            DirectImage ("This is an image", "https://dummyimage.com/600x400/000/fff", None)
                        ]
                    ]
                    |> formatParagraphs

                Expect.equal actual expected
            }

        ]

        test "MarkdownDocument.ToMarkdown works" {
            let expected =
                """---
title: Hello world
---

## Hello **world**
<div class="strong">Hello world</div>
This is a *Paragraph* with some `code` and an image: ![This is an image](https://dummyimage.com/600x400/000/fff)

And here there is another paragraph, with a multiline line code block:

```fsharp
let add x y = x + y

let a = 42
let b = 5
let c = add a b
```
"""
            let actual =
                MarkdownDocument(
                    [
                        YamlFrontmatter [ "title: Hello world" ]
                        Heading (2, [ Literal "Hello "; Strong [ Literal "world" ] ])
                        InlineHtmlBlock "<div class=\"strong\">Hello world</div>"
                        Paragraph [
                            Literal "This is a "
                            Emphasis [ Literal "Paragraph" ]
                            Literal " with some "
                            InlineCode "code"
                            Literal " and an image: "
                            DirectImage ("This is an image", "https://dummyimage.com/600x400/000/fff", None)
                        ]
                        Paragraph [
                            Literal "And here there is another paragraph, with a multiline line code block:"
                        ]
                        CodeBlock (
                            """let add x y = x + y

let a = 42
let b = 5
let c = add a b"""
                            , Some "```"
                            , Some "fsharp"
                        )
                    ]
                ).ToMarkdown()

            Expect.equal actual expected
        }

    ]
