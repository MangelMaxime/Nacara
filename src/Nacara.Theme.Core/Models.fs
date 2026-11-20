namespace Nacara.Theme.Core

open Nacara.Core

/// <summary>The page being rendered and the site around it, which is what every resolver reads.</summary>
type PageScope =
    {
        Site: SiteInfo
        /// Every page of the site.
        Pages: Page list
        Page: Page
    }

/// <summary>One locale's version of the page being read.</summary>
type TranslationModel =
    {
        Locale: Locale
        /// Where the translation is, or the locale's home page when there is none.
        Url: string
        /// Whether the page exists in this locale.
        Translated: bool
        /// Whether this is the locale of the page being read.
        Current: bool
    }

/// <summary>An image a link to a page is shown with on social sites, as a page or a site names
/// it.</summary>
type SocialImage =
    {
        /// A path relative to the site root, or an absolute url.
        Path: string
        Alt: string option
        /// Width in pixels.
        Width: int option
        /// Height in pixels.
        Height: int option
    }

[<RequireQualifiedAccess>]
module SocialImage =

    /// <summary>An image with nothing but its path.</summary>
    /// <param name="path">A path relative to the site root, or an absolute url.</param>
    let ofPath (path: string) =
        {
            Path = path
            Alt = None
            Width = None
            Height = None
        }

/// <summary>The image a link to the page is shown with on social sites.</summary>
type HeadImage =
    {
        /// The absolute url of the image, or a root-relative one when the site has no origin.
        Url: string
        Alt: string option
        Width: int option
        Height: int option
    }

/// <summary>What the <c>&lt;head&gt;</c> of a page says about it.</summary>
type HeadModel =
    {
        /// The page's own title.
        Title: string
        /// What the browser tab shows: the page's title and the site's.
        WindowTitle: string
        Description: string option
        /// The absolute url of the page, when the site knows where it is published.
        Canonical: string option
        SiteName: string
        Locale: string
        OgType: string
        /// The favicon's url.
        Favicon: string option
        Image: HeadImage option
    }
