namespace Nacara.Theme

open Feliz.ViewEngine
open Nacara.Core

/// <summary>An entry of the navigation bar.</summary>
type NavbarItem =
    | NavbarLink of label: string * url: string
    | NavbarSection of label: string * section: string * url: string
    | NavbarDropdown of label: string * items: NavbarItem list
    | NavbarDivider
    /// An icon-only link. The icon is inline SVG markup.
    | NavbarIcon of label: string * url: string * svg: string
    /// A link with a description, for use inside a dropdown.
    | NavbarDescribed of label: string * description: string * url: string
    /// Lists the locales of the site, linking to the translation of the page being read.
    | NavbarLocalePicker
    /// Raw markup, which is how a plugin contributes a widget of its own.
    | NavbarWidget of html: string
    /// Markup rendered from the site as it is being built, for a widget that needs to know
    /// something only settled by then - which version this build is, say.
    | NavbarDynamicWidget of render: (SiteInfo -> string)

/// <summary>A mark beside a menu entry: new, deprecated, whatever a site needs to say.</summary>
/// <remarks>
/// <c>Kind</c> is the styling hook rather than the text, so a badge reading "Nouveau" can still be
/// coloured as <c>new</c>. The theme knows <c>new</c>, <c>updated</c>, <c>experimental</c>,
/// <c>beta</c> and <c>deprecated</c>; anything else is drawn neutrally.
/// </remarks>
type MenuBadge =
    {
        Label: string
        Kind: string
    }

/// <summary>What a menu entry points at.</summary>
type MenuEntry =
    /// A page of a collection, referenced by the path of its source file.
    | MenuPage of path: string
    /// A label over a set of entries. At the top of a menu it is a heading; nested, it folds.
    | MenuSection of label: string * items: MenuItem list
    /// <summary>A group whose label is a page of its own - the overview of what it holds.</summary>
    /// <remarks>
    /// The page takes the group's place rather than sitting inside it. Put it at <c>index.md</c>
    /// beside the pages it introduces and it keeps their url.
    /// </remarks>
    | MenuGroup of page: string * items: MenuItem list
    | MenuLink of label: string * url: string

/// <summary>An entry of the sidebar menu.</summary>
/// <remarks>
/// Built with the <c>Menu</c> functions rather than by hand:
/// <c>Menu.page "guide/i18n.md" |&gt; Menu.badge "New"</c>.
/// </remarks>
and MenuItem =
    {
        Entry: MenuEntry
        Badge: MenuBadge option
    }

/// <summary>
/// The menus plugins offered for the sections they generate.
/// </summary>
/// <remarks>
/// Read when the theme is registered, which is why a theme is registered after the plugins whose
/// pages it will draw. A site that writes its own menu for a section never reaches these.
/// </remarks>
[<RequireQualifiedAccess>]
module OfferedMenus =

    let mutable private offered: MenuOutline list = []

    /// <summary>Remember what plugins offered.</summary>
    /// <param name="menus">Every <c>MenuOutline</c> registered so far.</param>
    let remember (menus: MenuOutline list) = offered <- menus

    /// <summary>The menu offered for a section, if one was.</summary>
    /// <param name="section">The first segment of the routes it covers.</param>
    let forSection (section: string) =
        offered |> List.tryFind (fun outline -> outline.Section = section)

/// <summary>
/// The badge kinds this theme paints.
/// </summary>
/// <remarks>
/// A kind is only a styling hook, and any string is one: a site that styles
/// <c>[data-kind="internal"]</c> in a stylesheet of its own passes <c>"internal"</c> and it works.
/// These are the five the theme already has colours for.
/// </remarks>
[<RequireQualifiedAccess>]
module Badge =

    /// <summary>Something that was not here before. Drawn in the tip colour.</summary>
    [<Literal>]
    let New = "new"

    /// <summary>Something that changed. Drawn in the tip colour.</summary>
    [<Literal>]
    let Updated = "updated"

    /// <summary>Something that may still move. Drawn in the warning colour.</summary>
    [<Literal>]
    let Experimental = "experimental"

    /// <summary>Something released ahead of being settled. Drawn in the warning colour.</summary>
    [<Literal>]
    let Beta = "beta"

    /// <summary>Something on its way out. Drawn in the danger colour.</summary>
    [<Literal>]
    let Deprecated = "deprecated"

/// <summary>Menu entries, built by pipeline.</summary>
[<RequireQualifiedAccess>]
module Menu =

    let private entry value =
        {
            Entry = value
            Badge = None
        }

    /// <summary>The menu a plugin offered, as this theme's own entries.</summary>
    /// <param name="outline">What a plugin registered for the section it generates.</param>
    /// <remarks>What a site writes for the section is used instead - an offer, not a rule.</remarks>
    let rec ofOutline (outline: MenuOutlineItem list) : MenuItem list =
        [
            for item in outline ->
                match item.Page, item.Children with
                | Some page, [] -> entry (MenuPage page)
                | Some page, children -> entry (MenuGroup(page, ofOutline children))
                | None, children -> entry (MenuSection(item.Label, ofOutline children))
        ]

    /// <summary>A page, referenced by the path of its source file.</summary>
    /// <param name="path">Relative to the content directory, extension included -
    /// <c>guide/getting-started.md</c>. The entry takes the page's own title, so renaming a
    /// heading does not mean editing the menu.</param>
    let page (path: string) = entry (MenuPage path)

    /// <summary>Anything outside the site.</summary>
    /// <param name="label">What the entry reads.</param>
    /// <param name="url">Where it goes. Nothing resolves it against the site, so write it in
    /// full.</param>
    let link (label: string) (url: string) = entry (MenuLink(label, url))

    /// <summary>A label over a set of entries.</summary>
    /// <param name="label">What the group reads. It is a heading, not a link.</param>
    /// <param name="items">What is under it. Nested deeper than the top level, a section folds.</param>
    let section (label: string) (items: MenuItem list) = entry (MenuSection(label, items))

    /// <summary>A group whose label is its own page.</summary>
    /// <param name="path">The overview page of what the group holds, relative to the content
    /// directory - <c>plugins/markdown/index.md</c>.</param>
    /// <param name="items">What is under it. The group opens by itself when the reader is on one
    /// of them.</param>
    let group (path: string) (items: MenuItem list) = entry (MenuGroup(path, items))

    /// <summary>Mark an entry. The text is shown; its slug decides the colour.</summary>
    /// <param name="label">What the badge reads - <c>New</c>, <c>Deprecated</c>. Its slug is the
    /// styling hook, so <c>New</c> and <c>new</c> look alike.</param>
    /// <param name="item">The entry to mark.</param>
    let badge (label: string) (item: MenuItem) =
        { item with
            Badge =
                Some
                    {
                        Label = label
                        Kind = Slug.create label
                    }
        }

    /// <summary>Mark an entry, choosing the styling hook rather than deriving it.</summary>
    /// <param name="kind">What the theme styles it as, whatever the label happens to read.
    /// <see cref="T:Nacara.Theme.Badge" /> holds the ones it has colours for.</param>
    /// <param name="label">What the badge reads.</param>
    /// <param name="item">The entry to mark.</param>
    let badgeOf (kind: string) (label: string) (item: MenuItem) =
        { item with
            Badge =
                Some
                    {
                        Label = label
                        Kind = kind
                    }
        }

/// <summary>Options of the default theme.</summary>
type ThemeOptions =
    {
        Navbar: NavbarItem list
        NavbarEnd: NavbarItem list
        /// Explicit menus, keyed by the first segment of a page's route.
        /// Sections with no entry here get a menu built from their pages.
        Menus: Map<string, MenuItem list>
        /// Base URL for "edit this page" links, for example
        /// <c>https://github.com/MangelMaxime/Nacara/edit/main/</c>.
        EditUrlBase: string option
        /// Extra markup injected at the end of <c>&lt;head&gt;</c>.
        HeadExtra: ReactElement list
        /// <summary>CSS added to every page, after the theme's own.</summary>
        /// <remarks>For a rule or two. A stylesheet of your own belongs in a file, shipped as a
        /// static asset and linked from <c>HeadExtra</c>.</remarks>
        Css: string list
        Footer: ReactElement option
        /// Path of the favicon, relative to the site root.
        FavIcon: string option
    }

/// <summary>What the theme needs to know about the page it is rendering.</summary>
/// <remarks>
/// Kept separate from the front-matter type so a site can define its own front matter and still use
/// the theme: it maps its type onto this record, and nothing else changes.
/// </remarks>
type DocPage =
    {
        Title: string
        Description: string option
        /// Show the table of contents next to the content.
        ShowToc: bool
        /// Show previous and next links at the bottom.
        ShowPageNav: bool
        /// Show the sidebar menu.
        ShowMenu: bool
        /// <summary>Offer a box that filters the menu.</summary>
        /// <remarks><c>None</c> leaves it to the theme, which offers one when the menu is
        /// long.</remarks>
        MenuFilter: bool option
        /// <summary>Carry the menu's folding from page to page.</summary>
        /// <remarks>Off, every page opens the menu the same way: the trail to itself, and nothing
        /// else.</remarks>
        MenuMemory: bool
        /// <summary>Whether the theme lays the content out.</summary>
        /// <remarks>Off, the page is a canvas: its title, the prose styles and the edit link are
        /// left out, and the content is placed as it was written.</remarks>
        Styled: bool
        /// <summary>Attributes to put on the page's <c>&lt;main&gt;</c>, name and value.</summary>
        /// <remarks><c>id</c>, <c>class</c> and <c>tabindex</c> are the theme's own and cannot be
        /// set here.</remarks>
        MainAttributes: (string * string) list
    }

[<RequireQualifiedAccess>]
module DocPage =

    /// <summary>A page shown with everything the theme offers.</summary>
    /// <param name="title">Its heading, its entry in the menu, and its <c>&lt;title&gt;</c>.</param>
    let create title =
        {
            Title = title
            Description = None
            ShowToc = true
            ShowPageNav = true
            ShowMenu = true
            MenuFilter = None
            MenuMemory = true
            Styled = true
            MainAttributes = []
        }

    /// <summary>What the page is about, for search results and social cards.</summary>
    /// <param name="value">One sentence.</param>
    /// <param name="page">The page being described.</param>
    let description value (page: DocPage) =
        { page with
            Description = Some(value: string)
        }

    /// <summary>The same, from front matter that may or may not carry one.</summary>
    /// <param name="value">The description, or <c>None</c> to leave the site's own in
    /// charge.</param>
    /// <param name="page">The page being described.</param>
    let describedBy value (page: DocPage) =
        { page with
            Description = value
        }

    /// <summary>No table of contents beside the content.</summary>
    /// <param name="page">The page being described.</param>
    let withoutToc (page: DocPage) =
        { page with
            ShowToc = false
        }

    /// <summary>No sidebar: the content takes the width.</summary>
    /// <param name="page">The page being described.</param>
    let withoutMenu (page: DocPage) =
        { page with
            ShowMenu = false
        }

    /// <summary>No previous and next links at the bottom.</summary>
    /// <param name="page">The page being described.</param>
    let withoutPageNav (page: DocPage) =
        { page with
            ShowPageNav = false
        }

    /// <summary>Offer a box that filters the menu, however short the menu is.</summary>
    /// <param name="page">The page being described.</param>
    let withMenuFilter (page: DocPage) =
        { page with
            MenuFilter = Some true
        }

    /// <summary>No filter box over the menu, however long the menu is.</summary>
    /// <param name="page">The page being described.</param>
    let withoutMenuFilter (page: DocPage) =
        { page with
            MenuFilter = Some false
        }

    /// <summary>
    /// The menu forgets what a reader folded: every page opens it the same way.
    /// </summary>
    /// <remarks>For a section read by name rather than in order, such as a reference.</remarks>
    /// <param name="page">The page being described.</param>
    let withoutMenuMemory (page: DocPage) =
        { page with
            MenuMemory = false
        }

    /// <summary>Attributes to put on the page's <c>&lt;main&gt;</c>.</summary>
    /// <param name="value">Each attribute as its name and its value.</param>
    /// <param name="page">The page being described.</param>
    let mainAttributes value (page: DocPage) =
        { page with
            MainAttributes = value
        }

    /// <summary>A canvas: the navbar and the footer, and the content laid out by the page.</summary>
    /// <param name="page">The page being described.</param>
    let bare (page: DocPage) =
        { page with
            ShowMenu = false
            ShowToc = false
            ShowPageNav = false
            Styled = false
        }

/// <summary>What a page says about its table of contents.</summary>
type TocSetting =
    /// No table of contents beside the content.
    | TocOff
    /// The heading levels it holds.
    | TocLevels of TocRange

/// <summary>
/// Front matter understood by the theme's ready-made collection.
/// </summary>
/// <remarks>
/// The shape of every page's <c>---</c> block. A site wanting fields of its own declares its own
/// record and its own decoder; this one is what <c>Theme.docs</c> reads, and the only required
/// field is the title.
/// </remarks>
type DocFrontMatter =
    {
        /// <summary>The page's title: its heading, its entry in the menu, and what search shows.</summary>
        Title: string
        /// <summary>One sentence about the page, for search results and the page's meta description.</summary>
        Description: string option
        /// <summary>
        /// Where the page sits among the pages of its section.
        /// </summary>
        /// <remarks>
        /// Read only when the section has no menu declared - a menu says the order outright, and
        /// then this says nothing. Pages without one come last, in title order.
        /// </remarks>
        Order: int option
        /// <summary>
        /// Which layout renders the page, when it is not the ordinary one.
        /// </summary>
        /// <remarks>
        /// <c>layout: bare</c> is a page with no chrome: no menu, no table of contents, no
        /// previous and next links. A landing page is the usual reason.
        /// </remarks>
        Layout: string option
        /// <summary>Whether this page offers the previous and next pages of its section.</summary>
        /// <remarks>
        /// <c>pageNav: false</c> for a page that is not part of a sequence - a landing page, or one
        /// of a set a reader arrives at by name rather than by reading through.
        /// </remarks>
        PageNav: bool option
        /// <summary>Whether a box for filtering the section's menu is offered.</summary>
        /// <remarks>
        /// Left out, the theme decides: a menu long enough that finding a name means opening folds
        /// gets one, a menu you can read at a glance does not. <c>menuFilter: true</c> asks for one
        /// regardless, <c>menuFilter: false</c> refuses it.
        /// </remarks>
        MenuFilter: bool option
        /// <summary>Whether the menu carries a reader's folding over to the next page.</summary>
        /// <remarks>
        /// <c>menuMemory: false</c> for a section read by name rather than in order - a reference -
        /// where every page opens the menu the same way: the trail to itself, and nothing else.
        /// </remarks>
        MenuMemory: bool option
        /// <summary>
        /// The table of contents: <c>toc: false</c> for none, or the heading levels it holds.
        /// <code>
        /// toc:
        ///   from: 2
        ///   to: 2
        /// </code>
        /// </summary>
        /// <remarks>
        /// A page whose sections are uninteresting on their own - a changelog, where the versions
        /// are what a reader navigates by - says so here. Left out, the markdown plugin's option
        /// decides for the whole site. Either bound may be left out: <c>from</c> is 2, a page's own
        /// title being the only heading above it, and <c>to</c> is 6.
        /// </remarks>
        Toc: TocSetting option
        /// <summary>
        /// Attributes to put on the page's <c>&lt;main&gt;</c>:
        /// <code>
        /// main:
        ///   data-pagefind-weight: "0.3"
        /// </code>
        /// </summary>
        /// <remarks><c>id</c>, <c>class</c> and <c>tabindex</c> are the theme's own and cannot be
        /// set here.</remarks>
        Main: (string * string) list
    }

[<RequireQualifiedAccess>]
module DocFrontMatter =

    /// The theme puts these on <main> itself.
    let private reserved =
        set
            [
                "id"
                "class"
                "tabindex"
            ]

    let private tocDecoder =
        Decode.oneOf
            "false or a range of heading levels"
            [
                Decode.bool
                |> Decode.andThen (fun shown ->
                    if shown then
                        Decode.error' "Leave 'toc' out for the table of contents the site gives it"
                    else
                        Decode.succeed TocOff
                )

                Decode.object (fun toc ->
                    TocLevels
                        {
                            From = toc.Optional.Field "from" Decode.int |> Option.defaultValue 2
                            To = toc.Optional.Field "to" Decode.int |> Option.defaultValue 6
                        }
                )
            ]

    let private mainAttributesDecoder =
        Decode.keyValuePairs Decode.string
        |> Decode.andThen (fun pairs ->
            let taken =
                pairs
                |> List.tryFind (fun (name, _) -> reserved.Contains(name.ToLowerInvariant()))

            match taken with
            | Some(name, _) ->
                Decode.error' $"'%s{name}' is the theme's own attribute, so main cannot set it"
            | None -> Decode.succeed pairs
        )

    /// <summary>Reads the theme's front matter, failing the build with file and line when it cannot.</summary>
    let decoder: Decoder<DocFrontMatter> =
        Decode.object (fun get ->
            {
                Title = get.Required.Field "title" Decode.string
                Description = get.Optional.Field "description" Decode.string
                Order = get.Optional.Field "order" Decode.int
                Layout = get.Optional.Field "layout" Decode.string
                PageNav = get.Optional.Field "pageNav" Decode.bool
                MenuFilter = get.Optional.Field "menuFilter" Decode.bool
                MenuMemory = get.Optional.Field "menuMemory" Decode.bool
                Toc = get.Optional.Field "toc" tocDecoder
                Main = get.Optional.Field "main" mainAttributesDecoder |> Option.defaultValue []
            }
        )

    /// <summary>The theme's view of a page described by this front matter.</summary>
    let toDocPage (frontMatter: DocFrontMatter) =
        let page =
            DocPage.create frontMatter.Title
            |> DocPage.describedBy frontMatter.Description
            |> DocPage.mainAttributes frontMatter.Main

        let page =
            match frontMatter.Layout with
            | Some "bare" -> DocPage.bare page
            | _ -> page

        let page =
            match frontMatter.PageNav with
            | Some false -> DocPage.withoutPageNav page
            | _ -> page

        let page =
            match frontMatter.MenuFilter with
            | Some true -> DocPage.withMenuFilter page
            | Some false -> DocPage.withoutMenuFilter page
            | None -> page

        let page =
            match frontMatter.MenuMemory with
            | Some false -> DocPage.withoutMenuMemory page
            | _ -> page

        match frontMatter.Toc with
        | Some TocOff -> DocPage.withoutToc page
        | _ -> page
