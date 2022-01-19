module Tests.Render

open Expecto
open FSharp.Formatting.ApiDocs
open Utils
open System.IO
open Giraffe.ViewEngine
open Markdown

let cwd = Directory.GetCurrentDirectory()

let libDir = Path.GetFullPath("Project/bin/Debug/net5.0/publish", cwd)
let dllFile = Path.Combine(libDir, "TestProject.dll")
let project = "TestProject"
let baseUrl = "/test-project"

let apiDocInput = ApiDocInput.FromFile(dllFile)

let apiDocModel =
    ApiDocs.GenerateModel(
        [ apiDocInput ],
        project,
        [],
        qualify = true,
        libDirs = [
            libDir
        ],
        root = baseUrl
    )

let pureNamespace =
    apiDocModel.Collection.Namespaces
    |> List.find (fun info ->
        info.Name = "PureNamespace"
    )

let globalNamespace =
    apiDocModel.Collection.Namespaces
    |> List.find (fun info ->
        info.Name = "global"
    )

let fixtureModule =
    globalNamespace.Entities
    |> List.find (fun entity ->
        entity.Name = "Fixtures"
    )

let moduleWithFunctionsAndValues =
    globalNamespace.Entities
    |> List.find (fun entity ->
        entity.Name = "ModuleWithFunctionsAndValues"
    )

let generateLink (urlBaseName : string) =
    $"/reference/TestProject/{urlBaseName}.html"


let tests =
    ftestList "Nacara.ApiGen.Render" [

        test "renderNamespace for a normal namespace" {
            let actual =
                Nacara.ApiGen.Render.renderNamespace
                    id
                    pureNamespace

            let expected =
                [
                    InlineHtmlBlock (
                        h2
                            [ _class "title is-3" ]
                            [ str "PureNamespace" ]
                    )
                ]

            Expect.equal actual expected
        }

        test "renderNamespace works for the global one" {
            let actual =
                Nacara.ApiGen.Render.renderNamespace
                    generateLink
                    globalNamespace

            let expected =
                [
                    InlineHtmlBlock (
                        h2
                            [ _class "title is-3" ]
                            [ str "global" ]
                    )
                    InlineHtmlBlock (
                        p [ _class "is-size-5" ]
                            [
                                strong [ ]
                                    [ str "Declared modules" ]
                            ]
                    )

                    Paragraph [ HardLineBreak ]

                    InlineHtmlBlock (
                        table [ _class "table is-bordered docs-modules" ]
                            [
                                thead [ ]
                                    [
                                        tr [ ]
                                            [
                                                th [ _width "25%" ]
                                                    [ str "Module" ]
                                                th [ _width "75%" ]
                                                    [ str "Description" ]
                                            ]
                                    ]
                                tbody [ ]
                                    [
                                        tr [ ]
                                            [
                                                td [ ]
                                                    [
                                                        a [ _href (generateLink "global-fixtures") ]
                                                            [ str "Fixtures" ]
                                                    ]
                                                td [ ]
                                                    [ str "" ]
                                            ]

                                        tr [ ]
                                            [
                                                td [ ]
                                                    [
                                                        a [ _href (generateLink "global-globalmodulea") ]
                                                            [ str "GlobalModuleA" ]
                                                    ]
                                                td [ ]
                                                    [
                                                        str "\n\nThis is a global module\n\n"
                                                    ]
                                            ]

                                        tr [ ]
                                            [
                                                td [ ]
                                                    [
                                                        a [ _href (generateLink "global-globalmoduleb") ]
                                                            [ str "GlobalModuleB" ]
                                                    ]
                                                td [ ]
                                                    [
                                                        str "\n\nThis is a second global module.\n\n"
                                                    ]
                                            ]
                                    ]
                            ]
                    )
                ]

            Expect.equal actual expected
        }

        test "renderIndex works" {
            let actual =
                Nacara.ApiGen.Render.renderIndex
                    generateLink
                    apiDocModel.Collection.Namespaces

            let expected =
                [
                    InlineHtmlBlock (
                        p [ _class "is-size-5" ]
                            [
                                strong [ ]
                                    [ str "Declared namespaces" ]
                            ]
                    )

                    Paragraph [ HardLineBreak ]

                    InlineHtmlBlock (
                        table [ _class "table is-bordered docs-modules" ]
                            [
                                thead [ ]
                                    [
                                        tr [ ]
                                            [
                                                th [ _width "25%" ]
                                                    [ str "Namespace" ]
                                                th [ _width "75%" ]
                                                    [ str "Description" ]
                                            ]
                                    ]
                                tbody [ ]
                                    [
                                        tr [ ]
                                            [
                                                td [ ]
                                                    [
                                                        a [ _href (generateLink "namespacewithmodule") ]
                                                            [ str "NamespaceWithModule" ]
                                                    ]
                                                td [ ] [ ]
                                            ]

                                        tr [ ]
                                            [
                                                td [ ]
                                                    [
                                                        a [ _href (generateLink "purenamespace") ]
                                                            [ str "PureNamespace" ]
                                                    ]
                                                td [ ] [ ]
                                            ]
                                    ]
                            ]
                    )

                    InlineHtmlBlock (
                        p [ _class "is-size-5" ]
                            [
                                strong [ ]
                                    [ str "Declared modules" ]
                            ]
                    )

                    Paragraph [ HardLineBreak ]

                    InlineHtmlBlock (
                        table [ _class "table is-bordered docs-modules" ]
                            [
                                thead [ ]
                                    [
                                        tr [ ]
                                            [
                                                th [ _width "25%" ]
                                                    [ str "Module" ]
                                                th [ _width "75%" ]
                                                    [ str "Description" ]
                                            ]
                                    ]
                                tbody [ ]
                                    [
                                        tr [ ]
                                            [
                                                td [ ]
                                                    [
                                                        a [ _href (generateLink "global-fixtures") ]
                                                            [ str "Fixtures" ]
                                                    ]
                                                td [ ]
                                                    [ str "" ]
                                            ]

                                        tr [ ]
                                            [
                                                td [ ]
                                                    [
                                                        a [ _href (generateLink "global-globalmodulea") ]
                                                            [ str "GlobalModuleA" ]
                                                    ]
                                                td [ ]
                                                    [
                                                        str "\n\nThis is a global module\n\n"
                                                    ]
                                            ]

                                        tr [ ]
                                            [
                                                td [ ]
                                                    [
                                                        a [ _href (generateLink "global-globalmoduleb") ]
                                                            [ str "GlobalModuleB" ]
                                                    ]
                                                td [ ]
                                                    [
                                                        str "\n\nThis is a second global module.\n\n"
                                                    ]
                                            ]
                                    ]
                            ]
                    )
                ]

            Expect.equal actual expected
        }

        testList "renderValueOrFunction" [
            test "works for value without a comment" {
                let actual =
                    Nacara.ApiGen.Render.renderValueOrFunction
                        generateLink
                        moduleWithFunctionsAndValues.ValuesAndFuncs

                ()
            }
        ]

    ]
