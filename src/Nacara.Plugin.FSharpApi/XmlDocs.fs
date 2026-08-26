namespace Nacara.Plugins.Internal

open System
open Nacara.Plugins
open System.IO
open System.Text
open System.Text.RegularExpressions
open System.Xml.Linq

/// <remarks>
/// What comes out is markdown: <c>&lt;c&gt;</c> becomes a code span, <c>&lt;code&gt;</c> a fenced
/// block, <c>&lt;see cref="T:My.Type"/&gt;</c> the name it refers to.
/// </remarks>
/// <summary>How a <c>cref</c> travels from the XML file to the page that resolves it.</summary>
[<RequireQualifiedAccess>]
module Reference =

    /// <summary>Link scheme of a cross-reference waiting to be resolved.</summary>
    [<Literal>]
    let scheme = "nacara-api:"

[<RequireQualifiedAccess>]
module XmlDocs =

    /// <summary>Everything one declaration's documentation says.</summary>
    type Entry =
        {
            Summary: string option
            Remarks: string option
            Examples: string list
            Parameters: (string * string) list
            /// Type parameters the author documented, by name.
            TypeParameters: (string * string) list
            Returns: string option
            /// What it raises, and when.
            Exceptions: (string * string) list
            SeeAlso: string list
        }

    let private trimLines (text: string) =
        text.Replace("\r\n", "\n").Split('\n')
        |> Array.map _.TrimEnd()
        // The compiler indents every line by the depth of the comment.
        |> Array.map (fun line -> line.TrimStart())
        |> String.concat "\n"
        |> _.Trim()

    /// The name a cref points at, without the kind letter the compiler prefixes it with.
    let private crefName (cref: string) =
        let name =
            if cref.Length > 2 && cref[1] = ':' then
                cref.Substring 2
            else
                cref

        name.Split('(')[0]

    let rec private toMarkdown (node: XNode) : string =
        match node with
        | :? XText as text -> text.Value
        | :? XElement as element ->
            let inner () =
                element.Nodes() |> Seq.map toMarkdown |> String.concat ""

            match element.Name.LocalName.ToLowerInvariant() with
            | "c" -> "`" + inner () + "`"
            | "code" ->
                let language =
                    match element.Attribute(XName.Get "lang") with
                    | null -> "fsharp"
                    | attribute -> attribute.Value

                $"\n\n```%s{language}\n%s{trimLines (inner ())}\n```\n\n"
            | "para" -> "\n\n" + inner () + "\n\n"
            | "br" -> "\n"
            | "see"
            | "seealso" ->
                match element.Attribute(XName.Get "cref"), element.Attribute(XName.Get "href") with
                | null, null -> inner ()
                | null, href ->
                    let label = inner ()

                    let text =
                        if label = "" then
                            href.Value
                        else
                            label

                    $"[%s{text}](%s{href.Value})"
                | cref, _ ->
                    let label = inner ()
                    let name = crefName cref.Value

                    let text =
                        if label = "" then
                            "`" + (name.Split('.') |> Array.last) + "`"
                        else
                            label

                    $"[%s{text}](%s{Reference.scheme}%s{name})"
            | "paramref"
            | "typeparamref" ->
                match element.Attribute(XName.Get "name") with
                | null -> inner ()
                | name -> "`" + name.Value + "`"
            | "list" ->
                let items =
                    element.Elements(XName.Get "item")
                    |> Seq.map (fun item ->
                        "- "
                        + (item.Nodes() |> Seq.map toMarkdown |> String.concat "" |> trimLines)
                    )
                    |> String.concat "\n"

                "\n\n" + items + "\n\n"
            | _ -> inner ()
        | _ -> ""

    let private textOf (element: XElement) =
        element.Nodes() |> Seq.map toMarkdown |> String.concat "" |> trimLines

    let private optional (element: XElement) (name: string) =
        match element.Element(XName.Get name) with
        | null -> None
        | child ->
            match textOf child with
            | "" -> None
            | text -> Some text

    /// <summary>Read the documentation file that belongs to an assembly, if it is there.</summary>
    let read (assemblyPath: string) : Map<string, Entry> =
        let path = Path.ChangeExtension(assemblyPath, ".xml")

        if not (File.Exists path) then
            Map.empty
        else

            try
                let document =
                    try
                        XDocument.Load path
                    with _ ->
                        // The compiler writes an anonymous record parameter as <>f__AnonymousType..., which is not valid XML.
                        let repaired =
                            Regex.Replace(
                                File.ReadAllText path,
                                "name=\"([^\"]*)\"",
                                fun found ->
                                    let value =
                                        found.Groups[1]
                                            .Value.Replace("<", "&lt;")
                                            .Replace(">", "&gt;")

                                    $"name=\"%s{value}\""
                            )

                        XDocument.Parse repaired

                document.Descendants(XName.Get "member")
                |> Seq.choose (fun element ->
                    match element.Attribute(XName.Get "name") with
                    | null -> None
                    | name ->
                        let entry =
                            {
                                Summary = optional element "summary"
                                Remarks = optional element "remarks"
                                Examples =
                                    element.Elements(XName.Get "example")
                                    |> Seq.map textOf
                                    |> Seq.filter (fun text -> text <> "")
                                    |> List.ofSeq
                                Parameters =
                                    element.Elements(XName.Get "param")
                                    |> Seq.choose (fun parameter ->
                                        match parameter.Attribute(XName.Get "name") with
                                        | null -> None
                                        | parameterName ->
                                            Some(parameterName.Value, textOf parameter)
                                    )
                                    |> List.ofSeq
                                TypeParameters =
                                    element.Elements(XName.Get "typeparam")
                                    |> Seq.choose (fun parameter ->
                                        match parameter.Attribute(XName.Get "name") with
                                        | null -> None
                                        | parameterName ->
                                            Some(parameterName.Value, textOf parameter)
                                    )
                                    |> List.ofSeq
                                Returns = optional element "returns"
                                Exceptions =
                                    element.Elements(XName.Get "exception")
                                    |> Seq.choose (fun raised ->
                                        match raised.Attribute(XName.Get "cref") with
                                        | null -> None
                                        | cref -> Some(crefName cref.Value, textOf raised)
                                    )
                                    |> List.ofSeq
                                SeeAlso =
                                    element.Elements(XName.Get "seealso")
                                    |> Seq.choose (fun link ->
                                        match link.Attribute(XName.Get "cref") with
                                        | null -> None
                                        | cref -> Some(crefName cref.Value)
                                    )
                                    |> List.ofSeq
                            }

                        Some(name.Value, entry)
                )
                |> Map.ofSeq
            with _ ->
                Map.empty
