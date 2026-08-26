module Nacara.Tests.Locale

open Scriptorium.Nib.Assertion
open type Scriptorium.Quill.Test
open System.IO
open Nacara.Core
open Nacara.Tests

/// A site with a root locale and a prefixed one, sharing the same collection.
let private multilingual =
    Fixture.site
    |> Site.locales
        [
            Locale.root "en"
            Locale.other "fr" |> Locale.withLabel "Français"
        ]

let private withFrenchPages () =
    let root = Fixture.copyToTemporaryDirectory ()
    let french = Path.Combine(AbsolutePath.value root, "docs/fr/guide")
    Directory.CreateDirectory french |> ignore

    File.WriteAllText(
        Path.Combine(AbsolutePath.value root, "docs/fr/index.md"),
        "---\ntitle: Accueil\n---\n\n# Accueil\n\nBonjour.\n"
    )

    File.WriteAllText(
        Path.Combine(french, "getting-started.md"),
        "---\ntitle: Démarrer\n---\n\n## Installation\n\nBonjour.\n"
    )

    root

let all =
    testList (
        "Locale",
        [
            test (
                "a locale directory claims its pages",
                fun _ ->
                    let root = withFrenchPages ()
                    let result = Build.run root multilingual

                    let byLocale =
                        result.Pages |> List.countBy (fun page -> page.Locale.Code) |> Map.ofList

                    assertThat
                        (Map.tryFind "fr" byLocale)
                        (tag "every page exists in French" >> isEqualTo (Some 3))

                    assertThat
                        (Map.tryFind "en" byLocale)
                        (tag "the English ones are unchanged" >> isEqualTo (Some 3))
            )

            test (
                "the root locale keeps its urls unprefixed",
                fun _ ->
                    let root = withFrenchPages ()
                    Build.run root multilingual |> ignore
                    let output = Path.Combine(AbsolutePath.value root, "output")

                    assertThat
                        (File.Exists(Path.Combine(output, "index.html")))
                        (tag "the English home page" >> isTrue)

                    assertThat
                        (File.Exists(Path.Combine(output, "guide/getting-started/index.html")))
                        (tag "and its guide" >> isTrue)
            )

            test (
                "other locales are written under their prefix",
                fun _ ->
                    let root = withFrenchPages ()
                    Build.run root multilingual |> ignore
                    let output = Path.Combine(AbsolutePath.value root, "output")

                    assertThat
                        (File.Exists(Path.Combine(output, "fr/index.html")))
                        (tag "the French home page" >> isTrue)

                    assertThat
                        (File.Exists(Path.Combine(output, "fr/guide/getting-started/index.html")))
                        (tag "and its guide, at the same path under /fr/" >> isTrue)
            )

            test (
                "a translation shares its key with the original",
                fun _ ->
                    let root = withFrenchPages ()
                    let result = Build.run root multilingual

                    let keyOf locale =
                        result.Pages
                        |> List.filter (fun page -> page.Locale.Code = locale)
                        |> List.map (fun page -> Route.translationKey page.Route)
                        |> List.sort

                    assertThat
                        (keyOf "fr" |> List.forall (fun key -> List.contains key (keyOf "en")))
                        (tag
                            "every French page matches an English one by key, which is how a language picker finds it"
                         >> isTrue)
            )

            test (
                "menus and links stay inside a locale",
                fun _ ->
                    let root = withFrenchPages ()
                    Build.run root multilingual |> ignore

                    let french =
                        File.ReadAllText(
                            Path.Combine(AbsolutePath.value root, "output/fr/index.html")
                        )

                    assertThat
                        (french.Contains "href=\"/fr/")
                        (tag "French pages link to French pages" >> isTrue)

                    assertThat
                        (french.Contains "href=\"/guide/getting-started/\"")
                        (tag "and not across into the root locale" >> isFalse)
            )

            test (
                "an untranslated page still exists in the other locale",
                fun _ ->
                    let root = withFrenchPages ()
                    let result = Build.run root multilingual

                    let fallback =
                        result.Pages
                        |> List.filter (fun page ->
                            page.TryData<string> PageData.UntranslatedFrom = Some "en"
                        )

                    assertThat
                        (List.length fallback)
                        (tag "only the missing page is filled in" >> isEqualTo 1)

                    assertThat
                        (File.Exists(
                            Path.Combine(
                                AbsolutePath.value root,
                                "output/fr/guide/advanced/index.html"
                            )
                        ))
                        (tag "and it is written, so the French menu has no dead entry" >> isTrue)
            )

            test (
                "the fallback can be turned off",
                fun _ ->
                    let root = withFrenchPages ()
                    let site = multilingual |> Site.fallBackToDefaultLocale false
                    let result = Build.run root site

                    assertThat
                        (result.Pages
                         |> List.filter (fun page -> page.Locale.Code = "fr")
                         |> List.length)
                        (tag "only translated pages are generated" >> isEqualTo 2)
            )

            test (
                "a real translation is not replaced by a fallback",
                fun _ ->
                    let root = withFrenchPages ()
                    Build.run root multilingual |> ignore

                    let html =
                        File.ReadAllText(
                            Path.Combine(
                                AbsolutePath.value root,
                                "output/fr/guide/getting-started/index.html"
                            )
                        )

                    assertThat (html.Contains "Démarrer") (tag "the translation wins" >> isTrue)
            )

            test (
                "a single-locale site pays nothing for locales",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()
                    let directory = Path.Combine(AbsolutePath.value root, "docs/fr")
                    Directory.CreateDirectory directory |> ignore

                    File.WriteAllText(
                        Path.Combine(directory, "page.md"),
                        "---\ntitle: Page\n---\n\nBody.\n"
                    )

                    Build.run root Fixture.site |> ignore

                    assertThat
                        (File.Exists(
                            Path.Combine(AbsolutePath.value root, "output/fr/page/index.html")
                        ))
                        (tag "'fr' is just a directory name when the site has one locale" >> isTrue)
            )

            test (
                "the version prefix applies to every url",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()
                    let versioned = Fixture.site |> Site.version "2.0"
                    Build.run root versioned |> ignore
                    let output = Path.Combine(AbsolutePath.value root, "output")

                    assertThat
                        (File.Exists(Path.Combine(output, "index.html")))
                        (tag "files are written where the host serves them, without the prefix"
                         >> isTrue)

                    let html =
                        File.ReadAllText(Path.Combine(output, "guide/getting-started/index.html"))

                    assertThat
                        (html.Contains "href=\"/2.0/")
                        (tag
                            "but links carry the version, so a version deploys into its own directory"
                         >> isTrue)
            )
        ]
    )
