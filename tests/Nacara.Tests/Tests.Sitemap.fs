module Nacara.Tests.Sitemap

open Scriptorium.Nib.Assertion
open type Scriptorium.Quill.Test
open System.IO
open System.Xml.Linq
open Nacara.Core
open Nacara.Plugins
open Nacara.Tests

let private published =
    Fixture.site
    |> Site.origin "https://example.com"
    |> Site.plugin (Sitemap.create ())

let all =
    testList (
        "Sitemap",
        [
            test (
                "a sitemap lists every page, absolutely",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()
                    let result = Build.run root published
                    let path = Path.Combine(AbsolutePath.value root, "output/sitemap.xml")

                    assertThat (File.Exists path) (tag "the sitemap is written" >> isTrue)

                    let document = XDocument.Load path
                    let ns = XNamespace.Get "http://www.sitemaps.org/schemas/sitemap/0.9"

                    let locations =
                        document.Root.Elements(ns + "url")
                        |> Seq.map (fun url -> url.Element(ns + "loc").Value)
                        |> List.ofSeq

                    assertThat
                        (List.length locations)
                        (tag "one entry per page" >> isEqualTo (List.length result.Pages))

                    assertThat
                        (locations |> List.forall (fun url -> url.StartsWith "https://example.com/"))
                        (tag "and every url is absolute" >> isTrue)
            )

            test (
                "robots.txt points at the sitemap",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()
                    Build.run root published |> ignore

                    let robots =
                        File.ReadAllText(Path.Combine(AbsolutePath.value root, "output/robots.txt"))

                    assertThat
                        (robots.Contains "Sitemap: https://example.com/sitemap.xml")
                        (tag "so a crawler finds it without being told" >> isTrue)
            )

            test (
                "no origin means no sitemap, and a warning",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()
                    let result = Build.run root (Fixture.site |> Site.plugin (Sitemap.create ()))

                    assertThat
                        (File.Exists(Path.Combine(AbsolutePath.value root, "output/sitemap.xml")))
                        (tag "nothing is written" >> isFalse)

                    assertThat
                        (result.Diagnostics
                         |> List.exists (fun item -> item.Code = "sitemap/origin-missing"))
                        (tag "and the build says why" >> isTrue)
            )

            test (
                "translations are cross-referenced",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()
                    let french = Path.Combine(AbsolutePath.value root, "docs/fr")
                    Directory.CreateDirectory french |> ignore

                    File.WriteAllText(
                        Path.Combine(french, "index.md"),
                        "---\ntitle: Accueil\n---\n\nBonjour.\n"
                    )

                    let site =
                        published
                        |> Site.locales
                            [
                                Locale.root "en"
                                Locale.other "fr"
                            ]

                    Build.run root site |> ignore

                    let sitemap =
                        File.ReadAllText(
                            Path.Combine(AbsolutePath.value root, "output/sitemap.xml")
                        )

                    assertThat
                        (sitemap.Contains "hreflang=\"fr\"" && sitemap.Contains "hreflang=\"en\"")
                        (tag "each entry offers the other language" >> isTrue)
            )

            test (
                "two producers writing one file is reported",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    File.WriteAllText(
                        Path.Combine(AbsolutePath.value root, "static/robots.txt"),
                        "User-agent: *\n"
                    )

                    let result = Build.run root published

                    assertThat
                        (result.Diagnostics
                         |> List.exists (fun item -> item.Code = "nacara/duplicate-output"))
                        (tag "the collision is reported rather than left to flap" >> isTrue)
            )

            test (
                "a rebuild that changes nothing touches nothing on disk",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()
                    let cache = BuildCache()
                    Build.runWith cache root published |> ignore
                    let output = Path.Combine(AbsolutePath.value root, "output")

                    let stamps () =
                        Directory.EnumerateFiles(output, "*", SearchOption.AllDirectories)
                        |> Seq.map (fun file -> file, File.GetLastWriteTimeUtc file)
                        |> Map.ofSeq

                    let before = stamps ()
                    System.Threading.Thread.Sleep 20
                    let result = Build.runWith cache root published
                    let after = stamps ()

                    assertThat
                        result.WrittenFiles
                        (tag "the build reports writing nothing" >> isEqualTo 0)

                    let touched =
                        after
                        |> Map.filter (fun file stamp -> Map.tryFind file before <> Some stamp)
                        |> Map.keys
                        |> Seq.map Path.GetFileName
                        |> List.ofSeq

                    assertThat
                        touched
                        (tag "and nothing was touched, sitemap and robots.txt included"
                         >> isEqualTo [])
            )

            test (
                "the sitemap survives the next build",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()
                    let cache = BuildCache()
                    Build.runWith cache root published |> ignore
                    let result = Build.runWith cache root published

                    assertThat
                        result.PrunedFiles
                        (tag "pruning leaves what the plugin declared" >> isEqualTo 0)

                    assertThat
                        (File.Exists(Path.Combine(AbsolutePath.value root, "output/sitemap.xml")))
                        (tag "so it is still there" >> isTrue)
            )
        ]
    )
