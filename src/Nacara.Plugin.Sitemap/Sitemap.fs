namespace Nacara.Plugins

open System
open System.Text
open Nacara.Core

/// <summary>Options of the sitemap plugin.</summary>
type SitemapOptions =
    {
        /// Where the sitemap is written, relative to the output directory.
        Path: string
        /// Also write a robots.txt pointing at the sitemap.
        WriteRobots: bool
        /// Pages to leave out, by collection name.
        ExcludeCollections: string list
    }

/// <summary>
/// A sitemap of the built site, and optionally a robots.txt pointing at it.
/// </summary>
/// <remarks>
/// Only pages that exist in this build are listed, and only when the site declares where it is
/// published. Translations are cross-referenced with <c>hreflang</c>, so a search engine can offer
/// a reader the language they asked for.
/// </remarks>
[<RequireQualifiedAccess>]
module Sitemap =

    let defaults =
        {
            Path = "sitemap.xml"
            WriteRobots = true
            ExcludeCollections = []
        }

    let private escape (value: string) =
        value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")

    /// <summary>Render the sitemap for a set of pages.</summary>
    /// <param name="site">Where the site is published, for the absolute urls a sitemap needs.</param>
    /// <param name="options">What to leave out, and where the file goes.</param>
    /// <param name="pages">Every page the build produced.</param>
    let render (site: SiteInfo) (options: SitemapOptions) (pages: Page list) =
        let pages =
            pages
            |> List.filter (fun page ->
                not (List.contains page.Collection options.ExcludeCollections)
            )
            |> List.sortBy (fun page -> Url.ofRoute site.Url page.Route)

        let byTranslation =
            pages
            |> List.groupBy (fun page -> page.Collection, Route.translationKey page.Route)
            |> Map.ofList

        let builder = StringBuilder()
        builder.AppendLine """<?xml version="1.0" encoding="UTF-8"?>""" |> ignore

        builder.AppendLine
            """<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9" xmlns:xhtml="http://www.w3.org/1999/xhtml">"""
        |> ignore

        for page in pages do
            match site.AbsoluteUrlOf page.Route with
            | None -> ()
            | Some url ->
                builder.AppendLine "  <url>" |> ignore
                builder.AppendLine $"    <loc>%s{escape url}</loc>" |> ignore

                let translations =
                    byTranslation
                    |> Map.tryFind (page.Collection, Route.translationKey page.Route)
                    |> Option.defaultValue []

                if List.length translations > 1 then
                    for translation in translations do
                        match site.AbsoluteUrlOf translation.Route with
                        | Some href ->
                            builder.AppendLine
                                $"""    <xhtml:link rel="alternate" hreflang="%s{escape translation.Locale.Code}" href="%s{escape href}" />"""
                            |> ignore
                        | None -> ()

                builder.AppendLine "  </url>" |> ignore

        builder.AppendLine "</urlset>" |> ignore

        builder.ToString()

    type private SitemapPlugin(options: SitemapOptions) =
        interface IPlugin with
            member _.Name = "sitemap"

            member _.Configure registry =
                registry
                |> Registry.onBuildComplete (fun context ->
                    match context.Site.Origin with
                    | None ->
                        context.Diagnostics.Add(
                            Diagnostic.warning
                                "origin-missing"
                                "No sitemap was written: the site does not say where it is published"
                            |> Diagnostic.withHint
                                "Declare it with Site.origin \"https://example.com\""
                        )
                    | Some origin ->
                        context.Write options.Path (render context.Site options context.Pages)
                        |> ignore

                        if options.WriteRobots then
                            let sitemapUrl = origin + Url.ofPath context.Site.Url options.Path

                            context.Write
                                "robots.txt"
                                $"User-agent: *\nAllow: /\nSitemap: %s{sitemapUrl}\n"
                            |> ignore
                )

    /// <summary>Where the sitemap is written, relative to the output directory.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let path value (options: SitemapOptions) =
        { options with
            Path = value
        }

    /// <summary>Also write a robots.txt pointing at the sitemap.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let writeRobots value (options: SitemapOptions) =
        { options with
            WriteRobots = value
        }

    /// <summary>Pages to leave out, by collection name.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let excludeCollections value (options: SitemapOptions) =
        { options with
            ExcludeCollections = value
        }

    /// <summary>A sitemap, with the default options.</summary>
    let create () = SitemapPlugin(defaults) :> IPlugin

    /// <summary>A sitemap, configured.</summary>
    /// <param name="configure">Given the defaults, the options to use. Anything it leaves alone keeps its default.</param>
    let createWith (configure: SitemapOptions -> SitemapOptions) =
        SitemapPlugin(configure defaults) :> IPlugin

    /// <summary>Add a sitemap to a site.</summary>
    let register (site: Site) = Site.plugin (create ()) site

    /// <summary>Add a sitemap to a site, configured.</summary>
    /// <param name="configure">Given the defaults, the options to use. Anything it leaves alone keeps its default.</param>
    /// <param name="site">The site being described.</param>
    let registerWith (configure: SitemapOptions -> SitemapOptions) (site: Site) =
        Site.plugin (createWith configure) site
