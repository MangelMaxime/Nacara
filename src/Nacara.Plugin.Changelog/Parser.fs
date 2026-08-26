namespace Nacara.Plugins

open System
open System.Text.RegularExpressions

/// <summary>One released (or unreleased) version of a changelog.</summary>
type ChangelogVersion =
    {
        /// Version number, or <c>Unreleased</c>.
        Version: string
        Date: string option
        /// Whether this entry is the unreleased section.
        IsUnreleased: bool
        /// The heading line as the file wrote it, brackets, dashes and all.
        Heading: string
        /// Markdown body of the version, headings included.
        Body: string
    }

/// <summary>A parsed changelog.</summary>
type ChangelogDocument =
    {
        /// <summary>What the file calls itself, from <c>name:</c> in its front matter.</summary>
        /// <remarks>EasyBuild.ShipIt writes and reads it the same way, so a changelog that already
        /// names itself for release notes names its page too.</remarks>
        Name: string option
        /// Everything before the first version, usually a title and a preamble.
        Preamble: string
        Versions: ChangelogVersion list
    }

/// <summary>
/// Reads changelogs written in the Keep a Changelog style.
/// </summary>
/// <remarks>
/// Which versions exist and when they were released - and deliberately nothing else. A version's
/// own markdown is carried through untouched, because a changelog entry is prose and anything that
/// rewrites prose eventually mangles some of it. A file following no convention still renders, as
/// one preamble.
/// </remarks>
[<RequireQualifiedAccess>]
module ChangelogParser =

    let private versionHeading =
        Regex(
            // A superset of what EasyBuild.ShipIt writes: a v before a number, brackets, a looser separator.
            @"^##\s+\[?(?:v(?=\d))?(?<version>[^\]\s]+)\]?(?:\s*[-–—]\s*(?<date>[\d]{4}-[\d]{2}-[\d]{2}))?\s*$",
            RegexOptions.Compiled
        )

    let parse (text: string) =
        let all = text.Replace("\r\n", "\n").Split('\n')

        // EasyBuild.ShipIt keeps its release configuration in the changelog's front matter.
        let frontMatter, lines =
            match Array.tryFindIndex (fun (line: string) -> line.Trim() <> "") all with
            | Some first when all[first].Trim() = "---" ->
                match
                    Array.tryFindIndex (fun (line: string) -> line.Trim() = "---") all[first + 1 ..]
                with
                | Some closing -> all[first + 1 .. first + closing], all[first + closing + 2 ..]
                | None -> [||], all
            | _ -> [||], all

        let name =
            frontMatter
            |> Array.tryPick (fun line ->
                let trimmed = line.Trim()

                if trimmed.StartsWith "name:" then
                    match trimmed.Substring(5).Trim().Trim('"', '\'') with
                    | "" -> None
                    | value -> Some value
                else
                    None
            )

        let starts =
            lines
            |> Array.indexed
            |> Array.choose (fun (index, line) ->
                let matched = versionHeading.Match line

                if matched.Success then
                    Some(index, matched)
                else
                    None
            )

        let preamble =
            match Array.tryHead starts with
            | Some(index, _) -> String.Join("\n", Array.take index lines)
            | None -> text

        let versions =
            starts
            |> Array.mapi (fun position (index, matched) ->
                let stop =
                    if position + 1 < starts.Length then
                        fst starts[position + 1]
                    else
                        lines.Length

                let version = matched.Groups["version"].Value

                {
                    Version = version
                    Heading = lines[index]
                    Date =
                        if matched.Groups["date"].Success then
                            Some matched.Groups["date"].Value
                        else
                            None
                    IsUnreleased = version.Equals("unreleased", StringComparison.OrdinalIgnoreCase)
                    Body = String.Join("\n", lines[index + 1 .. stop - 1]).Trim()
                }
            )
            |> List.ofArray

        {
            Name = name
            Preamble = preamble.Trim()
            Versions = versions
        }
