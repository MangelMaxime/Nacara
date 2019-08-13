module Types

open Thoth.Json
open System.Collections.Generic
open Fable.Import

type PostRenderDemos =
    { Script : string
      ImportSelector : string }

    static member Decoder =
        Decode.object (fun get ->
            { Script = get.Required.Field "script" Decode.string
              ImportSelector = get.Required.Field "importSelector" Decode.string }
        )

type PageAttributes =
    { Title : string
      PostRenderDemos : PostRenderDemos option
      Id : string option }

    static member Decoder =
        Decode.object (fun get ->
            { Title = get.Required.Field "title" Decode.string
              PostRenderDemos = get.Optional.Field "postRenderDemos" PostRenderDemos.Decoder
              Id = get.Optional.Field "id" Decode.string }
        )

type PageContext =
    { Path : string
      Attributes : PageAttributes
      TableOfContent : string
      Content : string }

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
    //   Community : string option
      Links : Link list }

    static member Decoder =
        Decode.object (fun get ->
            { ShowVersion = get.Optional.Field "showVersion" Decode.bool
                        |> Option.defaultValue false
              Doc = get.Optional.Field "doc" Decode.string
            //   Community = get.Optional.Field "community" Decode.string
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

type MenuState =
    | Static
    | Expanded
    | Collapsed

    static member Decoder =
        Decode.string
        |> Decode.andThen(
            function
            | "expanded" -> Decode.succeed Expanded
            | "collapsed" -> Decode.succeed Collapsed
            | "static" -> Decode.succeed Static
            | unkown ->
                sprintf "`%s` is an invalid value. Possible values are:\n-expanded\n- collapsed" unkown
                |> Decode.fail
        )

module Helpers =

    open Fable.Core

    [<Emit("Object.getPrototypeOf($0 || false) === Object.prototype")>]
    let isObject (_ : obj) : bool = jsNative

    let inline isArray (o: obj) : bool = JS.Array.isArray(o)

    let inline objectKeys (o: obj) : string seq = upcast JS.Object.keys(o)

open Fable.Core.JsInterop

type MenuItem =
    | MenuItem of string
    | MenuList of JS.Map<string, MenuItem list>

    static member Decoder =
        Decode.oneOf [
            Decode.string
            |> Decode.map MenuItem

            (fun path value ->
                if not (Helpers.isObject value) || Helpers.isArray value then
                    (path, Decode.BadPrimitive ("an object", value))
                    |> Error
                else
                    let keys = Helpers.objectKeys value

                    if Seq.length keys < 1 then
                        (path, Decode.BadPrimitive ("an object with 1 property", value))
                        |> Error
                    else
                        value
                        |> Helpers.objectKeys
                        |> Seq.map (fun key -> (key, value?(key) |> Decode.unwrap path (Decode.list MenuItem.Decoder)))
                        |> Seq.fold (fun (state : JS.Map<string, MenuItem list>) (key, value) ->
                            state.set(key, value)
                        ) (JS.Map.Create<string, MenuItem list>())
                        |> Ok )
            |> Decode.map MenuList
        ]

type MenuConfig = JS.Map<string, MenuItem list>

let menuConfigDecoder : Decode.Decoder<JS.Map<string, MenuItem list>> =
    fun path value ->
        if not (Helpers.isObject value) || Helpers.isArray value then
            (path, Decode.BadPrimitive ("an object", value))
            |> Error
        else
            value
            |> Helpers.objectKeys
            |> Seq.map (fun key -> (key, value?(key) |> Decode.unwrap path (Decode.list MenuItem.Decoder)))
            |> Seq.fold (fun (state : JS.Map<string, MenuItem list>) (key, value) ->
                state.set(key, value)
            ) (JS.Map.Create<string, MenuItem list>())
            |> Ok

type Config =
    { NpmURL : string option
      GithubURL : string option
      Url : string
      BaseUrl : string
      Title : string
      Version : string
      Source : string
      Output : string
      IsDebug : bool
      IsWatch : bool
      IsServer : bool
      ServerPort : int
      Changelog : string option
      Navbar : NavbarConfig option
      MenuConfig : MenuConfig option
      LightnerConfig : LightnerConfig option }

    static member Decoder =
        Decode.object (fun get ->
            let withDefault fieldName decoder defValue =
                get.Optional.Field fieldName decoder
                |> Option.defaultValue defValue
            let optionalFlag fieldName =
                withDefault fieldName Decode.bool false
            { NpmURL = get.Optional.Field "npmURL" Decode.string
              GithubURL = get.Optional.Field "githubURL" Decode.string
              Url = get.Required.Field "url" Decode.string
              BaseUrl = get.Required.Field "baseUrl" Decode.string
              Title = get.Required.Field "title" Decode.string
              Version = get.Required.Field "version" Decode.string
              Source = withDefault "source" Decode.string "docsrc"
              Output =  withDefault "output" Decode.string "docs"
              IsDebug = optionalFlag "debug"
              IsWatch = optionalFlag "watch"
              IsServer = optionalFlag "server"
              ServerPort = withDefault "serverPort" Decode.int 8080
              Changelog = get.Optional.Field "changelog" Decode.string
              Navbar = get.Optional.Field "navbar" NavbarConfig.Decoder
              MenuConfig = get.Optional.Field "menu" menuConfigDecoder
              LightnerConfig = get.Optional.Field "lightner" LightnerConfig.Decoder }
        )

type Model =
    { Config : Config
      FileWatcher : Chokidar.FSWatcher option
      Server : Node.Http.Server option
      WorkingDirectory : string
      IsDebug : bool
      JavaScriptFiles : Dictionary<string, string>
      DocFiles : Map<string, PageContext>
      LightnerCache : Map<string, CodeLightner.Config> }
