namespace Nacara.Plugins.Internal

open System
open Nacara.Plugins
open System.Text
open System.Text.RegularExpressions
open Nacara.Core

/// <summary>
/// The pages an API reference is made of.
/// </summary>
[<RequireQualifiedAccess>]
module Render =

    /// F# words that are not types, so a signature does not offer a link to them.
    let private keywords =
        set
            [
                "of"
                "with"
                "get"
                "set"
                "when"
                "and"
                "or"
                "type"
                "module"
                "exception"
                "interface"
                "abstract"
                "static"
                "member"
                "inline"
                "new"
                "val"
                "delegate"
                "namespace"
                "class"
                "struct"
                "end"
            ]

    /// <summary>Types F# writes in lower case, which are types all the same.</summary>
    let private primitives =
        set
            [
                "string"
                "int"
                "bool"
                "unit"
                "float"
                "float32"
                "double"
                "decimal"
                "char"
                "byte"
                "sbyte"
                "int16"
                "int64"
                "uint"
                "uint16"
                "uint32"
                "uint64"
                "nativeint"
                "obj"
                "exn"
                "bigint"
                "list"
                "array"
                "option"
                "voption"
                "seq"
                "ref"
                "byref"
                "inref"
                "outref"
            ]

    let private escapeHtml (text: string) =
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")

    /// <summary>What is between the words: brackets read as punctuation, arrows as operators.</summary>
    let private between (text: string) =
        let builder = StringBuilder()
        let mutable index = 0

        while index < text.Length do
            let start = index
            let isSpace = Char.IsWhiteSpace text[index]

            while index < text.Length && Char.IsWhiteSpace text[index] = isSpace do
                index <- index + 1

            let run = text.Substring(start, index - start)

            if isSpace then
                builder.Append(escapeHtml run) |> ignore
            else
                let cssClass =
                    if run |> Seq.exists (fun character -> "-><*=&".Contains character) then
                        "tok-operator"
                    else
                        "tok-punctuation"

                builder.Append $"""<span class="%s{cssClass}">%s{escapeHtml run}</span>"""
                |> ignore

        builder.ToString()

    let private word = Regex(@"[A-Za-z_][A-Za-z0-9_.\']*", RegexOptions.Compiled)

    /// <summary>Whether a word opens its own line, which is where a name is declared.</summary>
    let private introductions =
        set
            [
                "abstract"
                "member"
                "static"
                "new"
                "inline"
                "val"
            ]

    let private declaring (text: string) (index: int) =
        let start = text.LastIndexOf('\n', max 0 (index - 1)) + 1

        let before =
            text
                .Substring(start, index - start)
                .Trim(
                    [|
                        ' '
                        '|'
                    |]
                )

        before = ""
        || before.Split(' ')
           |> Array.forall (fun word -> word = "" || introductions.Contains word)

    /// <summary>What a member is called in a signature, which is not always a type.</summary>
    let private tokenOf (kind: FSharpApiMemberKind) =
        match kind with
        | FSharpApiMemberKind.UnionCase
        | FSharpApiMemberKind.EnumCase -> "tok-constructor"
        | FSharpApiMemberKind.RecordField
        | FSharpApiMemberKind.Property
        | FSharpApiMemberKind.Event -> "tok-property"
        | FSharpApiMemberKind.Value
        | FSharpApiMemberKind.Method
        | FSharpApiMemberKind.Constructor
        | FSharpApiMemberKind.ActivePattern
        | FSharpApiMemberKind.Extension -> "tok-function"

    let private write
        (locals: Map<string, string * string option>)
        (own: string)
        (resolve: string -> string option)
        (text: string)
        =
        let builder = StringBuilder()

        let hanging =
            if text.Contains "\n" then
                ""
            else
                " nacara-api__signature--hanging"

        builder.Append $"""<pre class="nacara-api__signature%s{hanging}"><code>"""
        |> ignore

        let mutable last = 0

        for matched in word.Matches text do
            builder.Append(between (text.Substring(last, matched.Index - last))) |> ignore

            let name = matched.Value

            let generic = matched.Index > 0 && text[matched.Index - 1] = '\''

            let named =
                let after = matched.Index + matched.Length

                after < text.Length && text[after] = ':'

            let declared =
                if generic || not (declaring text matched.Index) then
                    None
                else
                    Map.tryFind name locals

            let cssClass, link =
                match declared with
                | Some(cssClass, link) -> cssClass, link
                | None ->
                    let cssClass =
                        if keywords.Contains name then
                            "tok-keyword"
                        elif generic then
                            "tok-parameter"
                        elif primitives.Contains name then
                            "tok-type"
                        elif name.Length > 0 && Char.IsUpper name[0] then
                            "tok-type"
                        elif named then
                            "tok-parameter"
                        else
                            "tok-variable"

                    let link =
                        if generic || name = own then
                            None
                        else
                            resolve name

                    cssClass, link

            match link with
            | Some url ->
                builder.Append $"""<a class="%s{cssClass}" href="%s{url}">%s{escapeHtml name}</a>"""
                |> ignore
            | None ->
                builder.Append $"""<span class="%s{cssClass}">%s{escapeHtml name}</span>"""
                |> ignore

            last <- matched.Index + matched.Length

        builder.Append(between (text.Substring last)) |> ignore
        builder.Append "</code></pre>" |> ignore
        builder.ToString()

    /// <summary>A signature of a member or a type, with the types in it linked.</summary>
    let signature (resolve: string -> string option) (text: string) =
        write Map.empty "" resolve text

    /// <summary>
    /// The members of a page, in the order it shows them.
    /// </summary>
    let private grouped (members: FSharpApiMember list) =
        members
        |> List.groupBy _.Kind
        |> List.sortBy (fun (kind, _) ->
            match kind with
            | FSharpApiMemberKind.UnionCase -> 0
            | FSharpApiMemberKind.RecordField -> 1
            | FSharpApiMemberKind.EnumCase -> 2
            | FSharpApiMemberKind.Constructor -> 3
            | FSharpApiMemberKind.Value -> 4
            | FSharpApiMemberKind.Property -> 5
            | FSharpApiMemberKind.Method -> 6
            | FSharpApiMemberKind.ActivePattern -> 7
            | FSharpApiMemberKind.Event -> 8
            | FSharpApiMemberKind.Extension -> 9
        )
        |> List.map (fun (kind, items) ->
            match kind with
            | FSharpApiMemberKind.UnionCase
            | FSharpApiMemberKind.RecordField
            | FSharpApiMemberKind.EnumCase -> kind, items
            | _ -> kind, items |> List.sortBy _.Name
        )

    /// <summary>
    /// The declaration a page is about, with what it declares linked to its documentation below.
    /// </summary>
    let declaration (resolve: string -> string option) (entity: FSharpApiEntity) =
        let locals =
            entity.Members
            |> List.map (fun item -> item.Name, (tokenOf item.Kind, Some $"#%s{item.Anchor}"))
            |> List.distinctBy fst
            |> Map.ofList

        let text =
            match entity.Kind with
            | FSharpApiEntityKind.Class
            | FSharpApiEntityKind.Interface
            | FSharpApiEntityKind.Struct when not entity.Members.IsEmpty ->
                let written =
                    grouped entity.Members
                    |> List.collect snd
                    |> List.map (fun item -> "    " + item.Signature)
                    |> String.concat "\n"

                $"%s{entity.Signature} =\n%s{written}"
            | _ -> entity.Signature

        write locals entity.Name resolve text

    /// The first sentence of a summary, which is all a list of declarations has room for.
    let private firstSentence (text: string) =
        let text = text.Replace("\n", " ").Trim()

        match text.IndexOf ". " with
        | -1 -> text.TrimEnd '.'
        | stop -> text.Substring(0, stop)

    /// <summary>A pipe inside a table cell would end the cell, and F# is full of pipes.</summary>
    let private cell (text: string) = text.Replace("|", "\\|")

    let private summaryLine (doc: FSharpApiDoc) =
        doc.Summary |> Option.map firstSentence |> Option.defaultValue ""

    /// <summary>Anchors have to be unique on a page, and overloads share a name.</summary>
    let private uniqueAnchors (members: FSharpApiMember list) =
        members
        |> List.mapFold
            (fun seen (item: FSharpApiMember) ->
                let count = seen |> Map.tryFind item.Anchor |> Option.defaultValue 0

                let anchor =
                    if count = 0 then
                        item.Anchor
                    else
                        $"%s{item.Anchor}-%i{count}"

                { item with
                    Anchor = anchor
                },
                Map.add item.Anchor (count + 1) seen
            )
            Map.empty
        |> fst

    let private frontMatter (title: string) (description: string) (builder: StringBuilder) =
        builder.AppendLine "---" |> ignore
        builder.AppendLine $"title: %s{title}" |> ignore

        if description <> "" then
            // A summary is prose and prose contains colons.
            let quoted = description.Replace("\"", "'")
            builder.AppendLine("description: \"" + quoted + "\"") |> ignore

        builder.AppendLine "pageNav: false" |> ignore

        builder.AppendLine "menuMemory: false" |> ignore
        builder.AppendLine "---" |> ignore
        builder.AppendLine "" |> ignore

    let private code (language: string) (text: string) (builder: StringBuilder) =
        builder.AppendLine $"```%s{language}" |> ignore
        builder.AppendLine text |> ignore
        builder.AppendLine "```" |> ignore
        builder.AppendLine "" |> ignore

    let private prose (doc: FSharpApiDoc) (builder: StringBuilder) =
        match doc.Obsolete with
        | Some message ->
            builder.AppendLine ":::warning Deprecated" |> ignore

            builder.AppendLine(
                if message = "" then
                    "This is no longer supported."
                else
                    message
            )
            |> ignore

            builder.AppendLine ":::" |> ignore
            builder.AppendLine "" |> ignore
        | None -> ()

        match doc.Summary with
        | Some summary ->
            builder.AppendLine summary |> ignore
            builder.AppendLine "" |> ignore
        | None -> ()

        match doc.Remarks with
        | Some remarks ->
            builder.AppendLine remarks |> ignore
            builder.AppendLine "" |> ignore
        | None -> ()

    /// <summary>
    /// Turns what a <c>cref</c> pointed at into a link, when this build published it.
    /// </summary>
    let private crossLinks (resolve: string -> string option) (markdown: string) =
        Regex.Replace(
            markdown,
            @"\[([^\]]*)\]\(" + Regex.Escape Reference.scheme + @"([^)]*)\)",
            fun found ->
                let label = found.Groups[1].Value
                let name = found.Groups[2].Value
                let short = name.Split('.') |> Array.last

                match resolve name |> Option.orElseWith (fun () -> resolve short) with
                | Some url -> $"[%s{label}](%s{url})"
                | None -> label
        )

    let private seeAlso
        (resolve: string -> string option)
        (doc: FSharpApiDoc)
        (builder: StringBuilder)
        =
        if not (List.isEmpty doc.SeeAlso) then
            let links =
                doc.SeeAlso
                |> List.map (fun name ->
                    let short = name.Split('.') |> Array.last

                    match resolve short with
                    | Some url -> $"[`%s{short}`](%s{url})"
                    | None -> $"`%s{name}`"
                )

            builder.AppendLine("**See also.** " + String.Join(", ", links)) |> ignore
            builder.AppendLine "" |> ignore

    let private examples (doc: FSharpApiDoc) (builder: StringBuilder) =
        for example in doc.Examples do
            builder.AppendLine example |> ignore
            builder.AppendLine "" |> ignore

    /// <summary>A two-column table of named things and what was said about them.</summary>
    let private describedTable
        (header: string)
        (rows: (string * string) list)
        (builder: StringBuilder)
        =
        if not (List.isEmpty rows) then
            builder.AppendLine $"| %s{header} | |" |> ignore
            builder.AppendLine "|---|---|" |> ignore

            for name, description in rows do
                // A table cell is one line, and the prose beside a name may not be.
                let text = cell (description.Replace("\n", " "))
                builder.AppendLine $"| `%s{cell name}` | %s{text} |" |> ignore

            builder.AppendLine "" |> ignore

    /// <summary>
    /// The members of a page, each one an entry that opens.
    /// </summary>
    let private details
        (own: string)
        (resolve: string -> string option)
        (members: FSharpApiMember list)
        (builder: StringBuilder)
        =
        for kind, items in grouped members do
            builder.AppendLine $"## %s{kind.Label}" |> ignore
            builder.AppendLine "" |> ignore

            for item in items do
                let body = StringBuilder()
                prose item.Doc body
                describedTable "Type parameter" item.Doc.TypeParameters body
                describedTable "Parameter" item.Doc.Parameters body

                match item.Doc.Returns with
                | Some returns ->
                    body.AppendLine $"**Returns.** %s{returns}" |> ignore
                    body.AppendLine "" |> ignore
                | None -> ()

                describedTable "Raises" item.Doc.Exceptions body
                seeAlso resolve item.Doc body
                examples item.Doc body

                let deprecated =
                    if item.Doc.Obsolete.IsSome then
                        " nacara-api__entry--deprecated"
                    else
                        ""

                let opens = body.ToString().Trim() <> ""

                let tag =
                    if opens then
                        "details"
                    else
                        "div"

                builder.AppendLine
                    $"""<div class="nacara-api__entry%s{deprecated}" id="%s{item.Anchor}">"""
                |> ignore

                builder.AppendLine
                    $"""<a class="nacara-api__anchor"
                           href="#%s{item.Anchor}"
                           aria-label="Link to %s{escapeHtml item.Name}"
                           data-pagefind-ignore></a>"""
                |> ignore

                builder.AppendLine $"""<%s{tag} class="nacara-api__member">""" |> ignore

                if opens then
                    builder.AppendLine "<summary>" |> ignore

                let named = Map.ofList [ item.Name, (tokenOf item.Kind, None) ]

                builder.AppendLine(write named "" resolve item.Signature) |> ignore

                if opens then
                    builder.AppendLine "</summary>" |> ignore
                    builder.AppendLine """<div class="nacara-api__body">""" |> ignore
                    // A blank line hands what follows back to markdown.
                    builder.AppendLine "" |> ignore
                    builder.Append(body.ToString()) |> ignore
                    builder.AppendLine "" |> ignore
                    builder.AppendLine "</div>" |> ignore

                builder.AppendLine $"</%s{tag}>" |> ignore
                builder.AppendLine "</div>" |> ignore
                builder.AppendLine "" |> ignore

    /// <summary>What a type carries besides its members, when it carries any.</summary>
    let private facts (assembly: bool) (entity: FSharpApiEntity) (builder: StringBuilder) =
        let rows =
            [
                if assembly && entity.Assembly <> "" then
                    "Assembly", $"`%s{cell entity.Assembly}`"

                match entity.BaseType with
                | Some baseType -> "Inherits", $"`%s{cell baseType}`"
                | None -> ()

                if not (List.isEmpty entity.Interfaces) then
                    "Implements",
                    entity.Interfaces
                    |> List.map (fun name -> $"`%s{cell name}`")
                    |> String.concat ", "

                if not (List.isEmpty entity.Attributes) then
                    "Attributes",
                    entity.Attributes
                    |> List.map (fun name -> $"`[<%s{cell name}>]`")
                    |> String.concat " "
            ]

        if not (List.isEmpty rows) then
            builder.AppendLine "| | |" |> ignore
            builder.AppendLine "|---|---|" |> ignore

            for label, value in rows do
                builder.AppendLine $"| %s{label} | %s{value} |" |> ignore

            builder.AppendLine "" |> ignore

    /// <summary>The page of one type or module.</summary>
    let entity
        (link: string -> string)
        (resolve: string -> string option)
        (assembly: bool)
        (entity: FSharpApiEntity)
        =
        let builder = StringBuilder()
        frontMatter entity.Name (summaryLine entity.Doc) builder

        builder.AppendLine(declaration resolve entity) |> ignore
        builder.AppendLine "" |> ignore
        facts assembly entity builder
        prose entity.Doc builder
        seeAlso resolve entity.Doc builder
        examples entity.Doc builder

        if not (List.isEmpty entity.Nested) then
            builder.AppendLine "## Declared inside" |> ignore
            builder.AppendLine "" |> ignore
            builder.AppendLine "| | |" |> ignore
            builder.AppendLine "|---|---|" |> ignore

            for nested in entity.Nested do
                let url = link nested.Slug
                let entry = $"[`%s{cell nested.Name}`](%s{url})"

                builder.AppendLine $"| %s{entry} | %s{cell (summaryLine nested.Doc)} |"
                |> ignore

            builder.AppendLine "" |> ignore

        let members = uniqueAnchors entity.Members

        if not (List.isEmpty members) then
            details entity.Name resolve members builder

        crossLinks resolve (builder.ToString())

    /// <summary>The page of one namespace: what it declares, and nothing about the insides.</summary>
    let ``namespace``
        (link: string -> string)
        (resolve: string -> string option)
        (assembly: bool)
        (ns: FSharpApiNamespace)
        =
        let builder = StringBuilder()
        frontMatter ns.Name $"The declarations of %s{ns.Name}" builder

        let groups =
            ns.Entities |> List.groupBy _.Kind |> List.sortBy (fun (kind, _) -> kind.Order)

        for kind, entities in groups do
            let heading =
                if List.length entities = 1 then
                    kind.Label
                else
                    kind.Plural

            builder.AppendLine $"## %s{heading}" |> ignore
            builder.AppendLine "" |> ignore

            if assembly then
                builder.AppendLine "| | | Assembly |" |> ignore
                builder.AppendLine "|---|---|---|" |> ignore
            else
                builder.AppendLine "| | |" |> ignore
                builder.AppendLine "|---|---|" |> ignore

            for entity in entities do
                let entry = $"[`%s{cell entity.Name}`](%s{link entity.Slug})"
                let summary = cell (summaryLine entity.Doc)

                if assembly then
                    builder.AppendLine $"| %s{entry} | %s{summary} | `%s{cell entity.Assembly}` |"
                    |> ignore
                else
                    builder.AppendLine $"| %s{entry} | %s{summary} |" |> ignore

            builder.AppendLine "" |> ignore

        crossLinks resolve (builder.ToString())

    /// <summary>How many declarations, said the way a table cell wants it.</summary>
    let private counted (entities: int) =
        match entities with
        | 1 -> "1 declaration"
        | count -> $"%i{count} declarations"

    /// <summary>The way into one package: the namespaces it declares, and how much is in each.</summary>
    /// <param name="name">The package's own name, which is the page's title.</param>
    /// <param name="link">How a slug becomes a url.</param>
    /// <param name="namespaces">What this package declares.</param>
    let package (name: string) (link: string -> string) (namespaces: FSharpApiNamespace list) =
        let builder = StringBuilder()
        frontMatter name "" builder

        builder.AppendLine "| Namespace | |" |> ignore
        builder.AppendLine "|---|---|" |> ignore

        for ns in namespaces do
            builder.AppendLine
                $"| [`%s{ns.Name}`](%s{link ns.Slug}) | %s{counted (List.length ns.Entities)} |"
            |> ignore

        builder.AppendLine "" |> ignore
        crossLinks (fun _ -> None) (builder.ToString())

    /// <summary>
    /// The way in: the packages documented, or their namespaces when there is only one package.
    /// </summary>
    /// <param name="title">The page's title, from the plugin's options.</param>
    /// <param name="link">How a slug becomes a url.</param>
    /// <param name="namespaces">Everything this build published.</param>
    let index' (title: string) (link: string -> string) (namespaces: FSharpApiNamespace list) =
        let builder = StringBuilder()
        frontMatter title "" builder

        let packages = namespaces |> List.groupBy _.Assembly

        match packages with
        | [ _ ] ->
            builder.AppendLine "| Namespace | |" |> ignore
            builder.AppendLine "|---|---|" |> ignore

            for ns in namespaces do
                let entry = $"[`%s{ns.Name}`](%s{link ns.Slug})"

                builder.AppendLine $"| %s{entry} | %s{counted (List.length ns.Entities)} |"
                |> ignore
        | packages ->
            builder.AppendLine "| Package | |" |> ignore
            builder.AppendLine "|---|---|" |> ignore

            for package, namespaces in packages do
                let entry = $"[`%s{package}`](%s{link (Slug.create package)})"

                let declarations = namespaces |> List.sumBy (fun ns -> List.length ns.Entities)

                builder.AppendLine $"| %s{entry} | %s{counted declarations} |" |> ignore

        builder.AppendLine "" |> ignore

        crossLinks (fun _ -> None) (builder.ToString())
