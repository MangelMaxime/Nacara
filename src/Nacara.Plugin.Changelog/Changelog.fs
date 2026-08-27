namespace Nacara.Plugins

open System
open System.IO
open System.Reflection
open System.Text
open Nacara.Core

/// <summary>A changelog to publish.</summary>
type ChangelogSource =
    {
        /// Title of the generated page.
        Label: string
        /// Path of the changelog file, relative to the project root.
        Path: string
        /// Route segment. Defaults to a slug of the label.
        Slug: string option
        /// <summary>Group this changelog belongs to, like "Plugins" or "Themes".</summary>
        /// <remarks>The menu offered by the plugin uses it: changelogs sharing the same group are
        /// listed together under a heading with that name. If you don't set it, the changelog is
        /// listed on its own.</remarks>
        Group: string option
        /// <summary>Set when <c>Path</c> is a pattern: how each match is named.</summary>
        /// <remarks>Given the full path of the file that matched.</remarks>
        LabelFrom: (string -> string) option
    }

[<RequireQualifiedAccess>]
module ChangelogSource =

    /// <summary>A changelog file to publish as a page.</summary>
    /// <param name="label">What the page is called - the package the changelog belongs to.</param>
    /// <param name="path">Where the file is, relative to the project root. <c>../CHANGELOG.md</c>
    /// reaches above the site's own project.</param>
    let create label path =
        {
            Label = label
            Path = path
            Slug = None
            Group = None
            LabelFrom = None
        }

    /// <summary>How a match is named when nothing else says.</summary>
    /// <remarks>
    /// The directory holding the file, which is the package in the usual layout. One at the root of
    /// a repository is named after the file instead - the directory there is only what whoever
    /// cloned it chose.
    /// </remarks>
    /// <param name="path">The full path of the file.</param>
    let defaultLabel (path: string) =
        let directory = Path.GetDirectoryName path

        let isRepositoryRoot =
            let git = Path.Combine(directory, ".git")
            // A worktree keeps a file there rather than a directory.
            Directory.Exists git || File.Exists git

        if isRepositoryRoot then
            let name = Path.GetFileNameWithoutExtension path
            string (Char.ToUpperInvariant name[0]) + name.Substring(1).ToLowerInvariant()
        else
            Path.GetFileName directory

    /// <summary>How a matched path is named, when the default does not suit your layout.</summary>
    /// <remarks>
    /// Given the full path of the file that matched, so you can take whichever part of it names
    /// the thing. Only used by <see cref="M:Nacara.Plugins.ChangelogSource.matching" />.
    /// </remarks>
    /// <param name="value">Path in, name out.</param>
    /// <param name="source">The changelogs being described.</param>
    let labelledBy value (source: ChangelogSource) =
        { source with
            LabelFrom = Some(value: string -> string)
        }

    /// <summary>Every changelog matching a pattern, named after the directory holding it.</summary>
    /// <remarks>
    /// <para>One line instead of a list you have to keep in step with your solution:
    /// <c>../src/*/CHANGELOG.md</c> finds them all, and adding a package needs no edit here.</para>
    /// <para>Anything set on it applies to every match, so a group is declared once. Use
    /// <see cref="M:Nacara.Plugins.ChangelogSource.labelledBy" /> when the directory is not the
    /// name you want.</para>
    /// </remarks>
    /// <example>
    /// <code lang="fsharp">
    /// ChangelogSource.matching "../src/Nacara.Plugin.*/CHANGELOG.md"
    /// |> ChangelogSource.group "Plugins"
    /// </code>
    /// </example>
    /// <param name="pattern">Where to look, relative to the project root, with <c>*</c> in it.</param>
    let matching pattern =
        {
            Label = ""
            Path = pattern
            Slug = None
            Group = None
            LabelFrom = Some defaultLabel
        }

    /// <summary>What the page is called in a URL, rather than a slug of its label.</summary>
    /// <param name="value">The segment to publish it under - <c>core</c> rather than
    /// <c>nacara-core</c>.</param>
    /// <param name="source">The changelog being described.</param>
    let slug value (source: ChangelogSource) =
        { source with
            Slug = Some(value: string)
        }

    /// <summary>Put this changelog under a heading in the menu.</summary>
    /// <param name="value">Name of the heading, for example "Plugins". Changelogs you give the
    /// same name are listed together, in the order you declared them.</param>
    /// <param name="source">The changelog to group.</param>
    let group value (source: ChangelogSource) =
        { source with
            Group = Some(value: string)
        }

[<RequireQualifiedAccess>]
module internal Sources =

    /// <summary>What the file calls itself, when it says.</summary>
    let private declaredName (file: AbsolutePath) =
        let path = AbsolutePath.value file

        if File.Exists path then
            (ChangelogParser.parse (File.ReadAllText path)).Name
        else
            None

    /// <summary>A source list with its patterns expanded and its names settled.</summary>
    /// <remarks>
    /// The one place a label is decided, so the pages and the menu cannot disagree about what a
    /// changelog is called. <c>name:</c> in the file wins, then whatever the site said.
    /// </remarks>
    /// <param name="root">What a relative pattern is resolved against.</param>
    /// <param name="sources">What the site declared.</param>
    let resolve (root: AbsolutePath) (sources: ChangelogSource list) =
        sources
        |> List.collect (fun source ->
            match source.LabelFrom with
            | None ->
                let file = AbsolutePath.combine root [ source.Path ]

                [
                    { source with
                        Label = declaredName file |> Option.defaultValue source.Label
                    }
                ]
            | Some name ->
                Glob.files root source.Path
                |> List.map (fun file ->
                    // AbsolutePath.combine lets a rooted path win outright.
                    let found = AbsolutePath.value file

                    { source with
                        Label = declaredName file |> Option.defaultValue (name found)
                        Path = found
                        LabelFrom = None
                    }
                )
        )

[<RequireQualifiedAccess>]
module Changelog =

    /// <summary>Turn a parsed changelog into a page, versions only.</summary>
    /// <remarks>
    /// Everything above the first version - front matter, title, the note about Keep a Changelog -
    /// belongs to the file rather than to a page about releases, and is left out. What is kept goes
    /// through unchanged, so a version's markdown gets the same highlighting, link resolution and
    /// anchors as any hand-written page. Which is why the plugin ships no layout.
    /// </remarks>
    /// <param name="label">The page's title.</param>
    /// <param name="document">The parsed changelog. A file with no version heading is published
    /// whole rather than as a blank page.</param>
    let toMarkdown (label: string) (document: ChangelogDocument) =
        let builder = StringBuilder()
        builder.AppendLine "---" |> ignore
        builder.AppendLine $"title: %s{label}" |> ignore

        builder.AppendLine "pageNav: false" |> ignore
        builder.AppendLine "---" |> ignore
        builder.AppendLine "" |> ignore

        if List.isEmpty document.Versions then
            builder.AppendLine document.Preamble |> ignore
        else
            for version in document.Versions do
                // '## 0.0.0' has no letter to start an id with, so markdown would call it 'section'.
                let anchor = "v" + Slug.create version.Version

                builder.AppendLine $"%s{version.Heading} {{#%s{anchor}}}" |> ignore
                builder.AppendLine "" |> ignore
                builder.AppendLine version.Body |> ignore
                builder.AppendLine "" |> ignore

        // Public, and callable without a pipeline to normalize what comes back.
        builder.ToString().Replace("\r\n", "\n")

    /// <summary>
    /// A collection whose pages are generated from changelog files.
    /// </summary>
    /// <remarks>
    /// The front-matter type and the layout come from the site, so the pages look exactly like every
    /// other page of the site and the plugin stays independent of any theme.
    /// </remarks>
    /// <param name="name">What you call the collection, and what a menu refers to it by.</param>
    /// <param name="decoder">Reads the front matter the plugin writes into the site's own type.</param>
    /// <param name="sources">One entry per package. Each becomes a page, named by its label and
    /// routed by its slug.</param>
    let collection (name: string) (decoder: Decoder<'FrontMatter>) (sources: ChangelogSource list) =
        Collection.create name decoder
        |> Collection.toc (fun _ ->
            Some
                {
                    From = 2
                    To = 2
                }
        )
        |> Collection.producer
            "changelog"
            (fun context ->
                sources
                |> Sources.resolve context.ProjectRoot
                |> List.map (fun source ->
                    let path = AbsolutePath.combine context.ProjectRoot [ source.Path ]
                    let slug = source.Slug |> Option.defaultValue (Slug.create source.Label)

                    if not (File.Exists(AbsolutePath.value path)) then
                        context.Diagnostics.Add(
                            Diagnostic.error
                                "source-missing"
                                $"Changelog not found: %s{AbsolutePath.toLog path}"
                            |> Diagnostic.withHint
                                $"'%s{source.Path}' is resolved from the project root"
                        )

                        GeneratedContent.create
                            $"%s{slug}.md"
                            $"---\ntitle: %s{source.Label}\npageNav: false\n---\n\nThis changelog could not be read.\n"
                    else

                        let document =
                            File.ReadAllText(AbsolutePath.value path) |> ChangelogParser.parse

                        if List.isEmpty document.Versions then
                            context.Diagnostics.Add(
                                Diagnostic.warning
                                    "no-versions"
                                    $"No version was found in %s{AbsolutePath.toLog path}, so the page is the whole file"
                                |> Diagnostic.withHint
                                    "A version is a heading like '## 1.2.3' or '## [1.2.3] - 2024-06-01'"
                            )

                        GeneratedContent.create $"%s{slug}.md" (toMarkdown source.Label document)
                        |> GeneratedContent.dependsOn [ path ]
                )
            )

    /// <summary>Builds the menu for a section from the changelogs it publishes.</summary>
    /// <remarks>
    /// The sources are the menu: changelogs with a group are listed together under a heading with
    /// that name, in the order the groups first appear, and the rest where you declared them. A
    /// menu the site writes for the section is used instead.
    /// </remarks>
    /// <param name="section">Name of the section this menu is for - the collection's name.</param>
    /// <param name="sources">The changelogs the collection publishes.</param>
    let menu (section: string) (sources: ChangelogSource list) =
        let sources =
            Sources.resolve (AbsolutePath.create (Nacara.defaultProjectRoot ())) sources

        let page (source: ChangelogSource) =
            let slug = source.Slug |> Option.defaultValue (Slug.create source.Label)

            {
                Label = source.Label
                Page = Some $"%s{slug}.md"
                Children = []
            }

        let groups = sources |> List.choose _.Group |> List.distinct

        {
            Section = section
            Items =
                [
                    for source in sources do
                        if source.Group.IsNone then
                            yield page source

                    for group in groups ->
                        {
                            Label = group
                            Page = None
                            Children =
                                [
                                    for source in sources do
                                        if source.Group = Some group then
                                            yield page source
                                ]
                        }
                ]
        }

    let private readResource = Resource.text (Assembly.GetExecutingAssembly())

    let private changelogCss = lazy readResource "changelog.css"

    type private ChangelogPlugin(offered: MenuOutline option) =
        interface IPlugin with
            member _.Name = "changelog"

            member _.Configure registry =
                let registry =
                    registry
                    |> Registry.asset (
                        WriteText(changelogCss.Value, RelativePath.create "assets/changelog.css")
                    )
                    |> Registry.extra (Stylesheet "assets/changelog.css")

                match offered with
                | Some outline -> registry |> Registry.extra outline
                | None -> registry

    /// <summary>The styling of changelog pages. The collection produces the pages.</summary>
    let create () = ChangelogPlugin(None) :> IPlugin

    /// <summary>The styling, plus a menu built from the changelogs you publish.</summary>
    /// <param name="section">The collection's name, which is the section the menu is for.</param>
    /// <param name="sources">The same changelogs you gave the collection.</param>
    let createWith (section: string) (sources: ChangelogSource list) =
        ChangelogPlugin(Some(menu section sources)) :> IPlugin

    /// <summary>Add changelog styling to a site.</summary>
    let register (site: Site) = Site.plugin (create ()) site

    /// <summary>Adds the styling, and lets the plugin offer the menu for the section.</summary>
    /// <param name="section">The collection's name.</param>
    /// <param name="sources">The same changelogs you gave the collection.</param>
    /// <param name="site">The site you are describing.</param>
    let registerWith (section: string) (sources: ChangelogSource list) (site: Site) =
        Site.plugin (createWith section sources) site
