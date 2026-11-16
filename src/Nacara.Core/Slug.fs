namespace Nacara.Core

open System
open System.Text
open System.Globalization

/// <summary>
/// Turns arbitrary text into a URL segment.
/// </summary>
/// <remarks>
/// The same text always gives the same segment, and every segment is safe in a path. Diacritics
/// are folded, so "Créer" becomes "creer" rather than losing the letter altogether. A <c>#</c>
/// after a letter or a digit is spelled <c>sharp</c>, so "F#" becomes "fsharp".
/// </remarks>
[<RequireQualifiedAccess>]
module Slug =

    let private foldDiacritics (text: string) =
        text.Normalize(NormalizationForm.FormD)
        |> Seq.filter (fun char ->
            CharUnicodeInfo.GetUnicodeCategory char <> UnicodeCategory.NonSpacingMark
        )
        |> Seq.toArray
        |> String
        |> _.Normalize(NormalizationForm.FormC)

    /// <summary>Create a lowercase, dash-separated slug from <paramref name="text" />.</summary>
    /// <example>
    /// <code lang="fsharp">
    /// Slug.create "Get started!" // "get-started"
    /// Slug.create "Nacara.Core"  // "nacara-core"
    /// Slug.create "F# tips"      // "fsharp-tips"
    /// </code>
    /// </example>
    let create (text: string) =
        let builder = StringBuilder(text.Length)

        (foldDiacritics text).ToLowerInvariant()
        |> Seq.iter (fun char ->
            if Char.IsLetterOrDigit char then
                builder.Append char |> ignore
            elif
                char = '#'
                && builder.Length > 0
                && Char.IsLetterOrDigit builder[builder.Length - 1]
            then
                builder.Append "sharp" |> ignore
            elif builder.Length > 0 && builder[builder.Length - 1] <> '-' then
                builder.Append '-' |> ignore
        )

        builder.ToString().Trim('-')
