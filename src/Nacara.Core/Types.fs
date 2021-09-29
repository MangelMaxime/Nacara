module Nacara.Core.Types

open Thoth.Json
open Fable.Core
open Fable.React
open Node

#nowarn "21"
#nowarn "40"

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

type LabelLink =
    {
        Section : string option
        Url : string
        IsPinned : bool
        Label : string
    }

module LabelLink =

    let decoder : Decoder<LabelLink> =
            Decode.object (fun get ->
                {
                    Section = get.Optional.Field "section" Decode.string
                    Url = get.Required.Field "url" Decode.string
                    Label = get.Required.Field "label" Decode.string
                    IsPinned = get.Optional.Field "pinned" Decode.bool
                            |> Option.defaultValue false
                }
            )


type IconLink =
    {
        Url : string
        Label : string
        Icon : string
    }

module IconLink =

    let decoder : Decoder<IconLink> =
            Decode.object (fun get ->
                {
                    Url = get.Required.Field "url" Decode.string
                    Label = get.Required.Field "label" Decode.string
                    Icon = get.Required.Field "icon" Decode.string
                }
            )

type DropdownLink =
    {
        Section : string option
        Url : string
        Label : string
        Description : string option
    }

module DropdownLink =

    let decoder : Decoder<DropdownLink> =
        Decode.object (fun get ->
            {
                Section = get.Optional.Field "section" Decode.string
                Url = get.Required.Field "url" Decode.string
                Label = get.Required.Field "label" Decode.string
                Description = get.Optional.Field "description" Decode.string
            }
        )

[<RequireQualifiedAccess>]
type DropdownItem =
    | Divider
    | Link of DropdownLink

module DropdownItem =

    let decoder : Decoder<DropdownItem> =
        Decode.oneOf [
            Decode.string
            |> Decode.andThen (function
                | "divider" ->
                    Decode.succeed DropdownItem.Divider

                | invalid ->
                    Decode.fail $"`{invalid}` is not a valid DropdownItem value. Did you mean 'spacer'?"
            )

            DropdownLink.decoder
            |> Decode.map DropdownItem.Link
        ]

type DropdownInfo =
    {
        Label : string
        Items : DropdownItem list
        IsPinned : bool
        IsFullWidth : bool
        Partial : string option
    }

module DropdownInfo =

    let decoder : Decoder<DropdownInfo> =
        Decode.object (fun get ->
            {
                Label = get.Required.Field "label" Decode.string
                Items = get.Required.Field "items" (Decode.list DropdownItem.decoder)
                IsPinned = get.Optional.Field "pinned" Decode.bool
                            |> Option.defaultValue false
                IsFullWidth = get.Optional.Field "fullwidth" Decode.bool
                            |> Option.defaultValue false
                Partial = get.Optional.Field "partial" Decode.string
            }
        )

[<RequireQualifiedAccess>]
type StartNavbarItem =
    | LabelLink of LabelLink
    | Dropdown of DropdownInfo

module StartNavbarItem =

    let decoder : Decoder<StartNavbarItem> =
        Decode.oneOf [
            LabelLink.decoder
            |> Decode.map StartNavbarItem.LabelLink


            DropdownInfo.decoder
            |> Decode.map StartNavbarItem.Dropdown
        ]

type NavbarConfig =
    {
        Start : StartNavbarItem list
        End : IconLink list
    }

module NavbarConfig =

    let decoder : Decoder<NavbarConfig> =
        Decode.object (fun get ->
            {
                Start = get.Optional.Field "start" (Decode.list StartNavbarItem.decoder)
                            |> Option.defaultValue []
                End = get.Optional.Field "end" (Decode.list IconLink.decoder)
                        |> Option.defaultValue []
            }
        )

    let empty : NavbarConfig =
        {
            Start = []
            End = []
        }

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
            genericMsg msg value true + ("\nNode `" + fieldName + "` is unknown.")
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

let private unwrapWith (errors: ResizeArray<DecoderError>) path (decoder: Decoder<'T>) value: 'T =
    match decoder path value with
    | Ok v -> v
    | Error er -> errors.Add(er); Unchecked.defaultof<'T>

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

[<RequireQualifiedAccess>]
type FlatMenu =
    | Link of MenuItemLink
    | Page of MenuItemPage

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

                | "section" ->
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
                    Decode.fail $"`%s{invalidType}` is not a valid type for a menu Item. Supported types are:\n- page\n- section\n- link"
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

type SiteMetadata =
    {
        Title: string
        Url: string
        BaseUrl: string
        EditUrl: string option
        FavIcon: string option
    }

module SiteMetadata =

    let decoder : Decoder<SiteMetadata> =
        Decode.object (fun get ->
            {
                Title = get.Required.Field "title" Decode.string
                Url = get.Required.Field "url" Decode.string
                BaseUrl = get.Required.Field "baseUrl" Decode.string
                EditUrl = get.Optional.Field "editUrl" Decode.string
                FavIcon = get.Optional.Field "favIcon" Decode.string
            }
        )

[<NoComparison>]
type RemarkPlugin =
    {
        Resolve : string
        Property : string option
        Options : obj option
    }

module RemarkPlugin =

    let decoder : Decoder<RemarkPlugin> =
        Decode.object (fun get ->
            {
                Resolve = get.Required.Field "resolve" Decode.string
                Property = get.Optional.Field "property" Decode.string
                Options = get.Optional.Field "options" Decode.value
            }
        )

[<NoComparison>]
type RehypePlugin =
    {
        Resolve : string
        Property : string option
        Options : obj option
    }

module RehypePlugin =

    let decoder : Decoder<RehypePlugin> =
        Decode.object (fun get ->
            {
                Resolve = get.Required.Field "resolve" Decode.string
                Property = get.Optional.Field "property" Decode.string
                Options = get.Optional.Field "options" Decode.value
            }
        )

[<NoComparison>]
type Config =
    {
        WorkingDirectory : string
        SourceFolder : string
        Output : string
        Navbar : NavbarConfig
        RemarkPlugins : RemarkPlugin array
        RehypePlugins : RehypePlugin array
        Layouts : string array
        ServerPort : int
        IsWatch : bool
        SiteMetadata: SiteMetadata
    }

    member this.DestinationFolder
        with get () =
            path.join(this.WorkingDirectory, this.Output)

// Minimal binding
type MarkdownIt =
    abstract render : string -> string


[<NoComparison>]
type Partial =
    {
        Id : string
        Module : {| ``default`` : ReactElement |}
    }

module MarkdownToHtml =
    open Fable.Core.JsInterop

    let unified : unit -> obj =
        importMember "unified"

    let remarkParse : obj =
        import "default" "remark-parse"

    let remarkRehype : obj =
        import "default" "remark-rehype"

    let rehypeRaw : obj =
        import "default" "rehype-raw"

    let rehypeFormat : obj =
        import "default" "rehype-format"

    let rehypeStringify : obj =
        import "default" "rehype-stringify"

    let rehypePresetMinify : obj =
        import "default" "rehype-preset-minify"

    // For now, this function is using dynamic typing
    // But later it would be nice to have bindings for the different remark & rehype plugins
    // And rewrite this function with them
    let markdownToHtml
        (remarkPlugins : RemarkPlugin array)
        (rehypePlugins : RehypePlugin array)
        (isWatch : bool)
        (markdownText : string) : JS.Promise<string> =

        promise {
            let chain =
                unified()
                    ?``use``(remarkParse)

            // Apply the remark plugins
            for remarkPlugin in remarkPlugins do
                let! instance = importDynamic remarkPlugin.Resolve

                match remarkPlugin.Property with
                | Some property ->
                    chain?``use``(instance?``default``?(property), remarkPlugin.Options)

                | None ->
                    chain?``use``(instance?``default``, remarkPlugin.Options)

            // Convert from remark to rehype
            chain
                ?``use``(remarkRehype, {| allowDangerousHtml = true |})
                ?``use``(rehypeRaw)

            // Apply the rehype plugins
            for rehypePlugin in rehypePlugins do
                let! instance = importDynamic rehypePlugin.Resolve

                match rehypePlugin.Property with
                | Some property ->
                    chain?``use``(instance?``default``?(property), rehypePlugin.Options)

                | None ->
                    chain?``use``(instance?``default``, rehypePlugin.Options)

            chain
                ?``use``(rehypeFormat)
                ?``use``(rehypeStringify)

            if not isWatch then
                chain?``use``(rehypePresetMinify)

            return chain?``process``(markdownText)
        }

[<NoComparison; NoEquality; AttachMembers>]
// AttachMembers is important so the MarkdownToHtml method is available from JavaScript
type RendererContext =
    {
        Config : Config
        SectionMenu : Menu option
        Partials : Partial array
        Menus : MenuConfig array
        Pages : PageContext array

    }

    // MarkdownToHtml is a method so layout can add additional plugins if needed
    // See: Nacara.Layout.Standard/Page.Standard.fs render function
    member this.MarkdownToHtml
        (
            markdownText: string,
            ?remarkPlugins : RemarkPlugin array,
            ?rehypePlugins : RehypePlugin array
        ) =

        let remarkPlugins =
            remarkPlugins
            |> Option.map (Array.append this.Config.RemarkPlugins)
            |> Option.defaultValue this.Config.RemarkPlugins

        let rehypePlugins =
            rehypePlugins
            |> Option.map (Array.append this.Config.RehypePlugins)
            |> Option.defaultValue this.Config.RehypePlugins

        MarkdownToHtml.markdownToHtml
            remarkPlugins
            rehypePlugins
            this.Config.IsWatch
            markdownText

type LayoutDependency =
    {
        Source : string
        Destination : string
    }

type LayoutRenderFunc = RendererContext -> PageContext -> JS.Promise<ReactElement>

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

    let decoder (cwd : string) (isWatch : bool) : Decoder<Config> =
        Decode.object (fun get ->
            {
                WorkingDirectory = cwd
                SourceFolder = get.Optional.Field "source" Decode.string
                                |> Option.defaultValue "docs"
                Output = get.Optional.Field "output" Decode.string
                            |> Option.defaultValue "docs_deploy"
                Navbar = get.Optional.Field "navbar" NavbarConfig.decoder
                            |> Option.defaultValue NavbarConfig.empty
                RemarkPlugins = get.Optional.Field "remarkPlugins" (Decode.array RemarkPlugin.decoder)
                            |> Option.defaultValue [||]
                RehypePlugins = get.Optional.Field "rehypePlugins" (Decode.array RehypePlugin.decoder)
                            |> Option.defaultValue [||]
                Layouts = get.Required.Field "layouts" (Decode.array Decode.string)
                ServerPort = get.Optional.Field "serverPort" Decode.int
                                |> Option.defaultValue 8080
                IsWatch = isWatch
                SiteMetadata = get.Required.Field "siteMetadata" SiteMetadata.decoder
            }
        )

[<RequireQualifiedAccess; NoComparison>]
type QueueFile =
    | Markdown of PageContext
    | Sass of filePath : string
    | JavaScript of filePath : string
    | LayoutDependency of LayoutDependency
    | Other of filePath : string
