module Nacara.Tests.LinkValidator

open System.IO
open System.Net
open System.Net.Sockets
open System.Threading
open Scriptorium.Nib.Assertion
open type Scriptorium.Quill.Test
open Nacara.Core
open Nacara.Plugins
open Nacara.Tests

let private site = Fixture.site |> Site.plugin (LinkValidator.create ())

/// <summary>A server that answers the way real ones do, including badly.</summary>
/// <summary>
/// A free port, and a listener already serving on it.
/// </summary>
/// <remarks>
/// <c>HttpListener</c> cannot be asked for a free port, so one is found by binding a socket, letting
/// it go, and claiming it again - and between those two something else can take it. That is a race
/// nothing can close, so it is retried instead: losing twice in a row is not something to design
/// around, and losing once is what made this test flake.
/// </remarks>
let rec private listen (attempt: int) =
    let listener = new TcpListener(IPAddress.Loopback, 0)
    listener.Start()
    let port = (listener.LocalEndpoint :?> IPEndPoint).Port
    listener.Stop()

    let http = new HttpListener()
    http.Prefixes.Add $"http://localhost:%i{port}/"

    try
        http.Start()
        port, http
    with :? HttpListenerException when attempt < 5 ->
        Thread.Sleep 20
        listen (attempt + 1)

let private serve (answer: string -> HttpListenerResponse -> unit) =
    let port, http = listen 0

    let requests = ref 0

    let loop =
        async {
            while http.IsListening do
                let! context = http.GetContextAsync() |> Async.AwaitTask
                Interlocked.Increment requests |> ignore

                answer
                    (context.Request.Url.AbsolutePath + "|" + context.Request.HttpMethod)
                    context.Response

                context.Response.Close()
        }

    Async.Start loop
    port, requests, (fun () -> http.Stop())

let all =
    testList (
        "Links",
        [
            test (
                "every href and src of a page is a link",
                fun _ ->
                    let html =
                        """<a href="/guide/">Guide</a><img src="/assets/logo.png"><a href="/guide/">again</a><link href="/style.css">"""

                    assertThat
                        (LinkValidator.linksOf html)
                        (tag "in source order, without repeats"
                         >> isEqualTo
                             [
                                 "/guide/"
                                 "/assets/logo.png"
                                 "/style.css"
                             ])
            )

            test (
                "what is not ours to check is left alone",
                fun _ ->
                    let leftAlone =
                        [
                            "#section"
                            "mailto:someone@example.com"
                            "tel:+3312345678"
                            "data:image/png;base64,AAAA"
                            "javascript:void(0)"
                            "//example.com/thing"
                            ""
                        ]

                    for url in leftAlone do
                        assertThat (LinkValidator.isCheckable url) (tag url >> isFalse)

                    assertThat (LinkValidator.isCheckable "/guide/") (tag "a page is" >> isTrue)

                    assertThat
                        (LinkValidator.isCheckable "https://example.com")
                        (tag "so is a url" >> isTrue)
            )

            test (
                "a url names the file a host would serve for it",
                fun _ ->
                    let cases =
                        [
                            "/docs/guide/", Some "guide/index.html"
                            "/docs/", Some "index.html"
                            "/docs/guide", Some "guide/index.html"
                            "/docs/404.html", Some "404.html"
                            "/docs/assets/app.css", Some "assets/app.css"
                            "/docs/guide/#anchor", Some "guide/index.html"
                            "/docs/guide/?q=1", Some "guide/index.html"
                            "/elsewhere/guide/", None
                        ]

                    for url, expected in cases do
                        assertThat
                            (LinkValidator.fileOf "/docs/" url)
                            (tag url >> isEqualTo expected)
            )

            test (
                "the prefix is the base url and the version together",
                fun _ ->
                    assertThat
                        (LinkValidator.sitePrefix (SiteUrl.create "/"))
                        (tag "a site at the root" >> isEqualTo "/")

                    assertThat
                        (LinkValidator.sitePrefix (SiteUrl.create "/Nacara/"))
                        (tag "a site in a subdirectory" >> isEqualTo "/Nacara/")

                    assertThat
                        (LinkValidator.sitePrefix (
                            SiteUrl.create "/Nacara/" |> SiteUrl.withVersionPrefix "2.0"
                        ))
                        (tag "and one of several versions" >> isEqualTo "/Nacara/2.0/")
            )

            test (
                "a link written in html is checked like any other",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    File.WriteAllText(
                        Path.Combine(AbsolutePath.value root, "docs/raw.md"),
                        "---\ntitle: Raw\n---\n\n<a href=\"/nowhere/\">gone</a>\n<img src=\"/assets/missing.png\">\n"
                    )

                    let result = Build.run root site

                    let reported =
                        result.Diagnostics
                        |> List.filter (fun item -> item.Code = "link-validator/target-missing")
                        |> List.length

                    assertThat reported (tag "the page and the asset" >> isEqualTo 2)
            )

            test (
                "an anchor is checked against the page that was written",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    File.WriteAllText(
                        Path.Combine(AbsolutePath.value root, "docs/anchors.md"),
                        "---\ntitle: Anchors\n---\n\n## A heading\n\n<a href=\"/anchors/#a-heading\">there</a>\n<a href=\"/anchors/#not-there\">gone</a>\n"
                    )

                    let result = Build.run root site

                    let reported =
                        result.Diagnostics
                        |> List.filter (fun item -> item.Code = "link-validator/anchor-missing")
                        |> List.map _.Message

                    assertThat
                        (List.length reported)
                        (tag "only the one that is not there" >> isEqualTo 1)

                    assertThat
                        (reported |> List.forall (fun message -> message.Contains "not-there"))
                        (tag "named in the message" >> isTrue)
            )

            test (
                "a link that leaves the site is asked for",
                fun _ ->
                    let port, requests, stop =
                        serve (fun request response ->
                            match request with
                            | "/gone|HEAD"
                            | "/gone|GET" -> response.StatusCode <- 404
                            | "/picky|HEAD" -> response.StatusCode <- 405
                            | _ -> response.StatusCode <- 200
                        )

                    try
                        let root = Fixture.copyToTemporaryDirectory ()

                        File.WriteAllText(
                            Path.Combine(AbsolutePath.value root, "docs/outward.md"),
                            $"---\ntitle: Outward\n---\n\n[fine](http://localhost:%i{port}/fine)\n[picky](http://localhost:%i{port}/picky)\n[gone](http://localhost:%i{port}/gone)\n"
                        )

                        let checking =
                            Fixture.site
                            |> Site.plugin (
                                LinkValidator.createWith (fun options ->
                                    { options with
                                        CheckExternal = true
                                    }
                                )
                            )

                        let result = Build.run root checking

                        let reported =
                            result.Diagnostics
                            |> List.filter (fun item ->
                                item.Code = "link-validator/external-unreachable"
                            )
                            |> List.map _.Message

                        assertThat
                            (List.length reported)
                            (tag "the one that answered 404, and only it" >> isEqualTo 1)

                        assertThat
                            (reported |> List.forall (fun message -> message.Contains "/gone"))
                            (tag "a server refusing HEAD is asked properly before being believed"
                             >> isTrue)

                        assertThat
                            requests.Value
                            (tag "four requests: three heads, and the get the picky one forced"
                             >> isGreaterOrEqual 4)
                    finally
                        stop ()
            )

            test (
                "a relative link is reported, whichever page it is read from",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    File.WriteAllText(
                        Path.Combine(AbsolutePath.value root, "docs/relative.md"),
                        "---\ntitle: Relative\n---\n\n<a href=\"../guide/\">up one</a>\n"
                    )

                    let result = Build.run root site

                    assertThat
                        (result.Diagnostics
                         |> List.exists (fun item -> item.Code = "link-validator/relative-link"))
                        (tag "where it lands depends on where it is read" >> isTrue)
            )
        ]
    )
