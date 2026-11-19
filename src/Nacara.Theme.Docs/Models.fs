namespace Nacara.Theme.Docs

open Nacara.Core
open Nacara.Theme
open Nacara.Theme.Core

/// <summary>An entry of the navbar, with every url resolved and every state settled.</summary>
[<RequireQualifiedAccess>]
type NavbarEntry =
    | Link of label: string * url: string
    /// A link to a section, lit when the page being read is in it.
    | Section of label: string * url: string * active: bool
    | Icon of label: string * url: string * svg: string
    | Described of label: string * description: string * url: string
    | Dropdown of label: string * items: NavbarEntry list
    | Divider
    /// The locales of the site, each with this page's translation.
    | LocalePicker of current: Locale * translations: TranslationModel list
    /// Markup a plugin contributed, rendered already.
    | Widget of html: string

/// <summary>The navbar of a page.</summary>
type NavbarModel =
    {
        BrandLabel: string
        BrandUrl: string
        /// <summary>The icon before the title. An <c>Image</c> holds the image's url.</summary>
        BrandIcon: BrandIcon option
        /// After the brand.
        Start: NavbarEntry list
        /// At the other end of the bar.
        End: NavbarEntry list
        /// The section the page being read is in.
        Section: string
    }

/// <summary>What a menu node stands for.</summary>
[<RequireQualifiedAccess>]
type MenuNodeKind =
    | Page
    | Link
    /// A page with pages under it.
    | Group
    /// A label with pages under it.
    | Section

/// <summary>One entry of a sidebar menu, with what hangs under it.</summary>
type MenuNode =
    {
        Label: string
        /// <c>None</c> for a section, which is a label rather than a link.
        Url: string option
        Kind: MenuNodeKind
        /// Whether this is the page being read.
        Current: bool
        /// Whether the page being read is this node or under it.
        Open: bool
        Badge: MenuBadge option
        Children: MenuNode list
    }

/// <summary>Where a section's menu came from.</summary>
[<RequireQualifiedAccess>]
type MenuSource =
    /// The site declared it.
    | Declared
    /// A plugin offered it for the pages it generates.
    | Offered
    /// Nobody said: the section's pages, in their own order.
    | SectionPages

/// <summary>The box that filters a menu.</summary>
type MenuFilterModel =
    {
        /// Where the section's whole menu is written, when the page holds only a part of it.
        SourceUrl: string option
    }

/// <summary>The sidebar of a page.</summary>
type SidebarModel =
    {
        /// The navbar's entries, for a screen too narrow to show the bar.
        Drawer: NavbarEntry list
        Tree: MenuNode list
        Source: MenuSource
        /// <c>None</c> when the page offers no filter box.
        Filter: MenuFilterModel option
        /// Whether the menu carries a reader's folding from page to page.
        Memory: bool
        /// Whether the page shows a menu at all.
        ShowMenu: bool
        Section: string
    }

/// <summary>The table of contents of a page.</summary>
type TocModel =
    {
        Headings: Heading list
    }

/// <summary>A page, as a link points at it.</summary>
type PageLink =
    {
        Title: string
        Url: string
    }

/// <summary>The previous and next pages of a section.</summary>
type PageNavModel =
    {
        Previous: PageLink option
        Next: PageLink option
    }

/// <summary>The link that edits a page at its source.</summary>
type EditLinkModel =
    {
        Url: string
    }
