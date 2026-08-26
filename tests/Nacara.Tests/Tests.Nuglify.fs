module Nacara.Tests.Nuglify

open Scriptorium.Nib.Assertion
open type Scriptorium.Quill.Test
open System.IO
open Nacara.Core
open Nacara.Plugins
open Nacara.Tests

let private minifiedHtml source =
    match Nuglify.html Nuglify.htmlDefaults source with
    | Ok html -> html
    | Error message -> failwith message

let private minifiedJs options source =
    match Nuglify.js options source with
    | Ok code -> code
    | Error message -> failwith message

let private plainJs = minifiedJs Nuglify.jsDefaults

let html =
    testList (
        "html",
        [
            test (
                "a stylesheet inside the page is left exactly as it was",
                fun _ ->
                    let css = "a {\n    color:   red;\n}"

                    let html =
                        minifiedHtml
                            $"<html><head><style>\n%s{css}\n</style></head><body>x</body></html>"

                    assertThat (html.Contains css) (tag "every space of it survives" >> isTrue)
            )

            test (
                "a script inside the page is left exactly as it was",
                fun _ ->
                    let js = "const x = 1;\nconsole.log( x );"

                    let html =
                        minifiedHtml
                            $"<html><head><script>\n%s{js}\n</script></head><body>x</body></html>"

                    assertThat (html.Contains js) (tag "every space of it survives" >> isTrue)
            )

            test (
                "a style attribute and an event handler are left alone too",
                fun _ ->
                    let html =
                        minifiedHtml
                            """<html><body><p style="color:   red" onclick="foo( 1 )">x</p></body></html>"""

                    assertThat
                        (html.Contains """style="color:   red""")
                        (tag "the inline css keeps its spacing" >> isTrue)

                    assertThat
                        (html.Contains """onclick="foo( 1 )""")
                        (tag "and so does the inline javascript" >> isTrue)
            )

            test (
                "comments go, and whitespace between blocks with them",
                fun _ ->
                    let html =
                        minifiedHtml
                            "<html><body>\n  <!-- gone -->\n  <div>  a  </div>\n\n  <div>b</div>\n</body></html>"

                    assertThat (html.Contains "gone") (tag "the comment is removed" >> isFalse)

                    assertThat
                        (html.Contains "<div>a</div><div>b</div>")
                        (tag "and the padding around a block goes with it" >> isTrue)
            )

            test (
                "a space that the reader would see is kept",
                fun _ ->
                    let html = minifiedHtml "<p><em>one</em>\n<em>two</em></p>"

                    assertThat
                        (html.Contains "</em> <em>" || html.Contains "</em>\n<em>")
                        (tag "the words do not run together" >> isTrue)
            )

            test (
                "the end tags a tool might look for are kept",
                fun _ ->
                    // Nacara's own dev server injects its reload script by looking for </body>.
                    let html = minifiedHtml "<html><body><p>x</p></body></html>"

                    assertThat (html.Contains "</body>") (tag "body still closes" >> isTrue)

                    let dropped =
                        match
                            Nuglify.html
                                { Nuglify.htmlDefaults with
                                    RemoveOptionalTags = true
                                }
                                "<html><body><p>x</p></body></html>"
                        with
                        | Ok html -> html
                        | Error message -> failwith message

                    assertThat
                        (dropped.Contains "</body>")
                        (tag "unless the site asks for them to go" >> isFalse)
            )

            test (
                "something that cannot be parsed is said, not thrown",
                fun _ ->
                    match Nuglify.html Nuglify.htmlDefaults "<p>unclosed <div></p>" with
                    | Ok _ -> ()
                    | Error message ->
                        assertThat
                            (message.Length > 0)
                            (tag "with something a reader can act on" >> isTrue)
            )
        ]
    )

let js =
    testList (
        "js",
        [
            test (
                "a local name is shortened, a reachable one is not",
                fun _ ->
                    let code =
                        plainJs
                            """
                            class NacaraTabs extends HTMLElement {
                                connectedCallback() {
                                    const selectedTabIndex = 0;
                                    this.dataset.index = selectedTabIndex;
                                }
                            }
                            customElements.define("nacara-tabs", NacaraTabs);
                            """

                    assertThat
                        (code.Contains "nacara-tabs")
                        (tag "the element keeps the name the markup asks for" >> isTrue)

                    assertThat
                        (code.Contains "connectedCallback")
                        (tag "and the callback the browser calls" >> isTrue)

                    assertThat
                        (code.Contains "selectedTabIndex")
                        (tag "while a local is shortened" >> isFalse)
            )

            test (
                "names are left alone when the site asks",
                fun _ ->
                    let code =
                        minifiedJs
                            { Nuglify.jsDefaults with
                                ShortenNames = false
                            }
                            "function f(el) { const selectedTabIndex = el.getAttribute('x'); el.dataset.a = selectedTabIndex; el.dataset.b = selectedTabIndex; }"

                    assertThat
                        (code.Contains "selectedTabIndex")
                        (tag "every name is the one that was written" >> isTrue)
            )

            test (
                "a licence survives, an ordinary comment does not",
                fun _ ->
                    let code = plainJs "/*! Copyright someone */\n// an explanation\nconst x = 1;\n"

                    assertThat
                        (code.Contains "Copyright someone")
                        (tag "a licence you must keep is kept" >> isTrue)

                    assertThat
                        (code.Contains "an explanation")
                        (tag "and prose for the reader of the source is not" >> isFalse)
            )

            test (
                "statements keep their semicolons",
                fun _ ->
                    let code = plainJs "const a = 1\nconst b = 2\n"

                    assertThat (code.EndsWith ";") (tag "including the last one" >> isTrue)
            )

            test (
                "the syntax a modern theme is written in is understood",
                fun _ ->
                    let code =
                        plainJs
                            """
                            const bundle = trigger?.dataset?.bundle ?? "/pagefind/";
                            const all = [...one, ...two];
                            const greet = (name) => `hello ${name}`;
                            """

                    assertThat
                        (code.Length > 0)
                        (tag "optional chaining, nullish coalescing, spread and template literals"
                         >> isTrue)
            )

            test (
                "a plugin's ES module is minified rather than refused",
                fun _ ->
                    let code =
                        plainJs
                            """
                            import { colour } from "./highlighting.js";

                            export async function paint(text) {
                                const node = document.createElement("pre");
                                await colour(text, node);
                                return node;
                            }
                            """

                    assertThat
                        (code.Contains "import")
                        (tag "the imports survive, so the module still resolves" >> isTrue)

                    assertThat
                        (code.Contains "export")
                        (tag "and so does what it exports" >> isTrue)

                    assertThat (code.Length < 220) (tag "and it really was minified" >> isTrue)
            )

            test (
                "a classic script reads the same as it always did",
                fun _ ->
                    let code =
                        plainJs
                            """
                            (function () {
                                var count = 0;
                                document.addEventListener("click", function () { count += 1; });
                            })();
                            """

                    assertThat
                        (code.Contains "addEventListener")
                        (tag "an ordinary immediately-invoked script still minifies" >> isTrue)
            )

            test (
                "a namespace import is one it cannot read yet",
                fun _ ->
                    match
                        Nuglify.js
                            Nuglify.jsDefaults
                            """import * as Editor from "./editor.js"; export const a = Editor;"""
                    with
                    | Ok _ -> () // if a later NUglify learns it, this test has done its job
                    | Error message ->
                        assertThat
                            (message.Contains "from")
                            (tag "and it names what it stumbled on" >> isTrue)
            )

            test (
                "syntax it does not know yet is a warning, not a broken site",
                fun _ ->
                    // NUglify cannot parse a static initialisation block today.
                    match
                        Nuglify.js Nuglify.jsDefaults "class T { static #x; static { T.#x = 1; } }"
                    with
                    | Ok _ ->
                        // If a later NUglify learns it, this test has done its job and can go.
                        ()
                    | Error message ->
                        assertThat
                            (message.Length > 0)
                            (tag "and it says what it could not read" >> isTrue)
            )
        ]
    )

/// <summary>Which builds minify and which do not.</summary>
/// <remarks>
/// The policy, in one place: <c>build</c> minifies, <c>watch</c> does not. Not because it is slow -
/// 34 ms for every page of this site - but because what you read in the browser while you work
/// should be the page you wrote.
/// </remarks>
let whenItRuns =
    testList (
        "when it runs",
        [
            test (
                "build minifies, watch does not",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()
                    let site = Fixture.site |> Nuglify.minifyHtml

                    let page () =
                        File.ReadAllText(
                            Path.Combine(
                                AbsolutePath.value root,
                                "output/guide/getting-started/index.html"
                            )
                        )

                    Build.run root site |> ignore
                    let built = page ()

                    Build.runWatch (BuildCache()) root site |> ignore
                    let watched = page ()

                    assertThat
                        (watched.Length > built.Length)
                        (tag "a watch build leaves the page as it was written" >> isTrue)
            )

            test (
                "a site can ask for minifying while it watches",
                fun _ ->
                    let root = Fixture.copyToTemporaryDirectory ()

                    let site =
                        Fixture.site
                        |> Nuglify.minifyHtmlWith (fun options ->
                            { options with
                                MinifyWhileWatching = true
                            }
                        )

                    let page () =
                        File.ReadAllText(
                            Path.Combine(
                                AbsolutePath.value root,
                                "output/guide/getting-started/index.html"
                            )
                        )

                    Build.runWatch (BuildCache()) root site |> ignore
                    let watched = page ()

                    Build.run root site |> ignore
                    let built = page ()

                    assertThat
                        (watched = built)
                        (tag "and then the two agree, byte for byte" >> isTrue)
            )
        ]
    )

let all =
    testList (
        "Nuglify",
        [
            html
            js
            whenItRuns
        ]
    )
