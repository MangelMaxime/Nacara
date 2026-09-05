namespace Nacara.Plugins.Internal

open System
open Nacara.Plugins
open System.IO
open System.Reflection.Metadata
open System.Reflection.PortableExecutable
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Symbols
open Nacara.Core

/// <summary>
/// Reads the public API of a compiled assembly.
/// </summary>
[<RequireQualifiedAccess>]
module Reader =

    let private display = FSharpDisplayContext.Empty.WithShortTypeNames true

    let private isVisible (symbol: FSharpSymbol) =
        match symbol with
        | :? FSharpEntity as entity -> entity.Accessibility.IsPublic
        | :? FSharpMemberOrFunctionOrValue as value -> value.Accessibility.IsPublic
        | _ -> true

    /// Members the CLR gives everything, which say nothing about the library.
    let private inherited =
        set
            [
                "ToString"
                "Equals"
                "GetHashCode"
                "GetType"
                "CompareTo"
                "Finalize"
                "MemberwiseClone"
                "get_Tag"
                "get_IsX"
            ]

    let private kindOf (entity: FSharpEntity) =
        if entity.IsMeasure then
            FSharpApiEntityKind.Measure
        elif entity.IsFSharpModule then
            FSharpApiEntityKind.Module
        elif entity.IsFSharpRecord then
            FSharpApiEntityKind.Record
        elif entity.IsFSharpUnion then
            FSharpApiEntityKind.Union
        elif entity.IsInterface then
            FSharpApiEntityKind.Interface
        elif entity.IsEnum then
            FSharpApiEntityKind.Enum
        elif entity.IsFSharpAbbreviation then
            FSharpApiEntityKind.Abbreviation
        elif entity.IsFSharpExceptionDeclaration then
            FSharpApiEntityKind.Exception
        elif entity.IsDelegate then
            FSharpApiEntityKind.Delegate
        elif entity.IsValueType then
            FSharpApiEntityKind.Struct
        else
            FSharpApiEntityKind.Class

    /// <summary>
    /// The type a member was added to, when it was added to one it does not live in.
    /// </summary>
    let private extendedType (value: FSharpMemberOrFunctionOrValue) =
        try
            // IsExtensionMember answers for source and says nothing about a compiled assembly.
            let marked =
                value.Attributes
                |> Seq.exists (fun attribute ->
                    attribute.AttributeType.DisplayName.StartsWith "Extension"
                )

            if not (value.IsExtensionMember || marked) then
                None
            else
                value.CurriedParameterGroups
                |> Seq.tryHead
                |> Option.bind Seq.tryHead
                |> Option.map (fun parameter -> parameter.Type.TypeDefinition.DisplayName)
        with _ ->
            None

    let private memberKind (value: FSharpMemberOrFunctionOrValue) =
        if value.IsConstructor then
            FSharpApiMemberKind.Constructor
        elif value.IsEvent || value.IsEventAddMethod || value.IsEventRemoveMethod then
            FSharpApiMemberKind.Event
        elif value.IsProperty || value.IsPropertyGetterMethod || value.IsPropertySetterMethod then
            FSharpApiMemberKind.Property
        elif value.IsActivePattern then
            FSharpApiMemberKind.ActivePattern
        elif value.IsMember then
            FSharpApiMemberKind.Method
        else
            FSharpApiMemberKind.Value

    /// <summary>
    /// The anchor a link can point at.
    /// </summary>
    let private anchorOf (name: string) =
        match Slug.create name with
        | "" ->
            let spelled =
                name.Trim(
                    [|
                        '('
                        ')'
                        ' '
                    |]
                )
                |> Seq.choose (fun character ->
                    match character with
                    | '=' -> Some "eq"
                    | '>' -> Some "gt"
                    | '<' -> Some "lt"
                    | '+' -> Some "plus"
                    | '-' -> Some "minus"
                    | '*' -> Some "star"
                    | '/' -> Some "slash"
                    | '%' -> Some "percent"
                    | '&' -> Some "amp"
                    | '|' -> Some "bar"
                    | '^' -> Some "hat"
                    | '!' -> Some "bang"
                    | '?' -> Some "question"
                    | '~' -> Some "tilde"
                    | '@' -> Some "at"
                    | '.' -> Some "dot"
                    | ':' -> Some "colon"
                    | '$' -> Some "dollar"
                    | _ -> None
                )
                |> String.concat "-"

            if spelled = "" then
                "op"
            else
                "op-" + spelled
        | slug -> slug

    let private docOf
        (docs: Map<string, XmlDocs.Entry>)
        (signature: string)
        (obsolete: string option)
        =
        match Map.tryFind signature docs with
        | None ->
            { FSharpApiDoc.Empty with
                Obsolete = obsolete
            }
        | Some entry ->
            {
                Summary = entry.Summary
                Remarks = entry.Remarks
                Examples = entry.Examples
                Parameters = entry.Parameters
                TypeParameters = entry.TypeParameters
                Returns = entry.Returns
                Exceptions = entry.Exceptions
                Obsolete = obsolete
                SeeAlso = entry.SeeAlso
            }

    /// <summary><c>[&lt;Obsolete&gt;]</c>, and what it said.</summary>
    let private obsoleteOf (attributes: seq<FSharpAttribute>) =
        attributes
        |> Seq.tryPick (fun attribute ->
            try
                if attribute.AttributeType.DisplayName.StartsWith "Obsolete" then
                    attribute.ConstructorArguments
                    |> Seq.tryPick (fun (_, value) ->
                        match value with
                        | :? string as message -> Some message
                        | _ -> None
                    )
                    |> Option.orElse (Some "")
                else
                    None
            with _ ->
                None
        )

    let private readMember
        (docs: Map<string, XmlDocs.Entry>)
        (value: FSharpMemberOrFunctionOrValue)
        =
        let extends = extendedType value

        let name =
            if value.IsConstructor then
                "Constructor"
            else
                value.DisplayName

        {
            Name = name
            Kind =
                match extends with
                | Some _ -> FSharpApiMemberKind.Extension
                | None -> memberKind value
            Signature = Signature.ofMember value
            ShortSignature = Signature.short value
            Parameters =
                value.CurriedParameterGroups
                |> Seq.collect id
                |> Seq.map (fun parameter ->
                    {
                        Name = parameter.Name
                        Type = parameter.Type.Format display
                        Summary = None
                    }
                )
                |> List.ofSeq
            ReturnType =
                try
                    Some(value.ReturnParameter.Type.Format display)
                with _ ->
                    None
            IsStatic = not value.IsInstanceMember
            Extends = extends
            Doc = docOf docs value.XmlDocSig (obsoleteOf value.Attributes)
            Anchor = anchorOf name
        }

    let rec private readEntity
        (docs: Map<string, XmlDocs.Entry>)
        (parentSlug: string)
        (entity: FSharpEntity)
        =
        let name = entity.DisplayName

        let slug =
            if parentSlug = "" then
                Slug.create name
            else
                $"%s{parentSlug}/%s{Slug.create name}"

        let members =
            [
                if entity.IsFSharpUnion then
                    for case in entity.UnionCases do
                        {
                            Name = case.DisplayName
                            Kind = FSharpApiMemberKind.UnionCase
                            Signature = Signature.ofUnionCase case
                            ShortSignature = Signature.ofUnionCase case
                            Parameters = []
                            ReturnType = None
                            IsStatic = true
                            Extends = None
                            Doc = docOf docs case.XmlDocSig (obsoleteOf case.Attributes)
                            Anchor = anchorOf case.DisplayName
                        }

                if entity.IsEnum then
                    for field in entity.FSharpFields do
                        if field.IsLiteral then
                            {
                                Name = field.DisplayName
                                Kind = FSharpApiMemberKind.EnumCase
                                Signature =
                                    match field.LiteralValue with
                                    | Some value -> $"%s{field.DisplayName} = %A{value}"
                                    | None -> field.DisplayName
                                ShortSignature =
                                    field.LiteralValue
                                    |> Option.map (fun value -> $"%A{value}")
                                    |> Option.defaultValue ""
                                Parameters = []
                                ReturnType = None
                                IsStatic = true
                                Extends = None
                                Doc = docOf docs field.XmlDocSig None
                                Anchor = anchorOf field.DisplayName
                            }

                if entity.IsFSharpRecord then
                    for field in entity.FSharpFields do
                        {
                            Name = field.DisplayName
                            Kind = FSharpApiMemberKind.RecordField
                            Signature = Signature.ofField field
                            ShortSignature = field.FieldType.Format display
                            Parameters = []
                            ReturnType = None
                            IsStatic = false
                            Extends = None
                            Doc = docOf docs field.XmlDocSig (obsoleteOf field.PropertyAttributes)
                            Anchor = anchorOf field.DisplayName
                        }

                for value in entity.MembersFunctionsAndValues do
                    if
                        isVisible value
                        && not (inherited.Contains value.DisplayName)
                        && not value.IsPropertyGetterMethod
                        && not value.IsPropertySetterMethod
                        && not (value.DisplayName.StartsWith "get_")
                        && not (value.DisplayName.StartsWith "set_")
                        && not value.IsEventAddMethod
                        && not value.IsEventRemoveMethod
                        && not (value.DisplayName.StartsWith "add_")
                        && not (value.DisplayName.StartsWith "remove_")
                    then
                        readMember docs value
            ]
            // Overloads share a name, and two headings cannot share an anchor.
            |> List.mapFold
                (fun seen item ->
                    match Map.tryFind item.Anchor seen with
                    | None -> item, Map.add item.Anchor 1 seen
                    | Some count ->
                        { item with
                            Anchor = $"%s{item.Anchor}-%i{count}"
                        },
                        Map.add item.Anchor (count + 1) seen
                )
                Map.empty
            |> fst

        {
            Name = name
            FullName =
                try
                    entity.FullName
                with _ ->
                    name
            Namespace = entity.Namespace |> Option.defaultValue ""
            Kind = kindOf entity
            Signature = Signature.ofEntity entity
            TypeParameters =
                entity.GenericParameters |> Seq.map (fun p -> "'" + p.DisplayName) |> List.ofSeq
            Members = members
            Interfaces =
                entity.DeclaredInterfaces
                |> Seq.choose (fun interfaceType ->
                    try
                        Some(interfaceType.Format display)
                    with _ ->
                        None
                )
                |> Seq.filter (fun name ->
                    not (
                        name.StartsWith "IComparable"
                        || name.StartsWith "IEquatable"
                        || name.StartsWith "IStructural"
                        || name = "IComparable"
                    )
                )
                |> Seq.distinct
                |> List.ofSeq
            BaseType =
                try
                    match entity.BaseType with
                    | Some baseType ->
                        let name = baseType.Format display

                        if
                            name = "obj" || name = "System.Object" || name.StartsWith "ValueType"
                        then
                            None
                        else
                            Some name
                    | None -> None
                with _ ->
                    None
            Attributes =
                entity.Attributes
                |> Seq.choose (fun attribute ->
                    try
                        let name = attribute.AttributeType.DisplayName

                        if name.StartsWith "CompilationMapping" || name.StartsWith "Debugger" then
                            None
                        else
                            Some(name.Replace("Attribute", ""))
                    with _ ->
                        None
                )
                |> Seq.distinct
                |> List.ofSeq
            Nested =
                entity.NestedEntities
                |> Seq.filter isVisible
                |> Seq.map (readEntity docs slug)
                |> List.ofSeq
            Doc = docOf docs entity.XmlDocSig (obsoleteOf entity.Attributes)
            Slug = slug
            // Filled in once the assembly it came from is known.
            Assembly = ""
        }

    /// <summary>
    /// Extension members belong to the type they extend.
    /// </summary>
    let private attachExtensions (entities: FSharpApiEntity list) =
        let extended =
            entities
            |> List.collect (fun entity ->
                entity.Members
                |> List.filter (fun item -> item.Kind = FSharpApiMemberKind.Extension)
                |> List.map (fun item -> item.Extends |> Option.defaultValue "", item)
            )
            |> List.filter (fun (target, _) -> target <> "")
            |> List.groupBy fst
            |> List.map (fun (target, items) -> target, items |> List.map snd)
            |> Map.ofList

        let published = entities |> List.map _.Name |> Set.ofList

        entities
        |> List.map (fun entity ->
            let gained = Map.tryFind entity.Name extended |> Option.defaultValue []

            let kept =
                entity.Members
                |> List.filter (fun item ->
                    match item.Kind, item.Extends with
                    | FSharpApiMemberKind.Extension, Some target ->
                        not (published.Contains target)
                    | _ -> true
                )

            { entity with
                Members = kept @ gained
            }
        )
        |> List.filter (fun entity ->
            entity.Members.IsEmpty |> not
            || (entities
                |> List.tryFind (fun before -> before.Name = entity.Name)
                |> Option.map (fun before -> before.Members.IsEmpty)
                |> Option.defaultValue true)
        )

    let rec private mergeCompanions (entities: FSharpApiEntity list) =
        entities
        |> List.groupBy _.Name
        |> List.map (fun (_, group) ->
            let companions, declarations =
                group |> List.partition (fun entity -> entity.Kind = FSharpApiEntityKind.Module)

            let merged =
                match companions, declarations with
                | [ companion ], [ declaration ] ->
                    { declaration with
                        Members = declaration.Members @ companion.Members
                        Nested = declaration.Nested @ companion.Nested
                        Doc =
                            if declaration.Doc.Summary.IsSome then
                                declaration.Doc
                            else
                                companion.Doc
                    }
                | _ -> List.head group

            { merged with
                Nested = mergeCompanions merged.Nested
            }
        )
        |> List.sortBy _.Name

    /// <summary>Whether a file is a managed assembly, and so worth handing to the compiler.</summary>
    /// <remarks>
    /// The shared framework keeps coreclr.dll beside System.Runtime.dll, and one native library
    /// offered as a reference is not skipped - it fails the whole project.
    /// </remarks>
    let private isManagedAssembly (path: string) =
        try
            use file = File.OpenRead path
            use pe = new PEReader(file)
            pe.HasMetadata && pe.GetMetadataReader().IsAssembly
        with _ ->
            false

    /// <summary>
    /// Every public declaration of an assembly, grouped by namespace.
    /// </summary>
    /// <remarks>
    /// The assembly's own dependencies have to be reachable or the compiler cannot say what a
    /// signature means. Three places are looked in without being asked: beside the assembly, the
    /// running runtime, and the directory this process runs from - which for a documentation site
    /// referencing the library it documents is where its dependencies already are.
    /// </remarks>
    let readAllWith
        (searchPaths: string list)
        (assemblyPaths: AbsolutePath list)
        : Result<FSharpApiAssembly, string> list
        =
        let paths = assemblyPaths |> List.map AbsolutePath.value
        let present = paths |> List.filter File.Exists

        if List.isEmpty present then
            paths |> List.map (fun path -> Error $"No assembly at %s{path}")
        else

            let checker = FSharpChecker.Create()

            let references =
                [
                    yield! present |> List.map Path.GetDirectoryName
                    Path.GetDirectoryName typeof<obj>.Assembly.Location
                    AppContext.BaseDirectory
                    yield! searchPaths
                ]
                |> List.filter Directory.Exists
                |> List.distinct
                |> List.collect (fun directory ->
                    Directory.GetFiles(directory, "*.dll") |> List.ofArray
                )
                // Before the de-duplication, so a native library cannot shadow a managed one.
                |> List.filter isManagedAssembly
                |> List.sortBy (fun dll ->
                    if List.contains dll present then
                        0
                    else
                        1
                )
                |> List.distinctBy Path.GetFileName
                |> List.map (fun dll -> "-r:" + dll)

            let source =
                Path.Combine(
                    Path.GetTempPath(),
                    "nacara-api-" + Guid.NewGuid().ToString "N" + ".fs"
                )

            File.WriteAllText(source, "module Nacara.Api.Probe\n")

            try
                let options =
                    checker.GetProjectOptionsFromCommandLineArgs(
                        Path.ChangeExtension(source, ".fsproj"),
                        [|
                            "--simpleresolution"
                            "--noframework"
                            "--targetprofile:netcore"
                            yield! references
                            source
                        |]
                    )

                let results = checker.ParseAndCheckProject options |> Async.RunSynchronously

                // Reading a project the compiler gave up on throws, without saying what went wrong.
                let loaded = lazy results.ProjectContext.GetReferencedAssemblies()

                let readOne (path: string) =
                    let docs = XmlDocs.read path
                    let name = Path.GetFileNameWithoutExtension path
                    let name' = name

                    match
                        loaded.Value |> List.tryFind (fun assembly -> assembly.SimpleName = name)
                    with
                    | None -> Error $"The compiler could not load %s{name}"
                    | Some assembly ->
                        let namespaces =
                            assembly.Contents.Entities
                            |> Seq.filter isVisible
                            |> Seq.collect (fun entity ->
                                // An assembly's root holds namespaces and declarations in none; both arrive as entities.
                                if entity.IsNamespace then
                                    entity.NestedEntities
                                    |> Seq.filter isVisible
                                    |> Seq.map (fun child -> entity.DisplayName, child)
                                else
                                    Seq.singleton (
                                        entity.Namespace |> Option.defaultValue "",
                                        entity
                                    )
                            )
                            |> Seq.groupBy fst
                            |> Seq.map (fun (name, entities) ->
                                let slug =
                                    let ns =
                                        if name = "" then
                                            "global"
                                        else
                                            Slug.create name

                                    $"%s{Slug.create (Path.GetFileNameWithoutExtension path)}/%s{ns}"

                                {
                                    Name =
                                        if name = "" then
                                            "Global"
                                        else
                                            name
                                    Slug = slug
                                    Assembly = name'
                                    Entities =
                                        entities
                                        |> Seq.choose (fun (_, entity) ->
                                            try
                                                Some(readEntity docs slug entity)
                                            with _ ->
                                                None
                                        )
                                        |> Seq.sortBy _.Name
                                        |> List.ofSeq
                                        |> mergeCompanions
                                        |> attachExtensions
                                }
                            )
                            |> Seq.sortBy _.Name
                            |> List.ofSeq

                        let rec stamp (entity: FSharpApiEntity) =
                            { entity with
                                Assembly = name
                                Nested = entity.Nested |> List.map stamp
                            }

                        Ok
                            {
                                Name = name
                                Namespaces =
                                    namespaces
                                    |> List.map (fun ns ->
                                        { ns with
                                            Entities = ns.Entities |> List.map stamp
                                        }
                                    )
                            }

                if results.HasCriticalErrors then
                    let reported = results.Diagnostics |> Array.map _.Message |> String.concat "; "

                    present
                    |> List.map (fun path ->
                        Error $"The compiler rejected the references for %s{path}: %s{reported}"
                    )
                else
                    present |> List.map readOne
            finally
                try
                    File.Delete source
                with _ ->
                    ()

    /// <summary>Every public declaration of one assembly, grouped by namespace.</summary>
    let readWith (searchPaths: string list) (assemblyPath: AbsolutePath) =
        match readAllWith searchPaths [ assemblyPath ] with
        | [ result ] -> result
        | _ -> Error $"No assembly at %s{AbsolutePath.value assemblyPath}"

    /// <summary>Every public declaration of an assembly, grouped by namespace.</summary>
    let read (assemblyPath: AbsolutePath) = readWith [] assemblyPath
