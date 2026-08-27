module Nacara.Tests.Build

open Scriptorium.Nib.Assertion
open Scriptorium.Nib.Snapshot
open type Scriptorium.Quill.Test
open System.IO
open Nacara.Core
open Nacara.Plugins
open Nacara.Theme
open Nacara.Tests

let private buildFixture () =
    let root = Fixture.copyToTemporaryDirectory ()
    let result = Build.run root Fixture.site
    root, result

let private outputText (root: AbsolutePath) (path: string) =
    File.ReadAllText(Path.Combine(AbsolutePath.value root, "output", path))

let all =
    testList (
        "Build",
        [
            // One file holds these snapshots, so tests writing it at the same time lose all but one.
            testSequenced (
                "rendered pages",
                [
                    test (
                        "the home page renders as expected",
                        fun context ->
                            let root, _ = buildFixture ()
                            context.snapshotWith (id, outputText root "index.html")
                    )
                    test (
                        "a page with code blocks renders as expected",
                        fun context ->
                            let root, _ = buildFixture ()

                            context.snapshotWith (
                                id,
                                outputText root "guide/getting-started/index.html"
                            )
                    )
                    test (
                        "frames, line numbers and collapsing render as expected",
                        fun context ->
                            let root, _ = buildFixture ()
                            context.snapshotWith (id, outputText root "guide/advanced/index.html")
                    )
                ]
            )

            test (
                "a rule a site writes is added after the theme's own",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    let theme =
                        Theme.defaults
                        |> Theme.css """[data-section="guide"] { --nacara-sidebar-width: 20rem; }"""
                        |> Theme.css "body { font-feature-settings: \"ss01\"; }"

                    let site =
                        Site.create "Fixture"
                        |> Site.baseUrl "/"
                        |> Site.output "output"
                        |> Site.noStaticFiles
                        |> Markdown.register
                        |> Theme.register theme
                        |> Site.collection (Theme.docs theme "docs")

                    Build.run root site |> ignore

                    let html =
                        File.ReadAllText(Path.Combine(AbsolutePath.value root, "output/index.html"))

                    assertThat
                        (html.Contains "--nacara-sidebar-width: 20rem")
                        (tag "the rule reaches the page" >> isTrue)

                    assertThat
                        (html.IndexOf "sidebar-width" < html.IndexOf "font-feature-settings")
                        (tag "and a second call adds to the first" >> isTrue)

                    assertThat
                        (html.LastIndexOf "<link rel=\"stylesheet\"" < html.IndexOf "sidebar-width")
                        (tag "and lands after the theme's own css" >> isTrue)
            )

            test (
                "a menu group folds, and opens on the page being read",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    let theme =
                        Theme.defaults
                        |> Theme.menu
                            "guide"
                            [
                                Menu.section
                                    "Section"
                                    [
                                        Menu.section "Open group" [ Menu.page "guide/advanced.md" ]
                                        Menu.section
                                            "Folded group"
                                            [ Menu.page "guide/getting-started.md" ]
                                    ]
                            ]

                    let site =
                        Site.create "Fixture"
                        |> Site.baseUrl "/"
                        |> Site.output "output"
                        |> Site.noStaticFiles
                        |> Markdown.register
                        |> Theme.register theme
                        |> Site.collection (Theme.docs theme "docs")

                    Build.run root site |> ignore

                    let html =
                        File.ReadAllText(
                            Path.Combine(
                                AbsolutePath.value root,
                                "output/guide/advanced/index.html"
                            )
                        )

                    let group (label: string) =
                        let marker = html.IndexOf $"data-nacara-menu-group=\"%s{label}\""
                        let opening = html.LastIndexOf("<details", marker)
                        html.Substring(opening, marker - opening)

                    assertThat
                        ((group "Open group").Contains "open")
                        (tag "the group holding this page is open" >> isTrue)

                    assertThat
                        ((group "Folded group").Contains "open")
                        (tag "and the one that does not is folded" >> isFalse)

                    assertThat
                        (html.Contains "nacara-sidebar__group-title")
                        (tag "a nested group is something a reader can open and close" >> isTrue)
            )

            test (
                "a written page can link to a generated one",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    File.WriteAllText(
                        Path.Combine(AbsolutePath.value root, "docs/points-at.md"),
                        "---\ntitle: Points at\n---\n\nSee [the note](../notes/hello.md).\n"
                    )

                    let notes =
                        Collection.create "notes" Fixture.decoder
                        |> Collection.producer
                            "notes"
                            (fun _ ->
                                [
                                    GeneratedContent.create
                                        "hello.md"
                                        "---\ntitle: Hello\n---\n\nMade up on the spot.\n"
                                ]
                            )
                        |> Collection.title _.Title
                        |> Collection.layout Fixture.layout

                    let result = Build.run root (Fixture.site |> Site.collection notes)

                    assertThat
                        (result.Diagnostics
                         |> List.exists (fun item -> item.Code = "markdown/link-target-missing"))
                        (tag "the link resolves" >> isFalse)

                    let html =
                        File.ReadAllText(
                            Path.Combine(AbsolutePath.value root, "output/points-at/index.html")
                        )

                    assertThat
                        (html.Contains ".md\"")
                        (tag "no markdown path survives into the output" >> isFalse)

                    assertThat
                        (html.Contains "hello")
                        (tag "and the link names the page it found" >> isTrue)
            )

            test (
                "a file tree says which entries are directories",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    File.WriteAllText(
                        Path.Combine(AbsolutePath.value root, "docs/tree.md"),
                        "---\ntitle: Tree\n---\n\n:::filetree\n- src\n  - Program.fs\n- empty/\n- README.md\n:::\n"
                    )

                    Build.run root Fixture.site |> ignore

                    let html =
                        File.ReadAllText(
                            Path.Combine(AbsolutePath.value root, "output/tree/index.html")
                        )

                    assertThat
                        (html.Contains
                            """data-kind="directory"><details class="nacara-file-tree__folder""")
                        (tag "an entry holding a list is a directory" >> isTrue)

                    assertThat
                        (html.Contains """open><summary class="nacara-file-tree__name">src""")
                        (tag "that opens, with its name as the summary" >> isTrue)

                    assertThat
                        (html.Contains
                            "data-kind=\"directory\"><span class=\"nacara-file-tree__name\">empty<")
                        (tag "so is one written with a trailing slash, which is not shown back"
                         >> isTrue)

                    assertThat
                        (html.Contains
                            "data-kind=\"file\"><span class=\"nacara-file-tree__name\">README.md")
                        (tag "and everything else is a file" >> isTrue)
            )

            test (
                "a page can say it is not part of a sequence",
                fun _ ->
                    let read yaml =
                        match Yaml.decode DocFrontMatter.decoder yaml with
                        | Ok frontMatter -> DocFrontMatter.toDocPage frontMatter
                        | Error error -> failwith $"%A{error}"

                    let standalone = read "title: Standalone\npageNav: false"

                    assertThat
                        standalone.ShowPageNav
                        (tag "pageNav: false drops previous and next" >> isFalse)

                    assertThat
                        (standalone.ShowMenu, standalone.ShowToc)
                        (tag "and leaves the rest of the page alone" >> isEqualTo (true, true))

                    assertThat
                        (read "title: Ordinary").ShowPageNav
                        (tag "a page that says nothing keeps them" >> isTrue)
            )

            test (
                "a page says what its menu does",
                fun _ ->
                    let read yaml =
                        match Yaml.decode DocFrontMatter.decoder yaml with
                        | Ok frontMatter -> DocFrontMatter.toDocPage frontMatter
                        | Error error -> failwith $"%A{error}"

                    let ordinary = read "title: Ordinary"

                    assertThat
                        (ordinary.MenuFilter, ordinary.MenuMemory)
                        (tag "left out, the theme decides whether to filter, and folding carries"
                         >> isEqualTo (None, true))

                    assertThat
                        (read "title: Reference\nmenuFilter: true").MenuFilter
                        (tag "menuFilter: true asks for the box" >> isEqualTo (Some true))

                    assertThat
                        (read "title: Short\nmenuFilter: false").MenuFilter
                        (tag "menuFilter: false refuses it" >> isEqualTo (Some false))

                    assertThat
                        (read "title: Reference\nmenuMemory: false").MenuMemory
                        (tag "menuMemory: false opens the menu the same way on every page"
                         >> isFalse)
            )

            test (
                "check renders everything and writes nothing",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()
                    let output = Path.Combine(AbsolutePath.value root, "output")
                    let result = Build.check root Fixture.site

                    assertThat
                        (List.length result.Pages)
                        (tag "every page was built" >> isEqualTo 3)

                    assertThat result.Succeeded (tag "and the site is sound" >> isTrue)

                    assertThat
                        (Directory.Exists output
                         && Directory.EnumerateFiles(output, "*", SearchOption.AllDirectories)
                            |> Seq.isEmpty
                            |> not)
                        (tag "with nothing put on disk" >> isFalse)

                    assertThat
                        (result.WrittenFiles > 0)
                        (tag "and what it would have written is known" >> isTrue)
            )

            test (
                "check leaves a site that is already built alone",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()
                    let output = Path.Combine(AbsolutePath.value root, "output")
                    Build.run root Fixture.site |> ignore
                    let before = Directory.GetFiles(output, "*", SearchOption.AllDirectories)

                    let written =
                        before |> Array.map (fun file -> file, File.GetLastWriteTimeUtc file)

                    Build.check root Fixture.site |> ignore

                    assertThat
                        (written
                         |> Array.forall (fun (file, at) -> File.GetLastWriteTimeUtc file = at))
                        (tag "not one file was touched" >> isTrue)
            )

            test (
                "a language no highlighter covers is reported where it is written",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    File.WriteAllText(
                        Path.Combine(AbsolutePath.value root, "docs/fences.md"),
                        "---\ntitle: Fences\n---\n\n```gleam\npub fn main() { 1 }\n```\n\n```text\nplain on purpose\n```\n"
                    )

                    let result = Build.check root Fixture.site

                    let reported =
                        result.Diagnostics
                        |> List.filter (fun item -> item.Code = "markdown/unknown-language")

                    assertThat
                        (List.length reported)
                        (tag "the language nobody covers, and only it" >> isEqualTo 1)

                    assertThat
                        (reported |> List.map (fun item -> item.Span |> Option.map _.Line))
                        (tag "at the line the fence is on" >> isEqualTo [ Some 5 ])
            )

            test (
                "inline code says its language the way a fence does",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    File.WriteAllText(
                        Path.Combine(AbsolutePath.value root, "docs/inline.md"),
                        "---\ntitle: Inline\n---\n\nA `let a = 1{:fsharp}` B `let b = 2`{fsharp} "
                        + "C `let c = 3`{lang=fsharp} D `{:js}` E `plain`{disabled} F `let d = 4{:gleam}`\n"
                    )

                    let result = Build.run root Fixture.site
                    let html = outputText root "inline/index.html"

                    /// What one of the code spans rendered as, found by the letter before it.
                    let span (letter: string) =
                        let opening = html.IndexOf($"%s{letter} <code")
                        let closing = html.IndexOf("</code>", opening)

                        html.Substring(
                            opening + letter.Length + 1,
                            closing - opening - letter.Length + 6
                        )

                    for letter in
                        [
                            "A"
                            "B"
                            "C"
                        ] do
                        assertThat
                            ((span letter).Contains "<span class=\"tok-keyword\">let</span>")
                            (tag $"%s{letter} is coloured" >> isTrue)

                    assertThat
                        ((span "A").Contains "{:fsharp}")
                        (tag "the marker is not left in the text" >> isFalse)

                    assertThat
                        ((span "B").Contains "fsharp")
                        (tag "a claimed attribute is consumed" >> isFalse)

                    assertThat
                        (span "D")
                        (tag "a marker with nothing before it is text"
                         >> isEqualTo "<code>{:js}</code>")

                    assertThat
                        ((span "E").Contains "disabled=\"\"")
                        (tag "an attribute nobody claims is left alone" >> isTrue)

                    let reported =
                        result.Diagnostics
                        |> List.filter (fun item -> item.Code = "markdown/unknown-language")

                    assertThat
                        (List.length reported)
                        (tag "the language nobody covers, and only it" >> isEqualTo 1)
            )

            test (
                "a plugin can add a command of its own",
                fun _ ->
                    let ran = ResizeArray<string>()

                    let plugin =
                        { new IPlugin with
                            member _.Name = "greeter"

                            member _.Configure registry =
                                registry
                                |> Registry.command (
                                    PluginCommand.create
                                        "greet"
                                        "Say hello"
                                        (fun context ->
                                            ran.AddRange context.Arguments
                                            0
                                        )
                                )
                        }

                    let registry = Registry.ofPlugins [ plugin ]

                    assertThat
                        (registry.Commands |> List.map _.Name)
                        (tag "the command is there to be found" >> isEqualTo [ "greet" ])

                    let command = List.head registry.Commands

                    assertThat
                        command.Source
                        (tag "and knows which plugin brought it" >> isEqualTo "greeter")

                    assertThat command.Help (tag "with room for its own usage" >> isEqualTo None)

                    assertThat
                        (command |> PluginCommand.help "greet [name]" |> _.Help)
                        (tag "which a plugin fills in" >> isEqualTo (Some "greet [name]"))

                    let root = AbsolutePath.create "/tmp"

                    assertThat
                        (command.Run
                            {
                                Site = Site.toInfo (Site.create "Greeting")
                                ProjectRoot = root
                                OutputDirectory = AbsolutePath.combine root [ "output" ]
                                Arguments =
                                    [
                                        "--loudly"
                                        "world"
                                    ]
                            })
                        (tag "and running it returns its exit code" >> isEqualTo 0)

                    assertThat
                        (List.ofSeq ran)
                        (tag "with the arguments passed through untouched"
                         >> isEqualTo
                             [
                                 "--loudly"
                                 "world"
                             ])
            )

            test (
                "a plugin can add a Markdig extension of its own",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    File.WriteAllText(
                        Path.Combine(AbsolutePath.value root, "docs/smart.md"),
                        "---\ntitle: Smart\n---\n\nHe said \"hello\" to her.\n"
                    )

                    let quotes =
                        { new IPlugin with
                            member _.Name = "smarty"

                            member _.Configure registry =
                                registry
                                |> Registry.extra (
                                    Markdig.Extensions.SmartyPants.SmartyPantsExtension(
                                        Markdig.Extensions.SmartyPants.SmartyPantOptions()
                                    )
                                    :> Markdig.IMarkdownExtension
                                )
                        }

                    Build.run root (Fixture.site |> Site.plugin quotes) |> ignore

                    let html =
                        File.ReadAllText(
                            Path.Combine(AbsolutePath.value root, "output/smart/index.html")
                        )

                    assertThat
                        (html.Contains "&ldquo;hello&rdquo;")
                        (tag "the extension the plugin registered ran" >> isTrue)
            )

            test (
                "a preview shows the source and what it renders as",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    File.WriteAllText(
                        Path.Combine(AbsolutePath.value root, "docs/preview.md"),
                        "---\ntitle: Preview\n---\n\n:::::preview\n````markdown\n:::note Careful\nWatch out.\n:::\n````\n:::::\n"
                    )

                    Build.run root Fixture.site |> ignore

                    let html =
                        File.ReadAllText(
                            Path.Combine(AbsolutePath.value root, "output/preview/index.html")
                        )

                    assertThat
                        (html.Contains "nacara-preview__result")
                        (tag "the example is rendered below its source" >> isTrue)

                    assertThat
                        (html.Contains "data-kind=\"note\"")
                        (tag "as the callout it describes, not as text" >> isTrue)

                    assertThat
                        (html.Contains ":::note Careful")
                        (tag "and the markdown that produced it is on the page" >> isTrue)
            )

            test (
                "a preview of html is inserted as it is",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    File.WriteAllText(
                        Path.Combine(AbsolutePath.value root, "docs/raw-preview.md"),
                        "---\ntitle: Raw preview\n---\n\n::::preview\n```html\n<span class=\"badge\">New</span>\n```\n::::\n"
                    )

                    Build.run root Fixture.site |> ignore

                    let html =
                        File.ReadAllText(
                            Path.Combine(AbsolutePath.value root, "output/raw-preview/index.html")
                        )

                    assertThat
                        (html.Contains "<span class=\"badge\">New</span>")
                        (tag "a component with no markdown syntax can still be shown" >> isTrue)
            )

            test (
                "a group can be the page that introduces what it holds",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    let theme =
                        { Theme.defaults with
                            Menus =
                                Map
                                    [
                                        "guide",
                                        [
                                            Menu.section
                                                "Section"
                                                [
                                                    Menu.group
                                                        "guide/advanced.md"
                                                        [
                                                            Menu.page "guide/getting-started.md"
                                                            |> Menu.badge "New"
                                                        ]
                                                ]
                                        ]
                                    ]
                        }

                    let site =
                        Site.create "Fixture"
                        |> Site.baseUrl "/"
                        |> Site.output "output"
                        |> Site.noStaticFiles
                        |> Markdown.register
                        |> Theme.register theme
                        |> Site.collection (Theme.docs theme "docs")

                    Build.run root site |> ignore

                    let html =
                        File.ReadAllText(
                            Path.Combine(
                                AbsolutePath.value root,
                                "output/guide/getting-started/index.html"
                            )
                        )

                    assertThat
                        (html.Contains "nacara-sidebar__group-link\" href=\"/guide/advanced/\"")
                        (tag "the group's label links to its own page" >> isTrue)

                    assertThat
                        (html.Contains "nacara-sidebar__link\" href=\"/guide/advanced/\"")
                        (tag "it is not also listed inside itself" >> isFalse)

                    assertThat
                        (html.Contains "<span class=\"nacara-badge\" data-kind=\"new\">New</span>")
                        (tag "and an entry wears the badge its site gave it" >> isTrue)
            )

            test (
                "a site with no 404 page gets the theme's",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()
                    let site = Fixture.site |> Theme.register Theme.defaults
                    let cache = BuildCache()
                    Build.runWith cache root site |> ignore

                    let path = Path.Combine(AbsolutePath.value root, "output/404.html")
                    assertThat (File.Exists path) (tag "the theme writes one" >> isTrue)

                    let html = File.ReadAllText path

                    assertThat
                        (html.Contains "Page not found")
                        (tag "with something to read" >> isTrue)

                    assertThat
                        (html.Contains "nacara-navbar")
                        (tag "and the site around it" >> isTrue)

                    let again = Build.runWith cache root site

                    assertThat
                        again.WrittenFiles
                        (tag "and rebuilding does not touch it" >> isEqualTo 0)
            )

            test (
                "a site's own 404 page wins over the theme's",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    File.WriteAllText(
                        Path.Combine(AbsolutePath.value root, "docs/404.md"),
                        "---\ntitle: Lost\n---\n\nMine, not the theme's.\n"
                    )

                    let site =
                        Site.create "Fixture"
                        |> Site.baseUrl "/"
                        |> Site.output "output"
                        |> Site.noStaticFiles
                        |> Markdown.register
                        |> Theme.register Theme.defaults
                        |> Site.collection (
                            Fixture.docs
                            |> Collection.route (fun page ->
                                if RelativePath.value page.RelativePath = "404.md" then
                                    Route.file page.Locale "404.html"
                                else
                                    Collection.defaultRoute page
                            )
                        )

                    let result = Build.run root site

                    let html =
                        File.ReadAllText(Path.Combine(AbsolutePath.value root, "output/404.html"))

                    assertThat
                        (html.Contains "Mine, not the theme")
                        (tag "the site's page is the one written" >> isTrue)

                    assertThat
                        (result.Diagnostics
                         |> List.forall (fun item -> item.Code <> "nacara/duplicate-output"))
                        (tag "and the theme stands down rather than fighting it for the file"
                         >> isTrue)
            )

            test (
                "a page can be written as a file rather than a directory",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    File.WriteAllText(
                        Path.Combine(AbsolutePath.value root, "docs/404.md"),
                        "---\ntitle: Not found\n---\n\nNothing here.\n"
                    )

                    let site =
                        Site.create "Fixture"
                        |> Site.baseUrl "/"
                        |> Site.output "output"
                        |> Site.noStaticFiles
                        |> Markdown.register
                        |> Site.collection (
                            Fixture.docs
                            |> Collection.route (fun page ->
                                if RelativePath.value page.RelativePath = "404.md" then
                                    Route.file page.Locale "404.html"
                                else
                                    Collection.defaultRoute page
                            )
                        )

                    Build.run root site |> ignore
                    let output = Path.Combine(AbsolutePath.value root, "output")

                    assertThat
                        (File.Exists(Path.Combine(output, "404.html")))
                        (tag "written where it is named" >> isTrue)

                    assertThat
                        (File.Exists(Path.Combine(output, "404/index.html")))
                        (tag "and not as a directory, which no host would use" >> isFalse)
            )

            test (
                "a file nothing knows how to read is reported",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    let site =
                        Site.create "Bare"
                        |> Site.output "output"
                        |> Site.noStaticFiles
                        |> Site.collection Fixture.docs

                    let result = Build.run root site

                    assertThat result.Succeeded (tag "the build fails" >> isFalse)

                    match
                        result.Diagnostics
                        |> List.tryFind (fun item ->
                            item.Code = "nacara/unknown-front-matter-format"
                        )
                    with
                    | None -> assertThat false (tag "and says nothing can read the file" >> isTrue)
                    | Some diagnostic ->
                        assertThat
                            (diagnostic.Message.Contains ".md")
                            (tag "naming the kind of file" >> isTrue)

                        assertThat
                            diagnostic.Hint.IsSome
                            (tag "and pointing at the plugin that would" >> isTrue)
            )

            test (
                "two formats claiming one extension is reported, and the last wins",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    File.WriteAllText(
                        Path.Combine(AbsolutePath.value root, "docs/curly.md"),
                        "{{{\ntitle: Curly\n}}}\n\nBody.\n"
                    )

                    let curly =
                        { new IPlugin with
                            member _.Name = "curly"

                            member _.Configure registry =
                                registry
                                |> Registry.frontMatter
                                    {
                                        Name = "curly"
                                        Extensions = [ ".md" ]
                                        Opening = "{{{"
                                        Closing = "}}}"
                                        Wrapper = None
                                    }
                        }

                    let result = Build.run root (Fixture.site |> Site.plugin curly)

                    assertThat
                        (result.Diagnostics
                         |> List.exists (fun item ->
                             item.Code = "nacara/duplicate-front-matter-format"
                         ))
                        (tag "the clash is reported" >> isTrue)

                    assertThat
                        (result.Pages |> List.exists (fun page -> page.Title = "Curly"))
                        (tag "and the last format registered is the one used" >> isTrue)
            )

            test (
                "a missing minifier is a warning, not a failed build",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    let site =
                        Fixture.site
                        |> Site.plugin (
                            { new IPlugin with
                                member _.Name = "css"

                                member _.Configure registry =
                                    registry
                                    |> Registry.asset (
                                        WriteText(
                                            "body { color: red }",
                                            RelativePath.create "assets/x.css"
                                        )
                                    )
                            }
                        )
                        |> LightningCss.registerWith (fun options ->
                            { options with
                                BinaryPath = Some "/nonexistent/lightningcss"
                            }
                        )

                    let result = Build.run root site

                    assertThat result.Succeeded (tag "the build still succeeds" >> isTrue)

                    assertThat
                        (result.Diagnostics
                         |> List.exists (fun item -> item.Code = "lightningcss/css-not-minified"))
                        (tag "and says why the css is not minified" >> isTrue)

                    assertThat
                        (File.ReadAllText(
                            Path.Combine(AbsolutePath.value root, "output/assets/x.css")
                        ))
                        (tag "with the stylesheet shipped unchanged"
                         >> isEqualTo "body { color: red }")
            )

            test (
                "a text asset is transformed on its way out",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    let plugin =
                        { new IPlugin with
                            member _.Name = "shouty"

                            member _.Configure registry =
                                registry
                                |> Registry.asset (
                                    WriteText(
                                        "body { color: red }",
                                        RelativePath.create "assets/x.css"
                                    )
                                )
                                |> Registry.asset (
                                    WriteText("hello", RelativePath.create "assets/x.txt")
                                )
                                |> Registry.assetTransform
                                    {
                                        Name = "shout"
                                        Extensions = [ ".css" ]
                                        Transform =
                                            fun context -> context.Content.ToUpperInvariant()
                                    }
                        }

                    Build.run root (Fixture.site |> Site.plugin plugin) |> ignore

                    let read name =
                        File.ReadAllText(
                            Path.Combine(AbsolutePath.value root, "output/assets/" + name)
                        )

                    assertThat
                        (read "x.css")
                        (tag "the stylesheet went through the transform"
                         >> isEqualTo "BODY { COLOR: RED }")

                    assertThat (read "x.txt") (tag "and nothing else did" >> isEqualTo "hello")
            )

            test (
                "register is exactly Site.plugin of create",
                fun _ ->
                    let sugared = Site.create "Docs" |> Markdown.register
                    let explicit = Site.create "Docs" |> Site.plugin (Markdown.create ())

                    assertThat
                        (List.length sugared.Plugins)
                        (tag "the same number of plugins"
                         >> isEqualTo (List.length explicit.Plugins))

                    assertThat
                        (sugared.Plugins |> List.map _.Name)
                        (tag "and the same plugin"
                         >> isEqualTo (explicit.Plugins |> List.map _.Name))
            )

            test (
                "a build produces one page per markdown file",
                fun _ ->
                    let _, result = buildFixture ()

                    assertThat
                        (List.length result.Pages)
                        (tag "three markdown files, three pages" >> isEqualTo 3)

                    assertThat result.Succeeded (tag "the fixture builds without errors" >> isTrue)
            )

            test (
                "pages are written as directory-style urls",
                fun _ ->
                    let root, _ = buildFixture ()
                    let output = Path.Combine(AbsolutePath.value root, "output")

                    assertThat
                        (File.Exists(Path.Combine(output, "index.html")))
                        (tag "the home page" >> isTrue)

                    assertThat
                        (File.Exists(Path.Combine(output, "guide/getting-started/index.html")))
                        (tag "a nested page" >> isTrue)
            )

            test (
                "static files are copied",
                fun _ ->
                    let root, _ = buildFixture ()

                    assertThat
                        (File.Exists(Path.Combine(AbsolutePath.value root, "output/humans.txt")))
                        (tag "humans.txt" >> isTrue)
            )

            test (
                "markdown links are rewritten to routes",
                fun _ ->
                    let root, _ = buildFixture ()
                    let html = outputText root "guide/getting-started/index.html"

                    assertThat
                        (html.Contains "href=\"/\"")
                        (tag "a link to ../index.md becomes the site root" >> isTrue)

                    assertThat
                        (html.Contains "href=\"/guide/advanced/#going-further\"")
                        (tag "an anchor link keeps its fragment" >> isTrue)

                    assertThat
                        (html.Contains ".md\"")
                        (tag "no link still points at a markdown file" >> isFalse)
            )

            test (
                "a link to nothing fails the build",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    File.WriteAllText(
                        Path.Combine(AbsolutePath.value root, "docs/broken.md"),
                        "---\ntitle: Broken\n---\n\nSee [the missing page](nowhere.md).\n"
                    )

                    let result = Build.run root Fixture.site
                    assertThat result.Succeeded (tag "an unresolved link is an error" >> isFalse)

                    assertThat
                        (result.Diagnostics
                         |> List.exists (fun item -> item.Code = "markdown/link-target-missing"))
                        (tag "and it is reported as markdown/link-target-missing" >> isTrue)
            )

            test (
                "a link to a missing anchor fails the build",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    File.WriteAllText(
                        Path.Combine(AbsolutePath.value root, "docs/anchor.md"),
                        "---\ntitle: Anchor\n---\n\nSee [the missing anchor](guide/advanced.md#nope).\n"
                    )

                    let result = Build.run root Fixture.site

                    assertThat
                        (result.Diagnostics
                         |> List.exists (fun item -> item.Code = "markdown/anchor-missing"))
                        (tag "anchors are checked once every page has been rendered" >> isTrue)
            )

            test (
                "a link to an anchor below the table of contents levels is fine",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    File.WriteAllText(
                        Path.Combine(AbsolutePath.value root, "docs/deep.md"),
                        "---\ntitle: Deep\n---\n\n#### A small heading\n\nSee [it](deep.md#a-small-heading).\n"
                    )

                    let result = Build.run root Fixture.site

                    assertThat
                        (result.Diagnostics
                         |> List.exists (fun item -> item.Code = "markdown/anchor-missing"))
                        (tag "the anchor exists on the page, so nothing is reported" >> isFalse)
            )

            test (
                "headings keep the letters they are written with",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    File.WriteAllText(
                        Path.Combine(AbsolutePath.value root, "docs/unicode.md"),
                        "---\ntitle: Unicode\n---\n\n## \u4E2D\u6587\u6807\u9898\n\n## \u5B89\u88C5\n\nSee [it](unicode.md#\u4E2D\u6587\u6807\u9898).\n"
                    )

                    let result = Build.run root Fixture.site

                    let html =
                        File.ReadAllText(
                            Path.Combine(AbsolutePath.value root, "output/unicode/index.html")
                        )

                    assertThat
                        (html.Contains "id=\"\u4E2D\u6587\u6807\u9898\"")
                        (tag "a heading with no ASCII in it still gets a meaningful anchor"
                         >> isTrue)

                    assertThat
                        (html.Contains "id=\"section\"")
                        (tag "rather than a counter that moves when a heading is inserted above it"
                         >> isFalse)

                    assertThat
                        (result.Diagnostics
                         |> List.exists (fun item -> item.Code = "markdown/anchor-missing"))
                        (tag "and a link to it resolves" >> isFalse)
            )

            test (
                "a page decides what goes in its own table of contents",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    let body = "\n## A version\n\n### A section of it\n\n#### Deeper still\n"

                    File.WriteAllText(
                        Path.Combine(AbsolutePath.value root, "docs/releases.md"),
                        "---\ntitle: Releases\ntoc:\n  from: 2\n  to: 2\n---\n" + body
                    )

                    File.WriteAllText(
                        Path.Combine(AbsolutePath.value root, "docs/usual.md"),
                        "---\ntitle: Usual\n---\n" + body
                    )

                    let result = Build.run root Fixture.site

                    let headings name =
                        result.Pages
                        |> List.find (fun page -> page.Id = $"docs:%s{name}.md")
                        |> _.Headings
                        |> List.map _.Text

                    assertThat
                        (headings "releases")
                        (tag "the front matter is obeyed" >> isEqualTo [ "A version" ])

                    assertThat
                        (headings "usual")
                        (tag "and a page that says nothing gets the markdown plugin's levels"
                         >> isEqualTo
                             [
                                 "A version"
                                 "A section of it"
                             ])
            )

            test (
                "invalid front matter points at the offending line",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    File.WriteAllText(
                        Path.Combine(AbsolutePath.value root, "docs/invalid.md"),
                        "---\ndescription: no title here\n---\n\nBody.\n"
                    )

                    let result = Build.run root Fixture.site

                    let diagnostic =
                        result.Diagnostics
                        |> List.tryFind (fun item -> item.Code = "nacara/front-matter-invalid")

                    match diagnostic with
                    | None ->
                        assertThat false (tag "a missing required field is reported" >> isTrue)
                    | Some diagnostic ->
                        assertThat
                            (diagnostic.Message.Contains "title")
                            (tag "the message names the field" >> isTrue)

                        // The front matter opens on line 1, so 'description:' is line 2.
                        assertThat
                            (diagnostic.Span |> Option.map _.Line)
                            (tag "and points at the line of the file, not of the yaml"
                             >> isEqualTo (Some 2))
            )

            test (
                "two pages fighting over one url fail the build",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    File.WriteAllText(
                        Path.Combine(AbsolutePath.value root, "docs/guide/getting-started.markdown"),
                        "---\ntitle: Clash\n---\n\nBody.\n"
                    )

                    let site =
                        Fixture.site
                        |> Site.collection (
                            Fixture.docs |> Collection.source "docs" [ "**/*.markdown" ]
                        )

                    let result = Build.run root site

                    assertThat
                        (result.Diagnostics
                         |> List.exists (fun item -> item.Code = "nacara/duplicate-route"))
                        (tag "a route collision is reported rather than silently overwritten"
                         >> isTrue)
            )

            test (
                "rebuilding writes nothing when nothing changed",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()
                    let cache = BuildCache()
                    let first = Build.runWith cache root Fixture.site
                    let second = Build.runWith cache root Fixture.site

                    assertThat (first.WrittenFiles > 0) (tag "the first build writes" >> isTrue)
                    assertThat second.WrittenFiles (tag "the second writes nothing" >> isEqualTo 0)

                    assertThat
                        (second.UnchangedFiles > 0)
                        (tag "and reports the files it left alone" >> isTrue)
            )

            test (
                "a rebuilt page says again what it said the first time",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    File.WriteAllText(
                        Path.Combine(AbsolutePath.value root, "docs/guide/advanced.md"),
                        "---\ntitle: Advanced\n---\n\n```gleam\npub fn main() { 1 }\n```\n"
                    )

                    let cache = BuildCache()
                    let first = Build.runWith cache root Fixture.site
                    let second = Build.runWith cache root Fixture.site

                    let unknown (result: BuildResult) =
                        result.Diagnostics
                        |> List.filter (fun item -> item.Code.EndsWith "unknown-language")

                    assertThat
                        (unknown first |> List.length)
                        (tag "the first build reports the language nobody colours" >> isEqualTo 1)

                    assertThat
                        (second.WrittenFiles)
                        (tag "the second build renders from cache" >> isEqualTo 0)

                    assertThat
                        (unknown second)
                        (tag "and says exactly the same thing" >> isEqualTo (unknown first))
            )

            test (
                "rebuilding only writes the pages that changed",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()
                    let cache = BuildCache()
                    Build.runWith cache root Fixture.site |> ignore

                    let page = Path.Combine(AbsolutePath.value root, "docs/guide/advanced.md")
                    File.WriteAllText(page, File.ReadAllText(page) + "\n\nOne more paragraph.\n")

                    let result = Build.runWith cache root Fixture.site

                    assertThat
                        result.WrittenFiles
                        (tag "only the edited page is written again" >> isEqualTo 1)
            )

            test (
                "preserved paths survive pruning",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    let plugin =
                        { new IPlugin with
                            member _.Name = "preserving"

                            member _.Configure registry =
                                registry
                                |> Registry.preserve "generated"
                                |> Registry.onBuildComplete (fun context ->
                                    let directory =
                                        Path.Combine(
                                            AbsolutePath.value context.OutputDirectory,
                                            "generated"
                                        )

                                    Directory.CreateDirectory directory |> ignore

                                    File.WriteAllText(
                                        Path.Combine(directory, "index.bin"),
                                        "payload"
                                    )
                                )
                        }

                    let site = Fixture.site |> Site.plugin plugin
                    let cache = BuildCache()
                    Build.runWith cache root site |> ignore
                    let result = Build.runWith cache root site

                    assertThat
                        (File.Exists(
                            Path.Combine(AbsolutePath.value root, "output/generated/index.bin")
                        ))
                        (tag "a preserved file survives the next build" >> isTrue)

                    assertThat
                        result.PrunedFiles
                        (tag "and is not counted as pruned" >> isEqualTo 0)
            )

            test (
                "clean takes the cache with the output",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    Build.runWith (BuildCache()) root Fixture.site |> ignore

                    let entry = ProjectCache.directory root "bundles" "abcd1234"
                    File.WriteAllText(Path.Combine(AbsolutePath.value entry, "bundle.js"), "")

                    Build.clean root Fixture.site

                    assertThat
                        (Directory.Exists(Path.Combine(AbsolutePath.value root, "output")))
                        (tag "the output is gone" >> isFalse)

                    assertThat
                        (Directory.Exists(
                            Path.Combine(
                                AbsolutePath.value root,
                                ProjectCache.PROJECT_CACHE_DIR_NAME
                            )
                        ))
                        (tag "and so is what the build worked out" >> isFalse)
            )

            test (
                "deleting a page removes its output",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()
                    let cache = BuildCache()
                    Build.runWith cache root Fixture.site |> ignore

                    File.Delete(Path.Combine(AbsolutePath.value root, "docs/guide/advanced.md"))
                    let result = Build.runWith cache root Fixture.site

                    assertThat result.PrunedFiles (tag "the orphaned file is pruned" >> isEqualTo 1)

                    assertThat
                        (File.Exists(
                            Path.Combine(
                                AbsolutePath.value root,
                                "output/guide/advanced/index.html"
                            )
                        ))
                        (tag "so a deleted page cannot stay online" >> isFalse)
            )
        ]
    )
