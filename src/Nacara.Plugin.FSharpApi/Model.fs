namespace Nacara.Plugins

/// <summary>What a declaration is, which decides where it is listed and how it is drawn.</summary>
[<RequireQualifiedAccess>]
type FSharpApiEntityKind =
    | Module
    | Record
    | Union
    | Class
    | Interface
    | Struct
    | Enum
    | Abbreviation
    | Exception
    | Delegate
    | Measure

    member this.Label =
        match this with
        | Module -> "Module"
        | Record -> "Record"
        | Union -> "Union"
        | Class -> "Class"
        | Interface -> "Interface"
        | Struct -> "Struct"
        | Enum -> "Enum"
        | Abbreviation -> "Type abbreviation"
        | Exception -> "Exception"
        | Delegate -> "Delegate"
        | Measure -> "Unit of measure"

    /// <summary>Heading of the group these are listed under.</summary>
    member this.Plural =
        match this with
        | Class -> "Classes"
        | Measure -> "Units of measure"
        | _ -> this.Label + "s"

    /// <summary>Where the group goes: what a reader calls first, what it is made of after.</summary>
    member this.Order =
        match this with
        | Module -> 0
        | Record -> 1
        | Union -> 2
        | Class -> 3
        | Interface -> 4
        | Struct -> 5
        | Enum -> 6
        | Abbreviation -> 7
        | Exception -> 8
        | Delegate -> 9
        | Measure -> 10

/// <summary>What a member is, which decides the group it is listed under.</summary>
[<RequireQualifiedAccess>]
type FSharpApiMemberKind =
    /// A let-bound function or value of a module.
    | Value
    | Constructor
    | Property
    | Method
    | UnionCase
    | RecordField
    | EnumCase
    | ActivePattern
    | Event
    /// A member another type gained from this library.
    | Extension

    member this.Label =
        match this with
        | Value -> "Functions and values"
        | Constructor -> "Constructors"
        | Property -> "Properties"
        | Method -> "Methods"
        | UnionCase -> "Cases"
        | RecordField -> "Fields"
        | EnumCase -> "Cases"
        | ActivePattern -> "Active patterns"
        | Event -> "Events"
        | Extension -> "Extension members"

/// <summary>A parameter of a function or member, as the reader needs to see it.</summary>
type FSharpApiParameter =
    {
        Name: string option
        Type: string
        Summary: string option
    }

/// <summary>Everything the XML documentation said about one declaration.</summary>
type FSharpApiDoc =
    {
        Summary: string option
        Remarks: string option
        /// Examples, in the order they were written.
        Examples: string list
        Parameters: (string * string) list
        /// Type parameters the author documented, by name.
        TypeParameters: (string * string) list
        Returns: string option
        /// What it raises, and when.
        Exceptions: (string * string) list
        /// <c>[&lt;Obsolete&gt;]</c>, and what it said.
        Obsolete: string option
        SeeAlso: string list
    }

    static member Empty =
        {
            Summary = None
            Remarks = None
            Examples = []
            Parameters = []
            TypeParameters = []
            Returns = None
            Exceptions = []
            Obsolete = None
            SeeAlso = []
        }

/// <summary>One member of a type or module.</summary>
type FSharpApiMember =
    {
        Name: string
        Kind: FSharpApiMemberKind
        /// The declaration as a reader would write it.
        Signature: string
        /// A one-line form for the index at the top of a page.
        ShortSignature: string
        Parameters: FSharpApiParameter list
        ReturnType: string option
        IsStatic: bool
        /// The type this extends, when the library added it to one it does not own.
        Extends: string option
        Doc: FSharpApiDoc
        Anchor: string
    }

/// <summary>A type, a module, or anything else a page is written about.</summary>
type FSharpApiEntity =
    {
        Name: string
        /// Namespace and name, as the compiler knows it.
        FullName: string
        Namespace: string
        Kind: FSharpApiEntityKind
        /// The declaration line: <c>type Route = { … }</c>, <c>module Url</c>.
        Signature: string
        TypeParameters: string list
        Members: FSharpApiMember list
        /// Interfaces this type implements, as a reader would write them.
        Interfaces: string list
        /// What it inherits from, when that is worth saying.
        BaseType: string option
        /// Attributes it carries, without the <c>Attribute</c> suffix.
        Attributes: string list
        /// Modules and types declared inside this one.
        Nested: FSharpApiEntity list
        Doc: FSharpApiDoc
        /// Path of the page written for it, relative to the api root.
        Slug: string
        /// The assembly it shipped in, which is the package a reader has to reference.
        Assembly: string
    }

/// <summary>The declarations of one namespace, which is one page.</summary>
type FSharpApiNamespace =
    {
        Name: string
        Entities: FSharpApiEntity list
        Slug: string
        /// The package that declares this part of the namespace.
        Assembly: string
    }

/// <summary>Everything read from one assembly.</summary>
type FSharpApiAssembly =
    {
        Name: string
        Namespaces: FSharpApiNamespace list
    }

/// <summary>One declaration, as a menu entry: what it is called and which page it is.</summary>
type FSharpApiOutlineEntry =
    {
        Name: string
        /// Path of the page, relative to the collection's content root.
        Page: string
        /// What is declared inside it, which is where a menu nests them.
        Children: FSharpApiOutlineEntry list
    }

/// <summary>What one namespace offers a menu.</summary>
type FSharpApiOutlinePackage =
    {
        /// The package's name, as a reader knows it: <c>Nacara.Plugin.Markdown</c>.
        Name: string
        /// Its own page, which a menu can hang the group on.
        Page: string
        /// What it declares, one entry per namespace.
        Namespaces: FSharpApiOutlineNamespace list
    }

/// <summary>What one package declares in one namespace, as a menu entry.</summary>
and FSharpApiOutlineNamespace =
    {
        Name: string
        /// The package that declares this part of it, and what a menu groups by.
        Assembly: string
        /// The namespace's own page, which a menu can hang the group on.
        Page: string
        /// What it declares at the top level; what those declare is under them.
        Entries: FSharpApiOutlineEntry list
    }
