namespace Nacara.Plugins

open System.Diagnostics
open System.IO
open Nacara.Core
open Nacara.Plugins.Internal

/// <summary>Options of the Lightning CSS plugin.</summary>
type LightningCssOptions =
    {
        /// Path of an existing lightningcss binary. Downloaded and cached when not set.
        BinaryPath: string option
        /// <summary>Browsers to compile the CSS for, in browserslist syntax.</summary>
        /// <remarks>Decides what gets rewritten as well as what gets prefixed: name older browsers
        /// and nesting, <c>color-mix()</c> and the rest come out as something they understand.</remarks>
        Targets: string
        /// Minify while watching too. Off by default: the saving is invisible locally and it
        /// costs a process per stylesheet on every rebuild.
        MinifyWhileWatching: bool
    }

/// <summary>
/// Compiles the site's CSS for the browsers you name, and minifies it.
/// </summary>
/// <remarks>
/// <para>Minifying is the half you notice; the other half is <c>Targets</c>. Name the browsers in
/// browserslist syntax and whatever they do not understand is rewritten - nesting flattened,
/// <c>color-mix()</c> computed, prefixes added and unneeded ones dropped - so a theme can be
/// written in modern CSS with no build step of its own.</para>
/// <para>It applies to every stylesheet the build writes. If the binary cannot be found or fails,
/// the stylesheet ships unchanged and the build says so.</para>
/// </remarks>
[<RequireQualifiedAccess>]
module LightningCss =

    let defaults =
        {
            BinaryPath = None
            Targets = "defaults"
            MinifyWhileWatching = false
        }

    /// Resolved once for the life of the plugin, not once per stylesheet.
    let private binary (options: LightningCssOptions) =
        lazy
            (match options.BinaryPath with
             | Some path when File.Exists path -> Ok path
             | Some path -> Error $"no such file: '%s{path}'"
             | None -> LightningCssBinary.resolve ())

    let private run (binary: string) (options: LightningCssOptions) (content: string) =
        let input = Path.GetTempFileName() + ".css"
        let output = Path.GetTempFileName() + ".min.css"

        try
            try
                File.WriteAllText(input, content)

                let startInfo =
                    ProcessStartInfo(
                        binary,
                        $"--minify --targets \"%s{options.Targets}\" --output-file \"%s{output}\" \"%s{input}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    )

                use lightning = Process.Start startInfo
                let error = lightning.StandardError.ReadToEnd()
                lightning.StandardOutput.ReadToEnd() |> ignore
                lightning.WaitForExit()

                if lightning.ExitCode = 0 && File.Exists output then
                    Ok(File.ReadAllText output)
                else
                    Error(error.Trim())
            with exn ->
                Error exn.Message
        finally
            for file in
                [
                    input
                    output
                ] do
                try
                    File.Delete file
                with _ ->
                    ()

    let private bundle (binary: string) (entry: string) =
        let output = Path.GetTempFileName() + ".css"

        try
            try
                let startInfo =
                    ProcessStartInfo(
                        binary,
                        $"--bundle --output-file \"%s{output}\" \"%s{entry}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    )

                use lightning = Process.Start startInfo
                let error = lightning.StandardError.ReadToEnd()
                lightning.StandardOutput.ReadToEnd() |> ignore
                lightning.WaitForExit()

                if lightning.ExitCode = 0 && File.Exists output then
                    Ok(File.ReadAllText output)
                else
                    Error(error.Trim())
            with exn ->
                Error exn.Message
        finally
            try
                File.Delete output
            with _ ->
                ()

    type private MinifyPlugin(options: LightningCssOptions) =
        let binary = binary options
        let mutable reported = false

        interface IPlugin with
            member _.Name = "lightningcss"

            member _.Configure registry =
                registry
                |> Registry.assetBundler
                    {
                        Name = "lightningcss"
                        Extensions = [ ".css" ]
                        Bundle =
                            fun context ->
                                match binary.Value with
                                | Error message -> Error message
                                | Ok binary -> bundle binary (AbsolutePath.value context.Entry)
                    }
                |> Registry.assetTransform
                    {
                        Name = "lightningcss"
                        Extensions = [ ".css" ]
                        Transform =
                            fun context ->
                                let report message =
                                    if not reported then
                                        reported <- true

                                        context.Diagnostics.Add(
                                            Diagnostic.warning
                                                "css-not-minified"
                                                $"CSS is not minified: %s{message}"
                                            |> Diagnostic.withHint
                                                "The site is built and correct, only larger. Set BinaryPath to use your own lightningcss."
                                        )

                                if context.IsWatch && not options.MinifyWhileWatching then
                                    context.Content
                                else

                                    match binary.Value with
                                    | Error message ->
                                        report message
                                        context.Content
                                    | Ok binary ->
                                        match run binary options context.Content with
                                        | Ok minified -> minified
                                        | Error message ->
                                            report
                                                $"%s{RelativePath.value context.Path} could not be minified: %s{message}"

                                            context.Content
                    }

    /// <summary>Path of an existing lightningcss binary.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let binaryPath value (options: LightningCssOptions) =
        { options with
            BinaryPath = value
        }

    /// <summary>Browsers to compile the CSS for, in browserslist syntax.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let targets value (options: LightningCssOptions) =
        { options with
            Targets = value
        }

    /// <summary>Minify while watching too.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let minifyWhileWatching value (options: LightningCssOptions) =
        { options with
            MinifyWhileWatching = value
        }

    /// <summary>Minify the site's CSS, with the default options.</summary>
    let create () = MinifyPlugin(defaults) :> IPlugin

    /// <summary>Minify the site's CSS, configured.</summary>
    /// <param name="configure">Given the defaults, the options to use. Anything it leaves alone keeps its default.</param>
    let createWith (configure: LightningCssOptions -> LightningCssOptions) =
        MinifyPlugin(configure defaults) :> IPlugin

    /// <summary>Add CSS minification to a site.</summary>
    let register (site: Site) = Site.plugin (create ()) site

    /// <summary>Add CSS minification to a site, configured.</summary>
    /// <param name="configure">Given the defaults, the options to use. Anything it leaves alone keeps its default.</param>
    /// <param name="site">The site being described.</param>
    let registerWith (configure: LightningCssOptions -> LightningCssOptions) (site: Site) =
        Site.plugin (createWith configure) site
