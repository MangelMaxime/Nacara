/// <summary>Fetching the programs plugins drive.</summary>
module Nacara.Tests.Tool

open System.IO
open Scriptorium.Nib.Assertion
open type Scriptorium.Quill.Test
open Nacara.Core

let all =
    testList (
        "Tool",
        [
            test (
                "a tool says where it would live",
                fun _ ->
                    let request =
                        {
                            Name = "pagefind"
                            Version = "1.5.2"
                            Url = "https://example.invalid/pagefind.tar.gz"
                            Archive = TarGzip
                            Files = [ "pagefind" ]
                            Executable = [ "pagefind" ]
                            Checksum = None
                        }

                    let where = Tool.directory request

                    assertThat
                        (where.EndsWith(Path.Combine("nacara", "pagefind", "1.5.2")))
                        (tag $"a directory per tool and version: {where}" >> isTrue)
            )

            test (
                "this machine is one the tools are named for",
                fun _ ->
                    match Tool.platform () with
                    | Error message ->
                        assertThat message (tag "the platform should be known" >> isEqualTo "")
                    | Ok platform ->
                        assertThat
                            (platform.Rid.Contains "-"
                             && (platform.Architecture = "x64" || platform.Architecture = "arm64"))
                            (tag $"a rid and an architecture: {platform.Rid}" >> isTrue)

                        assertThat
                            ([
                                platform.IsLinux
                                platform.IsMacOS
                                platform.IsWindows
                             ]
                             |> List.filter id
                             |> List.length)
                            (tag "one operating system, not two" >> isEqualTo 1)
            )
        ]
    )
