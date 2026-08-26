module Nacara.Tests.Changelog

open Scriptorium.Nib.Assertion
open type Scriptorium.Quill.Test
open System.IO
open Nacara.Core
open Nacara.Plugins
open Nacara.Tests

let private sample =
    """# Changelog

Everything notable, in one place.

## Unreleased

### Added

- Something new

## 2.1.0 - 2024-06-01

### Fixed

- A bug

## [2.0.0] - 2024-01-15

### Changed

- Everything
"""

let all =
    testList (
        "Changelog",
        [
            test (
                "the preamble stops at the first version",
                fun _ ->
                    let document = ChangelogParser.parse sample

                    assertThat
                        (document.Preamble.Contains "Everything notable")
                        (tag "the prose is kept" >> isTrue)

                    assertThat
                        (document.Preamble.Contains "Unreleased")
                        (tag "the versions are not" >> isFalse)
            )

            test (
                "every version is found",
                fun _ ->
                    let document = ChangelogParser.parse sample

                    assertThat
                        (document.Versions |> List.map _.Version)
                        (tag "in the order they appear, brackets or not"
                         >> isEqualTo
                             [
                                 "Unreleased"
                                 "2.1.0"
                                 "2.0.0"
                             ])
            )

            test (
                "dates are read when present",
                fun _ ->
                    let document = ChangelogParser.parse sample

                    let versions =
                        document.Versions |> List.map (fun version -> version.Version, version.Date)

                    assertThat
                        (List.item 1 versions)
                        (tag "a dated version" >> isEqualTo ("2.1.0", Some "2024-06-01"))

                    assertThat
                        (List.item 0 versions)
                        (tag "and one without a date" >> isEqualTo ("Unreleased", None))
            )

            test (
                "the unreleased section is recognised whatever its case",
                fun _ ->
                    let document = ChangelogParser.parse "## UNRELEASED\n\n- Something\n"

                    assertThat
                        (List.head document.Versions).IsUnreleased
                        (tag "case does not matter" >> isTrue)
            )

            test (
                "a version keeps its own body only",
                fun _ ->
                    let document = ChangelogParser.parse sample
                    let latest = List.item 1 document.Versions
                    assertThat (latest.Body.Contains "A bug") (tag "its own entries" >> isTrue)

                    assertThat
                        (latest.Body.Contains "Everything")
                        (tag "and not the next version's" >> isFalse)
            )

            test (
                "a file with no version headings still renders",
                fun _ ->
                    let document = ChangelogParser.parse "# Notes\n\nJust prose.\n"

                    assertThat
                        (List.length document.Versions)
                        (tag "no versions were invented" >> isEqualTo 0)

                    assertThat
                        (document.Preamble.Contains "Just prose")
                        (tag "and nothing was lost" >> isTrue)

                    assertThat
                        ((Changelog.toMarkdown "Notes" document).Contains "Just prose")
                        (tag "with nothing to strip down to, the whole file is the page" >> isTrue)
            )

            test (
                "a changelog's own front matter is not preamble",
                fun _ ->
                    let document =
                        ChangelogParser.parse
                            "---\nname: Thing\nlast_commit_released: abc123\n---\n\n# Changelog\n\nProse.\n\n## 1.0.0\n\n- Something\n"

                    assertThat
                        (document.Preamble.Contains "last_commit_released")
                        (tag "it describes the file, not the releases" >> isFalse)

                    assertThat
                        (document.Preamble.Contains "Prose")
                        (tag "what is written for a reader stays" >> isTrue)

                    assertThat
                        (List.length document.Versions)
                        (tag "and the versions are still found" >> isEqualTo 1)
            )

            test (
                "a pattern finds the changelogs so a list does not have to",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    for name in
                        [
                            "Alpha"
                            "Beta"
                        ] do
                        let directory = Path.Combine(AbsolutePath.value root, "packages", name)
                        Directory.CreateDirectory directory |> ignore
                        File.WriteAllText(Path.Combine(directory, "CHANGELOG.md"), sample)

                    let site =
                        Fixture.site
                        |> Site.collection (
                            Changelog.collection
                                "changelog"
                                Fixture.decoder
                                [
                                    ChangelogSource.matching "packages/*/CHANGELOG.md"
                                    |> ChangelogSource.group "Packages"
                                ]
                        )

                    Build.run root site |> ignore

                    let published =
                        Directory.GetDirectories(Path.Combine(AbsolutePath.value root, "output"))
                        |> Array.map Path.GetFileName
                        |> Array.filter (fun name -> name = "alpha" || name = "beta")
                        |> Array.sort
                        |> List.ofArray

                    assertThat
                        published
                        (tag "one page per match, named after the directory holding it"
                         >> isEqualTo
                             [
                                 "alpha"
                                 "beta"
                             ])
            )

            test (
                "a site can name the matches itself",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()
                    let directory = Path.Combine(AbsolutePath.value root, "packages", "alpha")
                    Directory.CreateDirectory directory |> ignore
                    File.WriteAllText(Path.Combine(directory, "CHANGELOG.md"), sample)

                    let site =
                        Fixture.site
                        |> Site.collection (
                            Changelog.collection
                                "changelog"
                                Fixture.decoder
                                [
                                    ChangelogSource.matching "packages/*/CHANGELOG.md"
                                    |> ChangelogSource.labelledBy (fun path ->
                                        "My." + Path.GetFileName(Path.GetDirectoryName path)
                                    )
                                ]
                        )

                    Build.run root site |> ignore

                    assertThat
                        (Directory.Exists(
                            Path.Combine(AbsolutePath.value root, "output", "my-alpha")
                        ))
                        (tag "the callback decides the name, and so the url" >> isTrue)
            )

            test (
                "what the file calls itself wins",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()
                    let directory = Path.Combine(AbsolutePath.value root, "packages", "alpha")
                    Directory.CreateDirectory directory |> ignore

                    File.WriteAllText(
                        Path.Combine(directory, "CHANGELOG.md"),
                        "---\nname: My Library\n---\n\n" + sample
                    )

                    let site =
                        Fixture.site
                        |> Site.collection (
                            Changelog.collection
                                "changelog"
                                Fixture.decoder
                                [ ChangelogSource.matching "packages/*/CHANGELOG.md" ]
                        )

                    Build.run root site |> ignore

                    assertThat
                        (Directory.Exists(
                            Path.Combine(AbsolutePath.value root, "output", "my-library")
                        ))
                        (tag "over the directory it sits in" >> isTrue)
            )

            test (
                "a changelog at the root of a repository is called after the file",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    Directory.CreateDirectory(Path.Combine(AbsolutePath.value root, ".git"))
                    |> ignore

                    File.WriteAllText(Path.Combine(AbsolutePath.value root, "CHANGELOG.md"), sample)

                    let named =
                        ChangelogSource.defaultLabel (
                            Path.Combine(AbsolutePath.value root, "CHANGELOG.md")
                        )

                    assertThat named (tag "so it takes the file's name" >> isEqualTo "Changelog")

                    let inPackage =
                        ChangelogSource.defaultLabel (
                            Path.Combine(AbsolutePath.value root, "src", "Alpha", "CHANGELOG.md")
                        )

                    assertThat
                        inPackage
                        (tag "and anywhere else is still the directory" >> isEqualTo "Alpha")
            )

            test (
                "a pattern that matches nothing publishes nothing, and says nothing",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    let site =
                        Fixture.site
                        |> Site.collection (
                            Changelog.collection
                                "changelog"
                                Fixture.decoder
                                [ ChangelogSource.matching "packages/*/CHANGELOG.md" ]
                        )

                    let result = Build.run root site

                    assertThat
                        (result.Diagnostics
                         |> List.exists (fun item -> item.Code = "changelog/source-missing"))
                        (tag "nothing to report" >> isFalse)
            )

            test (
                "a page says which file it was made from",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()
                    File.WriteAllText(Path.Combine(AbsolutePath.value root, "CHANGELOG.md"), sample)

                    let site =
                        Fixture.site
                        |> Site.collection (
                            Changelog.collection
                                "changelog"
                                Fixture.decoder
                                [ ChangelogSource.create "My library" "CHANGELOG.md" ]
                        )

                    let result = Build.run root site

                    let page =
                        result.Pages |> List.tryFind (fun page -> page.Collection = "changelog")

                    match page with
                    | None -> assertThat "" (tag "the changelog was published" >> isNotEqualTo "")
                    | Some page ->
                        assertThat
                            (page.Dependencies
                             |> List.map (fun path -> Path.GetFileName(AbsolutePath.value path)))
                            (tag
                                "the file it was read from, so a change to it is a change to the page"
                             >> isEqualTo [ "CHANGELOG.md" ])
            )

            test (
                "a changelog's table of contents is its versions",
                fun _ ->
                    let collection = Changelog.collection "changelog" Fixture.decoder []

                    assertThat
                        (collection.TocOf
                            {
                                Title = "Nacara"
                                Description = None
                                Order = None
                                Toc = None
                            })
                        (tag "every section of every release would bury them"
                         >> isEqualTo (
                             Some
                                 {
                                     From = 2
                                     To = 2
                                 }
                         ))
            )

            test (
                "a v before the number is not part of it",
                fun _ ->
                    let document = ChangelogParser.parse "## v1.2.3 - 2024-06-01\n\n- Something\n"

                    assertThat
                        (List.head document.Versions).Version
                        (tag "the v is a prefix" >> isEqualTo "1.2.3")

                    let other = ChangelogParser.parse "## version-2\n\n- Something\n"

                    assertThat
                        (List.head other.Versions).Version
                        (tag "but a word starting with v is left alone" >> isEqualTo "version-2")
            )

            test (
                "the generated page carries front matter and the headings as written",
                fun _ ->
                    let markdown = Changelog.toMarkdown "Nacara" (ChangelogParser.parse sample)

                    assertThat
                        (markdown.StartsWith "---\ntitle: Nacara\npageNav: false\n---")
                        (tag "front matter first, and a changelog is not read in order" >> isTrue)

                    assertThat
                        (markdown.Contains "## 2.1.0 - 2024-06-01")
                        (tag "date included" >> isTrue)

                    assertThat
                        (markdown.Contains "## [2.0.0] - 2024-01-15")
                        (tag "brackets and all - the heading is not rewritten" >> isTrue)

                    assertThat
                        (markdown.Contains "{#v2-1-0}" && markdown.Contains "{#v2-0-0}")
                        (tag "and every version is a link a reader can hand over" >> isTrue)
            )

            test (
                "nothing above the first version reaches the page",
                fun _ ->
                    let source =
                        "---\nname: Thing\nupdaters:\n  - package.json\n---\n\n# Changelog\n\nAll notable changes.\n\n## 1.0.0\n\n- Something\n"

                    let markdown = Changelog.toMarkdown "Thing" (ChangelogParser.parse source)

                    assertThat
                        (markdown.Contains "updaters")
                        (tag "the file's own metadata is not content" >> isFalse)

                    assertThat
                        (markdown.Contains "# Changelog")
                        (tag "nor its title, which the page has" >> isFalse)

                    assertThat
                        (markdown.Contains "All notable changes")
                        (tag "nor its preamble" >> isFalse)

                    assertThat
                        (markdown.Contains "- Something")
                        (tag "the versions are what is published" >> isTrue)
            )

            test (
                "a version's markdown is carried through untouched",
                fun _ ->
                    let entry =
                        String.concat
                            "\n"
                            [
                                "* Generate a concrete interface ([24210b7](https://example.com/c))"
                                ""
                                "    ```ts"
                                "    declare interface User<T extends Options = Options> {}"
                                "    ```"
                                ""
                                "    ```fs"
                                "    type User<'T when 'T :> Options> ="
                                "        interface end"
                                "    ```"
                                ""
                                "    Fix #211"
                            ]

                    let markdown =
                        Changelog.toMarkdown
                            "Thing"
                            (ChangelogParser.parse
                                $"# Changelog\n\n## 0.13.0\n\n### \U0001F41E Bug Fixes\n\n%s{entry}\n")

                    assertThat
                        (markdown.Contains entry)
                        (tag "every line, and every space in front of it" >> isTrue)

                    assertThat
                        (markdown.Contains "### \U0001F41E Bug Fixes")
                        (tag "including a section heading the engine knows nothing about" >> isTrue)
            )

            test (
                "the changelogs a site declares are the menu it gets",
                fun _ ->
                    let sources =
                        [
                            ChangelogSource.create "My library" "CHANGELOG.md"
                            ChangelogSource.create "Extras" "src/Extras/CHANGELOG.md"
                            |> ChangelogSource.group "Plugins"
                            ChangelogSource.create "Themes.Default" "src/Theme/CHANGELOG.md"
                            |> ChangelogSource.slug "theme"
                            |> ChangelogSource.group "Plugins"
                        ]

                    let menu = Changelog.menu "changelog" sources

                    assertThat
                        menu.Section
                        (tag "the menu says which section it is for" >> isEqualTo "changelog")

                    assertThat
                        (menu.Items |> List.map (fun item -> item.Label, item.Page))
                        (tag "a changelog with no group stands on its own, a group is a heading"
                         >> isEqualTo
                             [
                                 "My library", Some "my-library.md"
                                 "Plugins", None
                             ])

                    assertThat
                        (menu.Items
                         |> List.last
                         |> _.Children
                         |> List.map (fun item -> item.Label, item.Page))
                        (tag "and holds what named it, in the order they were declared"
                         >> isEqualTo
                             [
                                 "Extras", Some "extras.md"
                                 "Themes.Default", Some "theme.md"
                             ])
            )
        ]
    )
