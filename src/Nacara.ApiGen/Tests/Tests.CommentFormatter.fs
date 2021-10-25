module Tests.CommentFormatter

open Expecto
open Nacara.ApiGen.CommentFormatter

module Expect =

    let equal actual expected =
        Expect.equal actual expected ""

let tests =
    testList "Tests.CommentFormatter" [

        test "<p> is transformed correctly" {
            let actual =
                """
<para>content</para>
                """.Trim()
                |> format

            let expected =
                """
<p>content</p>
                """.Trim()

            Expect.equal actual expected
        }

        test "<code> is transformed correctly" {
            let actual =
                """
<code>
type User =
    {
        Firstname : string
        Lastname : string
    }

let hello (u : User) =
    printfn "Hello, %s" u.Firstname
</code>
                """.Trim()
                |> format

            let expected =
                """
```
type User =
    {
        Firstname : string
        Lastname : string
    }

let hello (u : User) =
    printfn "Hello, %s" u.Firstname

```
                """.Trim()

            Expect.equal actual expected
        }

        test "<code> support the lang attributes" {
            let actual =
                """
<code lang="fsharp">
type User =
    {
        Firstname : string
        Lastname : string
    }

let hello (u : User) =
    printfn "Hello, %s" u.Firstname
</code>
                """.Trim()
                |> format

            let expected =
                """
```fsharp
type User =
    {
        Firstname : string
        Lastname : string
    }

let hello (u : User) =
    printfn "Hello, %s" u.Firstname

```
                """.Trim()

            Expect.equal actual expected
        }

        test "<c> is transformed correctly" {
            let actual =
                """
<c>content</c>
                """.Trim()
                |> format

            let expected =
                """
<code>content</code>
                """.Trim()

            Expect.equal actual expected
        }

        test "<see> with href attributes is supported" {
            let actual =
                """
<see href="http://perdu.com">Awesome site</see>
                """.Trim()
                |> format

            let expected =
                """
[Awesome site](http://perdu.com)
                """.Trim()

            Expect.equal actual expected
        }

        // TODO: find the entity related to the <see> tag
        // and write a real link to it
        test "<see> with cref attributes works for non void element" {
            let actual =
                """
<see cref="P:Nacara.Core.Types.PageContext.Attributes">Attributes</see>
                """.Trim()
                |> format

            let expected =
                """
<code>Attributes</code>
                """.Trim()

            Expect.equal actual expected
        }

        test "<typeparamref> is supported" {
            let actual =
                """
The parameter and return value are both of an arbitrary type, <typeparamref name="T"/>
                """.Trim()
                |> format

            let expected =
                """
The parameter and return value are both of an arbitrary type, <code>T</code>
                """.Trim()

            Expect.equal actual expected
        }

        test "<table> are supported" {
            let actual =
                """
<table>
    <tr>
        <th>English</th>
        <th>French</th>
    </tr>
    <tr>
        <td>France</td>
        <td>Germany</td>
    </tr>
        <td>France</td>
        <td>Allemagne</td>
    </tr>
</table>
                """.Trim()
                |> format

            let expected =
                """
<table>
    <tr>
        <th>English</th>
        <th>French</th>
    </tr>
    <tr>
        <td>France</td>
        <td>Germany</td>
    </tr>
        <td>France</td>
        <td>Allemagne</td>
    </tr>
</table>
                """.Trim()

            Expect.equal actual expected
        }

        test "other rules are supported inside of the <table>" {
            let actual =
                """
<table>
    <tr>
        <th>English</th>
        <th>French</th>
    </tr>
    <tr>
        <td><c>France</c></td>
        <td>Germany</td>
    </tr>
        <td>France</td>
        <td>Allemagne</td>
    </tr>
</table>
                """.Trim()
                |> format

            let expected =
                """
<table>
    <tr>
        <th>English</th>
        <th>French</th>
    </tr>
    <tr>
        <td><code>France</code></td>
        <td>Germany</td>
    </tr>
        <td>France</td>
        <td>Allemagne</td>
    </tr>
</table>
                """.Trim()

            Expect.equal actual expected
        }

        test "bullet list with description only are supported" {
            let actual =
                """
<list type="bullet">
<item>
<description>Move forwards in a straight line.</description>
</item>
<item>
<description>Move backwards in a straight line.</description>
</item>
</list>
                """.Trim()
                |> format

            let expected =
                """
<ul>
<li>Move forwards in a straight line.</li>
<li>Move backwards in a straight line.</li>
</ul>
                """.Trim()

            Expect.equal actual expected
        }

        test "bullet list with term only are supported" {
            let actual =
                """
<list type="bullet">
<item>
<term>Move forwards in a straight line.</term>
</item>
<item>
<term>Move backwards in a straight line.</term>
</item>
</list>
                """.Trim()
                |> format

            let expected =
                """
<ul>
<li><strong>Move forwards in a straight line.</strong></li>
<li><strong>Move backwards in a straight line.</strong></li>
</ul>
                """.Trim()

            Expect.equal actual expected
        }

        test "bullet list with definition are supported" {
            let actual =
                """
<list type="bullet">
<item>
<term>Forward</term>
<description>Move forwards in a straight line.</description>
</item>
<item>
<term>Backward</term>
<description>Move backwards in a straight line.</description>
</item>
<item>
</list>
                """.Trim()
                |> format

            let expected =
                """
<ul>
<li><strong>Forward</strong> - Move forwards in a straight line.</li>
<li><strong>Backward</strong> - Move backwards in a straight line.</li>
</ul>
                """.Trim()

            Expect.equal actual expected
        }

        test "number list with description only are supported" {
            let actual =
                """
<list type="number">
<item>
<description>Move forwards in a straight line.</description>
</item>
<item>
<description>Move backwards in a straight line.</description>
</item>
</list>
                """.Trim()
                |> format

            let expected =
                """
<ol>
<li>Move forwards in a straight line.</li>
<li>Move backwards in a straight line.</li>
</ol>
                """.Trim()

            Expect.equal actual expected
        }

        test "number list with term only are supported" {
            let actual =
                """
<list type="number">
<item>
<term>Move forwards in a straight line.</term>
</item>
<item>
<term>Move backwards in a straight line.</term>
</item>
</list>
                """.Trim()
                |> format

            let expected =
                """
<ol>
<li><strong>Move forwards in a straight line.</strong></li>
<li><strong>Move backwards in a straight line.</strong></li>
</ol>
                """.Trim()

            Expect.equal actual expected
        }

        test "number list with definition are supported" {
            let actual =
                """
<list type="number">
<item>
<term>Forward</term>
<description>Move forwards in a straight line.</description>
</item>
<item>
<term>Backward</term>
<description>Move backwards in a straight line.</description>
</item>
<item>
</list>
                """.Trim()
                |> format

            let expected =
                """
<ol>
<li><strong>Forward</strong> - Move forwards in a straight line.</li>
<li><strong>Backward</strong> - Move backwards in a straight line.</li>
</ol>
                """.Trim()

            Expect.equal actual expected
        }


        test "table list are supported" {
            let actual =
                """
<list type="table">
<listheader>
<term>Action</term>
<term>Description</term>
<term>Power Consumption</term>
</listheader>
<item>
<term>Forward</term>
<term>Move forwards in a straight line.</term>
<term>50W</term>
</item>
<item>
<term>Backward</term>
<term>Move backwards in a straight line.</term>
<term>50W</term>
</item>
</list>
                """.Trim()
                |> format

            let expected =
                """
<table>
<thead><tr>
<th>Action</th><th>Description</th><th>Power Consumption</th>
</tr></thead>
<tbody>
<tr>
<td>Forward</td>
<td>Move forwards in a straight line.</td>
<td>50W</td>
</tr>
<tr>
<td>Forward</td>
<td>Move forwards in a straight line.</td>
<td>50W</td>
</tr>
<tr>
<td>Backward</td>
<td>Move backwards in a straight line.</td>
<td>50W</td>
</tr>
</tbody>
</table>
                """.Trim()

            Expect.equal actual expected
        }

    ]
