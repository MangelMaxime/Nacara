namespace Nacara.Theme

open Nacara.Core

/// <summary>What the theme needs to know about the page it is rendering.</summary>
/// <remarks>
/// Kept separate from the front-matter type so a site can define its own front matter and still use
/// the theme: it maps its type onto this record, and nothing else changes.
/// </remarks>
type DocPage =
    {
        Title: string
        Description: string option
        /// <summary>The image a link to the page is shown with on social sites, by its path
        /// relative to the site root or an absolute url.</summary>
        Image: string option
        ImageAlt: string option
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
        /// <summary>Whether the theme writes the page's title above the content.</summary>
        ShowTitle: bool
        /// <summary>Whether the theme offers the link that edits the page at its source.</summary>
        ShowEditLink: bool
        /// <summary>Whether the theme lays the content out.</summary>
        /// <remarks>Off, the page is a canvas: its title, the prose styles and the edit link are
        /// left out, and the content is placed as it was written.</remarks>
        Styled: bool
        /// <summary>Attributes to put on the page's <c>&lt;main&gt;</c>, name and value.</summary>
        /// <remarks><c>id</c>, <c>class</c> and <c>tabindex</c> are the theme's own and cannot be
        /// set here.</remarks>
        MainAttributes: (string * string) list
        /// <summary>Which of the theme's layouts draws the page.</summary>
        /// <remarks><c>doc</c> is the ordinary one. <c>bare</c> and <c>splash</c> are what
        /// <see cref="M:Nacara.Theme.DocPage.bare" /> and <see cref="M:Nacara.Theme.DocPage.splash" />
        /// set; a theme may know others.</remarks>
        Layout: string
    }

[<RequireQualifiedAccess>]
module DocPage =

    /// <summary>A page shown with everything the theme offers.</summary>
    /// <param name="title">Its heading, its entry in the menu, and its <c>&lt;title&gt;</c>.</param>
    let create title =
        {
            Title = title
            Description = None
            Image = None
            ImageAlt = None
            ShowToc = true
            ShowPageNav = true
            ShowMenu = true
            MenuFilter = None
            MenuMemory = true
            ShowTitle = true
            ShowEditLink = true
            Styled = true
            MainAttributes = []
            Layout = "doc"
        }

    /// <summary>What the page is about, for search results and social cards.</summary>
    /// <param name="value">One sentence.</param>
    /// <param name="page">The page being described.</param>
    let description (value: string) (page: DocPage) =
        { page with
            Description = Some value
        }

    /// <summary>The same, from front matter that may or may not carry one.</summary>
    /// <param name="value">The description, or <c>None</c> to leave the site's own in
    /// charge.</param>
    /// <param name="page">The page being described.</param>
    let describedBy value (page: DocPage) =
        { page with
            Description = value
        }

    /// <summary>The image a link to the page is shown with on social sites.</summary>
    /// <param name="path">A path relative to the site root, or an absolute url.</param>
    /// <param name="alt">What the image shows.</param>
    /// <param name="page">The page being described.</param>
    let image (path: string) (alt: string option) (page: DocPage) =
        { page with
            Image = Some path
            ImageAlt = alt
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

    /// <summary>The theme's styling and spacing, with no menu and no table of contents.</summary>
    /// <remarks>Wider than the reading measure, at <c>--nacara-splash-width</c>.</remarks>
    /// <param name="page">The page being described.</param>
    let splash (page: DocPage) =
        { page with
            ShowMenu = false
            ShowToc = false
            ShowPageNav = false
            ShowTitle = false
            ShowEditLink = false
            Layout = "splash"
        }

    /// <summary>A canvas: the navbar and the footer, and the content laid out by the page.</summary>
    /// <param name="page">The page being described.</param>
    let bare (page: DocPage) =
        { page with
            ShowMenu = false
            ShowToc = false
            ShowPageNav = false
            ShowTitle = false
            ShowEditLink = false
            Styled = false
            Layout = "bare"
        }

/// <summary>What a page says about its table of contents.</summary>
type TocSetting =
    /// No table of contents beside the content.
    | TocOff
    /// The heading levels it holds.
    | TocLevels of TocRange
