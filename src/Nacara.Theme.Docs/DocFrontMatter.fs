namespace Nacara.Theme

open Nacara.Core
open Nacara.Theme.Core

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
        /// <summary>The image a link to the page is shown with on social sites.</summary>
        /// <remarks>
        /// A path relative to the site root or an absolute url, a mapping with <c>url</c>,
        /// <c>alt</c>, <c>width</c> and <c>height</c>, or <c>false</c> for no image at all:
        /// <code>
        /// ogImage:
        ///   url: social/install.png
        ///   alt: The installer
        /// </code>
        /// </remarks>
        OgImage: PageImage
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

    let private ogImageMapping =
        Decode.object (fun image ->
            PageImage.Image
                {
                    Path = image.Required.Field "url" Decode.string
                    Alt = image.Optional.Field "alt" Decode.string
                    Width = image.Optional.Field "width" Decode.int
                    Height = image.Optional.Field "height" Decode.int
                }
        )

    let private ogImagePath =
        Decode.string
        |> Decode.andThen (fun value ->
            match value.ToLowerInvariant() with
            | "false"
            | "none" -> Decode.succeed PageImage.OptedOut
            | "true" -> Decode.error' "Name the image, or write false for none"
            | _ -> Decode.succeed (PageImage.Image(SocialImage.ofPath value))
        )

    let private ogImageDecoder: Decoder<PageImage> =
        fun path node ->
            match node with
            | :? YamlDotNet.RepresentationModel.YamlMappingNode -> ogImageMapping path node
            | _ -> ogImagePath path node

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

    /// <summary>The layouts every theme built on this vocabulary knows.</summary>
    /// <remarks>Keyed by what <c>layout:</c> says in front matter. A theme with layouts of its own
    /// adds to this map and reads front matter with
    /// <see cref="M:Nacara.Theme.DocFrontMatter.decoderWith" />.</remarks>
    let layouts: Map<string, DocPage -> DocPage> =
        Map
            [
                "bare", DocPage.bare
                "splash", DocPage.splash
            ]

    /// <summary>Reads the theme's front matter, failing the build with file and line when it cannot.</summary>
    /// <param name="known">The layouts a page may ask for. Any other fails the build.</param>
    let decoderWith (known: string list) : Decoder<DocFrontMatter> =
        let layoutDecoder =
            Decode.string
            |> Decode.andThen (fun name ->
                if List.contains name known then
                    Decode.succeed name
                else
                    let offered = known |> List.map (sprintf "'%s'") |> String.concat ", "

                    Decode.error'
                        $"'%s{name}' is not a layout this theme knows; it knows %s{offered}"
            )

        Decode.object (fun get ->
            {
                Title = get.Required.Field "title" Decode.string
                Description = get.Optional.Field "description" Decode.string
                OgImage =
                    get.Optional.Field "ogImage" ogImageDecoder
                    |> Option.defaultValue PageImage.Unspecified
                Order = get.Optional.Field "order" Decode.int
                Layout = get.Optional.Field "layout" layoutDecoder
                PageNav = get.Optional.Field "pageNav" Decode.bool
                MenuFilter = get.Optional.Field "menuFilter" Decode.bool
                MenuMemory = get.Optional.Field "menuMemory" Decode.bool
                Toc = get.Optional.Field "toc" tocDecoder
                Main = get.Optional.Field "main" mainAttributesDecoder |> Option.defaultValue []
            }
        )

    /// <summary>Reads the theme's front matter, knowing the layouts of
    /// <see cref="P:Nacara.Theme.DocFrontMatter.layouts" />.</summary>
    let decoder: Decoder<DocFrontMatter> = decoderWith (List.ofSeq layouts.Keys)

    /// <summary>The theme's view of a page described by this front matter.</summary>
    /// <param name="layouts">What each <c>layout:</c> a page may ask for does to the page.</param>
    /// <param name="frontMatter">What the page said about itself.</param>
    let toDocPageWith (layouts: Map<string, DocPage -> DocPage>) (frontMatter: DocFrontMatter) =
        let page =
            DocPage.create frontMatter.Title
            |> DocPage.describedBy frontMatter.Description
            |> fun page ->
                { page with
                    Image = frontMatter.OgImage
                }
            |> DocPage.mainAttributes frontMatter.Main

        let page =
            match frontMatter.Layout with
            | Some name ->
                match Map.tryFind name layouts with
                | Some apply ->
                    { apply page with
                        Layout = name
                    }
                | None -> page
            | None -> page

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

    /// <summary>The theme's view of a page, knowing the layouts of
    /// <see cref="P:Nacara.Theme.DocFrontMatter.layouts" />.</summary>
    /// <param name="frontMatter">What the page said about itself.</param>
    let toDocPage (frontMatter: DocFrontMatter) = toDocPageWith layouts frontMatter
