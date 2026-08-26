namespace Nacara.Plugins

open Nacara.Core
open NUglify
open NUglify.Html
open NUglify.JavaScript

/// <summary>Options for minifying HTML.</summary>
type NuglifyHtmlOptions =
    {
        /// <summary>Collapse runs of whitespace between elements.</summary>
        /// <remarks>The whole risk of minifying HTML lives here: a newline between two inline
        /// elements is a space the browser draws. <c>KeepOneSpace</c> is what makes collapsing
        /// safe, and turning that off is how words run together.</remarks>
        CollapseWhitespace: bool
        /// Leave one space where a run of whitespace was, so nothing the reader sees moves.
        KeepOneSpace: bool
        /// Remove HTML comments.
        RemoveComments: bool
        /// Remove quotes from attribute values that do not need them.
        RemoveAttributeQuotes: bool
        /// <summary>Remove end tags HTML5 says are optional - <c>&lt;/body&gt;</c>, <c>&lt;/li&gt;</c>.</summary>
        /// <remarks>Off by default. It is valid, and it saves a few bytes a page, but plenty of
        /// tools look for <c>&lt;/body&gt;</c> to insert something - Nacara's own dev server does -
        /// and a page without one is a surprise waiting for whoever meets it next.</remarks>
        RemoveOptionalTags: bool
        /// Minify while watching too.
        MinifyWhileWatching: bool
    }

/// <summary>Options for minifying JavaScript.</summary>
type NuglifyJsOptions =
    {
        /// <summary>Shorten the names of local variables.</summary>
        /// <remarks>Locals only. Anything reachable from outside - a custom element, a function on
        /// <c>window</c> - keeps the name it was written with, or nothing could call it.</remarks>
        ShortenNames: bool
        /// <summary>Keep comments marked as important, the <c>/*! … */</c> kind.</summary>
        /// <remarks>On: that is where a licence lives.</remarks>
        KeepLicenceComments: bool
        /// Minify while watching too.
        MinifyWhileWatching: bool
    }

/// <summary>Options for minifying CSS.</summary>
type NuglifyCssOptions =
    {
        /// Keep <c>/*! … */</c> comments, where a licence lives.
        KeepLicenceComments: bool
        /// Minify while watching too.
        MinifyWhileWatching: bool
    }

/// <summary>
/// Minifies what a site writes: its HTML, its JavaScript, its CSS.
/// </summary>
/// <remarks>
/// <para>One registration per format, so ask for the ones you want:</para>
/// <code lang="fsharp">
/// Site.create "My library"
/// |> Nuglify.minifyHtml
/// |> Nuglify.minifyJs
/// |> LightningCss.register   // the CSS, from a tool that also compiles it
/// </code>
/// <para>Each touches its own format: minifying the HTML leaves <c>style</c> and <c>script</c> as
/// it found them. Nothing is downloaded, and a file that cannot be parsed is written as it was and
/// reported rather than failing the build.</para>
/// </remarks>
[<RequireQualifiedAccess>]
module Nuglify =

    let htmlDefaults =
        {
            CollapseWhitespace = true
            KeepOneSpace = true
            RemoveComments = true
            RemoveAttributeQuotes = false
            RemoveOptionalTags = false
            MinifyWhileWatching = false
        }

    let jsDefaults =
        {
            ShortenNames = true
            KeepLicenceComments = true
            MinifyWhileWatching = false
        }

    let cssDefaults =
        {
            KeepLicenceComments = true
            MinifyWhileWatching = false
        }

    /// <summary>Minify one document, or say why it could not be.</summary>
    /// <param name="options">What the site asked for.</param>
    /// <param name="content">The document as it would have been written.</param>
    let html (options: NuglifyHtmlOptions) (content: string) =
        try
            let settings = HtmlSettings()
            settings.CollapseWhitespaces <- options.CollapseWhitespace
            settings.KeepOneSpaceWhenCollapsing <- options.KeepOneSpace
            settings.RemoveComments <- options.RemoveComments
            settings.RemoveAttributeQuotes <- options.RemoveAttributeQuotes
            settings.RemoveOptionalTags <- options.RemoveOptionalTags

            settings.MinifyCss <- false
            settings.MinifyCssAttributes <- false
            settings.MinifyJs <- false
            settings.MinifyJsAttributes <- false

            let result = Uglify.Html(content, settings)

            if result.HasErrors then
                result.Errors
                |> Seq.map (fun error -> $"line %i{error.StartLine}: %s{error.Message}")
                |> String.concat "; "
                |> Error
            else
                Ok result.Code
        with exn ->
            Error exn.Message

    /// <summary>Minify one script, or say why it could not be.</summary>
    /// <param name="options">What the site asked for.</param>
    /// <param name="content">The script as it would have been written.</param>
    let js (options: NuglifyJsOptions) (content: string) =
        try
            let settingsFor mode =
                let settings = CodeSettings(SourceMode = mode)

                settings.LocalRenaming <-
                    if options.ShortenNames then
                        LocalRenaming.CrunchAll
                    else
                        LocalRenaming.KeepAll

                settings.PreserveImportantComments <- options.KeepLicenceComments

                // Without this, automatic semicolon insertion makes two files concatenated by a CDN stop being two files.
                settings.TermSemicolons <- true
                settings

            // A module's top level is its own scope, so a name asked to be kept is a local there and gets shortened anyway.
            let asScript = Uglify.Js(content, settingsFor JavaScriptSourceMode.Program)

            let result =
                if asScript.HasErrors then
                    Uglify.Js(content, settingsFor JavaScriptSourceMode.Module)
                else
                    asScript

            if result.HasErrors then
                result.Errors
                |> Seq.map (fun error -> $"line %i{error.StartLine}: %s{error.Message}")
                |> String.concat "; "
                |> Error
            else
                Ok result.Code
        with exn ->
            Error exn.Message

    /// <summary>Minify one stylesheet, or say why it could not be.</summary>
    /// <remarks>
    /// This minifies and stops there. <see cref="T:Nacara.Plugins.LightningCss" /> also compiles -
    /// nesting flattened, <c>color-mix()</c> computed, prefixes added for the browsers you name - so a
    /// site written in modern CSS wants that one instead. Register one or the other, never both.
    /// </remarks>
    /// <param name="options">What the site asked for.</param>
    /// <param name="content">The stylesheet as it would have been written.</param>
    let css (options: NuglifyCssOptions) (content: string) =
        try
            let settings = NUglify.Css.CssSettings()

            settings.CommentMode <-
                if options.KeepLicenceComments then
                    NUglify.Css.CssComment.Important
                else
                    NUglify.Css.CssComment.None

            let result = Uglify.Css(content, settings)

            if result.HasErrors then
                result.Errors
                |> Seq.map (fun error -> $"line %i{error.StartLine}: %s{error.Message}")
                |> String.concat "; "
                |> Error
            else
                Ok result.Code
        with exn ->
            Error exn.Message

    /// <summary>Collapse runs of whitespace between elements.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let htmlCollapseWhitespace value (options: NuglifyHtmlOptions) =
        { options with
            CollapseWhitespace = value
        }

    /// <summary>Leave one space where a run of whitespace was, so nothing the reader sees moves.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let htmlKeepOneSpace value (options: NuglifyHtmlOptions) =
        { options with
            KeepOneSpace = value
        }

    /// <summary>Remove HTML comments.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let htmlRemoveComments value (options: NuglifyHtmlOptions) =
        { options with
            RemoveComments = value
        }

    /// <summary>Remove quotes from attribute values that do not need them.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let htmlRemoveAttributeQuotes value (options: NuglifyHtmlOptions) =
        { options with
            RemoveAttributeQuotes = value
        }

    /// <summary>Remove end tags HTML5 says are optional - <c>&lt;/body&gt;</c>, <c>&lt;/li&gt;</c>.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let htmlRemoveOptionalTags value (options: NuglifyHtmlOptions) =
        { options with
            RemoveOptionalTags = value
        }

    /// <summary>Minify while watching too.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let htmlMinifyWhileWatching value (options: NuglifyHtmlOptions) =
        { options with
            MinifyWhileWatching = value
        }

    /// <summary>Shorten the names of local variables.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let jsShortenNames value (options: NuglifyJsOptions) =
        { options with
            ShortenNames = value
        }

    /// <summary>Keep comments marked as important, the <c>/*! … */</c> kind.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let jsKeepLicenceComments value (options: NuglifyJsOptions) =
        { options with
            KeepLicenceComments = value
        }

    /// <summary>Minify while watching too.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let jsMinifyWhileWatching value (options: NuglifyJsOptions) =
        { options with
            MinifyWhileWatching = value
        }

    /// <summary>Keep <c>/*! … */</c> comments, where a licence lives.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let cssKeepLicenceComments value (options: NuglifyCssOptions) =
        { options with
            KeepLicenceComments = value
        }

    /// <summary>Minify while watching too.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let cssMinifyWhileWatching value (options: NuglifyCssOptions) =
        { options with
            MinifyWhileWatching = value
        }

    /// One registration per format, each claiming its own extension and saying so under its own name.
    type private MinifyPlugin
        (
            name: string,
            extension: string,
            whileWatching: bool,
            minify: string -> Result<string, string>
        )
        =
        interface IPlugin with
            member _.Name = name

            member _.Configure registry =
                registry
                |> Registry.assetTransform
                    {
                        Name = name
                        Extensions = [ extension ]
                        Transform =
                            fun context ->
                                if context.IsWatch && not whileWatching then
                                    context.Content
                                else

                                    match minify context.Content with
                                    | Ok minified -> minified
                                    | Error message ->
                                        context.Diagnostics.Add(
                                            Diagnostic.warning
                                                "not-minified"
                                                $"%s{RelativePath.value context.Path} is not minified: %s{message}"
                                            |> Diagnostic.withHint
                                                "It is written as it was, and the site is correct - only larger."
                                        )

                                        context.Content
                    }

    /// <summary>Minify the site's HTML.</summary>
    /// <param name="site">The site you are describing.</param>
    let minifyHtml (site: Site) =
        Site.plugin
            (MinifyPlugin(
                "nuglify-html",
                ".html",
                htmlDefaults.MinifyWhileWatching,
                html htmlDefaults
            ))
            site

    /// <summary>Minify the site's HTML, configured.</summary>
    /// <param name="configure">Given the defaults, the options to use.</param>
    /// <param name="site">The site you are describing.</param>
    let minifyHtmlWith (configure: NuglifyHtmlOptions -> NuglifyHtmlOptions) (site: Site) =
        let options = configure htmlDefaults

        Site.plugin
            (MinifyPlugin("nuglify-html", ".html", options.MinifyWhileWatching, html options))
            site

    /// <summary>Minify the site's JavaScript.</summary>
    /// <param name="site">The site you are describing.</param>
    let minifyJs (site: Site) =
        Site.plugin
            (MinifyPlugin("nuglify-js", ".js", jsDefaults.MinifyWhileWatching, js jsDefaults))
            site

    /// <summary>Minify the site's JavaScript, configured.</summary>
    /// <param name="configure">Given the defaults, the options to use.</param>
    /// <param name="site">The site you are describing.</param>
    let minifyJsWith (configure: NuglifyJsOptions -> NuglifyJsOptions) (site: Site) =
        let options = configure jsDefaults

        Site.plugin
            (MinifyPlugin("nuglify-js", ".js", options.MinifyWhileWatching, js options))
            site

    /// <summary>Minify the site's CSS.</summary>
    /// <remarks>Use this or <c>LightningCss.register</c>, not both.</remarks>
    /// <param name="site">The site you are describing.</param>
    let minifyCss (site: Site) =
        Site.plugin
            (MinifyPlugin("nuglify-css", ".css", cssDefaults.MinifyWhileWatching, css cssDefaults))
            site

    /// <summary>Minify the site's CSS, configured.</summary>
    /// <param name="configure">Given the defaults, the options to use.</param>
    /// <param name="site">The site you are describing.</param>
    let minifyCssWith (configure: NuglifyCssOptions -> NuglifyCssOptions) (site: Site) =
        let options = configure cssDefaults

        Site.plugin
            (MinifyPlugin("nuglify-css", ".css", options.MinifyWhileWatching, css options))
            site
