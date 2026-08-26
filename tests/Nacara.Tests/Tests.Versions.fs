module Nacara.Tests.Versions

open Scriptorium.Nib.Assertion
open type Scriptorium.Quill.Test
open System.IO
open Nacara.Core
open Nacara.Plugins
open Nacara.Tests

let private versions =
    [
        SiteVersion.root "2.0"
        SiteVersion.create "1.0" "1.0"
    ]

let private options =
    { Versions.defaults with
        Versions = versions
    }

let all =
    testList (
        "Versions",
        [
            test (
                "the manifest lists every version",
                fun _ ->
                    let manifest = Versions.manifest "" options
                    assertThat (manifest.Contains "\"label\":\"2.0\"") (tag "the latest" >> isTrue)

                    assertThat
                        (manifest.Contains "\"label\":\"1.0\"")
                        (tag "and the older one" >> isTrue)
            )

            test (
                "the manifest says which version this build is",
                fun _ ->
                    let manifest = Versions.manifest "1.0" options

                    assertThat
                        (manifest.Contains "\"prefix\":\"1.0\",\"latest\":false,\"current\":true")
                        (tag "the build's own version is current" >> isTrue)

                    assertThat
                        (manifest.Contains "\"prefix\":\"\",\"latest\":true,\"current\":false")
                        (tag "and the root version is the latest, not the current one" >> isTrue)
            )

            test (
                "the switcher carries the base url and the versions",
                fun _ ->
                    let site =
                        Site.create "Docs"
                        |> Site.baseUrl "/Nacara/"
                        |> Site.version "1.0"
                        |> Site.toInfo

                    let html = Versions.switcher options site

                    assertThat
                        (html.Contains "data-base=\"/Nacara/\"")
                        (tag "so it can rewrite paths" >> isTrue)

                    assertThat
                        (html.Contains "&quot;current&quot;:true")
                        (tag "and knows where it stands" >> isTrue)
            )

            test (
                "a build writes the manifest and keeps it",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()
                    let site = Fixture.site |> Site.plugin (Versions.create versions)
                    let cache = BuildCache()
                    Build.runWith cache root site |> ignore
                    let result = Build.runWith cache root site

                    let manifest = Path.Combine(AbsolutePath.value root, "output/versions.json")
                    assertThat (File.Exists manifest) (tag "the manifest is written" >> isTrue)

                    assertThat
                        result.PrunedFiles
                        (tag "and pruning leaves it alone on the next build" >> isEqualTo 0)

                    assertThat
                        ((File.ReadAllText manifest).Contains "1.0")
                        (tag "it lists the versions" >> isTrue)
            )

            test (
                "a versioned build puts every url under its prefix",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    let site =
                        Fixture.site |> Site.version "1.0" |> Site.plugin (Versions.create versions)

                    Build.run root site |> ignore

                    let html =
                        File.ReadAllText(
                            Path.Combine(
                                AbsolutePath.value root,
                                "output/guide/getting-started/index.html"
                            )
                        )

                    assertThat
                        (html.Contains "href=\"/1.0/\"")
                        (tag "links carry the prefix" >> isTrue)

                    let info = Site.toInfo site

                    assertThat
                        (info.UrlOfAsset "assets/theme.css")
                        (tag "an asset url carries the version too"
                         >> isEqualTo "/1.0/assets/theme.css")
            )
        ]
    )
