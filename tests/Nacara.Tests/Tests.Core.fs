module Nacara.Tests.Core

open Scriptorium.Nib.Assertion
open type Scriptorium.Quill.Test
open System
open System.IO
open System.Threading
open Nacara.Core
open Nacara.Tests

let private en = Locale.root "en"
let private fr = Locale.other "fr"

let slug =
    testList (
        "slug",
        [
            test (
                "Slug.create lowercases and dashes",
                fun _ ->
                    assertThat
                        (Slug.create "Get started!")
                        (tag "punctuation becomes a separator" >> isEqualTo "get-started")
            )
            test (
                "Slug.create collapses runs of separators",
                fun _ ->
                    assertThat
                        (Slug.create "Nacara  ---  Core")
                        (tag "runs collapse to one dash" >> isEqualTo "nacara-core")
            )
            test (
                "Slug.create trims leading and trailing separators",
                fun _ ->
                    assertThat
                        (Slug.create "  (draft) ")
                        (tag "no dangling dashes" >> isEqualTo "draft")
            )
            test (
                "Slug.create folds diacritics",
                fun _ ->
                    assertThat
                        (Slug.create "Créer une page")
                        (tag "accents are folded, not dropped" >> isEqualTo "creer-une-page")
            )
            test (
                "Slug.create keeps digits",
                fun _ ->
                    assertThat
                        (Slug.create "Nacara 2.0")
                        (tag "digits survive" >> isEqualTo "nacara-2-0")
            )
            test (
                "Slug.create is total on symbols",
                fun _ ->
                    assertThat
                        (Slug.create "***")
                        (tag "an all-symbol title slugs to the empty string" >> isEqualTo "")
            )
        ]
    )

let route =
    testList (
        "route",
        [
            test (
                "Route.ofPath slugifies every segment",
                fun _ ->
                    assertThat
                        (Route.ofPath en "docs/Getting Started").Segments
                        (tag "segments are slugified independently"
                         >> isEqualTo
                             [
                                 "docs"
                                 "getting-started"
                             ])
            )
            test (
                "Route.ofPath ignores empty segments",
                fun _ ->
                    assertThat
                        (Route.ofPath en "//docs///guide//").Segments
                        (tag "no empty segments"
                         >> isEqualTo
                             [
                                 "docs"
                                 "guide"
                             ])
            )
            test (
                "Route.home is the locale root",
                fun _ ->
                    assertThat
                        (Route.isHome (Route.home fr))
                        (tag "a home route has no segments" >> isTrue)
            )
            test (
                "Route.translationKey ignores the locale",
                fun _ ->
                    assertThat
                        (Route.translationKey (Route.ofPath fr "docs/guide"))
                        (tag "translations of a page share a key"
                         >> isEqualTo (Route.translationKey (Route.ofPath en "docs/guide")))
            )
        ]
    )

let url =
    let siteUrl = SiteUrl.create "/Nacara/"

    testList (
        "url",
        [
            test (
                "Url.ofRoute prefixes the base url",
                fun _ ->
                    assertThat
                        (Url.ofRoute siteUrl (Route.ofPath en "docs/guide"))
                        (tag "base url is applied and the url ends with a slash"
                         >> isEqualTo "/Nacara/docs/guide/")
            )
            test (
                "Url.ofRoute leaves the root locale unprefixed",
                fun _ ->
                    assertThat
                        ((Url.ofRoute siteUrl (Route.ofPath en "docs")).Contains "/en/")
                        (tag "the root locale never appears in urls" >> isFalse)
            )
            test (
                "Url.ofRoute prefixes other locales",
                fun _ ->
                    assertThat
                        (Url.ofRoute siteUrl (Route.ofPath fr "docs/guide"))
                        (tag "non-root locales are prefixed" >> isEqualTo "/Nacara/fr/docs/guide/")
            )
            test (
                "Url.ofRoute applies the version prefix",
                fun _ ->
                    assertThat
                        (Url.ofRoute
                            (SiteUrl.withVersionPrefix "2.0" siteUrl)
                            (Route.ofPath fr "docs"))
                        (tag "version comes from the build, between base url and locale"
                         >> isEqualTo "/Nacara/2.0/fr/docs/")
            )
            test (
                "Url.ofRoute of a home route is the site root",
                fun _ ->
                    assertThat
                        (Url.ofRoute (SiteUrl.create "/") (Route.home en))
                        (tag "the home url is a bare slash" >> isEqualTo "/")
            )
            test (
                "Url.outputPath writes directory-style pages",
                fun _ ->
                    assertThat
                        (RelativePath.value (Url.outputPath (Route.ofPath fr "docs/guide")))
                        (tag "routes map to index.html inside a directory"
                         >> isEqualTo "fr/docs/guide/index.html")
            )
        ]
    )

let fileRoutes =
    testList (
        "fileRoutes",
        [
            test (
                "a segment with an extension makes the route a file",
                fun _ ->
                    assertThat
                        (Route.isFile (Route.file en "404.html"))
                        (tag "404.html is a file" >> isTrue)

                    assertThat
                        (Route.isFile (Route.ofPath en "guide/setup"))
                        (tag "a page is not" >> isFalse)
            )

            test (
                "a file route is written where it is named",
                fun _ ->
                    assertThat
                        (RelativePath.value (Url.outputPath (Route.file en "404.html")))
                        (tag "no index.html is added" >> isEqualTo "404.html")
            )

            test (
                "a file url has no trailing slash",
                fun _ ->
                    assertThat
                        (Url.ofRoute (SiteUrl.create "/Nacara/") (Route.file en "404.html"))
                        (tag "the url is the file" >> isEqualTo "/Nacara/404.html")
            )

            test (
                "a file route still takes the locale and the version",
                fun _ ->
                    assertThat
                        (RelativePath.value (Url.outputPath (Route.file fr "404.html")))
                        (tag "one per locale" >> isEqualTo "fr/404.html")

                    assertThat
                        (Url.ofRoute
                            (SiteUrl.withVersionPrefix "2.0" (SiteUrl.create "/"))
                            (Route.file en "404.html"))
                        (tag "and one per version" >> isEqualTo "/2.0/404.html")
            )
        ]
    )

let paths =
    testList (
        "paths",
        [
            test (
                "AbsolutePath.create rejects relative paths",
                fun _ ->
                    assertThat
                        (fun () -> AbsolutePath.create "docs/guide.md" |> ignore)
                        (tag "relative input is rejected" >> throws)
            )
            test (
                "AbsolutePath.create normalizes separators",
                fun _ ->
                    assertThat
                        ((AbsolutePath.value (AbsolutePath.create "/tmp/docs/guide.md")).Contains
                            "\\")
                        (tag "paths are stored with forward slashes" >> isFalse)
            )
            test (
                "RelativePath.fromRoot expresses a path against a root",
                fun _ ->
                    assertThat
                        (RelativePath.value (
                            RelativePath.fromRoot
                                (AbsolutePath.create "/tmp/site")
                                (AbsolutePath.create "/tmp/site/docs/guide.md")
                        ))
                        (tag "the root is stripped" >> isEqualTo "docs/guide.md")
            )
            test (
                "RelativePath.create keeps a leading dot in a file name",
                fun _ ->
                    assertThat
                        (RelativePath.value (RelativePath.create "./static/.nojekyll"))
                        (tag "only the ./ prefix is dropped" >> isEqualTo "static/.nojekyll")
            )
            test (
                "RelativePath.changeExtension replaces the extension",
                fun _ ->
                    assertThat
                        (RelativePath.value (
                            RelativePath.changeExtension
                                ".html"
                                (RelativePath.create "docs/guide.md")
                        ))
                        (tag "extension is replaced, path is kept" >> isEqualTo "docs/guide.html")
            )
        ]
    )

let diagnostics =
    testList (
        "diagnostics",
        [
            test (
                "Diagnostic.render uses the editor-friendly shape",
                fun _ ->
                    // Windows answers a leading slash with a drive, and the shape is the point.
                    let file = AbsolutePath.create "/tmp/site/docs/guide.md"

                    assertThat
                        (Diagnostic.error "nacara/front-matter-invalid" "Missing 'title'"
                         |> Diagnostic.at file 3 1
                         |> Diagnostic.render)
                        (tag "file(line,column): severity code: message"
                         >> isEqualTo
                             $"%s{AbsolutePath.value file}(3,1): error nacara/front-matter-invalid: Missing 'title'")
            )
            test (
                "Diagnostic.render appends the hint",
                fun _ ->
                    assertThat
                        ((Diagnostic.warning "nacara/unknown-layout" "Unknown layout"
                          |> Diagnostic.withHint "Did you mean 'doc'?"
                          |> Diagnostic.render)
                            .EndsWith
                            "hint: Did you mean 'doc'?")
                        (tag "the hint is what turns a diagnostic into a fix" >> isTrue)
            )
            test (
                "a sink stamps the source onto the rule",
                fun _ ->
                    let bag = DiagnosticBag()
                    let sink = DiagnosticSink(bag, "nacara")
                    sink.For("markdown").Add(Diagnostic.warning "link-target-missing" "nowhere")
                    sink.Add(Diagnostic.error "nacara/duplicate-route" "twice")

                    assertThat
                        (bag.ToList() |> Seq.map _.Code |> List.ofSeq)
                        (tag
                            "a plugin writes the rule, the engine says who reported it - and a whole code is left alone"
                         >> isEqualTo
                             [
                                 "markdown/link-target-missing"
                                 "nacara/duplicate-route"
                             ])
            )
            test (
                "DiagnosticBag reports errors",
                fun _ ->
                    let bag = DiagnosticBag()
                    bag.Add(Diagnostic.warning "nacara/unknown-layout" "a warning")
                    assertThat bag.HasErrors (tag "warnings alone do not fail a build" >> isFalse)
                    bag.Add(Diagnostic.error "nacara/front-matter-invalid" "an error")
                    assertThat bag.HasErrors (tag "an error does" >> isTrue)
            )
            test (
                "DiagnosticBag orders output deterministically",
                fun _ ->
                    let bag = DiagnosticBag()
                    let file = AbsolutePath.create "/tmp/site/docs/guide.md"

                    bag.Add(
                        Diagnostic.error "nacara/front-matter-invalid" "second"
                        |> Diagnostic.at file 10 1
                    )

                    bag.Add(
                        Diagnostic.error "nacara/front-matter-invalid" "first"
                        |> Diagnostic.at file 2 1
                    )

                    assertThat
                        (bag.ToList() |> Seq.map _.Message |> List.ofSeq)
                        (tag
                            "diagnostics come out sorted by position, whatever order they were reported in"
                         >> isEqualTo
                             [
                                 "first"
                                 "second"
                             ])
            )
        ]
    )

let projectRoot =
    testList (
        "projectRoot",
        [
            test (
                "the default project root is the project, not the working directory",
                fun _ ->
                    let resolved = Nacara.defaultProjectRoot () |> System.IO.DirectoryInfo

                    assertThat
                        resolved.Name
                        (tag "it resolves to the project that produced the running assembly"
                         >> isEqualTo "Nacara.Tests")

                    assertThat
                        (resolved.EnumerateFiles "*.fsproj" |> Seq.isEmpty |> not)
                        (tag "which is the directory holding the project file" >> isTrue)
            )
        ]
    )

let projectCache =
    let scratch () =
        let path =
            Path.Combine(Path.GetTempPath(), "nacara-tests", Guid.NewGuid().ToString "N")

        Directory.CreateDirectory path |> ignore
        AbsolutePath.create path

    testList (
        "projectCache",
        [
            test (
                "the cache ignores itself, so a project needs no rule of its own",
                fun _ ->
                    let root = scratch ()

                    ProjectCache.directory root "bundles" "abcd1234" |> ignore

                    let gitignore =
                        Path.Combine(
                            AbsolutePath.value root,
                            ProjectCache.PROJECT_CACHE_DIR_NAME,
                            ".gitignore"
                        )

                    assertThat
                        (File.Exists gitignore)
                        (tag "the cache carries a gitignore" >> isTrue)

                    assertThat
                        ((File.ReadAllText gitignore).Contains "*")
                        (tag "which covers everything in it, the file included" >> isTrue)

                    Directory.Delete(AbsolutePath.value root, true)
            )

            test (
                "an entry this build did not ask for is dropped",
                fun _ ->
                    let root = scratch ()

                    ProjectCache.directory root "bundles" "before" |> ignore
                    ProjectCache.directory root "bundles" "after" |> ignore

                    ProjectCache.forgetOthers root "bundles" [ "after" ]

                    let group =
                        Path.Combine(
                            AbsolutePath.value root,
                            ProjectCache.PROJECT_CACHE_DIR_NAME,
                            "bundles"
                        )

                    assertThat
                        (Directory.EnumerateDirectories group
                         |> Seq.map Path.GetFileName
                         |> List.ofSeq)
                        (tag "only what the build used is left" >> isEqualTo [ "after" ])

                    Directory.Delete(AbsolutePath.value root, true)
            )

            test (
                "tidying one job leaves another job's entries alone",
                fun _ ->
                    let root = scratch ()

                    ProjectCache.directory root "bundles" "stale" |> ignore
                    ProjectCache.directory root "live-example" "kept" |> ignore

                    ProjectCache.forgetOthers root "bundles" []

                    let entries (group: string) =
                        Path.Combine(
                            AbsolutePath.value root,
                            ProjectCache.PROJECT_CACHE_DIR_NAME,
                            group
                        )
                        |> Directory.EnumerateDirectories
                        |> Seq.map Path.GetFileName
                        |> List.ofSeq

                    assertThat
                        (entries "bundles")
                        (tag "the group asked about is emptied" >> isEqualTo [])

                    assertThat
                        (entries "live-example")
                        (tag "and no other group is touched" >> isEqualTo [ "kept" ])

                    Directory.Delete(AbsolutePath.value root, true)
            )
        ]
    )

let watcher =
    testList (
        "watcher",
        [
            test (
                "a file outside the project is followed once a build asks for it",
                fun _ ->
                    let temporary =
                        Path.Combine(
                            Path.GetTempPath(),
                            "nacara-tests",
                            Guid.NewGuid().ToString "N"
                        )

                    let project = Path.Combine(temporary, "docs")
                    let elsewhere = Path.Combine(temporary, "packages")
                    Directory.CreateDirectory project |> ignore
                    Directory.CreateDirectory elsewhere |> ignore

                    let followed = Path.Combine(elsewhere, "CHANGELOG.md")
                    File.WriteAllText(followed, "## 1.0.0\n")

                    let seen = System.Collections.Concurrent.ConcurrentBag<string>()

                    use watcher =
                        new Watcher(
                            AbsolutePath.create project,
                            [],
                            TimeSpan.FromMilliseconds 20.,
                            fun changes -> changes |> List.iter seen.Add
                        )

                    watcher.Start()
                    watcher.Follow [ AbsolutePath.create followed ]

                    File.WriteAllText(followed, "## 1.1.0\n")

                    let deadline = DateTime.UtcNow.AddSeconds 5.

                    while Seq.isEmpty seen && DateTime.UtcNow < deadline do
                        Thread.Sleep 25

                    assertThat
                        (seen |> Seq.exists (fun path -> Path.GetFileName path = "CHANGELOG.md"))
                        (tag "the change reached the watcher" >> isTrue)

                    Directory.Delete(temporary, true)
            )

            test (
                "what the build wrote into the cache does not ask for another build",
                fun _ ->
                    let temporary =
                        Path.Combine(
                            Path.GetTempPath(),
                            "nacara-tests",
                            Guid.NewGuid().ToString "N"
                        )

                    Directory.CreateDirectory temporary |> ignore
                    let root = AbsolutePath.create temporary

                    let entry = ProjectCache.directory root "bundles" "abcd1234"
                    let bundle = Path.Combine(AbsolutePath.value entry, "bundle.js")
                    File.WriteAllText(bundle, "let x = 1\n")

                    let seen = System.Collections.Concurrent.ConcurrentBag<string>()

                    use watcher =
                        new Watcher(
                            root,
                            [],
                            TimeSpan.FromMilliseconds 20.,
                            fun changes -> changes |> List.iter seen.Add
                        )

                    watcher.Start()
                    File.WriteAllText(bundle, "let x = 2\n")

                    let page = Path.Combine(temporary, "index.md")
                    File.WriteAllText(page, "# Hello\n")

                    let deadline = DateTime.UtcNow.AddSeconds 5.

                    let arrived () =
                        seen |> Seq.exists (fun path -> Path.GetFileName path = "index.md")

                    while not (arrived ()) && DateTime.UtcNow < deadline do
                        Thread.Sleep 25

                    assertThat (arrived ()) (tag "a page the site is made of is reported" >> isTrue)

                    Thread.Sleep 400

                    assertThat
                        (seen |> Seq.exists (fun path -> Path.GetFileName path = "bundle.js"))
                        (tag "and what the build itself wrote is not" >> isEqualTo false)

                    Directory.Delete(temporary, true)
            )

            test (
                "what a tool cached in a dotted directory does not ask for another build",
                fun _ ->
                    let temporary =
                        Path.Combine(
                            Path.GetTempPath(),
                            "nacara-tests",
                            Guid.NewGuid().ToString "N"
                        )

                    Directory.CreateDirectory temporary |> ignore
                    let root = AbsolutePath.create temporary

                    let cache = Path.Combine(temporary, ".rumdl_cache")
                    Directory.CreateDirectory cache |> ignore
                    let index = Path.Combine(cache, "workspace_index.bin")
                    File.WriteAllText(index, "1\n")

                    let seen = System.Collections.Concurrent.ConcurrentBag<string>()

                    use watcher =
                        new Watcher(
                            root,
                            [],
                            TimeSpan.FromMilliseconds 20.,
                            fun changes -> changes |> List.iter seen.Add
                        )

                    watcher.Start()
                    File.WriteAllText(index, "2\n")

                    let page = Path.Combine(temporary, "index.md")
                    File.WriteAllText(page, "# Hello\n")

                    let deadline = DateTime.UtcNow.AddSeconds 5.

                    let arrived () =
                        seen |> Seq.exists (fun path -> Path.GetFileName path = "index.md")

                    while not (arrived ()) && DateTime.UtcNow < deadline do
                        Thread.Sleep 25

                    assertThat (arrived ()) (tag "a page the site is made of is reported" >> isTrue)

                    Thread.Sleep 400

                    assertThat
                        (seen
                         |> Seq.exists (fun path -> Path.GetFileName path = "workspace_index.bin"))
                        (tag "and the linter's own index is not" >> isEqualTo false)

                    Directory.Delete(temporary, true)
            )

            test (
                "a file already inside the project is not followed twice",
                fun _ ->
                    let temporary =
                        Path.Combine(
                            Path.GetTempPath(),
                            "nacara-tests",
                            Guid.NewGuid().ToString "N"
                        )

                    Directory.CreateDirectory temporary |> ignore
                    let inside = Path.Combine(temporary, "CHANGELOG.md")
                    File.WriteAllText(inside, "## 1.0.0\n")

                    use watcher =
                        new Watcher(
                            AbsolutePath.create temporary,
                            [],
                            TimeSpan.FromMilliseconds 20.,
                            ignore
                        )

                    watcher.Follow [ AbsolutePath.create inside ]
                    watcher.Follow [ AbsolutePath.create inside ]

                    assertThat
                        true
                        (tag "following what is already watched changes nothing" >> isTrue)

                    Directory.Delete(temporary, true)
            )
        ]
    )

let decoding =
    testList (
        "decoding",
        [
            test (
                "keyValuePairs reads an object whatever its keys are",
                fun _ ->
                    let document = "main:\n  data-pagefind-weight: \"0.3\"\n  data-kind: legacy\n"

                    assertThat
                        (Yaml.decode
                            (Decode.field "main" (Decode.keyValuePairs Decode.string))
                            document)
                        (tag "every pair, in the order written"
                         >> isEqualTo (
                             Ok
                                 [
                                     "data-pagefind-weight", "0.3"
                                     "data-kind", "legacy"
                                 ]
                         ))
            )

            test (
                "a nested field is reached by naming the path to it",
                fun _ ->
                    let document = "site:\n  author:\n    name: Maxime\n"

                    assertThat
                        (Yaml.decode
                            (Decode.at
                                [
                                    "site"
                                    "author"
                                    "name"
                                ]
                                Decode.string)
                            document)
                        (tag "read from the bottom of the path" >> isEqualTo (Ok "Maxime"))
            )

            test (
                "a step that is not there is missing, not absent",
                fun _ ->
                    let document = "site:\n  author:\n    name: Maxime\n"

                    let failure =
                        Yaml.decode
                            (Decode.at
                                [
                                    "site"
                                    "editor"
                                    "name"
                                ]
                                Decode.string)
                            document

                    match failure with
                    | Ok _ -> assertThat false (tag "a path to nothing fails" >> isTrue)
                    | Error decodeError ->
                        assertThat
                            decodeError.Message
                            (tag "and names the step that stopped it"
                             >> isEqualTo "Missing required field 'editor'")

                        assertThat
                            decodeError.Path
                            (tag "under the path walked so far" >> isEqualTo "site.editor")
            )

            test (
                "the optional form gives back nothing instead",
                fun _ ->
                    let document = "site:\n  author:\n    name: Maxime\n"

                    assertThat
                        (Yaml.decode
                            (Decode.optionalAt
                                [
                                    "site"
                                    "editor"
                                    "name"
                                ]
                                Decode.string)
                            document)
                        (tag "a path that runs out is simply absent" >> isEqualTo (Ok None))
            )

            test (
                "a value that is there but wrong is reported where it is written",
                fun _ ->
                    let document = "site:\n  author:\n    name:\n      - Maxime\n"

                    match
                        Yaml.decode
                            (Decode.at
                                [
                                    "site"
                                    "author"
                                    "name"
                                ]
                                Decode.string)
                            document
                    with
                    | Ok _ -> assertThat false (tag "a list is not a string" >> isTrue)
                    | Error decodeError ->
                        assertThat
                            decodeError.Line
                            (tag "the line the offending value starts on" >> isEqualTo 4)

                        assertThat
                            decodeError.Path
                            (tag "and the path to it" >> isEqualTo "site.author.name")
            )

            test (
                "reading a field of something that is not an object says so",
                fun _ ->
                    let document = "site: Maxime\n"

                    match
                        Yaml.decode
                            (Decode.at
                                [
                                    "site"
                                    "author"
                                ]
                                Decode.string)
                            document
                    with
                    | Ok _ -> assertThat false (tag "a scalar has no fields" >> isTrue)
                    | Error decodeError ->
                        assertThat
                            decodeError.Message
                            (tag "rather than claiming the field is missing"
                             >> isEqualTo "Expected an object but got the value 'Maxime'")
            )
        ]
    )

let all =
    testList (
        "Core",
        [
            slug
            route
            url
            fileRoutes
            paths
            diagnostics
            projectRoot
            projectCache
            watcher
            decoding
        ]
    )
