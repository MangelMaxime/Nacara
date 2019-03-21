module Types

open Thoth.Json
open System.Collections.Generic
open Fable.Import
open Fable.Core.JsInterop

type PostRenderDemos =
    { Script : string
      ImportSelector : string }

    static member Decoder =
        Decode.object (fun get ->
            { Script = get.Required.Field "script" Decode.string
              ImportSelector = get.Required.Field "importSelector" Decode.string }
        )

type PageAttributes =
    { Title : string option
      PostRenderDemos : PostRenderDemos option }

    static member Decoder =
        Decode.object (fun get ->
            { Title = get.Optional.Field "title" Decode.string
              PostRenderDemos = get.Optional.Field "postRenderDemos" PostRenderDemos.Decoder }
        )

type PageContext =
    { Path : string
      Attributes : PageAttributes
      TableOfContent : string
      Content : string }


type MenuState =
    | Expanded
    | Collapsed

    static member Decoder =
        Decode.string
        |> Decode.andThen(
            function
            | "expanded" -> Decode.succeed Expanded
            | "collapsed" -> Decode.succeed Collapsed
            | unkown ->
                sprintf "`%s` is an invalid value. Possible values are:\n-expanded\n- collapsed" unkown
                |> Decode.fail
        )

type Link =
    { Href : string
      Label : string option
      Icon : string option
      Color : string option
      IsExternal : bool }

    static member Decoder =
        Decode.object (fun get ->
            { Href = get.Required.Field "href" Decode.string
              Label = get.Optional.Field "label" Decode.string
              Icon = get.Optional.Field "icon" Decode.string
              Color = get.Optional.Field "color" Decode.string
              IsExternal = get.Optional.Field "isExternal" Decode.bool
                                  |> Option.defaultValue false }
        )

type NavbarConfig =
    { ShowVersion : bool
      Doc : string option
      Community : string option
      Links : Link list }

    static member Decoder =
        Decode.object (fun get ->
            { ShowVersion = get.Optional.Field "showVersion" Decode.bool
                        |> Option.defaultValue false
              Doc = get.Optional.Field "doc" Decode.string
              Community = get.Optional.Field "community" Decode.string
              Links = get.Optional.Field "links" (Decode.list Link.Decoder)
                        |> Option.defaultValue [] }
        )

type LightnerConfig =
    { BackgroundColor : string option
      TextColor : string option
      ThemeFile : string
      GrammarFiles : string list }

    static member Decoder =
        Decode.object (fun get ->
            { BackgroundColor = get.Optional.Field "backgroundColor" Decode.string
              TextColor = get.Optional.Field "textColor" Decode.string
              ThemeFile = get.Required.Field "themeFile" Decode.string
              GrammarFiles = get.Required.Field "grammars" (Decode.list Decode.string) }
        )

type Config =
    { NpmURL : string option
      GithubURL : string option
      Title : string
      Version : string
      Source : string
      Output : string
      IsDebug : bool
      Changelog : string option
      Navbar : NavbarConfig option
      MenuCollapsible : bool
      MenuDefault : MenuState
      LightnerConfig : LightnerConfig option }

    static member Decoder =
        Decode.object (fun get ->
            { NpmURL = get.Optional.Field "npmURL" Decode.string
              GithubURL = get.Optional.Field "githubURL" Decode.string
              Title = get.Required.Field "title" Decode.string
              Version = get.Required.Field "version" Decode.string
              Source = get.Optional.Field "source" Decode.string
                        |> Option.defaultValue "docsrc"
              Output = get.Optional.Field "output" Decode.string
                        |> Option.defaultValue "docs"
              IsDebug = get.Optional.Field "debug" Decode.bool
                        |> Option.defaultValue false
              Changelog = get.Optional.Field "changelog" Decode.string
              Navbar = get.Optional.Field "navbar" NavbarConfig.Decoder
              MenuCollapsible = get.Optional.Field "menuCollapsible" Decode.bool
                                |> Option.defaultValue false
              MenuDefault = get.Optional.Field "menuDefault" MenuState.Decoder
                            |> Option.defaultValue Collapsed
              LightnerConfig = get.Optional.Field "lightner" LightnerConfig.Decoder }
        )

type Model =
    { Config : Config
      FileWatcher : Chokidar.FSWatcher
      Server : Node.Http.Server
      WorkingDirectory : string
      IsDebug : bool
      JavaScriptFiles : Dictionary<string, string>
      DocFiles : Map<string, PageContext>
      LightnerCache : Map<string, CodeLightner.Config> }
