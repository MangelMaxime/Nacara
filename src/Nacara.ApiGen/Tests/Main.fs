module Main

open Expecto

let tests =
    testList "All" [
        Tests.CommentFormatter.tests
        Tests.Render.tests
        Tests.Markdown.tests
    ]

[<EntryPoint>]
let main args =
    runTestsWithCLIArgs [] args tests
