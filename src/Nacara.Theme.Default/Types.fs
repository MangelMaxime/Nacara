namespace Nacara.Theme

open Feliz.ViewEngine
open Nacara.Core
open Nacara.Theme.Core
open Nacara.Theme.Docs

/// <summary>What a part of the page is given besides its own model.</summary>
type PartContext =
    {
        Scope: PageScope
        /// What the page asked for.
        Doc: DocPage
        Options: ThemeOptions
    }

    member this.Page = this.Scope.Page
    member this.Site = this.Scope.Site
    member this.Pages = this.Scope.Pages

/// <summary>What goes inside the page's <c>&lt;main&gt;</c>, rendered already.</summary>
and ArticleSlots =
    {
        /// The page's body, as html.
        Content: string
        EditLink: ReactElement
        PageNav: ReactElement
    }

/// <summary>The pieces of the page, rendered already, for the frame to arrange.</summary>
and FrameSlots =
    {
        Navbar: ReactElement
        Sidebar: ReactElement
        Main: ReactElement
        /// <c>None</c> when the page shows no table of contents.
        Toc: ReactElement option
        Footer: ReactElement
    }

/// <summary>
/// What renders each part of a page. Replace one to change that part and keep the rest.
/// </summary>
/// <remarks>
/// Each is given the model the theme resolved, so a replacement draws the same navbar, menu or
/// table of contents in markup of its own. Wrap a default to add to it: call
/// <c>Parts.navbar</c> from your own function and put yours around it.
/// </remarks>
and ThemeParts =
    {
        /// The tags of the head: title, description, social cards, favicon.
        Head: PartContext -> HeadModel -> ReactElement list
        Navbar: PartContext -> NavbarModel -> ReactElement
        Sidebar: PartContext -> SidebarModel -> ReactElement
        Toc: PartContext -> TocModel -> ReactElement
        PageNav: PartContext -> PageNavModel -> ReactElement
        EditLink: PartContext -> EditLinkModel -> ReactElement
        /// The <c>&lt;main&gt;</c>: title, content, and what follows it.
        Article: PartContext -> ArticleSlots -> ReactElement
        Footer: PartContext -> ReactElement
        /// Arranges the pieces: navbar, then sidebar, main and table of contents, then footer.
        Frame: PartContext -> FrameSlots -> ReactElement
    }

/// <summary>Options of the default theme.</summary>
and ThemeOptions =
    {
        /// What every documentation theme is told: navbar, menus, where a page's source is.
        Docs: DocsOptions
        /// Extra markup injected at the end of <c>&lt;head&gt;</c>.
        HeadExtra: ReactElement list
        /// <summary>The layers of the theme's stylesheet, first one lowest.</summary>
        Styles: StyleLayer list
        /// <summary>CSS added to every page, outside any layer, so it wins over the theme's
        /// own.</summary>
        /// <remarks>For a rule or two. A stylesheet of your own belongs in a file, shipped as a
        /// static asset and linked from <c>HeadExtra</c>.</remarks>
        Css: string list
        Footer: ReactElement option
        /// Path of the favicon, relative to the site root.
        FavIcon: string option
        /// What renders each part of a page.
        Parts: ThemeParts
    }
