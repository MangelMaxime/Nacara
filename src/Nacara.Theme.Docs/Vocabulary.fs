namespace Nacara.Theme

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

/// <summary>What a documentation theme is told about the site it draws.</summary>
/// <remarks>
/// The part of a theme's options every documentation theme shares: what the navbar holds, which
/// menu each section shows, where a page's source is. A theme adds its own options beside these.
/// </remarks>
type DocsOptions =
    {
        Navbar: NavbarItem list
        NavbarEnd: NavbarItem list
        /// Explicit menus, keyed by the first segment of a page's route.
        /// Sections with no entry here get a menu built from their pages.
        Menus: Map<string, MenuItem list>
        /// Base URL for "edit this page" links, for example
        /// <c>https://github.com/MangelMaxime/Nacara/edit/main/</c>.
        EditUrlBase: string option
        /// <summary>How many pages a section's menu writes into a page before it prunes.</summary>
        /// <remarks>Under it the menu is written whole, so the filter box covers every page it
        /// holds. <c>0</c> never prunes, whatever it costs.</remarks>
        MenuLimit: int
        /// <summary>Which groups are pruned, once a menu is over
        /// <see cref="P:Nacara.Theme.DocsOptions.MenuLimit" />.</summary>
        /// <remarks>A group of more than this keeps the trail to the page being read and a link to
        /// its own page; a smaller one is written whole.</remarks>
        MenuGroupLimit: int
    }

[<RequireQualifiedAccess>]
module DocsOptions =

    let defaults =
        {
            Navbar = []
            NavbarEnd = []
            Menus = Map.empty
            EditUrlBase = None
            MenuLimit = 500
            MenuGroupLimit = 150
        }

    /// <summary>The items on the left of the navbar, after the site's title.</summary>
    /// <param name="value">The items, in the order they are shown.</param>
    /// <param name="options">The options so far.</param>
    let navbar value (options: DocsOptions) =
        { options with
            Navbar = value
        }

    /// <summary>The items on the right of the navbar - search, a repository link, the theme toggle.</summary>
    /// <param name="value">The items, in the order they are shown.</param>
    /// <param name="options">The options so far.</param>
    let navbarEnd value (options: DocsOptions) =
        { options with
            NavbarEnd = value
        }

    /// <summary>The menu for one section, replacing the one its pages would have produced.</summary>
    /// <remarks>
    /// Additive: call it once per section. A section you say nothing about gets a menu built from
    /// its pages, or the one a plugin offered.
    /// </remarks>
    /// <param name="section">The first segment of the routes it covers, such as <c>guide</c>.</param>
    /// <param name="items">What the menu holds.</param>
    /// <param name="options">The options so far.</param>
    let menu section items (options: DocsOptions) =
        { options with
            Menus = Map.add section items options.Menus
        }

    /// <summary>Every menu at once, replacing any set before.</summary>
    /// <param name="value">The menus, keyed by section.</param>
    /// <param name="options">The options so far.</param>
    let menus value (options: DocsOptions) =
        { options with
            Menus = value
        }

    /// <summary>Base URL for the "edit this page" link.</summary>
    /// <param name="value">Where an edit starts, such as
    /// <c>https://github.com/owner/repo/edit/main/docs</c>.</param>
    /// <param name="options">The options so far.</param>
    let editUrl value (options: DocsOptions) =
        { options with
            EditUrlBase = Some value
        }

    /// <summary>How many pages a section's menu writes into a page before it prunes.</summary>
    /// <param name="value">The value to use. <c>0</c> never prunes.</param>
    /// <param name="options">The options so far.</param>
    let menuLimit value (options: DocsOptions) =
        { options with
            MenuLimit = value
        }

    /// <summary>Which groups are pruned, once a menu is over the limit.</summary>
    /// <param name="value">The value to use. <c>0</c> prunes none of them.</param>
    /// <param name="options">The options so far.</param>
    let menuGroupLimit value (options: DocsOptions) =
        { options with
            MenuGroupLimit = value
        }
