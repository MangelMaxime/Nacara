namespace Nacara.Core

open System.ComponentModel.Design
open Legivel.Attributes
open System.IO

type DirectoryConfig =
    {
        Source: string
        Output: string
        Loaders: string
    }

type CopyConfig =
    | File of string
    | Directory of string

type RenderOutputAction = ChangeExtension of string

type RenderConfig =
    {
        Layout: string
        OutputAction: RenderOutputAction
        Script: string
    }

type FrontMatterConfig =
    {
        StartDelimiter: string
        EndDelimiter: string
    }

type TemplateConfig =
    {
        Extension: string
        FrontMatter: FrontMatterConfig
    }

[<RequireQualifiedAccess>]
type SassArg =
    | Input of string
    | Output of string
    | Watch

module SassArg =

    let getArgRank (arg: SassArg) =
        match arg with
        | SassArg.Watch -> 0
        | SassArg.Input _ -> 9998
        | SassArg.Output _ -> 9999

module SassArgs =

    let sort =
        List.sortWith (fun arg1 arg2 ->
            compare (SassArg.getArgRank arg1) (SassArg.getArgRank arg2)
        )

    let toString (args : SassArg list) =
        args
        |> List.map (
            function
            | SassArg.Watch -> "--watch"
            | SassArg.Input input -> input
            | SassArg.Output output -> output
        )
        |> String.concat " "

type Config =
    {
        Port: int
        Directory: DirectoryConfig
        Render: RenderConfig list
        Templates: TemplateConfig list
        Sass: (SassArg list) option
    }

module Path =

    let normalize (path: string) = path.Replace("\\", "/")

[<RequireQualifiedAccess>]
module AbsolutePath =

    type AbsolutePath = private AbsolutePath of string

    let create (value: string) = value |> Path.normalize |> AbsolutePath

    let value (AbsolutePath absolutePath) = absolutePath

    let toLog (AbsolutePath absolutePath) = "'" + absolutePath + "'"

    let getDirectoryName (AbsolutePath absolutePath) =
        Path.GetDirectoryName(absolutePath)

    let getFileName (AbsolutePath absolutePath) =
        Path.GetFileName(absolutePath)

    let isDirectory (AbsolutePath absolutePath) =
        let fileAttributes = File.GetAttributes absolutePath
        fileAttributes.HasFlag(FileAttributes.Directory)

[<RequireQualifiedAccess>]
module RelativePath =

    type RelativePath = private RelativePath of string

    let create (value: string) = value |> Path.normalize |> RelativePath

    let value (RelativePath relativePath) = relativePath

// module FileName =

//     type T = private FileName of string

//     let create (value: string) = FileName value

//     let toString (FileName fileName: T) = fileName

[<RequireQualifiedAccess>]
module ProjectRoot =

    type ProjectRoot = private ProjectRoot of string

    let create (value: string) = value |> Path.normalize |> ProjectRoot

    let value (ProjectRoot projectRoot) = projectRoot

[<RequireQualifiedAccess>]
module PageId =

    type PageId = private PageId of string

    let create (value: string) = PageId value

    let value (PageId pageId) = pageId

// Add the concept of virtual files?
// Virtual files are files that are not present on the filesystem
// and would allow user to "inject" pages from a loader.
// Example usager: API documentation generation.
// It would read information from an fsproj and then generate a bunch of
// virtuals file for the different API pages.
type PageContext =
    {
        RelativePath: RelativePath.RelativePath
        AbsolutePath: AbsolutePath.AbsolutePath
        PageId: PageId.PageId
        Layout: string
        RawText: string
        FrontMatter: string
        Content: string
    }

type PageFrontMatter =
    {
        [<YamlField("layout")>]
        Layout: string
    }

type Context
    (
        projectRoot: ProjectRoot.ProjectRoot,
        isWatch: bool,
        logInfo: string -> unit,
        logError: string -> unit,
        logWarn: string -> unit,
        logDebug: string -> unit,
        logSuccess: string -> unit
    ) =
    let container = new ServiceContainer()

    member _.Add(value: 'T) =
        let key = typeof<ResizeArray<'T>>

        match container.GetService(key) with
        | :? ResizeArray<'T> as service -> service.Add(value)
        | _ ->
            container.AddService(
                key,
                ResizeArray
                    [
                        value
                    ]
            )

    member _.Replace(value: ResizeArray<'T>) =
        let key = typeof<ResizeArray<'T>>

        container.RemoveService(key)
        container.AddService(key, value)

    member _.GetValues<'T>() : seq<'T> =
        let key = typeof<ResizeArray<'T>>
        container.GetService(key) :?> seq<'T>

    member this.TryGetValues<'T>() =
        let key = typeof<ResizeArray<'T>>

        if isNull (container.GetService(key)) then
            None
        else
            Some(this.GetValues<'T>())

    member this.TryGetValue<'T>() =
        this.TryGetValues<'T>() |> Option.bind (Seq.tryHead)

    member _.LogInfo(msg: string) = logInfo msg

    member _.LogError(msg: string) = logError msg

    member _.LogWarn(msg: string) = logWarn msg

    member _.LogDebug(msg: string) = logDebug msg

    member _.LogSuccess(msg: string) = logSuccess msg

    member this.Config = this.TryGetValue<Config>() |> Option.get

    member _.ProjectRoot = projectRoot

    member _.IsWatch = isWatch

    member this.OutputPath =
        Path.Combine(
            ProjectRoot.value projectRoot,
            this.Config.Directory.Output
        )
        |> AbsolutePath.create

    member _.ConfigPath =
        Path.Combine(ProjectRoot.value projectRoot, "nacara.fsx")
        |> AbsolutePath.create

    member this.SourcePath =
        Path.Combine(
            ProjectRoot.value projectRoot,
            this.Config.Directory.Source
        )
        |> AbsolutePath.create

    member this.LoadersPath =
        Path.Combine(
            ProjectRoot.value projectRoot,
            this.Config.Directory.Loaders
        )
        |> AbsolutePath.create

type DependencyWatchInfo =
    {
        DependencyPath: AbsolutePath.AbsolutePath
        RendererPath: AbsolutePath.AbsolutePath
    }
