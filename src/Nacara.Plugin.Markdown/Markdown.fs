namespace Nacara.Plugins

open System
open System.IO
open Markdig
open Markdig.Extensions.AutoIdentifiers
open Markdig.Renderers
open Markdig.Renderers.Html
open Markdig.Syntax
open Markdig.Syntax.Inlines
open Nacara.Core
open Nacara.Plugins.Internal

/// <summary>A link this page points at, kept for the whole-site checks.</summary>
type internal OutgoingLink =
    {
        TargetPageId: string option
        RawUrl: string
        Anchor: string option
    }

/// <summary>Options of the markdown plugin.</summary>
type MarkdownOptions =
    {
        /// Repository used to expand issue and commit references, for example <c>MangelMaxime/Nacara</c>.
        GithubRepo: string option
        /// Heading levels collected into the table of contents, for pages that do not say.
        Toc: TocRange
        /// Fail the build on a link that points at no page. Warn instead when false.
        StrictLinks: bool
        /// <summary>Report a fence naming a language no highlighter covers.</summary>
        /// <remarks>The engine knows which fence it was and where, so this is where it is said -
        /// a highlighter only ever sees a language and a piece of code.</remarks>
        WarnOnUnknownLanguage: bool
    }

[<RequireQualifiedAccess>]
module Markdown =

    [<Literal>]
    let private LinksKey = "markdown.links"

    let private AnchorsKey = "markdown.anchors"

    let defaults =
        {
            GithubRepo = None
            Toc =
                {
                    From = 2
                    To = 3
                }
            StrictLinks = true
            WarnOnUnknownLanguage = true
        }

    /// <summary>How a markdown file carries its front matter: a block at the top of the file.</summary>
    let frontMatterFormat =
        {
            Name = "markdown"
            Extensions =
                [
                    ".md"
                    ".markdown"
                ]
            Opening = "---"
            Closing = "---"
            Wrapper = None
        }

    /// One pipeline for the whole build. Extensions come from the registry, which does not change
    /// once plugins are configured, so it is built when the list it was built from is not the one
    /// being asked about.
    let private built = obj ()
    let mutable private cached: (obj * MarkdownPipeline) option = None

    let private build (extensions: IMarkdownExtension list) =
        let builder =
            (MarkdownPipelineBuilder()
                // Markdig's default AllowOnlyAscii turns a heading with no ASCII in it into 'section', 'section-1', ...
                .UseAutoIdentifiers(AutoIdentifierOptions.AutoLink)
                .UsePipeTables()
                .UseGridTables()
                .UseTaskLists()
                .UseFootnotes()
                .UseAutoLinks()
                .UseCustomContainers()
                .UseEmphasisExtras()
                .UseDefinitionLists()
                .UseMediaLinks()
                // Last, as Markdig asks: adding it changes the parsers already in the builder.
                .UseGenericAttributes())

        for extension in extensions do
            if not (builder.Extensions.Contains extension) then
                builder.Extensions.Add extension

        builder.Build()

    /// <summary>The pipeline every page of this build is parsed and rendered with.</summary>
    /// <param name="registry">What plugins contributed, read for <c>IMarkdownExtension</c>.</param>
    let private pipelineFor (registry: Registry) =
        let extensions = Registry.extras<IMarkdownExtension> registry

        lock
            built
            (fun () ->
                match cached with
                | Some(previous, pipeline) when obj.ReferenceEquals(previous, extensions) ->
                    pipeline
                | _ ->
                    let pipeline = build extensions
                    cached <- Some(box extensions, pipeline)
                    pipeline
            )

    let private plainText (block: LeafBlock) =
        if isNull block.Inline then
            ""
        else
            block.Inline.Descendants()
            |> Seq.cast<MarkdownObject>
            |> Seq.choose (
                function
                | :? LiteralInline as literal -> Some(literal.Content.ToString())
                | :? CodeInline as code -> Some code.Content
                | _ -> None
            )
            |> String.concat ""

    let private isExternal (url: string) =
        url.Contains "://"
        || url.StartsWith "//"
        || url.StartsWith "mailto:"
        || url.StartsWith "tel:"

    /// <summary>
    /// Point a markdown link at the page it means.
    /// </summary>
    let private resolveLink (context: TransformContext) (page: Page) (url: string) =
        let path, anchor =
            match url.IndexOf '#' with
            | -1 -> url, None
            | index -> url.Substring(0, index), Some(url.Substring(index + 1))

        if path = "" then
            Ok(
                url,
                {
                    TargetPageId = Some page.Id
                    RawUrl = url
                    Anchor = anchor
                }
            )
        else

            let extension = Path.GetExtension(path: string).ToLowerInvariant()

            match page.SourceFile with
            | None ->
                Ok(
                    url,
                    {
                        TargetPageId = None
                        RawUrl = url
                        Anchor = anchor
                    }
                )
            | Some source ->
                let target =
                    if path.StartsWith "/" then
                        AbsolutePath.combine context.ProjectRoot [ path.TrimStart '/' ]
                    else
                        AbsolutePath.combine (AbsolutePath.directory source) [ path ]

                let generated () =
                    let wanted = "/" + (AbsolutePath.value target).Replace("\\", "/")

                    context.Pages
                    |> List.tryFind (fun candidate ->
                        candidate.SourceFile.IsNone
                        && wanted.EndsWith("/" + candidate.Id.Replace(":", "/"))
                    )

                match
                    context.Pages
                    |> List.tryFind (fun candidate -> candidate.SourceFile = Some target)
                    |> Option.orElseWith generated
                with
                | Some found ->
                    let anchorPart =
                        anchor |> Option.map (fun anchor -> "#" + anchor) |> Option.defaultValue ""

                    Ok(
                        context.Site.UrlOf found.Route + anchorPart,
                        {
                            TargetPageId = Some found.Id
                            RawUrl = url
                            Anchor = anchor
                        }
                    )
                | None ->
                    if extension <> ".md" && extension <> ".markdown" && extension <> "" then
                        Ok(
                            url,
                            {
                                TargetPageId = None
                                RawUrl = url
                                Anchor = anchor
                            }
                        )
                    elif AbsolutePath.exists target then
                        Ok(
                            url,
                            {
                                TargetPageId = None
                                RawUrl = url
                                Anchor = anchor
                            }
                        )
                    else
                        Error url

    let private transform (options: MarkdownOptions) (context: TransformContext) (page: Page) =
        let pipeline = pipelineFor context.Registry

        // Markdig counts the body it was given; the file also has the front matter above it, and a preview nests.
        let nested = ref 0

        let inFile line = page.BodyLine + line + nested.Value
        let document = Markdig.Markdown.Parse(page.Body, pipeline)
        let links = ResizeArray<OutgoingLink>()

        for link in document.Descendants<LinkInline>() do
            if not link.IsImage && not (isNull link.Url) && not (isExternal link.Url) then
                match resolveLink context page link.Url with
                | Ok(url, outgoing) ->
                    link.Url <- url
                    links.Add outgoing
                | Error url ->
                    let diagnostic =
                        (if options.StrictLinks then
                             Diagnostic.error
                         else
                             Diagnostic.warning)
                            "link-target-missing"
                            $"This link points at an unknown page '%s{url}'"
                        |> Diagnostic.withHint
                            "A link names a file: one beside this page, or one from the project root with a leading '/'. A generated page answers to its collection and path, like 'changelog/core.md'."

                    match page.SourceFile with
                    | Some file ->
                        context.Diagnostics.Add(
                            diagnostic |> Diagnostic.at file (inFile link.Line) link.Column
                        )
                    | None -> context.Diagnostics.Add diagnostic

        let toc = page.TryData<TocRange> PageData.Toc |> Option.defaultValue options.Toc

        let headings =
            document.Descendants<HeadingBlock>()
            |> Seq.filter (fun heading -> toc.Covers heading.Level)
            |> Seq.map (fun heading ->
                {
                    Level = heading.Level
                    Text = plainText heading
                    Anchor =
                        let attributes = heading.GetAttributes()

                        if isNull attributes.Id then
                            Slug.create (plainText heading)
                        else
                            attributes.Id
                }
            )
            |> List.ofSeq

        let anchors =
            document.Descendants()
            |> Seq.choose (fun node ->
                match node.TryGetAttributes() with
                | null -> None
                | attributes -> Option.ofObj attributes.Id
            )
            |> Set.ofSeq

        let highlighters = Registry.extras<IHighlighter> context.Registry
        let codeRenderers = Registry.extras<ICodeBlockRenderer> context.Registry

        let html =
            use writer = new StringWriter()
            let renderer = HtmlRenderer(writer)
            pipeline.Setup(renderer :> IMarkdownRenderer)

            renderer.ObjectRenderers.Replace<Markdig.Renderers.Html.HeadingRenderer>(
                NacaraHeadingRenderer()
            )
            |> ignore

            let reportUnknownLanguage what language line =
                if options.WarnOnUnknownLanguage then
                    let diagnostic =
                        Diagnostic.warning
                            "unknown-language"
                            $"No highlighter knows '%s{language}', so this %s{what} is rendered without colour"
                        |> Diagnostic.withHint
                            "Check the spelling, register a highlighter that covers it, or label it 'text' when it is not code"

                    match page.SourceFile with
                    | Some file ->
                        context.Diagnostics.Add(diagnostic |> Diagnostic.at file (inFile line) 1)
                    | None -> context.Diagnostics.Add diagnostic

            let runChecks (block: CodeBlock) line =
                for check in context.Registry.CodeBlockChecks do
                    check.Check
                        {
                            Block = block
                            Source = page.SourceFile
                            Line = inFile line
                            Diagnostics = context.Diagnostics
                        }

            renderer.ObjectRenderers.Replace<Markdig.Renderers.Html.CodeBlockRenderer>(
                NacaraCodeBlockRenderer(
                    highlighters,
                    codeRenderers,
                    reportUnknownLanguage "block",
                    runChecks
                )
            )
            |> ignore

            renderer.ObjectRenderers.Replace<Markdig.Renderers.Html.Inlines.CodeInlineRenderer>(
                NacaraCodeInlineRenderer(highlighters, reportUnknownLanguage "snippet")
            )
            |> ignore

            renderer.ObjectRenderers.Replace<
                Markdig.Extensions.CustomContainers.HtmlCustomContainerRenderer
             >(
                NacaraContainerRenderer(
                    (fun code message hint line column ->
                        let diagnostic =
                            Diagnostic.warning code message |> Diagnostic.withHint hint

                        match page.SourceFile with
                        | Some file ->
                            context.Diagnostics.Add(
                                diagnostic |> Diagnostic.at file (inFile line) (column + 1)
                            )
                        | None -> context.Diagnostics.Add diagnostic
                    ),
                    lazy pipeline,
                    nested
                )
            )
            |> ignore

            renderer.Render document |> ignore
            writer.Flush()
            writer.ToString()

        { page with
            Html = html
            Headings = headings
        }
            .WithData(LinksKey, List.ofSeq links)
            .WithData(AnchorsKey, anchors)

    /// <summary>Check that every in-page anchor a link points at actually exists.</summary>
    let private checkAnchors (options: MarkdownOptions) (context: HookContext) =
        let byId = context.Pages |> List.map (fun page -> page.Id, page) |> Map.ofList

        let anchorsOf (page: Page) =
            match page.TryData<Set<string>> AnchorsKey with
            | Some anchors -> anchors
            | None -> page.Headings |> List.map _.Anchor |> Set.ofList

        for page in context.Pages do
            for link in page.TryData<OutgoingLink list> LinksKey |> Option.defaultValue [] do
                match link.TargetPageId, link.Anchor with
                | Some targetId, Some anchor ->
                    match Map.tryFind targetId byId with
                    | Some target when not ((anchorsOf target).Contains anchor) ->
                        let diagnostic =
                            (if options.StrictLinks then
                                 Diagnostic.error
                             else
                                 Diagnostic.warning)
                                "anchor-missing"
                                $"'%s{link.RawUrl}' points at an anchor that does not exist on the target page"

                        match page.SourceFile with
                        | Some file -> context.Diagnostics.Add(diagnostic |> Diagnostic.inFile file)
                        | None -> context.Diagnostics.Add diagnostic
                    | _ -> ()
                | _ -> ()

    type private MarkdownPlugin(options: MarkdownOptions) =
        interface IPlugin with
            member _.Name = "markdown"

            member _.Configure registry =
                registry
                |> Registry.frontMatter frontMatterFormat
                |> Registry.transform
                    {
                        Name = "markdown"
                        Extensions =
                            [
                                ".md"
                                ".markdown"
                            ]
                        Transform = transform options
                    }
                |> Registry.onPagesRouted (checkAnchors options)

    /// <summary>Repository used to expand issue and commit references, for example <c>MangelMaxime/Nacara</c>.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let githubRepo value (options: MarkdownOptions) =
        { options with
            GithubRepo = value
        }

    /// <summary>Heading levels collected into the table of contents, for pages that do not say.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let toc value (options: MarkdownOptions) =
        { options with
            Toc = value
        }

    /// <summary>Fail the build on a link that points at no page.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let strictLinks value (options: MarkdownOptions) =
        { options with
            StrictLinks = value
        }

    /// <summary>Report a fence naming a language no highlighter covers.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let warnOnUnknownLanguage value (options: MarkdownOptions) =
        { options with
            WarnOnUnknownLanguage = value
        }

    /// <summary>The markdown plugin, with its default options.</summary>
    let create () = MarkdownPlugin(defaults) :> IPlugin

    /// <summary>The markdown plugin, configured.</summary>
    /// <param name="configure">Given the defaults, the options to use. Anything it leaves alone keeps its default.</param>
    let createWith (configure: MarkdownOptions -> MarkdownOptions) =
        MarkdownPlugin(configure defaults) :> IPlugin

    /// <summary>Add markdown to a site.</summary>
    /// <example>
    /// <code lang="fsharp">
    /// Site.create "Docs" |> Markdown.register
    /// </code>
    /// </example>
    let register (site: Site) = Site.plugin (create ()) site

    /// <summary>Add markdown to a site, configured.</summary>
    /// <param name="configure">Given the defaults, the options to use. Anything it leaves alone keeps its default.</param>
    /// <param name="site">The site being described.</param>
    let registerWith (configure: MarkdownOptions -> MarkdownOptions) (site: Site) =
        Site.plugin (createWith configure) site
