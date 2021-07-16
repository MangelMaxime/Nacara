module Types

open Thoth.Json
open Fable.Core
open Fable.React
open Node

#nowarn "21"
#nowarn "40"


//// [<NoComparison>]
//// type PageAttributes =
////     {
////         Title : string
////         Layout : string
////         ExcludeFromNavigation : bool
////         Menu : bool
////         ShowTitle : bool
////         ShowEditButton : bool
////         ShowToc : bool
////         Id : string option
////         Extra : JsonValue option
////     }
//
////     static member Decoder =
////         Decode.object (fun get ->
////             {
////                 Title = get.Required.Field "title" Decode.string
////                 ExcludeFromNavigation = get.Optional.Field "excludeFromNavigation" Decode.bool
////                                             |> Option.defaultValue false
////                 Layout = get.Optional.Field "layout" Decode.string
////                             |> Option.defaultValue "default"
////                 Menu = get.Optional.Field "menu" Decode.bool
////                             |> Option.defaultValue true
////                 ShowTitle = get.Optional.Field "showTitle" Decode.bool
////                             |> Option.defaultValue true
////                 ShowEditButton = get.Optional.Field "showEditButton" Decode.bool
////                             |> Option.defaultValue true
////                 ShowToc = get.Optional.Field "showToc" Decode.bool
////                             |> Option.defaultValue true
////                 Id = get.Optional.Field "id" Decode.string
////                 Extra = get.Optional.Field "extra" Decode.value
////             }
////         )

type FrontMatterAttributes =
    class end

[<NoComparison>]
type PageContext =
    {
        PageId : string
        RelativePath : string
        FullPath : string
        Content : string
        Layout : string
        Section : string
        Title : string option
        Attributes : FrontMatterAttributes
    }

//type RawLink =
//    {
//        Category : string option
//        Href : string
//        Label : string option
//        Icon : string option
//        Color : string option
//        IsExternal : bool
//    }
//
//    static member Decoder =
//        Decode.object (fun get ->
//            {
//                Category = get.Optional.Field "category" Decode.string
//                Href = get.Required.Field "href" Decode.string
//                Label = get.Optional.Field "label" Decode.string
//                Icon = get.Optional.Field "icon" Decode.string
//                Color = get.Optional.Field "color" Decode.string
//                IsExternal = get.Optional.Field "isExternal" Decode.bool
//                                  |> Option.defaultValue false
//            }
//        )
//
//type LinkIconOnly =
//    {
//        Category : string option
//        Href : string
//        Icon : string
//        Color : string option
//        IsExternal : bool
//    }
//
//type LinkTextOnly =
//    {
//        Category : string option
//        Href : string
//        Label : string
//        Color : string option
//        IsExternal : bool
//    }
//
//type LinkIconAndLabel =
//    {
//        Category : string option
//        Href : string
//        Icon : string
//        Label : string
//        Color : string option
//        IsExternal : bool
//    }
//
//type LinkType =
//    | IconOnly of LinkIconOnly
//    | TextOnly of LinkTextOnly
//    | IconAndText of LinkIconAndLabel
//
//    static member Decoder =
//        Decode.andThen (fun (link : RawLink) ->
//            match link.Label, link.Icon with
//            | Some label, None ->
//                {
//                    Category = link.Category
//                    Href = link.Href
//                    Label = label
//                    Color = link.Color
//                    IsExternal = link.IsExternal
//                }
//                |> TextOnly
//                |> Decode.succeed
//
//            | None, Some icon ->
//                {
//                    Category = link.Category
//                    Href = link.Href
//                    Icon = icon
//                    Color = link.Color
//                    IsExternal = link.IsExternal
//                }
//                |> IconOnly
//                |> Decode.succeed
//
//            | Some label, Some icon ->
//                {
//                    Category = link.Category
//                    Href = link.Href
//                    Icon = icon
//                    Label = label
//                    Color = link.Color
//                    IsExternal = link.IsExternal
//                }
//                |> IconAndText
//                |> Decode.succeed
//
//            | None, None ->
//                $"Invalid navbar link found. Please check that link `%s{link.Href}` has at least Label and/or Icon set"
//                |> Decode.fail
//        )

type NavbarLink =
    {
        Section : string option
        Url : string
        Label : string option
        Icon : string option
        IconColor : string option
    }

module NavbarLink =

    let decoder : Decoder<NavbarLink> =
            Decode.object (fun get ->
                {
                    Section = get.Optional.Field "section" Decode.string
                    Url = get.Required.Field "url" Decode.string
                    Label = get.Optional.Field "label" Decode.string
                    Icon = get.Optional.Field "icon" Decode.string
                    IconColor = get.Optional.Field "iconColor" Decode.string
                }
            )

type NavbarConfig =
    {
        Start : NavbarLink list
        End : NavbarLink list
    }

module NavbarConfig =

    let decoder : Decoder<NavbarConfig> =
        Decode.object (fun get ->
            {
                Start = get.Optional.Field "start" (Decode.list NavbarLink.decoder)
                            |> Option.defaultValue []
                End = get.Optional.Field "end" (Decode.list NavbarLink.decoder)
                        |> Option.defaultValue []
            }
        )

type LightnerConfig =
    {
        BackgroundColor : string option
        TextColor : string option
        ThemeFile : string
        GrammarFiles : string list
    }

module LightnerConfig =

    let decoder : Decoder<LightnerConfig> =
        Decode.object (fun get ->
            {
                BackgroundColor = get.Optional.Field "backgroundColor" Decode.string
                TextColor = get.Optional.Field "textColor" Decode.string
                ThemeFile = get.Required.Field "themeFile" Decode.string
                GrammarFiles = get.Required.Field "grammars" (Decode.list Decode.string)
            }
        )
//
//// Copied from Thoth.Json in next version of Thoth.Json they should be accessible
//module Helpers =
//
//    open Fable.Core
//
//    [<Emit("typeof $0")>]
//    let jsTypeof (_ : JsonValue) : string = jsNative
//
//    [<Emit("$0 instanceof SyntaxError")>]
//    let isSyntaxError (_ : JsonValue) : bool = jsNative
//
//    let inline getField (fieldName: string) (o: JsonValue) = o?(fieldName)
//    let inline isString (o: JsonValue) : bool = o :? string
//
//    let inline isBoolean (o: JsonValue) : bool = o :? bool
//
//    let inline isNumber (o: JsonValue) : bool = jsTypeof o = "number"
//
//    let inline isArray (o: JsonValue) : bool = JS.Constructors.Array.isArray(o)
//
//    [<Emit("$0 === null ? false : (Object.getPrototypeOf($0 || false) === Object.prototype)")>]
//    let isObject (_ : JsonValue) : bool = jsNative
//
//    let inline isNaN (o: JsonValue) : bool = JS.Constructors.Number.isNaN(!!o)
//
//    let inline isNullValue (o: JsonValue): bool = isNull o
//
//    [<Emit("-2147483648 < $0 && $0 < 2147483647 && ($0 | 0) === $0")>]
//    let isValidIntRange (_: JsonValue) : bool = jsNative
//
//    [<Emit("isFinite($0) && !($0 % 1)")>]
//    let isIntFinite (_: JsonValue) : bool = jsNative
//
//    let isUndefined (o: JsonValue): bool = jsTypeof o = "undefined"
//
//    [<Emit("JSON.stringify($0, null, 4) + ''")>]
//    let anyToString (_: JsonValue) : string = jsNative
//
//    let inline isFunction (o: JsonValue) : bool = jsTypeof o = "function"
//
//    let inline objectKeys (o: JsonValue) : string seq = upcast JS.Constructors.Object.keys(o)
//    let inline asBool (o: JsonValue): bool = unbox o
//    let inline asInt (o: JsonValue): int = unbox o
//    let inline asFloat (o: JsonValue): float = unbox o
//    let inline asString (o: JsonValue): string = unbox o
//    let inline asArray (o: JsonValue): JsonValue[] = unbox o
//
let private genericMsg msg value newLine =
    try
        "Expecting "
            + msg
            + " but instead got:"
            + (if newLine then "\n" else " ")
            + (Decode.Helpers.anyToString value)
    with
        | _ ->
            "Expecting "
            + msg
            + " but decoder failed. Couldn't report given value due to circular structure."
            + (if newLine then "\n" else " ")

let private errorToString (path : string, error) =
    let reason =
        match error with
        | BadPrimitive (msg, value) ->
            genericMsg msg value false
        | BadType (msg, value) ->
            genericMsg msg value true
        | BadPrimitiveExtra (msg, value, reason) ->
            genericMsg msg value false + "\nReason: " + reason
        | BadField (msg, value) ->
            genericMsg msg value true
        | BadPath (msg, value, fieldName) ->
            genericMsg msg value true + ("\nNode `" + fieldName + "` is unkown.")
        | TooSmallArray (msg, value) ->
            "Expecting " + msg + ".\n" + (Decode.Helpers.anyToString value)
        | BadOneOf messages ->
            "The following errors were found:\n\n" + String.concat "\n\n" messages
        | FailMessage msg ->
            "The following `failure` occurred with the decoder: " + msg

    match error with
    | BadOneOf _ ->
        // Don't need to show the path here because each error case will show it's own path
        reason
    | _ ->
        "Error at: `" + path + "`\n" + reason

//module Decode =
//    let forward<'T> =
//        // DecodeLayout are function, for now just unbox them
//        // Later we can, check that they indeed are functions and perhaps their number of arguments
//        fun _ v ->
//            Ok (unbox<'T> v)
//
let private unwrapWith (errors: ResizeArray<DecoderError>) path (decoder: Decoder<'T>) value: 'T =
    match decoder path value with
    | Ok v -> v
    | Error er -> errors.Add(er); Unchecked.defaultof<'T>
//
//
//type MenuItem =
//    | MenuItem of string
//    | MenuList of string * MenuItem [] //JS.Map<string, MenuItem list>
//
//    static member DecodeObject : Decoder<MenuItem list> =
//        fun (path : string) (value : obj) ->
//            let mutable errors = ResizeArray<DecoderError>()
//
//            let keys = Helpers.objectKeys value
//            let values =
//                [
//                    for key in keys do
//                        let currentValue = value?(key)
//                        let path = path + "." + key
//                        if Helpers.isArray currentValue then
//                            let items = Helpers.asArray currentValue
//                            let x =
//                                [
//                                    for item in items do
//                                        yield! unwrapWith errors path MenuItem.Decoder item
//                                ]
//                                |> List.toArray
//                            yield MenuList (key, x)
//                        else
//                            let error = (path, BadPrimitive ("an array", value))
//                            errors.Add(error)
//                ]
//
//            match Seq.toList errors with
//            | [ ] ->
//                Ok values
//            | fst::_ as errors ->
//                if errors.Length = 1 then
//                    Error fst
//                else
//                    let errors =
//                        List.map errorToString errors
//
//                    Error (path, BadOneOf errors)
//
//    static member Decoder : Decoder<MenuItem list> =
//        Decode.oneOf [
//            Decode.string
//            |> Decode.map (fun pageId ->
//                [ MenuItem pageId ]
//            )
//
//            fun (path : string) (value : JsonValue) ->
//                if Helpers.isObject value then
//                    MenuItem.DecodeObject path value
//                else
//                    (path, BadPrimitive ("an object", value))
//                    |> Error
//    ]
//
//
//type MenuConfig = (string * MenuItem []) list
//
//let menuConfigDecoder : Decoder<MenuConfig> =
//    fun (path : string) (value : obj) ->
//        let mutable errors = ResizeArray<DecoderError>()
//
//        let keys = Helpers.objectKeys value
//        let values =
//            [
//                for key in keys do
//                    let currentValue = value?(key)
//                    let path = path + "." + key
//                    if Helpers.isArray currentValue then
//                        let items = Helpers.asArray currentValue
//                        let x =
//                            [
//                                for item in items do
//                                    yield! unwrapWith errors path MenuItem.Decoder item
//                            ]
//                            |> List.toArray
//                        yield (key, x)
//                    else
//                        let error = (path, BadPrimitive ("an array", value))
//                        errors.Add(error)
//            ]
//
//        match Seq.toList errors with
//        | [ ] ->
//            Ok values
//        | fst::_ as errors ->
//            if errors.Length = 1 then
//                Error fst
//            else
//                let errors =
//                    List.map errorToString errors
//
//                Error (path, BadOneOf errors)
//
//
//[<NoComparison>]
//type MarkdownPlugin =
//    {
//        Path : string
//        Args : JsonValue array
//    }
//
//    static member Decoder =
//        Decode.object (fun get ->
//            {
//                Path = get.Required.Field "path" Decode.string
//                Args = get.Optional.Field "args" (Decode.array Decode.value)
//                        |> Option.defaultValue [||]
//            }
//        )
//
//[<NoComparison>]
//type PluginsConfig =
//    {
//        Markdown : MarkdownPlugin array
//    }
//
//    static member Decoder =
//        Decode.object (fun get ->
//            {
//                Markdown = get.Optional.Field "markdown" (Decode.array MarkdownPlugin.Decoder)
//                                |> Option.defaultValue [||]
//            }
//        )
//
//    static member Empty =
//        {
//            Markdown = [||]
//        }
//
//type LayoutFunc = System.Func<Model, PageContext, JS.Promise<ReactElement>>
//
//[<NoComparison; NoEquality>]
//type Config =
//    {
//        NpmURL : string option
//        GithubURL : string option
//        Url : string
//        BaseUrl : string
//        Title : string
//        Version : string
//        Source : string
//        EditUrl : string option
//        Output : string
//        IsDebug : bool
//        Changelog : string option
//        Navbar : NavbarConfig option
//        MenuConfig : MenuConfig option
//        LightnerConfig : LightnerConfig option
//        LayoutConfig : Map<string, LayoutFunc>
//        Plugins : PluginsConfig
//        IsWatch : bool
//        ServerPort : int
//    }
//
//    static member Decoder =
//        Decode.object (fun get ->
//            {
//                NpmURL = get.Optional.Field "npmURL" Decode.string
//                GithubURL = get.Optional.Field "githubURL" Decode.string
//                Url = get.Required.Field "url" Decode.string
//                BaseUrl = get.Required.Field "baseUrl" Decode.string
//                Title = get.Required.Field "title" Decode.string
//                Version = get.Required.Field "version" Decode.string
//                Source = get.Optional.Field "source" Decode.string
//                            |> Option.defaultValue "docsrc"
//                EditUrl = get.Optional.Field "editUrl" Decode.string
//                Output = get.Optional.Field "output" Decode.string
//                            |> Option.defaultValue "docs"
//                IsDebug = get.Optional.Field "debug" Decode.bool
//                            |> Option.defaultValue false
//                Changelog = get.Optional.Field "changelog" Decode.string
//                Navbar = get.Optional.Field "navbar" NavbarConfig.Decoder
//                MenuConfig = get.Optional.Field "menu" menuConfigDecoder
//                LightnerConfig = get.Optional.Field "lightner" LightnerConfig.Decoder
//                LayoutConfig = get.Required.Field "layouts" (Decode.dict Decode.forward)
//                Plugins = get.Optional.Field "plugins" PluginsConfig.Decoder
//                            |> Option.defaultValue PluginsConfig.Empty
//                IsWatch = false
//                ServerPort = get.Optional.Field "serverPort" Decode.int
//                                |> Option.defaultValue 8080
//            }
//        )
//
//type Category = string
//type PageId = string
//
//[<NoComparison; NoEquality>]
//type Model =
//    {
//        Config : Config
//        ProcessQueue : string list
//        FileWatcher : Chokidar.FSWatcher option
//        Server : Node.Http.Server option
//        WorkingDirectory : string
//        IsDebug : bool
//        // We use a JS.Map instead of an F# Map because of a bug when upgrading to Fable 3
//        // I suspect that Fable 3, generate some reflection information differently when the file the file is being use in 2 differents project
//        // and so the comparer function or something is broken
//        // We need to investigate more later if we want to go back using an immutable map
//        DocFiles : JS.Map<Category, JS.Map<PageId, PageContext>>
//        LightnerCache : JS.Map<string, CodeLightner.Config>
//    }


type MenuItemPage =
    {
        Label : string option
        PageId : string
    }

type MenuItemLink =
    {
        Label : string
        Href : string
    }

type MenuItemList =
    {
        Label : string
        Items : MenuItem list
    }

and [<RequireQualifiedAccess>] MenuItem =
    | Page of MenuItemPage
    | List of MenuItemList
    | Link of MenuItemLink

type Menu = MenuItem list

module MenuItem =

    let rec decoder : Decoder<MenuItem> =
        Decode.oneOf [
            Decode.string
            |> Decode.map (fun pageId ->
                {
                    Label = None
                    PageId = pageId
                }
                |> MenuItem.Page
            )

            Decode.field "type" Decode.string
            |> Decode.andThen (function
                | "page" ->
                    Decode.object (fun get ->
                        {
                            Label = get.Optional.Field "label" Decode.string
                            PageId = get.Required.Field "pageId" Decode.string
                        }
                    )
                    |> Decode.map MenuItem.Page

                | "category" ->
                    Decode.object (fun get ->
                        {
                            Label = get.Required.Field "label" Decode.string
                            Items = get.Required.Field "items" (Decode.list decoder)
                        }
                    )
                    |> Decode.map MenuItem.List

                | "link" ->
                    Decode.object (fun get ->
                        {
                            Label = get.Required.Field "label" Decode.string
                            Href = get.Required.Field "href" Decode.string
                        }
                    )
                    |> Decode.map MenuItem.Link

                | invalidType ->
                    Decode.fail $"`%s{invalidType}` is not a valid type for a menu Item"
            )
        ]

module Menu =

    let decoder : Decoder<Menu> =
        Decode.list MenuItem.decoder

type MenuConfig =
    {
        Section : string
        Items : Menu
    }

type Config =
    {
        WorkingDirectory : string
        GithubURL : string option
        Url : string
        SourceFolder : string
        BaseUrl : string
        Title : string
        EditUrl : string option
        Output : string
        IsVerbose : bool
        Changelog : string option
        Navbar : NavbarConfig option
//        MenuConfig : MenuConfig option
        LightnerConfig : LightnerConfig option
        Layouts : string array
//        Plugins : PluginsConfig
        ServerPort : int
    }

    member this.DestinationFolder
        with get () =
            path.join(this.WorkingDirectory, this.Output)

// Minimal binding
type MarkdownIt =
    abstract render : string -> string


[<NoComparison; NoEquality>]
type RendererContext =
    {
        Config : Config
        SectionMenu : Menu option
        Menus : MenuConfig array
        Pages : PageContext array
        MarkdownToHtml : string -> JS.Promise<string>
        MarkdownToHtmlWithPlugins : (MarkdownIt -> MarkdownIt) -> string -> JS.Promise<string>
    }

type LayoutDependency =
    {
        Source : string
        Destination : string
    }

type LayoutRenderFunc = RendererContext -> PageContext -> JS.Promise<ReactElement>
    //System.Func<

    //>

[<NoComparison; NoEquality>]
type LayoutRenderer =
    {
        Name : string
        Func :  LayoutRenderFunc
    }

[<NoComparison; NoEquality>]
type LayoutInfo =
    {
        Dependencies : LayoutDependency array
        Renderers : LayoutRenderer array
    }

type LayoutInterface =
    abstract ``default`` : LayoutInfo with get

module Config =

    let decoder (cwd : string) : Decoder<Config> =
        Decode.object (fun get ->
            {
                WorkingDirectory = cwd
                GithubURL = get.Optional.Field "githubURL" Decode.string
                Url = get.Required.Field "url" Decode.string
                BaseUrl = get.Required.Field "baseUrl" Decode.string
                Title = get.Required.Field "title" Decode.string
                SourceFolder = get.Optional.Field "source" Decode.string
                                |> Option.defaultValue "docsrc"
                EditUrl = get.Optional.Field "editUrl" Decode.string
                Output = get.Optional.Field "output" Decode.string
                            |> Option.defaultValue "docs"
                IsVerbose = false
                Changelog = get.Optional.Field "changelog" Decode.string
                Navbar = get.Optional.Field "navbar" NavbarConfig.decoder
//                MenuConfig = get.Optional.Field "menu" menuConfigDecoder
                LightnerConfig = get.Optional.Field "lightner" LightnerConfig.decoder
                Layouts = get.Required.Field "layouts" (Decode.array Decode.string)
//                Plugins = get.Optional.Field "plugins" PluginsConfig.Decoder
//                            |> Option.defaultValue PluginsConfig.Empty
                ServerPort = get.Optional.Field "serverPort" Decode.int
                                |> Option.defaultValue 8080
            }
        )
