module Nacara.Tests.Feed

open Scriptorium.Nib.Assertion
open type Scriptorium.Quill.Test
open System
open System.IO
open System.Xml.Linq
open Nacara.Core
open Nacara.Plugins
open Nacara.Tests

let private atom = XNamespace.Get "http://www.w3.org/2005/Atom"

let private published = DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)

let private feedOf (configure: FeedOptions -> FeedOptions) =
    Feed.ofCollection
        Fixture.docs
        (fun page ->
            { Feed.entry page.Title published with
                Updated = page.Order |> Option.map (fun order -> published.AddDays(float order))
                Summary = page.Description
            }
        )
    |> configure

let private feedPath (root: AbsolutePath) =
    Path.Combine(AbsolutePath.value root, "output/feed.xml")

let all =
    testList (
        "Feed",
        [
            test (
                "a feed lists the pages of the collection, newest first, with absolute urls",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    File.WriteAllText(
                        Path.Combine(AbsolutePath.value root, "docs/recent.md"),
                        "---\ntitle: Recent\norder: 5\n---\n\nSee [home](index.md).\n"
                    )

                    let site =
                        Fixture.site
                        |> Site.origin "https://example.com"
                        |> Feed.register (feedOf (Feed.author "Ada"))

                    Build.run root site |> ignore

                    let document = XDocument.Load(feedPath root)
                    let entries = document.Root.Elements(atom + "entry") |> List.ofSeq

                    assertThat
                        (entries |> List.map (fun entry -> entry.Element(atom + "title").Value))
                        (tag "the page updated last comes first"
                         >> satisfy (fun titles -> List.head titles = "Recent" && titles.Length = 4))

                    let recent = entries.Head

                    assertThat
                        (recent.Element(atom + "id").Value)
                        (tag "its id is its absolute url" >> isEqualTo "https://example.com/recent/")

                    assertThat
                        (recent.Element(atom + "updated").Value)
                        (tag "in UTC" >> isEqualTo "2026-01-06T00:00:00Z")

                    let content = recent.Element(atom + "content")

                    assertThat
                        (content.Value.Contains "href=\"/index.html\""
                         || content.Value.Contains "href=\"/\"")
                        (tag "its body is the rendered html" >> isTrue)

                    assertThat
                        (content.Attribute(XNamespace.Xml + "base").Value)
                        (tag "resolved against the page" >> isEqualTo "https://example.com/recent/")

                    assertThat
                        (document.Root.Element(atom + "author").Element(atom + "name").Value)
                        (tag "and the author is named" >> isEqualTo "Ada")
            )

            test (
                "a limit keeps the newest entries",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    let site =
                        Fixture.site
                        |> Site.origin "https://example.com"
                        |> Feed.register (feedOf (Feed.limit (Some 1) >> Feed.includeContent false))

                    Build.run root site |> ignore

                    let document = XDocument.Load(feedPath root)

                    assertThat
                        (document.Root.Elements(atom + "entry") |> Seq.length)
                        (tag "one entry" >> isEqualTo 1)

                    assertThat
                        (document.Root.Descendants(atom + "content") |> Seq.isEmpty)
                        (tag "without its body" >> isTrue)
            )

            test (
                "no origin means no feed, and a warning",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()
                    let result = Build.run root (Fixture.site |> Feed.register (feedOf id))

                    assertThat (File.Exists(feedPath root)) (tag "nothing is written" >> isFalse)

                    assertThat
                        (result.Diagnostics
                         |> List.exists (fun item -> item.Code = "feed/origin-missing"))
                        (tag "and the build says why" >> isTrue)
            )

            test (
                "a page can link to the feed",
                fun _ ->
                    let site = Site.toInfo (Fixture.site |> Site.baseUrl "/docs/")
                    let html = Feliz.ViewEngine.Render.htmlView (Feed.headLink site (feedOf id))

                    assertThat
                        (html.Contains "href=\"/docs/feed.xml\"")
                        (tag "under the base url" >> isTrue)

                    assertThat
                        (html.Contains "type=\"application/atom+xml\"")
                        (tag "as an atom feed" >> isTrue)
            )
        ]
    )
