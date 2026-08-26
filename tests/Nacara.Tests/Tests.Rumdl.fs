/// <summary>The rumdl linter, as far as it goes without the binary.</summary>
module Nacara.Tests.Rumdl

open Scriptorium.Nib.Assertion
open type Scriptorium.Quill.Test
open Nacara.Plugins

let all =
    testList (
        "Rumdl",
        [
            test (
                "what rumdl says becomes what a diagnostic needs",
                fun _ ->
                    let findings =
                        Rumdl.readFindings
                            """[
                              {
                                "file": "docs/content/guide/deploy.md",
                                "line": 47,
                                "column": 93,
                                "rule": "MD009",
                                "message": "Trailing space found",
                                "severity": "warning",
                                "fixable": true,
                                "fix": null
                              }
                            ]"""

                    assertThat (List.length findings) (tag "one finding" >> isEqualTo 1)

                    let finding = List.head findings

                    assertThat
                        (finding.File, finding.Line, finding.Column)
                        (tag "where it is, as an editor would jump to it"
                         >> isEqualTo ("docs/content/guide/deploy.md", 47, 93))

                    assertThat
                        (finding.Rule, finding.Message)
                        (tag "what it is" >> isEqualTo ("MD009", "Trailing space found"))

                    assertThat
                        finding.Fixable
                        (tag "and whether rumdl would fix it itself" >> isEqualTo true)
            )

            test (
                "nothing said is nothing to report",
                fun _ ->
                    assertThat
                        (Rumdl.readFindings "[]" |> List.length)
                        (tag "an empty array is no findings" >> isEqualTo 0)
            )

            test (
                "the defaults turn off what the engine answers itself",
                fun _ ->
                    assertThat
                        (Rumdl.Defaults |> List.contains "MD057.enabled = false")
                        (tag "links are checked against the route table" >> isEqualTo true)

                    assertThat
                        (Rumdl.Defaults |> List.exists (fun setting -> setting.StartsWith "MD013"))
                        (tag "and prose is not measured like source" >> isEqualTo true)
            )
        ]
    )
