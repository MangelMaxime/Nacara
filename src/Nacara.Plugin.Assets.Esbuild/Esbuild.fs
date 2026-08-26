namespace Nacara.Plugins

open System.Diagnostics
open System.IO
open Nacara.Core
open Nacara.Plugins.Internal

/// <summary>Options of the esbuild plugin.</summary>
type EsbuildOptions =
    {
        /// Path of an existing esbuild binary. Downloaded and cached when not set.
        BinaryPath: string option
        /// Minify while watching too. Off by default: the saving is invisible locally and it costs
        /// a process per script on every rebuild.
        MinifyWhileWatching: bool
    }

/// <summary>
/// Resolves what a script imports into one file.
/// </summary>
/// <remarks>
/// <para>A script the site registers is loaded as a classic script, so an <c>import</c> in one is a
/// syntax error rather than something the browser resolves. Registering multi-file JavaScript
/// therefore needs this plugin, where the same for CSS does not.</para>
/// <para>Minifying is left to whichever plugin claims <c>.js</c>.</para>
/// </remarks>
[<RequireQualifiedAccess>]
module Esbuild =

    let defaults =
        {
            BinaryPath = None
            MinifyWhileWatching = false
        }

    /// Resolved once for the life of the plugin, not once per script.
    let private binary (options: EsbuildOptions) =
        lazy
            (match options.BinaryPath with
             | Some path when File.Exists path -> Ok path
             | Some path -> Error $"no such file: '%s{path}'"
             | None -> EsbuildBinary.resolve ())

    let private bundle (binary: string) (entry: string) =
        let output = Path.GetTempFileName() + ".js"

        try
            try
                let startInfo =
                    ProcessStartInfo(
                        binary,
                        $"\"%s{entry}\" --bundle --format=iife --outfile=\"%s{output}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    )

                use esbuild = Process.Start startInfo
                let error = esbuild.StandardError.ReadToEnd()
                esbuild.StandardOutput.ReadToEnd() |> ignore
                esbuild.WaitForExit()

                if esbuild.ExitCode = 0 && File.Exists output then
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

    let private minify (binary: string) (content: string) =
        let input = Path.GetTempFileName() + ".js"
        let output = Path.GetTempFileName() + ".min.js"

        try
            try
                File.WriteAllText(input, content)

                let startInfo =
                    ProcessStartInfo(
                        binary,
                        $"\"%s{input}\" --minify --outfile=\"%s{output}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    )

                use esbuild = Process.Start startInfo
                let error = esbuild.StandardError.ReadToEnd()
                esbuild.StandardOutput.ReadToEnd() |> ignore
                esbuild.WaitForExit()

                if esbuild.ExitCode = 0 && File.Exists output then
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

    type private EsbuildPlugin(options: EsbuildOptions) =
        let binary = binary options
        let mutable reported = false

        interface IPlugin with
            member _.Name = "esbuild"

            member _.Configure registry =
                registry
                |> Registry.assetBundler
                    {
                        Name = "esbuild"
                        Extensions = [ ".js" ]
                        Bundle =
                            fun context ->
                                match binary.Value with
                                | Error message -> Error message
                                | Ok binary -> bundle binary (AbsolutePath.value context.Entry)
                    }
                |> Registry.assetTransform
                    {
                        Name = "esbuild"
                        Extensions = [ ".js" ]
                        Transform =
                            fun context ->
                                let report message =
                                    if not reported then
                                        reported <- true

                                        context.Diagnostics.Add(
                                            Diagnostic.warning
                                                "js-not-minified"
                                                $"JavaScript is not minified: %s{message}"
                                            |> Diagnostic.withHint
                                                "The site is built and correct, only larger. Set BinaryPath to use your own esbuild."
                                        )

                                if context.IsWatch && not options.MinifyWhileWatching then
                                    context.Content
                                else

                                    match binary.Value with
                                    | Error message ->
                                        report message
                                        context.Content
                                    | Ok binary ->
                                        match minify binary context.Content with
                                        | Ok minified -> minified
                                        | Error message ->
                                            report message
                                            context.Content
                    }

    /// <summary>Bundle the site's JavaScript, with the default options.</summary>
    let create () = EsbuildPlugin(defaults) :> IPlugin

    /// <summary>Bundle the site's JavaScript, configured.</summary>
    /// <param name="configure">Given the defaults, the options to use. Anything it leaves alone keeps its default.</param>
    let createWith (configure: EsbuildOptions -> EsbuildOptions) =
        EsbuildPlugin(configure defaults) :> IPlugin

    /// <summary>Add JavaScript bundling to a site.</summary>
    let register (site: Site) = Site.plugin (create ()) site

    /// <summary>Add JavaScript bundling to a site, configured.</summary>
    /// <param name="configure">Given the defaults, the options to use. Anything it leaves alone keeps its default.</param>
    /// <param name="site">The site being described.</param>
    let registerWith (configure: EsbuildOptions -> EsbuildOptions) (site: Site) =
        Site.plugin (createWith configure) site
