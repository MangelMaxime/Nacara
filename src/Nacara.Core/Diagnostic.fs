namespace Nacara.Core

open System.Collections.Generic

[<RequireQualifiedAccess>]
type Severity =
    | Error
    | Warning
    | Information

/// <summary>A location inside a source file - 1-based.</summary>
type SourceSpan =
    {
        File: AbsolutePath
        Line: int
        Column: int
    }

/// <summary>
/// Anything worth telling the user about, from any phase of the build.
/// </summary>
/// <remarks><c>Code</c> says who is reporting and which rule; <c>Hint</c> is what turns an error
/// into a fix.</remarks>
type Diagnostic =
    {
        /// <summary>
        /// The source that reported this, then the rule: <c>markdown/link-target-missing</c>.
        /// </summary>
        /// <remarks><see cref="T:Nacara.Core.DiagnosticSink" /> stamps the source, so you cannot forget
        /// it.</remarks>
        Code: string
        Severity: Severity
        Message: string
        Hint: string option
        Span: SourceSpan option
    }

[<RequireQualifiedAccess>]
module Diagnostic =

    let private create severity code message =
        {
            Code = code
            Severity = severity
            Message = message
            Hint = None
            Span = None
        }

    /// <summary>Something the build cannot be right about: it fails.</summary>
    /// <param name="code">The rule that was broken, in kebab case and without a prefix, like
    /// <c>assembly-missing</c>. Your plugin's name is stamped in front of it.</param>
    /// <param name="message">What went wrong, in one line, naming the thing it went wrong
    /// about.</param>
    let error code message = create Severity.Error code message

    /// <summary>Something worth saying, which <c>check</c> turns into a failure.</summary>
    /// <param name="code">The rule that was broken, in kebab case and without a prefix.</param>
    /// <param name="message">What is wrong, in one line.</param>
    let warning code message = create Severity.Warning code message

    /// <summary>Something worth knowing, which never fails a build.</summary>
    /// <param name="code">The rule it is about, in kebab case and without a prefix.</param>
    /// <param name="message">What is worth knowing, in one line.</param>
    let information code message =
        create Severity.Information code message

    /// <summary>Say where it happened, so an editor can jump there.</summary>
    /// <param name="file">The file it is about.</param>
    /// <param name="line">Counting from 1.</param>
    /// <param name="column">Counting from 1.</param>
    /// <param name="diagnostic">What is being reported.</param>
    let at (file: AbsolutePath) (line: int) (column: int) (diagnostic: Diagnostic) =
        { diagnostic with
            Span =
                Some
                    {
                        File = file
                        Line = line
                        Column = column
                    }
        }

    /// <summary>Say which file it is about, when nothing more precise is known.</summary>
    /// <param name="file">The file it is about; it is reported at its first line.</param>
    /// <param name="diagnostic">What is being reported.</param>
    let inFile (file: AbsolutePath) (diagnostic: Diagnostic) = at file 1 1 diagnostic

    /// <summary>Add what to do about it, printed under the message.</summary>
    /// <param name="hint">What to do next, said plainly - which option to set, which package to
    /// add.</param>
    /// <param name="diagnostic">What is being reported.</param>
    let withHint hint (diagnostic: Diagnostic) =
        { diagnostic with
            Hint = Some(hint: string)
        }

    let private severityLabel =
        function
        | Severity.Error -> "error"
        | Severity.Warning -> "warning"
        | Severity.Information -> "info"

    /// <summary>
    /// Render in the shape editors and CI already understand:
    /// <c>path/to/file.md(3,1): error markdown/link-target-missing: message</c>.
    /// </summary>
    /// <param name="diagnostic">What to write out.</param>
    let render (diagnostic: Diagnostic) =
        let location =
            match diagnostic.Span with
            | Some span -> $"%s{AbsolutePath.value span.File}(%i{span.Line},%i{span.Column}): "
            | None -> ""

        let hint =
            match diagnostic.Hint with
            | Some hint -> $"\n    hint: %s{hint}"
            | None -> ""

        $"%s{location}%s{severityLabel diagnostic.Severity} %s{diagnostic.Code}: %s{diagnostic.Message}%s{hint}"

/// <summary>
/// Collects diagnostics during a build.
/// </summary>
/// <remarks>Locked, and <see cref="M:Nacara.Core.DiagnosticBag.ToList" /> sorts what comes
/// out.</remarks>
type DiagnosticBag() =
    let gate = obj ()
    let items = ResizeArray<Diagnostic>()

    member _.Add(diagnostic: Diagnostic) =
        lock gate (fun () -> items.Add diagnostic)

    member this.AddRange(diagnostics: Diagnostic seq) = diagnostics |> Seq.iter this.Add

    member _.HasErrors =
        lock gate (fun () -> items |> Seq.exists (fun item -> item.Severity = Severity.Error))

    member _.Count = lock gate (fun () -> items.Count)

    /// <summary>All diagnostics, ordered by file then position then code.</summary>
    member _.ToList() : IReadOnlyList<Diagnostic> =
        lock
            gate
            (fun () ->
                items
                |> Seq.sortBy (fun item ->
                    let file =
                        item.Span
                        |> Option.map (fun span -> AbsolutePath.value span.File)
                        |> Option.defaultValue ""

                    let line = item.Span |> Option.map _.Line |> Option.defaultValue 0
                    let column = item.Span |> Option.map _.Column |> Option.defaultValue 0
                    file, line, column, item.Code
                )
                |> Seq.toList
                :> IReadOnlyList<Diagnostic>
            )

/// <summary>
/// Adds diagnostics on behalf of one part of the build.
/// </summary>
/// <remarks>A plugin writes the rule it is reporting - <c>"link-target-missing"</c> - and the
/// source it belongs to is stamped here, from the plugin's own name.</remarks>
type DiagnosticSink(bag: DiagnosticBag, source: string) =

    /// <summary>What gets stamped onto a rule: a plugin's name, or <c>nacara</c>.</summary>
    member _.Source = source

    /// <summary>The same collection of diagnostics, reported by someone else.</summary>
    member _.For(source: string) = DiagnosticSink(bag, source)

    member _.Add(diagnostic: Diagnostic) =
        bag.Add(
            // Core writes whole codes, since it is the one source that never moves.
            if diagnostic.Code.Contains "/" then
                diagnostic
            else
                { diagnostic with
                    Code = $"%s{source}/%s{diagnostic.Code}"
                }
        )

    member this.AddRange(diagnostics: Diagnostic seq) = diagnostics |> Seq.iter this.Add
