module EasyBuild.Main

open Spectre.Console.Cli
open EasyBuild.Commands.Test
open EasyBuild.Commands.Format
open EasyBuild.Commands.Docs
open EasyBuild.Commands.Release
open EasyBuild.Commands.TreeSitter.Runtime
open EasyBuild.Commands.TreeSitter.Bundle
open EasyBuild.Commands.TreeSitter.Publish

[<EntryPoint>]
let main args =
    let app = CommandApp()

    app.Configure(fun config ->
        config.Settings.ApplicationName <- "./build.sh"

        config
            .AddCommand<TestCommand>("test")
            .WithDescription("Run the test suite")
            .WithExample("test")
            .WithExample("test --update-snapshots")
        |> ignore

        config
            .AddCommand<ReleaseCommand>("release")
            .WithDescription("Push the packages to nuget.org, in order")
        |> ignore

        config
            .AddCommand<FormatCommand>("format")
            .WithDescription("Format the F#, the css and the javascript")
            .WithExample("format")
            .WithExample("format --check")
        |> ignore

        config.AddBranch(
            "docs",
            fun (docs: IConfigurator<CommandSettings>) ->
                docs.SetDescription "Write and build this repository's documentation"

                docs
                    .AddCommand<WatchCommand>("watch")
                    .WithDescription("Serve it, rebuilding as you write")
                    .WithExample("docs watch")
                    .WithExample("docs watch --host")
                |> ignore

                docs
                    .AddCommand<BuildCommand>("build")
                    .WithDescription("Build it into docs/output")
                    .WithExample("docs build")
                |> ignore

                docs
                    .AddCommand<CheckCommand>("check")
                    .WithDescription("Build it all, write none of it, fail on anything wrong")
                    .WithExample("docs check")
                |> ignore

                docs
                    .AddCommand<CleanCommand>("clean")
                    .WithDescription("Remove what a build wrote")
                    .WithExample("docs clean")
                |> ignore

                docs
                    .AddCommand<DeployCommand>("deploy")
                    .WithDescription("Publish the last build to the gh-pages branch")
                    .WithExample("docs deploy --dry-run")
                    .WithExample("docs deploy")
                |> ignore
        )
        |> ignore

        config.AddBranch(
            "tree-sitter",
            fun (treeSitter: IConfigurator<CommandSettings>) ->
                treeSitter.SetDescription "Build what the tree-sitter plugin needs"

                treeSitter
                    .AddCommand<RuntimeCommand>("runtime")
                    .WithDescription("Build tree-sitter and fetch wasmtime, for this machine")
                    .WithExample("tree-sitter runtime")
                |> ignore

                treeSitter
                    .AddCommand<BundleCommand>("bundle")
                    .WithDescription("Build the grammars that ship inside the package")
                    .WithExample("tree-sitter bundle")
                |> ignore

                treeSitter
                    .AddCommand<PublishCommand>("publish")
                    .WithDescription("Publish this machine's runtime to npm")
                    .WithExample("tree-sitter publish --dry-run")
                |> ignore
        )
        |> ignore
    )

    app.Run args
