namespace Nacara.Plugins

open Feliz.ViewEngine
open Nacara.Core

/// <summary>What a <c>:::name</c> directive of your own is given.</summary>
type DirectiveContext =
    {
        Name: string
        /// The text after the name on the opening line, quotes removed.
        Argument: string option
        /// <summary>From <c>{key=value}</c>, <c>{.class}</c> and <c>{#id}</c> on the opening line.
        /// Classes are under <c>class</c>, space-separated, and the id under <c>id</c>.</summary>
        Attributes: Map<string, string>
        /// The markdown between the opening and closing lines, rendered.
        Content: string
        /// The page the directive is on.
        Page: Page
        Site: SiteInfo
        /// <summary>Adds a diagnostic at the directive's opening line. <c>Warn</c> and
        /// <c>Error</c> call it.</summary>
        Report: Diagnostic -> unit
    }

    member this.Attribute(name: string) = Map.tryFind name this.Attributes

    /// <summary>Reports a warning at the directive's opening line, with the code
    /// <c>directive-&lt;name&gt;</c>.</summary>
    /// <param name="message">What is wrong.</param>
    /// <param name="hint">How to fix it.</param>
    member this.Warn(message: string, ?hint: string) =
        this.Report(
            Diagnostic.warning $"directive-%s{this.Name.ToLowerInvariant()}" message
            |> fun diagnostic ->
                match hint with
                | Some hint -> Diagnostic.withHint hint diagnostic
                | None -> diagnostic
        )

    /// <summary>Reports an error at the directive's opening line, with the code
    /// <c>directive-&lt;name&gt;</c>. The build fails, and the page is still rendered.</summary>
    /// <param name="message">What is wrong.</param>
    /// <param name="hint">How to fix it.</param>
    member this.Error(message: string, ?hint: string) =
        this.Report(
            Diagnostic.error $"directive-%s{this.Name.ToLowerInvariant()}" message
            |> fun diagnostic ->
                match hint with
                | Some hint -> Diagnostic.withHint hint diagnostic
                | None -> diagnostic
        )

    /// <summary><c>Content</c> as an element, with no element around it.</summary>
    member this.Body = Interop.createRawTextElement this.Content

/// <summary>A <c>:::name</c> directive of your own.</summary>
type MarkdownDirective =
    {
        /// What follows the colons. Matched ignoring case.
        Name: string
        Render: DirectiveContext -> ReactElement
    }
