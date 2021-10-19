module Main

open Expecto

let tests =
    testList "All" [
        Tests.CommentFormatter.tests
    ]

[<EntryPoint>]
let main args =
    runTestsWithCLIArgs [] args tests
