module Tests.Render

open Expecto
open FSharp.Formatting.ApiDocs
open Utils
open System.IO
open Giraffe.ViewEngine

// let cwd = Directory.GetCurrentDirectory()

// let libDir = Path.GetFullPath("Project/bin/Debug/net5.0/publish", cwd)
// let dllFile = Path.Combine(libDir, "TestProject.dll")
// let project = "TestProject"
// let baseUrl = "/test-project"

// let apiDocInput = ApiDocInput.FromFile(dllFile)

// let apiDocModel =
//     ApiDocs.GenerateModel(
//         [ apiDocInput ],
//         project,
//         [],
//         qualify = true,
//         libDirs = [
//             libDir
//         ],
//         root = baseUrl
//     )

// let namespaceApiDocNamespace =
//     apiDocModel.Collection.Namespaces
//     |> List.find (fun info ->
//         info.Name = "PureNamespace"
//     )

let tests =
    testList "Nacara.ApiGen.Render" [

        // test "namespace works" {
        //     let actual =
        //         Nacara.ApiGen.Render.New.renderNamespace
        //             namespaceApiDocNamespace

        //     let expected =
        //         h2
        //             [ _class "title is-3" ]
        //             [ str "PureNamespace" ]

        //     Expect.equal actual expected
        // }

    ]
