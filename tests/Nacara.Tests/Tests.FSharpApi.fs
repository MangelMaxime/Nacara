module Nacara.Tests.FSharpApi

open System
open System.IO
open Scriptorium.Nib.Assertion
open type Scriptorium.Quill.Test
open Nacara.Core
open Nacara.Plugins
open Nacara.Plugins.Internal
open Nacara.Tests

// Walk up to the bin folder, so whatever MSBuild put in between comes with us: a developer
// command prompt exports Platform, and that adds a level - bin/x64/Debug/net10.0.
let private outputTail =
    let rec walk (directory: DirectoryInfo) (tail: string list) =
        if Operators.isNull directory then
            failwith $"'%s{AppContext.BaseDirectory}' is not under a bin directory"
        elif directory.Name = "bin" then
            tail
        else
            walk directory.Parent (directory.Name :: tail)

    walk (DirectoryInfo AppContext.BaseDirectory) []

/// <summary>
/// Where a sibling project put its assembly, in whichever configuration this test is running in.
/// </summary>
let private built (segments: string list) (assembly: string) =
    Path.Combine(
        [|
            __SOURCE_DIRECTORY__
            ".."
            yield! segments
            "bin"
            yield! outputTail
            assembly
        |]
    )
    |> Path.GetFullPath
    |> AbsolutePath.create

/// A library written to be documented: its bodies are `failwith`, its surface is the point.
let private fixture = built [ "fixture-api" ] "Fixture.Library.dll"

let private library =
    match Reader.read fixture with
    | Ok assembly -> assembly
    | Error message -> failwith message

let private declaration name =
    library.Namespaces
    |> List.collect _.Entities
    |> List.tryFind (fun entity -> entity.Name = name)
    |> Option.defaultWith (fun () -> failwith $"no declaration named {name}")

/// The engine's own assembly: a real F# library, with modules, records, unions and members.
let private engine =
    built
        [
            ".."
            "src"
            "Nacara.Core"
        ]
        "Nacara.Core.dll"

let all =
    testList (
        "Api",
        [
            test (
                "a page opens with what it is, then an index, then the members",
                fun _ ->
                    let page =
                        Render.entity
                            (fun slug -> $"/api/{slug}/")
                            (fun _ -> None)
                            false
                            (declaration "People")

                    assertThat
                        (page.StartsWith "---\ntitle: People")
                        (tag "front matter the site's own decoder can read" >> isTrue)

                    assertThat
                        (page.Contains "<pre class=\"nacara-api__signature"
                         && page.Contains "<code><span class=\"tok-keyword\">module</span>")
                        (tag "the declaration first" >> isTrue)

                    assertThat
                        (page.Contains "<div class=\"nacara-api__entry\" id=\"greet\">")
                        (tag "one entry per member" >> isTrue)

                    assertThat
                        (page.Contains "id=\"greet\">\n<a class=\"nacara-api__anchor\"")
                        (tag "with its link beside it, not inside it" >> isTrue)

                    assertThat
                        (page.Contains "href=\"#greet\"")
                        (tag "pointing at the entry it belongs to" >> isTrue)

                    assertThat
                        (page.Contains ":::warning Deprecated")
                        (tag "a deprecated member says so where it is read" >> isTrue)
            )

            test (
                "a signature leads to the types it mentions",
                fun _ ->
                    let resolve name =
                        if name = "Person" then
                            Some "/api/person/"
                        else
                            None

                    let page =
                        Render.entity
                            (fun slug -> $"/api/{slug}/")
                            resolve
                            false
                            (declaration "People")

                    assertThat
                        (page.Contains "<a class=\"tok-type\" href=\"/api/person/\">Person</a>")
                        (tag "a type this build published is a link" >> isTrue)

                    assertThat
                        (page.Contains "<span class=\"tok-parameter\">greeting</span>")
                        (tag "what a caller passes is named as such" >> isTrue)

                    assertThat
                        (page.Contains "<span class=\"tok-type\">string</span>")
                        (tag "a type F# writes in lower case is still a type" >> isTrue)

                    assertThat
                        (page.Contains "<span class=\"tok-punctuation\">")
                        (tag "and the punctuation is not body text" >> isTrue)
            )

            test (
                "a table cell survives the pipes F# is full of",
                fun _ ->
                    let shapes =
                        library.Namespaces |> List.find (fun ns -> ns.Name.EndsWith "Shapes")

                    let page =
                        Render.``namespace``
                            (fun slug -> $"/api/{slug}/")
                            (fun _ -> None)
                            false
                            shapes

                    assertThat
                        (page.Contains "Matches a \\| b")
                        (tag "a pipe in a summary does not end the cell it sits in" >> isTrue)
            )

            test (
                "a namespace page lists what it declares, grouped by kind",
                fun _ ->
                    let page =
                        Render.``namespace``
                            (fun slug -> $"/api/{slug}/")
                            (fun _ -> None)
                            false
                            library.Namespaces.Head

                    for heading in
                        [
                            "## Module"
                            "## Record"
                            "## Union"
                            "## Interface"
                        ] do
                        assertThat (page.Contains heading) (tag heading >> isTrue)

                    let people = declaration "People"

                    assertThat
                        (page.Contains $"[`People`](/api/%s{people.Slug}/)")
                        (tag "with a link to each page" >> isTrue)

                    assertThat
                        (page.Contains "### " || page.Contains "(greeting: string)")
                        (tag "and nothing about their insides" >> isFalse)
            )

            test (
                "the way in names packages, not the namespace they share",
                fun _ ->
                    let declared name assembly =
                        {
                            Name = name
                            Entities = []
                            Slug = Slug.create name
                            Assembly = assembly
                        }

                    let link slug = $"/reference/%s{slug}/"

                    let many =
                        Render.index'
                            "API reference"
                            link
                            [
                                declared "Nacara.Plugins" "Nacara.Plugin.Markdown"
                                declared "Nacara.Plugins" "Nacara.Plugin.Search"
                            ]

                    assertThat
                        (many.Contains "| Package |")
                        (tag "the column says what it lists" >> isTrue)

                    for package in
                        [
                            "Nacara.Plugin.Markdown"
                            "Nacara.Plugin.Search"
                        ] do
                        assertThat
                            (many.Contains $"[`%s{package}`](/reference/%s{Slug.create package}/)")
                            (tag $"%s{package} is named, and links to its own page" >> isTrue)

                    let one =
                        Render.index' "API reference" link [ declared "Nacara.Core" "Nacara.Core" ]

                    assertThat
                        (one.Contains "| Namespace |")
                        (tag "with one package, the namespaces are the way in" >> isTrue)
            )

            test (
                "the outline is shaped like the code",
                fun _ ->
                    let outline =
                        { FSharpApi.defaults with
                            Sources = [ FSharpApiSource.create "Fixture.Library.dll" ]
                        }
                        |> FSharpApi.outlineFrom (AbsolutePath.directory fixture)

                    let shapes =
                        outline
                        |> List.collect _.Namespaces
                        |> List.tryFind (fun ns -> ns.Name.EndsWith "Shapes")

                    match shapes with
                    | None -> assertThat "" (tag "the fixture has a namespace" >> isNotEqualTo "")
                    | Some shapes ->
                        assertThat
                            (shapes.Entries |> List.map _.Name)
                            (tag "a namespace offers what it declares, in reading order"
                             >> isEqualTo (shapes.Entries |> List.map _.Name |> List.sort))

                        assertThat
                            (shapes.Entries |> List.exists (fun entry -> entry.Name = "Counter"))
                            (tag "and top-level declarations stay at the top level" >> isTrue)

                    let engineOutline =
                        { FSharpApi.defaults with
                            Sources = [ FSharpApiSource.create "Nacara.Core.dll" ]
                        }
                        |> FSharpApi.outlineFrom (AbsolutePath.directory engine)

                    let nested =
                        engineOutline
                        |> List.collect _.Namespaces
                        |> List.collect _.Entries
                        |> List.collect (fun entry ->
                            entry.Children |> List.map (fun child -> entry.Name, child.Name)
                        )

                    assertThat
                        (nested |> List.contains ("Decode", "IGetters"))
                        (tag "what is declared inside a module hangs under it" >> isTrue)

                    assertThat
                        (engineOutline
                         |> List.collect _.Namespaces
                         |> List.collect _.Entries
                         |> List.exists (fun entry -> entry.Name = "IGetters"))
                        (tag "and is not lifted out of it as well" >> isFalse)
            )

            test (
                "awkward shapes are written the way they were declared",
                fun _ ->
                    let signatureOf name =
                        (declaration "Awkward").Members
                        |> List.tryFind (fun item -> item.Name = name)
                        |> Option.map _.Signature
                        |> Option.defaultValue ""

                    assertThat
                        (signatureOf "Find")
                        (tag "an optional argument is asked for the way it is passed"
                         >> isEqualTo "static member Find (name: string, ?limit: int) : string list")

                    assertThat
                        (signatureOf "TryFind")
                        (tag "a byref argument says so"
                         >> isEqualTo
                             "static member TryFind (name: string, found: byref<string>) : bool")

                    assertThat
                        (signatureOf "Formatter")
                        (tag "returning a function is not the same as taking one"
                         >> isEqualTo "static member Formatter (prefix: string) : string -> string")

                    assertThat
                        (signatureOf "Split")
                        (tag "and a tuple stays a tuple"
                         >> isEqualTo "static member Split (text: string) : string * int")
            )

            test (
                "one of every declaration is written the way F# writes it",
                fun _ ->
                    let signatureOf name = (declaration name).Signature

                    for name, expected in
                        [
                            "Size",
                            "[<Struct>]\ntype Size =\n    {\n        Width: float\n        Height: float\n    }"
                            "Type With Spaces",
                            "type ``Type With Spaces`` =\n    {\n        ``Field With Spaces``: string\n    }"
                            "m", "[<Measure>] type m"
                            "Refused", "exception Refused of message: string * position: int"
                            "Combine", "type Combine<'T> = delegate of 'T * 'T -> 'T"
                            "INamed", "[<Interface>]\ntype INamed"
                            "Shape", "[<AbstractClass>]\ntype Shape"
                            "Counter", "[<Class>]\ntype Counter"
                            "Volume",
                            "type Volume =\n    | Quiet = 0\n    | Normal = 1\n    | Loud = 2"
                        ] do
                        assertThat (signatureOf name) (tag name >> isEqualTo expected)

                    assertThat
                        (declaration "m").Kind
                        (tag "and a unit of measure is not filed as a class"
                         >> isEqualTo FSharpApiEntityKind.Measure)
            )

            test (
                "a declaration says which assembly it ships in, when that is a question",
                fun _ ->
                    let page assembly =
                        Render.entity
                            (fun slug -> $"/api/{slug}/")
                            (fun _ -> None)
                            assembly
                            (declaration "People")

                    assertThat
                        ((page true).Contains "| Assembly | `Fixture.Library` |")
                        (tag "a reference of several assemblies says which one" >> isTrue)

                    assertThat
                        ((page false).Contains "| Assembly |")
                        (tag "a reference of one says nothing, there being nothing to say"
                         >> isFalse)
            )

            test (
                "a class is written out with its members",
                fun _ ->
                    let page =
                        Render.entity
                            (fun slug -> $"/api/{slug}/")
                            (fun _ -> None)
                            false
                            (declaration "Counter")

                    let declared =
                        let block =
                            System.Text.RegularExpressions.Regex.Match(
                                page,
                                "<pre class=\"nacara-api__signature[^\"]*\"><code>(.*?)</code></pre>",
                                System.Text.RegularExpressions.RegexOptions.Singleline
                            )

                        System.Text.RegularExpressions.Regex
                            .Replace(block.Groups[1].Value, "<[^>]+>", "")
                            .Replace("&lt;", "<")
                            .Replace("&gt;", ">")

                    assertThat
                        declared
                        (tag "a class is declared by what it offers"
                         >> isEqualTo (
                             "[<Class>]\n"
                             + "type Counter =\n"
                             + "    new () : Counter\n"
                             + "    new (label: string) : Counter\n"
                             + "    member Count: int with get, set\n"
                             + "    member Label: string\n"
                             + "    static member Shared: Counter\n"
                             + "    static member (+) (left: Counter, right: Counter) : int\n"
                             + "    member Format (width: int) : string\n"
                             + "    member Format () : string\n"
                             + "    member Increment () : unit\n"
                             + "    member ``Reset Everything`` () : unit\n"
                             + "    member Changed: IEvent<int>"
                         ))
            )

            test (
                "one of every member is written the way it is called",
                fun _ ->
                    let memberOf entity name =
                        (declaration entity).Members |> List.tryFind (fun item -> item.Name = name)

                    let signatureOf entity name =
                        memberOf entity name |> Option.map _.Signature |> Option.defaultValue ""

                    assertThat
                        (signatureOf "Values" "Constructor")
                        (tag "a constructor is called `new`"
                         >> isEqualTo "new (values: int list) : Values")

                    assertThat
                        (signatureOf "Values" "Larger")
                        (tag "an inline member says so"
                         >> isEqualTo "member inline Larger<'T> (first: 'T, second: 'T) : 'T")

                    assertThat
                        (signatureOf "Values" "Item")
                        (tag "an indexed property keeps what it is asked for"
                         >> isEqualTo "member Item (index: int) : int")

                    assertThat
                        (signatureOf "Constants" "Retries")
                        (tag "a literal's value is part of it"
                         >> isEqualTo "[<Literal>] Retries: int = 3")

                    assertThat
                        (memberOf "Counter" "Changed" |> Option.map _.Kind)
                        (tag "an event is an event" >> isEqualTo (Some FSharpApiMemberKind.Event))

                    assertThat
                        ((declaration "Counter").Members
                         |> List.exists (fun item -> item.Name.StartsWith "add_"))
                        (tag "and the accessors it compiles into are nobody's business" >> isFalse)
            )

            test (
                "two members of one name get two anchors",
                fun _ ->
                    let anchors =
                        (declaration "Counter").Members
                        |> List.filter (fun item -> item.Name = "Format")
                        |> List.map _.Anchor

                    assertThat
                        (anchors |> List.distinct |> List.length)
                        (tag "overloads cannot share the link that points at them"
                         >> isEqualTo anchors.Length)

                    assertThat anchors.Length (tag "and both of them are published" >> isEqualTo 2)
            )

            test (
                "what sits outside every namespace still has a page",
                fun _ ->
                    let global' =
                        library.Namespaces
                        |> List.tryFind (fun ns ->
                            ns.Entities |> List.exists (fun e -> e.Name = "RootModule")
                        )

                    match global' with
                    | None -> assertThat "" (tag "a root module is published" >> isNotEqualTo "")
                    | Some global' ->
                        assertThat
                            global'.Slug
                            (tag "under a name a route can be made of" >> isNotEqualTo "")

                        assertThat
                            global'.Name
                            (tag "and one a reader can read" >> isEqualTo "Global")
            )

            test (
                "a cref points at the page of what it names",
                fun _ ->
                    let page =
                        Render.entity
                            (fun slug -> $"/reference/{slug}/")
                            (fun name ->
                                if name = "Person" then
                                    Some "/reference/fixture-library/person/"
                                else
                                    None
                            )
                            false
                            (declaration "Constrained")

                    assertThat
                        (page.Contains "[`Person`](/reference/fixture-library/person/)")
                        (tag "what the build published is a link" >> isTrue)

                    assertThat
                        (page.Contains "nacara-api:")
                        (tag "and nothing leaks the scheme it travelled under" >> isFalse)
            )

            test (
                "documentation the compiler wrote badly is still read",
                fun _ ->
                    let describe =
                        (declaration "Awkward").Members
                        |> List.tryFind (fun item -> item.Name = "Describe")

                    assertThat
                        (describe |> Option.bind _.Doc.Summary |> Option.defaultValue "")
                        (tag "what was written about it survives"
                         >> isEqualTo "Takes something with no name of its own.")
            )

            test (
                "an extension member says which type it extends",
                fun _ ->
                    let extension =
                        library.Namespaces
                        |> List.collect _.Entities
                        |> List.tryFind (fun entity -> entity.Name = "StringExtensions")
                        |> Option.bind (fun entity ->
                            entity.Members |> List.tryFind (fun item -> item.Name = "IsAccepted")
                        )

                    match extension with
                    | None ->
                        assertThat "" (tag "an extension member is published" >> isNotEqualTo "")
                    | Some extension ->
                        assertThat
                            extension.Kind
                            (tag "read as an extension" >> isEqualTo FSharpApiMemberKind.Extension)

                        assertThat
                            (extension.Extends |> Option.defaultValue "")
                            (tag "and pointing at the type it was added to" >> isEqualTo "string")
            )

            test (
                "an extension on a type we publish is shown on that type",
                fun _ ->
                    let person =
                        library.Namespaces
                        |> List.collect _.Entities
                        |> List.tryFind (fun entity -> entity.Name = "Person")

                    match person with
                    | None -> assertThat "" (tag "Person is published" >> isNotEqualTo "")
                    | Some person ->
                        assertThat
                            (person.Members
                             |> List.exists (fun item ->
                                 item.Name = "Initials"
                                 && item.Kind = FSharpApiMemberKind.Extension
                             ))
                            (tag "the member added to it is listed there" >> isEqualTo true)

                        let declaring =
                            library.Namespaces
                            |> List.collect _.Entities
                            |> List.tryFind (fun entity -> entity.Name = "PersonExtensions")

                        assertThat
                            (declaring
                             |> Option.map (fun entity ->
                                 entity.Members |> List.exists (fun item -> item.Name = "Initials")
                             )
                             |> Option.defaultValue false)
                            (tag "and not repeated where it was written" >> isEqualTo false)
            )

            test (
                "what a function raises and what its type parameter is come with it",
                fun _ ->
                    let larger =
                        (declaration "Constrained").Members
                        |> List.tryFind (fun item -> item.Name = "larger")

                    match larger with
                    | None -> assertThat "" (tag "the function is found" >> isNotEqualTo "")
                    | Some larger ->
                        assertThat
                            (larger.Doc.TypeParameters |> List.map fst)
                            (tag "its type parameter is documented" >> isEqualTo [ "T" ])

                        assertThat
                            (larger.Doc.Exceptions |> List.map fst)
                            (tag "and so is what it raises"
                             >> isEqualTo [ "System.ArgumentNullException" ])
            )

            test (
                "a constrained function keeps its constraint",
                fun _ ->
                    let larger =
                        library.Namespaces
                        |> List.collect _.Entities
                        |> List.tryFind (fun entity -> entity.Name = "Constrained")
                        |> Option.bind (fun entity ->
                            entity.Members |> List.tryFind (fun item -> item.Name = "larger")
                        )

                    match larger with
                    | None -> assertThat "larger" (tag "the function is found" >> isEqualTo "")
                    | Some larger ->
                        assertThat
                            larger.Signature
                            (tag "with the constraint that makes it work"
                             >> satisfy (fun (text: string) -> text.Contains "comparison"))
            )

            test (
                "an enum lists its cases with their values",
                fun _ ->
                    let volume = declaration "Volume"

                    assertThat
                        volume.Kind
                        (tag "read as an enum" >> isEqualTo FSharpApiEntityKind.Enum)

                    let cases =
                        volume.Members
                        |> List.filter (fun item -> item.Kind = FSharpApiMemberKind.EnumCase)
                        |> List.map _.Signature

                    assertThat
                        (cases |> List.exists (fun text -> text.StartsWith "Quiet = "))
                        (tag "with what each one is worth" >> isTrue)
            )

            test (
                "an operator keeps a usable anchor",
                fun _ ->
                    let operators = declaration "Operators"

                    match
                        operators.Members |> List.tryFind (fun item -> item.Name.Contains "=>")
                    with
                    | None -> assertThat "=>" (tag "the operator is published" >> isEqualTo "")
                    | Some operator ->
                        assertThat
                            operator.Anchor
                            (tag "an anchor a link can point at" >> isNotEqualTo "")

                        assertThat
                            (operator.Doc.SeeAlso |> List.isEmpty)
                            (tag "and what it said to see also" >> isFalse)
            )

            test (
                "a member says how it is called",
                fun _ ->
                    let person = declaration "Person"

                    let signature name =
                        person.Members
                        |> List.tryFind (fun item -> item.Name = name)
                        |> Option.map _.Signature
                        |> Option.defaultValue ""

                    assertThat
                        (signature "Named")
                        (tag "on the type"
                         >> isEqualTo "static member Named (name: string) : Person")

                    assertThat
                        (signature "Greeting")
                        (tag "or on a value of it" >> isEqualTo "member Greeting: string")
            )

            test (
                "a type and its companion module are one declaration",
                fun _ ->
                    let widgets =
                        library.Namespaces
                        |> List.collect _.Entities
                        |> List.filter (fun entity -> entity.Name = "Widget")

                    assertThat
                        (List.length widgets)
                        (tag "one page, not two with the same name" >> isEqualTo 1)

                    let widget = List.head widgets

                    assertThat
                        (widget.Kind)
                        (tag "the type is what it is" >> isEqualTo FSharpApiEntityKind.Record)

                    let names = widget.Members |> List.map _.Name

                    assertThat
                        (List.contains "Label" names)
                        (tag "with the type's own fields" >> isTrue)

                    assertThat
                        (List.contains "create" names && List.contains "relabel" names)
                        (tag "and what the module says you do with it" >> isTrue)
            )

            test (
                "a nested type and its companion module are one declaration",
                fun _ ->
                    let pairs =
                        (declaration "Codec").Nested
                        |> List.filter (fun entity -> entity.Name = "Pair")

                    assertThat
                        (List.length pairs)
                        (tag "one page, not two with the same name" >> isEqualTo 1)

                    let pair = List.head pairs

                    assertThat
                        (pair.Kind)
                        (tag "the type is what it is" >> isEqualTo FSharpApiEntityKind.Record)

                    let names = pair.Members |> List.map _.Name

                    assertThat
                        (List.contains "Left" names)
                        (tag "with the type's own fields" >> isTrue)

                    assertThat
                        (List.contains "create" names)
                        (tag "and what the module says you do with it" >> isTrue)

                    let twins =
                        (declaration "Codec").Nested
                        |> List.filter (fun entity -> entity.Name = "Twin")

                    assertThat
                        (List.length twins)
                        (tag "an abbreviation merges with its module too" >> isEqualTo 1)

                    assertThat
                        ((List.head twins).Members |> List.map _.Name |> List.contains "double")
                        (tag "keeping the module's functions" >> isTrue)
            )

            test (
                "a module's functions keep their names and their shape",
                fun _ ->
                    let people = declaration "People"

                    let signature name =
                        people.Members
                        |> List.tryFind (fun item -> item.Name = name)
                        |> Option.map _.Signature
                        |> Option.defaultValue ""

                    assertThat
                        (signature "greet")
                        (tag "curried arguments stay curried"
                         >> isEqualTo "greet (greeting: string) (person: Person) : string")

                    assertThat
                        (signature "between")
                        (tag "and a tuple stays one argument"
                         >> isEqualTo "between (first: Person, second: Person) : string")
            )

            test (
                "what the author wrote about a function comes with it",
                fun _ ->
                    let greet =
                        (declaration "People").Members
                        |> List.find (fun item -> item.Name = "greet")

                    assertThat
                        (greet.Doc.Summary |> Option.defaultValue "")
                        (tag "the summary" >> isEqualTo "Greet someone.")

                    assertThat
                        (greet.Doc.Parameters |> List.map fst)
                        (tag "every parameter it documented"
                         >> isEqualTo
                             [
                                 "greeting"
                                 "person"
                             ])

                    assertThat (greet.Doc.Returns.IsSome) (tag "what it gives back" >> isTrue)

                    assertThat
                        (greet.Doc.Examples |> List.length)
                        (tag "and its example, as markdown" >> isEqualTo 1)

                    assertThat
                        (greet.Doc.Examples.Head.Contains "```fsharp")
                        (tag "in a fence a highlighter can colour" >> isTrue)
            )

            test (
                "a deprecated member says so",
                fun _ ->
                    let sayHello =
                        (declaration "People").Members
                        |> List.find (fun item -> item.Name = "sayHello")

                    assertThat
                        (sayHello.Doc.Obsolete |> Option.defaultValue "")
                        (tag "with the reason the attribute gave" >> isEqualTo "Use greet instead")
            )

            test (
                "a union case keeps its named fields",
                fun _ ->
                    let source = declaration "Source"

                    let case name =
                        source.Members
                        |> List.tryFind (fun item -> item.Name = name)
                        |> Option.map _.Signature
                        |> Option.defaultValue ""

                    assertThat
                        (case "FromFile")
                        (tag "a single named field" >> isEqualTo "FromFile of path: string")

                    assertThat
                        (case "Unknown")
                        (tag "and several" >> isEqualTo "Unknown of reason: string * code: int")

                    assertThat (case "Generated") (tag "a case with none" >> isEqualTo "Generated")
            )

            test (
                "an assembly is read into namespaces and declarations",
                fun _ ->
                    match Reader.read engine with
                    | Error message ->
                        assertThat message (tag "the assembly should be readable" >> isEqualTo "")
                    | Ok assembly ->
                        assertThat
                            assembly.Name
                            (tag "named after the file" >> isEqualTo "Nacara.Core")

                        let core =
                            assembly.Namespaces |> List.tryFind (fun ns -> ns.Name = "Nacara.Core")

                        assertThat core.IsSome (tag "its namespace is there" >> isTrue)

                        let names = core.Value.Entities |> List.map _.Name

                        assertThat
                            (List.contains "Route" names)
                            (tag "with the declarations it holds" >> isTrue)

                        assertThat
                            (names |> List.forall (fun name -> not (name.StartsWith "<")))
                            (tag "and nothing the compiler generated" >> isTrue)
            )

            test (
                "a declaration is written the way it was declared",
                fun _ ->
                    match Reader.read engine with
                    | Error message -> assertThat message (tag "readable" >> isEqualTo "")
                    | Ok assembly ->
                        let entities = assembly.Namespaces |> List.collect _.Entities

                        let signature name =
                            entities
                            |> List.tryFind (fun entity -> entity.Name = name)
                            |> Option.map _.Signature
                            |> Option.defaultValue ""

                        assertThat
                            (signature "Route")
                            (tag "a record is written out"
                             >> isEqualTo
                                 "type Route =\n    {\n        Locale: Locale\n        Segments: string list\n    }")

                        assertThat
                            (signature "Slug")
                            (tag "a module is a module" >> isEqualTo "module Slug")

                        assertThat
                            ((signature "Decoder").Contains "=")
                            (tag "an abbreviation carries what it abbreviates" >> isTrue)

                        let slugCreate =
                            entities
                            |> List.tryFind (fun entity -> entity.Name = "Slug")
                            |> Option.bind (fun entity ->
                                entity.Members |> List.tryFind (fun m -> m.Name = "create")
                            )

                        match slugCreate with
                        | None -> assertThat "create" (tag "Slug.create is there" >> isEqualTo "")
                        | Some create ->
                            assertThat
                                create.Signature
                                (tag "with its parameters named, as they were written"
                                 >> isEqualTo "create (text: string) : string")

                            assertThat
                                create.ShortSignature
                                (tag "and a one-line shape for the index"
                                 >> isEqualTo "string -> string")
            )

            test (
                "the documentation beside the assembly is read back with it",
                fun _ ->
                    match Reader.read engine with
                    | Error message -> assertThat message (tag "readable" >> isEqualTo "")
                    | Ok assembly ->
                        let entity name =
                            assembly.Namespaces
                            |> List.collect _.Entities
                            |> List.tryFind (fun entity -> entity.Name = name)

                        match entity "AbsolutePath" with
                        | None ->
                            assertThat "AbsolutePath" (tag "the type is found" >> isEqualTo "")
                        | Some path ->
                            match path.Doc.Summary with
                            | None ->
                                assertThat "" (tag "its summary came along" >> isNotEqualTo "")
                            | Some summary ->
                                assertThat
                                    (summary.Contains "absolute path")
                                    (tag "as the prose the author wrote" >> isTrue)

                                assertThat
                                    (summary.Contains "<c>" || summary.Contains "&lt;")
                                    (tag "with its markup turned into markdown" >> isFalse)

                            assertThat
                                (path.Doc.Remarks.IsSome)
                                (tag "and its remarks too" >> isTrue)
            )

            test (
                "a record's fields and a union's cases are members of it",
                fun _ ->
                    match Reader.read engine with
                    | Error message -> assertThat message (tag "readable" >> isEqualTo "")
                    | Ok assembly ->
                        let entity name =
                            assembly.Namespaces
                            |> List.collect _.Entities
                            |> List.tryFind (fun entity -> entity.Name = name)

                        match entity "Route" with
                        | None -> assertThat "Route" (tag "the record is found" >> isEqualTo "")
                        | Some route ->
                            assertThat
                                (route.Members
                                 |> List.exists (fun m -> m.Kind = FSharpApiMemberKind.RecordField))
                                (tag "a record lists its fields" >> isTrue)

                        match entity "PageSource" with
                        | None -> assertThat "PageSource" (tag "the union is found" >> isEqualTo "")
                        | Some source ->
                            let cases =
                                source.Members
                                |> List.filter (fun m -> m.Kind = FSharpApiMemberKind.UnionCase)
                                |> List.map _.Name

                            assertThat
                                (List.contains "FromFile" cases && List.contains "Generated" cases)
                                (tag "a union lists its cases" >> isTrue)
            )
        ]
    )
