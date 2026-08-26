namespace Nacara.Plugins.Internal

open System
open FSharp.Compiler.Symbols

/// <summary>
/// Declarations, written the way they were written.
/// </summary>
[<RequireQualifiedAccess>]
module Signature =

    let private display = FSharpDisplayContext.Empty.WithShortTypeNames true

    let private typeOf (t: FSharpType) =
        try
            t.Format display
        with _ ->
            "_"

    /// <summary>
    /// What a generic parameter is required to be.
    /// </summary>
    let private constraintsOf (parameter: FSharpGenericParameter) =
        let name = "'" + parameter.DisplayName

        parameter.Constraints
        |> Seq.choose (fun c ->
            try
                if c.IsComparisonConstraint then
                    Some $"%s{name}: comparison"
                elif c.IsEqualityConstraint then
                    Some $"%s{name}: equality"
                elif c.IsSupportsNullConstraint then
                    Some $"%s{name}: null"
                elif c.IsReferenceTypeConstraint then
                    Some $"%s{name}: not struct"
                elif c.IsNonNullableValueTypeConstraint then
                    Some $"%s{name}: struct"
                elif c.IsRequiresDefaultConstructorConstraint then
                    Some $"%s{name}: (new: unit -> %s{name})"
                elif c.IsCoercesToConstraint then
                    Some $"%s{name} :> %s{c.CoercesToTarget.Format display}"
                elif c.IsEnumConstraint then
                    Some $"%s{name}: enum"
                elif c.IsDelegateConstraint then
                    Some $"%s{name}: delegate"
                elif c.IsUnmanagedConstraint then
                    Some $"%s{name}: unmanaged"
                elif c.IsMemberConstraint then
                    Some $"%s{name}: (member …)"
                else
                    None
            with _ ->
                None
        )
        |> List.ofSeq

    let private generics (parameters: seq<FSharpGenericParameter>) =
        let parameters = parameters |> List.ofSeq
        let names = parameters |> List.map (fun p -> "'" + p.DisplayName)

        match names with
        | [] -> ""
        | names ->
            let constraints = parameters |> List.collect constraintsOf

            let suffix =
                match constraints with
                | [] -> ""
                | constraints -> " when " + String.Join(" and ", constraints)

            "<" + String.Join(", ", names) + suffix + ">"

    let private parameterGroups (value: FSharpMemberOrFunctionOrValue) =
        value.CurriedParameterGroups
        |> Seq.map (fun group ->
            let parameters =
                group
                |> Seq.map (fun parameter ->
                    let optional = parameter.IsOptionalArg

                    let parameterType =
                        let written = typeOf parameter.Type

                        // The caller writes `?limit: int`, not `limit: int option`.
                        if optional && written.EndsWith " option" then
                            written.Substring(0, written.Length - " option".Length)
                        else
                            written

                    let prefix =
                        if optional then
                            "?"
                        else
                            ""

                    match parameter.Name with
                    | Some name when name <> "" -> $"%s{prefix}%s{name}: %s{parameterType}"
                    | _ -> parameterType
                )
                |> List.ofSeq

            match parameters with
            | [] -> "()"
            | [ single ] when single = "unit" -> "()"
            | [ single ] -> $"(%s{single})"
            | many -> "(" + String.Join(", ", many) + ")"
        )
        |> List.ofSeq

    /// <summary>A name F# would need backticks to accept keeps them.</summary>
    let private written (name: string) =
        let usable =
            name.Length > 0
            && (Char.IsLetter name[0] || name[0] = '_')
            && name
               |> Seq.forall (fun character -> Char.IsLetterOrDigit character || character = '_')

        if usable then
            name
        else
            $"``%s{name}``"

    /// <summary>A union case, as it is written in the type.</summary>
    let ofUnionCase (case: FSharpUnionCase) =
        let fields =
            case.Fields
            |> Seq.map (fun field ->
                let fieldType = typeOf field.FieldType

                // The compiler names a positional field Item, Item1, Item2 and so on.
                if field.Name.StartsWith "Item" || field.Name = "" then
                    fieldType
                else
                    $"%s{field.Name}: %s{fieldType}"
            )
            |> List.ofSeq

        match fields with
        | [] -> case.DisplayName
        | fields -> case.DisplayName + " of " + String.Join(" * ", fields)

    /// <summary>A record field.</summary>
    let ofField (field: FSharpField) =
        $"%s{field.DisplayName}: %s{typeOf field.FieldType}"

    /// <summary>The declaration of a type or module, without its contents.</summary>
    let ofEntity (entity: FSharpEntity) =
        let name = written entity.DisplayName + generics entity.GenericParameters

        let carries (attribute: string) =
            entity.Attributes
            |> Seq.exists (fun candidate ->
                try
                    candidate.AttributeType.DisplayName.StartsWith attribute
                with _ ->
                    false
            )

        let attribute =
            if entity.IsValueType && (entity.IsFSharpRecord || entity.IsFSharpUnion) then
                "[<Struct>]\n"
            else
                ""

        let marker =
            if entity.IsInterface then
                "[<Interface>]\n"
            elif carries "AbstractClass" then
                "[<AbstractClass>]\n"
            elif entity.IsValueType then
                "[<Struct>]\n"
            else
                "[<Class>]\n"

        let indented (lines: string list) =
            lines |> List.map (fun line -> "    " + line) |> String.concat "\n"

        if entity.IsMeasure then
            $"[<Measure>] type %s{name}"
        elif entity.IsFSharpModule then
            $"module %s{name}"
        elif entity.IsFSharpExceptionDeclaration then
            let fields =
                entity.FSharpFields
                |> Seq.map (fun field ->
                    if field.Name.StartsWith "Data" || field.Name = "" then
                        typeOf field.FieldType
                    else
                        $"%s{field.Name}: %s{typeOf field.FieldType}"
                )
                |> List.ofSeq

            match fields with
            | [] -> $"exception %s{name}"
            | fields -> $"exception %s{name} of " + String.Join(" * ", fields)
        elif entity.IsFSharpAbbreviation then
            $"type %s{name} = %s{typeOf entity.AbbreviatedType}"
        elif entity.IsFSharpRecord then
            let fields = entity.FSharpFields |> Seq.map ofField |> List.ofSeq

            $"%s{attribute}type %s{name} =\n"
            + indented ([ "{" ] @ (fields |> List.map (fun field -> "    " + field)) @ [ "}" ])
        elif entity.IsFSharpUnion then
            let cases =
                entity.UnionCases |> Seq.map (fun case -> "| " + ofUnionCase case) |> List.ofSeq

            $"%s{attribute}type %s{name} =\n" + indented cases
        elif entity.IsEnum then
            let cases =
                entity.FSharpFields
                |> Seq.filter _.IsLiteral
                |> Seq.map (fun field ->
                    match field.LiteralValue with
                    | Some value -> $"| %s{field.DisplayName} = %A{value}"
                    | None -> $"| %s{field.DisplayName}"
                )
                |> List.ofSeq

            $"type %s{name} =\n" + indented cases
        elif entity.IsDelegate then
            let signature =
                try
                    let arguments =
                        entity.FSharpDelegateSignature.DelegateArguments
                        |> Seq.map (fun (_, argumentType) -> typeOf argumentType)
                        |> List.ofSeq

                    let taken =
                        match arguments with
                        | [] -> "unit"
                        | arguments -> String.Join(" * ", arguments)

                    let returned = typeOf entity.FSharpDelegateSignature.DelegateReturnType

                    $" = delegate of %s{taken} -> %s{returned}"
                with _ ->
                    ""

            $"type %s{name}%s{signature}"
        else
            let writesMembers =
                try
                    entity.MembersFunctionsAndValues
                    |> Seq.exists (fun value ->
                        try
                            value.Accessibility.IsPublic
                        with _ ->
                            false
                    )
                with _ ->
                    false

            let primary =
                try
                    entity.MembersFunctionsAndValues
                    |> Seq.filter (fun value -> value.IsConstructor && value.Accessibility.IsPublic)
                    |> Seq.tryFind _.IsImplicitConstructor
                    |> Option.map (fun value -> String.Join(" ", parameterGroups value))
                    |> Option.filter (fun written -> written <> "" && written <> "()")
                    |> Option.map (fun written -> " " + written)
                    |> Option.defaultValue ""
                    |> fun written ->
                        if writesMembers then
                            ""
                        else
                            written
                with _ ->
                    ""

            $"%s{marker}type %s{name}%s{primary}"

    let private returnType (value: FSharpMemberOrFunctionOrValue) =
        try
            typeOf value.ReturnParameter.Type
        with _ ->
            "_"

    /// <summary>
    /// A function, value or member, as its author declared it.
    /// </summary>
    let ofMember (value: FSharpMemberOrFunctionOrValue) =
        let name =
            if value.IsConstructor then
                ""
            else
                value.DisplayName + generics value.GenericParameters

        let inlined =
            match value.InlineAnnotation with
            | FSharpInlineAnnotation.AlwaysInline
            | FSharpInlineAnnotation.AggressiveInline -> "inline "
            | _ -> ""

        let prefix =
            if not value.IsMember then
                ""
            elif value.IsConstructor then
                "new "
            elif value.IsDispatchSlot then
                "abstract member " + inlined
            elif value.IsInstanceMember then
                "member " + inlined
            else
                "static member " + inlined

        if value.IsProperty then
            let accessors =
                match value.HasGetterMethod, value.HasSetterMethod with
                | true, true -> " with get, set"
                | false, true -> " with set"
                | _ -> ""

            let index =
                if value.CurriedParameterGroups |> Seq.collect id |> Seq.isEmpty then
                    ""
                else
                    " " + String.Join(" ", parameterGroups value)

            let separator =
                if index = "" then
                    ": "
                else
                    " : "

            $"%s{prefix}%s{name}%s{index}%s{separator}%s{returnType value}%s{accessors}"
        elif value.LiteralValue.IsSome then
            let written =
                match value.LiteralValue.Value with
                | :? string as text -> $"\"%s{text}\""
                | other -> string other

            $"[<Literal>] %s{name}: %s{returnType value} = %s{written}"
        elif value.IsActivePattern then
            $"(%s{value.DisplayName}) %s{String.Join(' ', parameterGroups value)} : %s{returnType value}"
        else

            match parameterGroups value with
            | [] -> $"%s{prefix}%s{name}: %s{returnType value}"
            | groups ->
                let written = String.Join(" ", groups)

                if name = "" then
                    $"%s{prefix}%s{written} : %s{returnType value}"
                else
                    $"%s{prefix}%s{name} %s{written} : %s{returnType value}"

    /// <summary>
    /// The same declaration, as one line for an index.
    /// </summary>
    let short (value: FSharpMemberOrFunctionOrValue) =
        try
            if value.IsProperty then
                typeOf value.ReturnParameter.Type
            else
                let arguments =
                    value.CurriedParameterGroups
                    |> Seq.map (fun group ->
                        match group |> Seq.map (fun p -> typeOf p.Type) |> List.ofSeq with
                        | [] -> "unit"
                        | [ single ] -> single
                        | many -> String.Join(" * ", many)
                    )
                    |> List.ofSeq

                match arguments with
                | [] -> typeOf value.ReturnParameter.Type
                | arguments -> String.Join(" -> ", arguments) + " -> " + returnType value
        with _ ->
            ""
