/// Contains all the domain types used by Nacara
module Nacara.Core.Types

open Thoth.Json
open Fable.Core
open Fable.React
open Node

#nowarn "21"
#nowarn "40"

/// <summary>
/// Type representing front-matter attributes
///
/// If you need to access information from it consider it being an <c>obj</c>.
///
/// So you can either use dynamic typing or decode the object using Thoth.Json
/// </summary>
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

/// <summary>
/// Represents a simple link in the start section of the navbar
/// </summary>
[<NoComparison>]
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
    /// Represents a simple divider in the dropdown. This can helps structure your dropdown content.
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
[<RequireQualifiedAccess; NoComparison>]
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
[<NoComparison>]
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
/// Information specific to a menu item representing an internal page
/// </summary>
type MenuItemPage =
    {
        /// <summary>
        /// Text to display in the menu.
        ///
        /// If not set, the title of the page will be used.
        /// </summary>
        Label : string option
        /// <summary>
        /// PageId of the page to link to.
        /// </summary>
        PageId : string
    }

/// <summary>
/// Information specific to menu item representing a link
/// </summary>
type MenuItemLink =
    {
        /// <summary>
        /// Text to display in the menu.
        /// </summary>
        Label : string
        /// <summary>
        /// Url to redirect to.
        /// </summary>
        Href : string
    }

/// <summary>
/// Information specific to list of menu item, used to catagegorize the menu items.
/// </summary>
type MenuItemList =
    {
        /// <summary>
        /// Label to display on top of the list
        /// </summary>
        Label : string
        /// <summary>
        /// List of items contained in this list
        /// </summary>
        Items : MenuItem list
    }

/// <summary>
/// All the different representation of a menu item
/// </summary>
and [<RequireQualifiedAccess>] MenuItem =
    /// <summary>
    /// Menu item representing an internal page
    /// </summary>
    | Page of MenuItemPage
    /// <summary>
    /// Menu item representing a list of menu item
    /// </summary>
    | List of MenuItemList
    /// <summary>
    /// Menu item representing a link
    /// </summary>
    | Link of MenuItemLink

/// <summary>
/// A menu, consist of a list of <c>MenuItem</c>
/// </summary>
type Menu = MenuItem list

/// <summary>
/// Flatten representation of a menu, used when searching the previous or next menu item.
/// </summary>
[<RequireQualifiedAccess>]
type FlatMenu =
    /// <summary>
    /// Menu item representing a link
    /// </summary>
    | Link of MenuItemLink
    /// <summary>
    /// Menu item representing an internal page
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
/// Menu configuration associated to a section of the website
/// </summary>
type MenuConfig =
    {
        /// <summary>
        /// Name of the section associated to the menu
        ///
        /// Each folder under the source solder represents a section.
        /// <example>
        /// The following directory structure contains 1 section named <c>documentation</c>
        ///
        /// <code>
        /// docs
        /// └── documentation
        ///     └── page1.md
        /// </code>
        /// </example>
        /// </summary>
        Section : string
        /// <summary>
        /// Menu information associated to the section
        /// </summary>
        Items : Menu
    }

/// <summary>
/// All metadata associated to the website
/// </summary>
type SiteMetadata =
    {
        /// <summary>
        /// Title of the website
        /// </summary>
        Title: string
        /// <summary>
        /// URL for your website. This is the domain part of your URL.
        ///
        /// For example, <c>https://mangelmaxime.github.io</c> is the URL of <c>https://mangelmaxime.github.io/Nacara/</c>
        /// </summary>
        Url: string
        /// <summary>
        /// Base URL for your site. This is the path after the domain.
        ///
        /// For example, <c>/Nacara/</c> is the baseUrl of <c>https://mangelmaxime.github.io/Nacara/</c>.
        ///
        /// For URLs that have no path, the baseUrl should be set to <c>/</c>.
        /// </summary>
        BaseUrl: string
        /// <summary>
        /// URL for editing your documentation.
        /// </summary>
        EditUrl: string option
        /// <summary>
        /// Path to your site favIcon
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
/// Represents a partial script
///
/// A partial is a script that is invoked at generation time, it is used to
/// offer more customization to some elements of the website.
///
/// For example, some layout can detect if you add a <c>footer</c> partial and include it
/// on all your pages.
///
/// It can also be used to write custom downpdown content using full HTML instead of
/// the simple and predefined template.
/// </summary>
[<NoComparison>]
type Partial =
    {
        /// <summary>
        /// Unique name of the partial, it is generated from the partial file name.
        ///
        /// <example>
        /// With the following directory structure,
        ///
        /// <code>
        /// docs
        /// ├── index.md
        /// └── _partials
        ///     ├── dropdown
        ///         └── api.jsx
        ///     └── footer.jsx
        /// </code>
        ///
        /// There are 2 partials:
        /// - <c>dropdown/api</c>
        /// - <c>footer</c>
        ///
        /// </example>
        /// </summary>
        Id : string
        /// <summary>
        /// Module instance dynamically loaded of the partial.
        ///
        /// When inserting the partial Nacara will use the <c>default</c> exposed property
        /// </summary>
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

    /// <summary>
    /// Transform markdown content to HTML using the remark and rehype plugins provided
    /// </summary>
    /// <param name="config">Nacara configuration</param>
    /// <param name="remarkPlugins">List of remark plugins to use for the transformation</param>
    /// <param name="rehypePlugins">List of rehype plugins to use for the transformation</param>
    /// <param name="relativePath">Relative path of the markdown file</param>
    /// <param name="markdownText">Text to transform</param>
    /// <returns>HTML text</returns>
    let markdownToHtml
        (config : Config)
        (remarkPlugins : RemarkPlugin array)
        (rehypePlugins : RehypePlugin array)
        (relativePath : string)
        (markdownText : string) : JS.Promise<string> =

        let absolutePath =
            path.join(
                config.WorkingDirectory,
                config.SourceFolder,
                relativePath
            )

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

            return chain?``process``(
                {|
                    value = markdownText
                    path = absolutePath
                |}
            )
        }

/// <summary>
/// A context accessible when rendering a page.
///
/// It contains all the information about the application state like user configuration,
/// all the pages, partials, etc.
/// </summary>

[<NoComparison; NoEquality; AttachMembers>]
// AttachMembers is important so the MarkdownToHtml method is available from JavaScript
type RendererContext =
    {
        /// <summary>
        /// Config object coming from the <c>nacara.config.json</c> file
        /// </summary>
        Config : Config
        /// <summary>
        /// <c>Some X</c> if the page we are rendering is in a section with a menu.
        ///
        /// <c>None</c> Otherwise
        /// </summary>
        SectionMenu : Menu option
        /// <summary>
        /// All the partials available.
        ///
        /// If your layout support <c>footer</c> partial, then you can try to look
        /// for it here. If found, you can use it or emit a warning/error if missing.
        /// </summary>
        Partials : Partial array
        /// <summary>
        /// All the section menu available
        /// </summary>
        Menus : MenuConfig array
        /// <summary>
        /// All the pages available
        ///
        /// Having access, to all the page like that makes it really easy to generate
        /// index pages. For example, if you are generating a blog, you can want to generate
        /// and blog index page. To do that, you can search all the pages with a specific
        /// layout and access their title, summary, link, from it.
        /// </summary>
        Pages : PageContext array
    }

    // MarkdownToHtml is a method so layout can add additional plugins if needed
    // See: Nacara.Layout.Standard/Page.Standard.fs render function

    /// <summary>
    /// Transform the given markdown text to HTML using the the provided plugins.
    ///
    /// This will method will include by default the plugins configured via
    /// the configuration file. But if when writing a custom layout, you need a specific plugin,
    /// you can add it via the corresponding argument.
    /// </summary>
    /// <param name="markdownText">Text to transform</param>
    /// <param name="relativePath">Relative path to the markdown file. Generally it comes from the page context</param>
    /// <param name="remarkPlugins">Layout specific remark plugins</param>
    /// <param name="rehypePlugins">Layout specific rehype plugins</param>
    /// <returns></returns>
    member this.MarkdownToHtml
        (
            markdownText: string,
            relativePath: string,
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
            relativePath
            markdownText


/// <summary>
/// Representation a layout depdency
///
/// A layout depdencency is a file that will be copied from the <c>Source</c> file
/// to the <c>Destination</c> file inside of the output folder.
///
/// This is useful, when you want to include a JavaScript file to your layout
/// </summary>
type LayoutDependency =
    {
        /// <summary>
        ///
        /// </summary>
        Source : string
        /// <summary>
        ///
        /// </summary>
        Destination : string
    }

/// <summary>
/// Alias representing the function which generate a page
/// </summary>
type LayoutRenderFunc = RendererContext -> PageContext -> JS.Promise<ReactElement>

/// <summary>
/// Representation of a layout renderer
/// </summary>
[<NoComparison; NoEquality>]
type LayoutRenderer =
    {
        /// <summary>
        /// Name of the layout, this is the name that the user will need to
        /// specific via the <c>layout</c> property in their front-matter
        /// </summary>
        Name : string
        /// <summary>
        /// Function that will generate the page
        /// </summary>
        Func :  LayoutRenderFunc
    }

/// <summary>
/// Exposed contract of a layout
/// </summary>
[<NoComparison; NoEquality>]
type LayoutInfo =
    {

        /// <summary>
        /// List of dependencies that will be copied to the output folder
        /// </summary>
        Dependencies : LayoutDependency array

        /// <summary>
        /// List of renderers to make available
        /// </summary>
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

/// <summary>
/// Represents a file in the queue to be processed
/// </summary>
[<RequireQualifiedAccess; NoComparison>]
type QueueFile =
    /// <summary>
    /// Markdown file that need to be transform to HTML before being written
    /// </summary>
    | Markdown of PageContext
    /// <summary>
    /// SASS file that will trigger a SASS compilation before being written
    /// </summary>
    | Sass of filePath : string
    /// <summary>
    /// JavaScript file that will be copied as is to the output folder
    /// </summary>
    | JavaScript of filePath : string
    /// <summary>
    /// A file that will be copied as is to the output folder
    /// </summary>
    | LayoutDependency of LayoutDependency
    /// <summary>
    /// A file that will be copied as is to the output folder
    /// </summary>
    | Other of filePath : string
