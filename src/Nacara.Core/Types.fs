/// Contains all the domain types used by Nacara
module Nacara.Core.Types

open Thoth.Json
open Fable.Core
open Fable.React
open Node

#nowarn "21"
#nowarn "40"

type FrontMatterAttributes =
    class end

/// <summary>
/// <para>Represents the context of a page within Nacara.</para>
/// <para>It contains all the information available for the specified page like it's title, layout, etc.</para>
/// </summary>
[<NoComparison>]
type PageContext =
    {
        /// <summary>
        /// PageId of the page
        ///
        /// <example>
        /// - <c>index</c> is the page id of <c>docs/index.md</c>
        /// - <c>nacara/introduction</c> is the page id of <c>docs/nacara/introduction.md</c>
        /// </example>
        /// </summary>
        PageId : string
        /// <summary>
        /// Relative path of the <strong>page</strong> from the source directory
        /// </summary>
        /// <example>
        /// If source directory is <c>docs</c>, then <c>index.md</c> is the relative path of <c>docs/index.md</c>
        /// </example>
        RelativePath : string
        /// Absolute path of the page
        FullPath : string
        /// <summary>
        /// Content of the page stripped of the front-matter section
        /// </summary>
        Content : string
        /// <summary>
        /// Name of the layout to use for the page set from the <c>layout</c> front-matter attribute
        /// </summary>
        Layout : string
        /// <summary>
        /// Name of the section where the page is located in.
        ///
        /// Note: If the page is at the root level section will be <c>""</c>
        ///
        /// <example>
        /// <code>
        /// docs
        /// ├── index.md
        /// └── documentation
        ///     └── page1.md
        /// </code>
        ///
        /// - <c>index.md</c> has the section <c>""</c>
        /// - <c>page1.md</c> has the section <c>"documentation"</c>
        /// </example>
        /// </summary>
        Section : string
        /// <summary>
        /// Optional title of the page set from the <c>title</c> front-matter attribute
        /// </summary>
        Title : string option
        /// <summary>
        /// Raw front-matter attributes of the page
        ///
        /// You can use Thoth.Json to decode it using <c>Decode.fromValue</c> function
        /// </summary>
        Attributes : FrontMatterAttributes
    }

type LabelLink =
    {
        /// <summary>
        /// Section related to this label link.
        ///
        /// It is used to link a page to this dropdown via the section information.
        ///
        /// <example>
        /// - Generate a navigation breadcrumb
        /// - Mark the link as active when on a related page
        /// </example>
        /// </summary>
        Section : string option
        /// <summary>
        /// Url to redirect to when clicking the label
        /// </summary>
        Url : string
        /// <summary>
        /// If <c>true</c>, the label will be kept in the navbar when on mobile view.
        ///
        /// If <c>false</c>, the label will be moved to the mobile menu.
        /// </summary>
        IsPinned : bool
        /// <summary>
        /// Text to display
        /// </summary>
        Label : string
    }

module LabelLink =

    /// LabelLink decoder
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

/// <summary>
/// Represents a link at the end of the Navbar
/// </summary>
type EndNavbarItem =
    {
        /// <summary>
        /// Url to redirect to when clicking the label
        /// </summary>
        Url : string
        /// <summary>
        /// Text to display
        /// </summary>
        Label : string
        /// <summary>
        /// Icon class to display on large screen
        ///
        /// For example, if you are using nacara-standard-layout,
        /// it comes with Font Awesome support so the value could be <c>fab fa-github</c>
        /// </summary>
        Icon : string
    }

module EndNavbarItem =

    let decoder : Decoder<EndNavbarItem> =
        Decode.object (fun get ->
            {
                Url = get.Required.Field "url" Decode.string
                Label = get.Required.Field "label" Decode.string
                Icon = get.Required.Field "icon" Decode.string
            }
        )

/// <summary>
/// Simple representation of a dropdown link it can be used to generate the dropdown body if no <c>Partial</c> is provided to the dropdown.
///
/// It is also used to render the dropdown inside the mobile menu.
/// </summary>
type DropdownLink =
    {
        /// <summary>
        /// Section related to this label link.
        /// It is used to link a page to this dropdown via the section information.
        ///
        /// <example>
        /// - Generate a navigation breadcrumb
        /// - Mark the link as active when on a related page
        /// </example>
        /// </summary>
        Section : string option
        /// <summary>
        /// Url to redirect to when clicking the label
        /// </summary>
        Url : string
        /// <summary>
        /// Text to display
        /// </summary>
        Label : string
        /// <summary>
        /// Optional description to display in the dropdown
        /// </summary>
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

/// Represents a dropdown item.
[<RequireQualifiedAccess>]
type DropdownItem =
    /// <summary>
    /// Represents a simple divider in the dropdown. This can helps structure your dropdown down content.
    /// </summary>
    | Divider
    /// <summary>
    /// Standard dropdown item
    /// </summary>
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

/// Describe a dropdown element inside of the Navbar
type DropdownInfo =
    {
        /// <summary>
        /// Text to display
        /// </summary>
        Label : string
        /// <summary>
        /// List of dropdown items used as the main content if <c>Partial</c> is not set
        /// or used for the mobile menu when the dropdown is not pinned.
        /// </summary>
        Items : DropdownItem list
        /// <summary>
        /// If <c>true</c>, the dropdown as is will be available even on small screens.
        ///
        /// If <c>false</c>, the dropdown will available via the mobile menu.
        /// </summary>
        IsPinned : bool
        /// <summary>
        /// If <c>true</c>, the dropdown will take the full width of the screen.
        /// </summary>
        IsFullWidth : bool
        /// <summary>
        /// Optional partial to use to render the dropdown, it takes precedence over the <c>Items</c> property.
        /// </summary>
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

/// <summary>
/// Represents a start navbar item.
///
/// The start navbar is displayed on both the mobile and desktop screen.
/// </summary>
[<RequireQualifiedAccess>]
type StartNavbarItem =
    /// <summary>
    /// Textual navbar item
    /// </summary>
    | LabelLink of LabelLink
    /// <summary>
    /// Dropdown navbar item
    /// </summary>
    | Dropdown of DropdownInfo

module StartNavbarItem =

    let decoder : Decoder<StartNavbarItem> =
        Decode.oneOf [
            LabelLink.decoder
            |> Decode.map StartNavbarItem.LabelLink


            DropdownInfo.decoder
            |> Decode.map StartNavbarItem.Dropdown
        ]

/// <summary>
/// All the information about the navbar configuration
/// </summary>
type NavbarConfig =
    {
        /// <summary>
        /// Start elements of the navbar
        ///
        /// This is the main section of the navbar.
        /// </summary>
        Start : StartNavbarItem list
        /// <summary>
        /// End elements of the navbar.
        ///
        /// On desktop screen they are shown using icons
        ///
        /// On mobile screen they are shown inside the mobile menu
        /// </summary>
        End : EndNavbarItem list
    }

module NavbarConfig =

    let decoder : Decoder<NavbarConfig> =
        Decode.object (fun get ->
            {
                Start = get.Optional.Field "start" (Decode.list StartNavbarItem.decoder)
                            |> Option.defaultValue []
                End = get.Optional.Field "end" (Decode.list EndNavbarItem.decoder)
                        |> Option.defaultValue []
            }
        )

    /// <summary>
    /// Default navbar configuration
    /// </summary>
    /// <returns></returns>
    let empty : NavbarConfig =
        {
            Start = []
            End = []
        }

/// <summary>
///
/// </summary>
type MenuItemPage =
    {
        /// <summary>
        ///
        /// </summary>
        Label : string option
        /// <summary>
        ///
        /// </summary>
        PageId : string
    }

/// <summary>
///
/// </summary>
type MenuItemLink =
    {
        /// <summary>
        ///
        /// </summary>
        Label : string
        /// <summary>
        ///
        /// </summary>
        Href : string
    }

/// <summary>
///
/// </summary>
type MenuItemList =
    {
        /// <summary>
        ///
        /// </summary>
        Label : string
        /// <summary>
        ///
        /// </summary>
        Items : MenuItem list
    }

/// <summary>
///
/// </summary>
and [<RequireQualifiedAccess>] MenuItem =
    /// <summary>
    ///
    /// </summary>
    | Page of MenuItemPage
    /// <summary>
    ///
    /// </summary>
    | List of MenuItemList
    /// <summary>
    ///
    /// </summary>
    | Link of MenuItemLink

/// <summary>
///
/// </summary>
type Menu = MenuItem list

/// <summary>
///
/// </summary>
[<RequireQualifiedAccess>]
type FlatMenu =
    /// <summary>
    ///
    /// </summary>
    | Link of MenuItemLink
    /// <summary>
    ///
    /// </summary>
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

/// <summary>
///
/// </summary>
type MenuConfig =
    {
        /// <summary>
        ///
        /// </summary>
        Section : string
        /// <summary>
        ///
        /// </summary>
        Items : Menu
    }

/// <summary>
///
/// </summary>
type SiteMetadata =
    {
        /// <summary>
        ///
        /// </summary>
        Title: string
        /// <summary>
        ///
        /// </summary>
        Url: string
        /// <summary>
        ///
        /// </summary>
        BaseUrl: string
        /// <summary>
        ///
        /// </summary>
        EditUrl: string option
        /// <summary>
        ///
        /// </summary>
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

/// <summary>
/// Configuration to load a remark plugin
/// </summary>
[<NoComparison>]
type RemarkPlugin =
    {
        /// <summary>
        /// Path to resolve the plugin, it can be
        ///
        /// - a relative path
        /// - an absolute path
        /// - a NPM package name
        /// </summary>
        Resolve : string
        /// <summary>
        /// If the property to access the plugin is not <c>default</c>, you can specify it here.
        /// </summary>
        Property : string option
        /// <summary>
        /// Options to pass to the plugin.
        /// </summary>
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

/// <summary>
/// Configuration to load a rehype plugin
/// </summary>
[<NoComparison>]
type RehypePlugin =
    {
        /// <summary>
        /// Path to resolve the plugin, it can be
        ///
        /// - a relative path
        /// - an absolute path
        /// - a NPM package name
        /// </summary>
        Resolve : string
        /// <summary>
        /// If the property to access the plugin is not <c>default</c>, you can specify it here.
        /// </summary>
        Property : string option
        /// <summary>
        /// Options to pass to the plugin.
        /// </summary>
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

/// <summary>
/// Record which contains all the config information about the website
/// </summary>
[<NoComparison>]
type Config =
    {
        /// <summary>
        /// The working directory where Nacara has been invoked from.
        ///
        /// All paths are relative to this directory.
        /// </summary>
        WorkingDirectory : string
        /// <summary>
        /// Name of the folder where the sources files are located.
        /// </summary>
        SourceFolder : string
        /// <summary>
        /// Name of the folder where the compiled files are written.
        /// </summary>
        Output : string
        /// <summary>
        /// Navbar configuration
        /// </summary>
        Navbar : NavbarConfig
        /// <summary>
        /// List of remark plugins to load when generating the pages.
        /// </summary>
        RemarkPlugins : RemarkPlugin array
        /// <summary>
        /// List of rehype plugins to load when generating the pages.
        /// </summary>
        RehypePlugins : RehypePlugin array
        /// <summary>
        /// List of the layouts package or script to load.
        /// </summary>
        Layouts : string array
        /// <summary>
        /// Port to use when serving the files locally.
        /// </summary>
        ServerPort : int
        /// <summary>
        /// <c>true</c> if nacara has been started in watch mode.
        /// </summary>
        IsWatch : bool
        /// <summary>
        /// Site metadata available
        /// </summary>
        SiteMetadata: SiteMetadata
    }

    /// <summary>
    /// Return the destination folder absolute path
    /// </summary>
    member this.DestinationFolder
        with get () =
            path.join(this.WorkingDirectory, this.Output)

/// <summary>
///
/// </summary>
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
        (config : Config)
        (remarkPlugins : RemarkPlugin array)
        (rehypePlugins : RehypePlugin array)
        (markdownText : string) : JS.Promise<string> =

        promise {
            let chain =
                unified()
                    ?``use``(remarkParse)

            // Apply the remark plugins
            for remarkPlugin in remarkPlugins do
                let! instance =
                    Interop.importDynamic config.WorkingDirectory remarkPlugin.Resolve

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
                let! instance =
                    Interop.importDynamic config.WorkingDirectory rehypePlugin.Resolve

                match rehypePlugin.Property with
                | Some property ->
                    chain?``use``(instance?``default``?(property), rehypePlugin.Options)

                | None ->
                    chain?``use``(instance?``default``, rehypePlugin.Options)

            chain
                ?``use``(rehypeFormat)
                ?``use``(rehypeStringify)

            if not config.IsWatch then
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
            this.Config
            remarkPlugins
            rehypePlugins
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

    let decoder (cwd : string) (_ : bool) : Decoder<Config> =
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
                IsWatch = false
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
