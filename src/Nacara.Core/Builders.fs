namespace Nacara.Core

open System
open Nacara.Core

type ConfigBuilder() =

    member _.Yield(_: unit) = []

    member _.Yield(directory) =
        [
            fun (args: Config) ->
                { args with
                    Directory = directory
                }
        ]

    member _.Yield(renderConfig) =
        [
            fun (args: Config) ->
                { args with
                    Render = renderConfig :: args.Render
                }
        ]

    member _.Yield(templateConfig) =
        [
            fun (args: Config) ->
                { args with
                    Templates = templateConfig :: args.Templates
                }
        ]

    member _.Yield(sassArgs) =
        [
            fun (args: Config) ->
                { args with
                    Sass = Some sassArgs
                }
        ]

    member _.Run(args) =
        let initialConfig =
            {
                Port = 8080
                Directory =
                    {
                        Source = "docsrc"
                        Output = "public"
                        Loaders = "loaders"
                    }
                Render = []
                Templates = []
                Sass = None
            }

        List.fold (fun args f -> f args) initialConfig args

    member _.Combine(args) =
        let (a, b) = args

        List.concat
            [
                a
                b
            ]

    member this.For(args, delayedArgs) = this.Combine(args, delayedArgs ())

    member _.Delay f = f ()

    member _.Zero _ = ()

    [<CustomOperation("port")>]
    member _.Port(args, port : int) =
        List.Cons(
            (fun (config : Config) ->
                { config with
                    Port = port
                }
            ),
            args
        )


type DirectoryBuilder() =

    member _.Yield(_: unit) =
        {
            Source = "docs"
            Output = "public"
            Loaders = "loaders"
        }

    [<CustomOperation("source")>]
    member _.Source(directoryConfig, newValue: string) =
        { directoryConfig with
            Source = newValue
        }

    [<CustomOperation("output")>]
    member _.Output(directoryConfig, newValue: string) =
        { directoryConfig with
            Output = newValue
        }

    [<CustomOperation("loaders")>]
    member _.Loaders(directoryConfig, newValue: string) =
        { directoryConfig with
            Loaders = newValue
        }


type RenderBuilder() =
    member _.Yield(_: unit) =
        {
            Layout = ""
            OutputAction = ChangeExtension("html")
            Script = ""
        }

    member _.Run(renderConfig: RenderConfig) = renderConfig

    [<CustomOperation("layout")>]
    member _.Layout(renderConfig: RenderConfig, newValue: string) =
        { renderConfig with
            Layout = newValue
        }

    [<CustomOperation("change_extension_to")>]
    member _.ChangeExtension(renderConfig, newValue: string) =
        { renderConfig with
            OutputAction = ChangeExtension(newValue)
        }

    [<CustomOperation("script")>]
    member _.Script(renderConfig, newValue: string) =
        { renderConfig with
            Script = newValue
        }

type TemplateConfigBuilder() =

    member _.Yield(_: unit) = []

    member _.Yield(frontMatter) =
        [
            fun (args: TemplateConfig) ->
                { args with
                    FrontMatter = frontMatter
                }
        ]

    member _.Run(args) =
        let initialConfig =
            {
                Extension = ""
                FrontMatter =
                    {
                        StartDelimiter = ""
                        EndDelimiter = ""
                    }
            }

        let result =
            List.fold (fun args f -> f args) initialConfig args

        // Validate that the configuration is valid
        if String.IsNullOrEmpty result.Extension then
            eprintfn "Missing template extension"

        else if String.IsNullOrEmpty result.FrontMatter.StartDelimiter then
            eprintfn "Missing front matter start delimiter"

        else if String.IsNullOrEmpty result.FrontMatter.EndDelimiter then
            eprintfn "Missing front matter end delimiter"

        result

    member _.Combine(args) =
        let (a, b) = args

        List.concat
            [
                a
                b
            ]

    member this.For(args, delayedArgs) = this.Combine(args, delayedArgs ())

    member _.Delay f = f ()

    member _.Zero _ = ()

    [<CustomOperation("extension")>]
    member _.Extension(args, extension: string) =
        List.Cons(
            (fun (templateConfig: TemplateConfig) ->
                { templateConfig with
                    Extension = extension
                }
            ),
            args
        )

type FrontMatterConfigBuilder() =

    member _.Yield(_: unit) =
        {
            StartDelimiter = "---"
            EndDelimiter = "---"
        }

    [<CustomOperation("delimiter")>]
    member _.Source(config, newValue: string) =
        { config with
            StartDelimiter = newValue
            EndDelimiter = newValue
        }

    [<CustomOperation("start_delimiter")>]
    member _.StartDelimiter(config, newValue: string) =
        { config with
            StartDelimiter = newValue
        }

    [<CustomOperation("end_delimiter")>]
    member _.EndDelimiter(config, newValue: string) =
        { config with
            EndDelimiter = newValue
        }

type SassArgsBuilder() =

    member _.Yield(_: unit) =
        []

    [<CustomOperation("input")>]
    member _.Input(sassArgs, newValue: string) =
        (SassArg.Input newValue) :: sassArgs

    [<CustomOperation("output")>]
    member _.Output(sassArgs, newValue: string) =
        (SassArg.Output newValue) :: sassArgs

[<AutoOpen>]
module Builders =

    let config = ConfigBuilder()
    let directory = DirectoryBuilder()
    let render = RenderBuilder()
    let template = TemplateConfigBuilder()

    let front_matter =
        FrontMatterConfigBuilder()

    let sass = SassArgsBuilder()
