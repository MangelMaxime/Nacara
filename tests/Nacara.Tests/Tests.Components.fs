module Nacara.Tests.Components

open Scriptorium.Nib.Assertion
open type Scriptorium.Quill.Test
open System
open System.IO
open System.Net.Http
open Feliz.ViewEngine
open Nacara.Core
open Nacara.Plugins
open Nacara.Tests

let private write (root: AbsolutePath) (path: string) (text: string) =
    let file = Path.Combine(AbsolutePath.value root, path)
    Directory.CreateDirectory(Path.GetDirectoryName file) |> ignore
    File.WriteAllText(file, text)

let private output (root: AbsolutePath) (path: string) =
    File.ReadAllText(Path.Combine(AbsolutePath.value root, "output", path))

let private youtube (directive: DirectiveContext) =
    Html.figure
        [
            prop.className (directive.Attribute "class" |> Option.defaultValue "")
            prop.custom ("data-start", directive.Attribute "start" |> Option.defaultValue "0")
            prop.children
                [
                    Html.iframe
                        [ prop.src $"https://example.com/embed/%s{directive.Argument.Value}" ]
                    Html.figcaption [ directive.Body ]
                ]
        ]

let private directives =
    testList (
        "directives",
        [
            test (
                "a directive of the site renders what it holds",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    write
                        root
                        "docs/video.md"
                        "---\ntitle: Video\n---\n\n:::youtube abc123 {.wide start=30}\nThe **launch** talk.\n:::\n"

                    let result =
                        Build.run root (Fixture.site |> Markdown.directive "youtube" youtube)

                    assertThat
                        (result.Diagnostics
                         |> List.forall (fun item -> item.Severity <> Severity.Error))
                        (tag "the build is clean" >> isTrue)

                    let html = output root "video/index.html"

                    assertThat
                        (html.Contains "src=\"https://example.com/embed/abc123\"")
                        (tag "the argument is given" >> isTrue)

                    assertThat
                        (html.Contains "class=\"wide\"" && html.Contains "data-start=\"30\"")
                        (tag "and the attributes" >> isTrue)

                    assertThat
                        (html.Contains "<figcaption><p>The <strong>launch</strong> talk.</p>")
                        (tag "and what it holds, rendered" >> isTrue)
            )

            test (
                "a directive of the site replaces a built-in one of the same name",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()
                    write root "docs/note.md" "---\ntitle: Note\n---\n\n:::note\nCareful.\n:::\n"

                    Build.run
                        root
                        (Fixture.site
                         |> Markdown.directive
                             "note"
                             (fun directive ->
                                 Html.div
                                     [
                                         prop.className "my-note"
                                         prop.children [ directive.Body ]
                                     ]
                             ))
                    |> ignore

                    let html = output root "note/index.html"

                    assertThat
                        (html.Contains "<div class=\"my-note\">")
                        (tag "the site's is used" >> isTrue)

                    assertThat
                        (html.Contains "nacara-callout")
                        (tag "not the built-in one" >> isFalse)
            )

            test (
                "a directive that throws fails the build at its line",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    write
                        root
                        "docs/broken.md"
                        "---\ntitle: Broken\n---\n\n:::youtube\nNo id.\n:::\n"

                    let result =
                        Build.run root (Fixture.site |> Markdown.directive "youtube" youtube)

                    let failure =
                        result.Diagnostics
                        |> List.tryFind (fun item -> item.Code = "markdown/directive-failed")

                    assertThat failure.IsSome (tag "it is reported" >> isTrue)

                    assertThat
                        (failure |> Option.bind _.Span |> Option.map _.Line)
                        (tag "on the line that opens it" >> isEqualTo (Some 5))

                    assertThat
                        (failure.Value.Severity)
                        (tag "as an error" >> isEqualTo Severity.Error)
            )

            test (
                "a directive reports warnings and errors at its line, and is still rendered",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    write root "docs/video.md" "---\ntitle: Video\n---\n\n:::video\nNo id.\n:::\n"

                    let video (directive: DirectiveContext) =
                        directive.Warn "No title given"

                        directive.Error(
                            "A video needs its id",
                            "Write it after the name: :::video <id>"
                        )

                        Html.div
                            [
                                prop.className "video"
                                prop.children [ directive.Body ]
                            ]

                    let result = Build.run root (Fixture.site |> Markdown.directive "video" video)

                    let reported severity =
                        result.Diagnostics
                        |> List.tryFind (fun item ->
                            item.Code = "markdown/directive-video" && item.Severity = severity
                        )

                    assertThat
                        (reported Severity.Warning |> Option.bind _.Span |> Option.map _.Line)
                        (tag "the warning is on the line that opens it" >> isEqualTo (Some 5))

                    let error = reported Severity.Error

                    assertThat
                        (error |> Option.bind _.Hint)
                        (tag "the error keeps its hint"
                         >> isEqualTo (Some "Write it after the name: :::video <id>"))

                    assertThat
                        (result.Diagnostics
                         |> List.exists (fun item -> item.Code = "markdown/directive-failed"))
                        (tag "and it is not reported as a failure" >> isFalse)
            )

            test (
                "an unknown directive names the ones the site adds",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()
                    write root "docs/odd.md" "---\ntitle: Odd\n---\n\n:::vimeo\nX\n:::\n"

                    let result =
                        Build.run root (Fixture.site |> Markdown.directive "youtube" youtube)

                    assertThat
                        (result.Diagnostics
                         |> List.exists (fun item ->
                             item.Code = "markdown/unknown-directive"
                             && (item.Hint |> Option.defaultValue "").Contains
                                 "this site adds youtube"
                         ))
                        (tag "in the hint" >> isTrue)
            )
        ]
    )

let private inlineMarkdown =
    testList (
        "markdown outside the body",
        [
            test (
                "a layout renders markdown from front matter, its links resolved",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    let card =
                        Collection.create "docs" Fixture.decoder
                        |> Collection.source "docs" [ "**/*.md" ]
                        |> Collection.title _.Title
                        |> Collection.layout (fun context ->
                            Html.div
                                [
                                    Html.p
                                        [
                                            Markdown.renderInline
                                                context
                                                "Read *the* [advanced guide](guide/advanced.md#going-further)."
                                        ]
                                    Html.p [ Markdown.renderInline context "Back [home](/)." ]
                                    Markdown.renderBlock context "One\n\nTwo"
                                ]
                        )

                    let site =
                        Site.create "Inline"
                        |> Site.output "output"
                        |> Site.plugin (Markdown.create ())
                        |> Site.collection card

                    Build.run root site |> ignore
                    let html = output root "index.html"

                    assertThat
                        (html.Contains
                            "<p>Read <em>the</em> <a href=\"/guide/advanced/#going-further\">advanced guide</a>.</p>")
                        (tag "a file beside the page, with no paragraph around it" >> isTrue)

                    assertThat
                        (html.Contains "<p>Back <a href=\"/\">home</a>.</p>")
                        (tag "a path in the site" >> isTrue)

                    assertThat
                        (html.Contains "<p>One</p>" && html.Contains "<p>Two</p>")
                        (tag "and a block keeps its paragraphs" >> isTrue)
            )
        ]
    )

let private failures =
    testList (
        "failures",
        [
            test (
                "a layout that throws fails the build on its page, and the others are written",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    let docs =
                        Fixture.docs
                        |> Collection.layout (fun context ->
                            if context.FrontMatter.Title = "Advanced" then
                                failwith "no sidebar for you"

                            Fixture.layout context
                        )

                    let result =
                        Build.run
                            root
                            (Site.create "Failing"
                             |> Site.output "output"
                             |> Site.plugin (Markdown.create ())
                             |> Site.collection docs)

                    let failure =
                        result.Diagnostics
                        |> List.tryFind (fun item -> item.Code = "nacara/layout-failed")

                    assertThat failure.IsSome (tag "it is reported" >> isTrue)

                    assertThat
                        (failure.Value.Message.Contains "no sidebar for you")
                        (tag "with what was thrown" >> isTrue)

                    assertThat
                        (failure.Value.Span
                         |> Option.exists (fun span ->
                             (AbsolutePath.value span.File).EndsWith "advanced.md"
                         ))
                        (tag "on the page" >> isTrue)

                    assertThat
                        (File.Exists(Path.Combine(AbsolutePath.value root, "output/index.html")))
                        (tag "and the other pages are written" >> isTrue)
            )

            test (
                "the dev server shows the last build's problems",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()
                    let outputDirectory = AbsolutePath.combine root [ "output" ]
                    Directory.CreateDirectory(AbsolutePath.value outputDirectory) |> ignore
                    write root "output/index.html" "<html><body><p>Hi</p></body></html>"

                    let port = 41000 + Random.Shared.Next 2000
                    use server = new DevServer(outputDirectory, "/", "localhost", port)
                    server.Start()

                    server.ShowProblems(
                        root,
                        [
                            Diagnostic.error
                                "markdown/link-target-missing"
                                "This link points at nothing"
                            |> Diagnostic.at
                                (AbsolutePath.combine
                                    root
                                    [
                                        "docs"
                                        "index.md"
                                    ])
                                3
                                5
                        ]
                    )

                    use client = new HttpClient()

                    let get (path: string) =
                        client.GetStringAsync(server.Url.TrimEnd '/' + path).Result

                    let problems = get "/__nacara/problems"

                    assertThat
                        (problems.Contains "\"code\":\"markdown/link-target-missing\"")
                        (tag "as json" >> isTrue)

                    assertThat
                        (problems.Contains "\"location\":\"docs/index.md(3,5)\"")
                        (tag "located relative to the project" >> isTrue)

                    assertThat
                        ((get "/").Contains "/__nacara/problems")
                        (tag "and every page asks for them" >> isTrue)
            )
        ]
    )

let private writeBytes (root: AbsolutePath) (path: string) (bytes: byte array) =
    let file = Path.Combine(AbsolutePath.value root, path)
    Directory.CreateDirectory(Path.GetDirectoryName file) |> ignore
    File.WriteAllBytes(file, bytes)

let private be16 (value: int) =
    [|
        byte (value >>> 8)
        byte value
    |]

let private be32 (value: int) =
    [|
        byte (value >>> 24)
        byte (value >>> 16)
        byte (value >>> 8)
        byte value
    |]

let private le16 (value: int) =
    [|
        byte value
        byte (value >>> 8)
    |]

let private le24 (value: int) =
    [|
        byte value
        byte (value >>> 8)
        byte (value >>> 16)
    |]

let private ascii (text: string) = Text.Encoding.ASCII.GetBytes text

let private png (width: int) (height: int) =
    Array.concat
        [
            [| 0x89uy |]
            ascii "PNG\r\n\u001a\n"
            be32 13
            ascii "IHDR"
            be32 width
            be32 height
            Array.zeroCreate 5
        ]

let private jpeg (width: int) (height: int) (orientation: int) =
    let exif =
        Array.concat
            [
                ascii "Exif\u0000\u0000"
                ascii "MM"
                be16 42
                be32 8
                be16 1
                be16 0x0112
                be16 3
                be32 1
                be16 orientation
                be16 0
                be32 0
            ]

    Array.concat
        [
            [|
                0xFFuy
                0xD8uy
            |]
            [|
                0xFFuy
                0xE1uy
            |]
            be16 (exif.Length + 2)
            exif
            [|
                0xFFuy
                0xC0uy
            |]
            be16 17
            [| 8uy |]
            be16 height
            be16 width
            Array.zeroCreate 10
        ]

let private webp (width: int) (height: int) =
    Array.concat
        [
            ascii "RIFF"
            be32 0
            ascii "WEBPVP8X"
            le16 10
            le16 0
            Array.zeroCreate 4
            le24 (width - 1)
            le24 (height - 1)
        ]

let private avif (width: int) (height: int) =
    Array.concat
        [
            be32 20
            ascii "ftypavif"
            Array.zeroCreate 8
            be32 20
            ascii "ispe"
            be32 0
            be32 width
            be32 height
        ]

let private images =
    testList (
        "images",
        [
            test (
                "the size of an image is read from its header",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    let at name =
                        Path.Combine(AbsolutePath.value root, name)

                    writeBytes root "a.png" (png 640 480)

                    writeBytes
                        root
                        "a.gif"
                        (Array.concat
                            [
                                ascii "GIF89a"
                                le16 32
                                le16 16
                            ])

                    writeBytes root "a.jpg" (jpeg 800 600 1)
                    writeBytes root "turned.jpg" (jpeg 800 600 6)
                    writeBytes root "a.webp" (webp 300 200)
                    writeBytes root "a.avif" (avif 1200 630)

                    write
                        root
                        "sized.svg"
                        "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"24px\" height=\"12\"></svg>"

                    write
                        root
                        "box.svg"
                        "<?xml version=\"1.0\"?>\n<svg viewBox=\"0 0 100 50\"></svg>"

                    assertThat
                        (ImageSize.read (at "a.png"))
                        (tag "png" >> isEqualTo (Some(640, 480)))

                    assertThat (ImageSize.read (at "a.gif")) (tag "gif" >> isEqualTo (Some(32, 16)))

                    assertThat
                        (ImageSize.read (at "a.jpg"))
                        (tag "jpeg" >> isEqualTo (Some(800, 600)))

                    assertThat
                        (ImageSize.read (at "turned.jpg"))
                        (tag "a jpeg turned by its orientation" >> isEqualTo (Some(600, 800)))

                    assertThat
                        (ImageSize.read (at "a.webp"))
                        (tag "webp" >> isEqualTo (Some(300, 200)))

                    assertThat
                        (ImageSize.read (at "a.avif"))
                        (tag "avif" >> isEqualTo (Some(1200, 630)))

                    assertThat
                        (ImageSize.read (at "sized.svg"))
                        (tag "svg with a size" >> isEqualTo (Some(24, 12)))

                    assertThat
                        (ImageSize.read (at "box.svg"))
                        (tag "svg with a view box" >> isEqualTo (Some(100, 50)))

                    assertThat
                        (ImageSize.read (at "none.png"))
                        (tag "a missing file" >> isEqualTo None)
            )

            test (
                "an image from the static directory gets its size, and is loaded lazily",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()
                    writeBytes root "static/img/logo.png" (png 640 480)

                    write
                        root
                        "docs/pictures.md"
                        "---\ntitle: Pictures\n---\n\n![Logo](/img/logo.png)\n\n![Far](https://example.com/far.png)\n\n![Sized](/img/logo.png){width=100}\n"

                    let cache = BuildCache()
                    Build.runWatch cache root Fixture.site |> ignore

                    assertThat
                        ((output root "pictures/index.html").Contains
                            "<img src=\"/img/logo.png\" width=\"640\" height=\"480\" loading=\"lazy\" decoding=\"async\" alt=\"Logo\" />")
                        (tag "a local image is sized" >> isTrue)

                    assertThat
                        ((output root "pictures/index.html").Contains
                            "<img src=\"https://example.com/far.png\" loading=\"lazy\" decoding=\"async\" alt=\"Far\" />")
                        (tag "and a remote one is only loaded lazily" >> isTrue)

                    assertThat
                        ((output root "pictures/index.html").Contains
                            "<img src=\"/img/logo.png\" width=\"100\" loading=\"lazy\" decoding=\"async\" alt=\"Sized\" />")
                        (tag "an image with a size of its own keeps it" >> isTrue)

                    let under = Fixture.copyToTemporaryDirectory ()
                    writeBytes under "static/img/logo.png" (png 640 480)

                    write
                        under
                        "docs/pictures.md"
                        "---\ntitle: Pictures\n---\n\n![Logo](/docs/img/logo.png)\n"

                    Build.run under (Fixture.site |> Site.baseUrl "/docs/") |> ignore

                    assertThat
                        ((output under "pictures/index.html").Contains
                            "width=\"640\" height=\"480\"")
                        (tag "a path written under the base url is found too" >> isTrue)

                    writeBytes root "static/img/logo.png" (png 320 240)

                    File.SetLastWriteTimeUtc(
                        Path.Combine(AbsolutePath.value root, "static/img/logo.png"),
                        DateTime.UtcNow.AddMinutes 1.0
                    )

                    Build.runWatch cache root Fixture.site |> ignore

                    assertThat
                        ((output root "pictures/index.html").Contains "width=\"320\" height=\"240\"")
                        (tag "a changed image is sized again while serving" >> isTrue)
            )
        ]
    )

let all =
    testList (
        "Components",
        [
            directives
            inlineMarkdown
            failures
            images
        ]
    )
