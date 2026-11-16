namespace Nacara.Plugins

open System
open System.IO
open System.Text
open System.Xml
open System.Xml.Linq
open Feliz.ViewEngine
open Nacara.Core

type FeedEntry =
    {
        Title: string
        Published: DateTimeOffset
        /// When it last changed. <c>Published</c> stands in when it is <c>None</c>.
        Updated: DateTimeOffset option
        Summary: string option
        Categories: string list
    }

type FeedOptions =
    {
        Path: string
        /// The feed's title. The site's title stands in when it is <c>None</c>.
        Title: string option
        Authors: string list
        /// How many entries the feed holds, newest first. <c>None</c> holds every one.
        Limit: int option
        IncludeContent: bool
        Entries: Page list -> (Page * FeedEntry) list
    }

/// <summary>An Atom feed of the pages of a collection.</summary>
/// <remarks>
/// The feed holds absolute urls, so it is written only when the site declares where it is
/// published. It lists the pages of the root locale.
/// </remarks>
[<RequireQualifiedAccess>]
module Feed =

    let private atom = XNamespace.Get "http://www.w3.org/2005/Atom"

    let entry (title: string) (published: DateTimeOffset) =
        {
            Title = title
            Published = published
            Updated = None
            Summary = None
            Categories = []
        }

    /// <summary>A feed of the pages of a collection: 20 entries at <c>feed.xml</c>, with their
    /// bodies.</summary>
    /// <example>
    /// <code lang="fsharp">
    /// Feed.ofCollection posts (fun post ->
    ///     { Feed.entry post.Title post.Date with
    ///         Summary = Some post.Excerpt
    ///     }
    /// )
    /// </code>
    /// </example>
    /// <param name="collection">Its pages in the root locale are the entries. Drafts are left out of a
    /// build.</param>
    /// <param name="describe">The entries are ordered by the <c>Updated</c> date it returns, newest
    /// first.</param>
    let ofCollection (collection: Collection<'FrontMatter>) (describe: 'FrontMatter -> FeedEntry) =
        {
            Path = "feed.xml"
            Title = None
            Authors = []
            Limit = Some 20
            IncludeContent = true
            Entries =
                fun pages ->
                    Collection.pagesOf collection pages
                    |> List.map (fun typed -> typed.Page, describe typed.FrontMatter)
        }

    let path value (options: FeedOptions) =
        { options with
            Path = value
        }

    let title value (options: FeedOptions) =
        { options with
            Title = Some value
        }

    let author name (options: FeedOptions) =
        { options with
            Authors = options.Authors @ [ name ]
        }

    let limit value (options: FeedOptions) =
        { options with
            Limit = value
        }

    let includeContent value (options: FeedOptions) =
        { options with
            IncludeContent = value
        }

    let private updatedOf (entry: FeedEntry) =
        entry.Updated |> Option.defaultValue entry.Published

    let private stamp (date: DateTimeOffset) =
        date.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'")

    let private element (name: string) (content: string) = XElement(atom + name, content)

    let private link (href: string) (rel: string option) =
        let link = XElement(atom + "link", XAttribute(XName.Get "href", href))

        match rel with
        | Some rel -> link.Add(XAttribute(XName.Get "rel", rel))
        | None -> ()

        link

    let render (site: SiteInfo) (options: FeedOptions) (pages: Page list) =
        let absolute (route: Route) =
            site.AbsoluteUrlOf route |> Option.defaultWith (fun () -> site.UrlOf route)

        let origin = site.Origin |> Option.defaultValue ""
        let home = absolute (Route.home site.RootLocale)

        let entries =
            options.Entries pages
            |> List.filter (fun (page, _) -> page.Locale.Code = site.RootLocale.Code)
            |> List.sortByDescending (fun (page, entry) -> updatedOf entry, page.Title)
            |> fun entries ->
                match options.Limit with
                | Some limit -> List.truncate limit entries
                | None -> entries

        let updated =
            entries
            |> List.map (snd >> updatedOf)
            |> List.sortDescending
            |> List.tryHead
            |> Option.defaultValue DateTimeOffset.UnixEpoch

        let feed = XElement(atom + "feed")
        feed.Add(element "title" (options.Title |> Option.defaultValue site.Title))

        match site.Description with
        | Some description -> feed.Add(element "subtitle" description)
        | None -> ()

        feed.Add(link home None)
        feed.Add(link (origin + Url.ofPath site.Url options.Path) (Some "self"))
        feed.Add(element "id" home)
        feed.Add(element "updated" (stamp updated))

        for name in options.Authors do
            feed.Add(XElement(atom + "author", element "name" name))

        for page, entry in entries do
            let url = absolute page.Route
            let item = XElement(atom + "entry")
            item.Add(element "title" entry.Title)
            item.Add(link url None)
            item.Add(element "id" url)
            item.Add(element "published" (stamp entry.Published))
            item.Add(element "updated" (stamp (updatedOf entry)))

            for category in entry.Categories do
                item.Add(XElement(atom + "category", XAttribute(XName.Get "term", category)))

            match entry.Summary with
            | Some summary -> item.Add(element "summary" summary)
            | None -> ()

            if options.IncludeContent then
                item.Add(
                    XElement(
                        atom + "content",
                        XAttribute(XName.Get "type", "html"),
                        XAttribute(XNamespace.Xml + "base", url),
                        page.Html
                    )
                )

            feed.Add item

        use stream = new MemoryStream()

        let settings =
            XmlWriterSettings(Encoding = UTF8Encoding(false), Indent = true, NewLineChars = "\n")

        do
            use writer = XmlWriter.Create(stream, settings)
            XDocument(XDeclaration("1.0", "utf-8", null), [| box feed |]).Save writer

        Encoding.UTF8.GetString(stream.ToArray()) + "\n"

    let headLink (site: SiteInfo) (options: FeedOptions) =
        Html.link
            [
                prop.rel "alternate"
                prop.type' "application/atom+xml"
                prop.title (options.Title |> Option.defaultValue site.Title)
                prop.href (site.UrlOfAsset options.Path)
            ]

    type private FeedPlugin(options: FeedOptions) =
        interface IPlugin with
            member _.Name = "feed"

            member _.Configure registry =
                registry
                |> Registry.onPagesRouted (fun context ->
                    match context.Site.Origin with
                    | None ->
                        context.Diagnostics.Add(
                            Diagnostic.warning
                                "origin-missing"
                                $"No feed was written at '%s{options.Path}': the site does not say where it is published"
                            |> Diagnostic.withHint
                                "Declare it with Site.origin \"https://example.com\""
                        )
                    | Some _ ->
                        context.Write options.Path (render context.Site options context.Pages)
                        |> ignore
                )

    let create (options: FeedOptions) = FeedPlugin(options) :> IPlugin

    let register (options: FeedOptions) (site: Site) = Site.plugin (create options) site
