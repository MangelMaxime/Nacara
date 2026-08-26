namespace Nacara.Core

open System
open System.IO
open Microsoft.Extensions.FileSystemGlobbing

/// <summary>
/// An absolute path on disk, normalized to use <c>/</c> as its separator.
/// </summary>
/// <remarks>
/// Normalizing at the boundary means paths compare and hash consistently on every platform
/// </remarks>
[<Struct>]
type AbsolutePath =
    private
    | AbsolutePath of value: string

    override this.ToString() =
        let (AbsolutePath value) = this
        value

/// <summary>
/// A path relative to a known root, normalized to use <c>/</c> as its separator and
/// carrying no leading <c>./</c>.
/// </summary>
[<Struct>]
type RelativePath =
    private
    | RelativePath of value: string

    override this.ToString() =
        let (RelativePath value) = this
        value

[<RequireQualifiedAccess>]
module AbsolutePath =

    /// <summary>Create an absolute path, failing when <paramref name="value" /> is not rooted.</summary>
    /// <param name="value">A rooted path. It is normalised - resolved, and written with forward
    /// slashes on every platform - so two spellings of one path compare equal.</param>
    /// <exception cref="T:System.ArgumentException">The path is empty, or is relative.</exception>
    let create (value: string) =
        if String.IsNullOrWhiteSpace value then
            invalidArg (nameof value) "An absolute path cannot be empty"

        if not (Path.IsPathRooted value) then
            invalidArg (nameof value) $"Expected an absolute path but got '%s{value}'"

        value |> Path.GetFullPath |> _.Replace('\\', '/') |> AbsolutePath

    /// <summary>Create an absolute path by combining <paramref name="root" /> with segments.</summary>
    /// <param name="root">Where to start from.</param>
    /// <param name="segments">What to append. A segment that is itself rooted wins outright, which
    /// is how an option holding an absolute path overrides a project-relative one.</param>
    let combine (root: AbsolutePath) (segments: string list) =
        Path.Combine(string root :: segments |> Array.ofList) |> create

    /// <summary>The path as text, normalised, for the APIs that take a string.</summary>
    let value (AbsolutePath value) = value

    /// <summary>The last segment, extension included.</summary>
    let fileName (AbsolutePath value) = Path.GetFileName value

    /// <summary>The extension, dot included, or an empty string when there is none.</summary>
    let extension (AbsolutePath value) = Path.GetExtension value

    /// <summary>The directory holding it.</summary>
    let directory (AbsolutePath value) =
        value |> Path.GetDirectoryName |> create

    /// <summary>Whether anything is there: a file or a directory.</summary>
    let exists (AbsolutePath value) =
        File.Exists value || Directory.Exists value

    /// <summary>Quoted form used in log and diagnostic messages.</summary>
    let toLog (AbsolutePath value) = $"'%s{value}'"

[<RequireQualifiedAccess>]
module RelativePath =

    /// <summary>A path inside the output, however it was written.</summary>
    /// <param name="value">A relative path. Backslashes become forward slashes and any leading
    /// <c>./</c> is dropped, so <c>.\assets\api.css</c> and <c>assets/api.css</c> are one
    /// path.</param>
    let create (value: string) =
        value.Replace('\\', '/').TrimStart('.', '/') |> RelativePath

    /// <summary>Express <paramref name="path" /> relatively to <paramref name="root" />.</summary>
    /// <param name="root">What to express it against, usually the content or output directory.</param>
    /// <param name="path">The path to express.</param>
    let fromRoot (root: AbsolutePath) (path: AbsolutePath) =
        Path.GetRelativePath(AbsolutePath.value root, AbsolutePath.value path) |> create

    let value (RelativePath value) = value

    let segments (RelativePath value) =
        value.Split('/', StringSplitOptions.RemoveEmptyEntries) |> List.ofArray

    let extension (RelativePath value) = Path.GetExtension value

    let changeExtension (extension: string) (RelativePath value) =
        Path.ChangeExtension(value, extension) |> create

/// <summary>Finding files by pattern.</summary>
[<RequireQualifiedAccess>]
module Glob =

    /// <summary>Files matching a pattern, in a stable order.</summary>
    /// <remarks>
    /// The pattern is split at its first wildcard: what comes before is a directory, resolved
    /// against <paramref name="root" />, and what comes after is matched inside it. That is what
    /// lets a pattern reach outside the root - <c>../src/*/CHANGELOG.md</c> from a site that lives
    /// in <c>docs/</c>.
    /// </remarks>
    /// <param name="root">What a relative pattern is resolved against.</param>
    /// <param name="pattern">A path, with <c>*</c> or <c>?</c> in it or not.</param>
    /// <returns>What matched, sorted, so a build is reproducible whatever the file system says.</returns>
    let files (root: AbsolutePath) (pattern: string) =
        let segments = pattern.Replace('\\', '/').Split('/') |> List.ofArray

        let isWild (segment: string) =
            segment.Contains "*" || segment.Contains "?"

        let fixed', wild =
            segments |> List.takeWhile (isWild >> not), segments |> List.skipWhile (isWild >> not)

        let directory =
            if List.isEmpty fixed' then
                root
            else
                AbsolutePath.combine root fixed'

        if List.isEmpty wild then
            if File.Exists(AbsolutePath.value directory) then
                [ directory ]
            else
                []
        elif not (Directory.Exists(AbsolutePath.value directory)) then
            []
        else

            let matcher = Matcher()
            matcher.AddInclude(String.concat "/" wild) |> ignore

            matcher.GetResultsInFullPath(AbsolutePath.value directory)
            |> Seq.map AbsolutePath.create
            |> Seq.sort
            |> List.ofSeq
