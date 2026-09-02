namespace Nacara.Core

open System

/// <summary>Console output for the CLI and the build.</summary>
/// <remarks>
/// Colour is disabled when the output is redirected or when <c>NO_COLOR</c> is set, so build logs
/// stay readable in CI.
/// </remarks>
[<RequireQualifiedAccess>]
module Log =

    let mutable private verbose = false

    let private useColor =
        not (Console.IsOutputRedirected)
        && isNull (Environment.GetEnvironmentVariable "NO_COLOR")

    /// <summary>Whether a path may be written as a terminal hyperlink.</summary>
    let private useHyperlinks =
        useColor && isNull (Environment.GetEnvironmentVariable "NO_HYPERLINKS")

    /// <summary>The location of a diagnostic, made clickable where that means something.</summary>
    let private linked (value: Diagnostic) (rendered: string) =
        match value.Span with
        | Some span when useHyperlinks ->
            let path = AbsolutePath.value span.File
            let shown = $"%s{path}(%i{span.Line},%i{span.Column})"

            if rendered.StartsWith shown then
                // NACARA_EDITOR_URL names the scheme, e.g. 'vscode://file/{path}:{line}:{column}'.
                let target =
                    match Environment.GetEnvironmentVariable "NACARA_EDITOR_URL" with
                    | null
                    | "" -> Uri("file://" + path).AbsoluteUri
                    | template ->
                        template
                            .Replace("{path}", path)
                            .Replace("{line}", string span.Line)
                            .Replace("{column}", string span.Column)

                let opening = $"\u001b]8;;%s{target}\u001b\\"
                let closing = "\u001b]8;;\u001b\\"
                opening + shown + closing + rendered.Substring shown.Length
            else
                rendered
        | _ -> rendered

    /// <summary>Text the terminal opens when it is clicked, where that means something.</summary>
    /// <param name="target">Where clicking it goes.</param>
    /// <param name="label">What is written.</param>
    let hyperlink (target: string) (label: string) =
        if useHyperlinks then
            $"\u001b]8;;%s{target}\u001b\\%s{label}\u001b]8;;\u001b\\"
        else
            label

    /// <summary>One line, with its mark coloured - and the rest of it too when it is a problem.</summary>
    let private write (color: ConsoleColor) (whole: bool) (prefix: string) (message: string) =
        if useColor then
            let previous = Console.ForegroundColor
            Console.ForegroundColor <- color
            Console.Write prefix

            if not whole then
                Console.ForegroundColor <- previous

            Console.WriteLine message
            Console.ForegroundColor <- previous
        else
            Console.WriteLine(prefix + message)

    /// <summary>Whether <see cref="M:Nacara.Core.Log.debug"/> writes anything.</summary>
    /// <param name="value">On when the command line said <c>--verbose</c>.</param>
    let setVerbose value = verbose <- value

    /// <summary>Whether the command line said <c>--verbose</c>.</summary>
    let isVerbose () = verbose

    /// <summary>What the build is doing, as it does it.</summary>
    /// <param name="message">One line, present tense: <c>Changed: getting-started.md</c>.</param>
    let info message =
        write ConsoleColor.Blue false "» " message

    /// <summary>Something finished, and finished well.</summary>
    /// <param name="message">One line, with the numbers worth knowing: what was built, how
    /// long it took.</param>
    let success message =
        write ConsoleColor.Green false "✓ " message

    /// <summary>Something worth saying that does not stop the build.</summary>
    /// <param name="message">One line. A problem with a page belongs in a diagnostic instead,
    /// where it carries a file and a position.</param>
    let warn message =
        write ConsoleColor.Yellow true "! " message

    /// <summary>Something went wrong.</summary>
    /// <param name="message">One line. A problem with a page belongs in a diagnostic instead,
    /// where it carries a file and a position.</param>
    let error message =
        write ConsoleColor.Red true "✗ " message

    /// <summary>Detail for whoever asked for it, and silence for everyone else.</summary>
    /// <param name="message">One line, written only under <c>--verbose</c>.</param>
    let debug message =
        if verbose then
            write ConsoleColor.DarkGray false "· " message

    /// <summary>Report a diagnostic with the severity it deserves.</summary>
    /// <param name="value">What to report. Its severity picks the colour and the mark, and its
    /// position is written the way an editor can jump to.</param>
    let diagnostic (value: Diagnostic) =
        let rendered = Diagnostic.render value |> linked value

        match value.Severity with
        | Severity.Error -> error rendered
        | Severity.Warning -> warn rendered
        | Severity.Information -> info rendered
