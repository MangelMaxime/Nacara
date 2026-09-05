namespace Fixture.Library

open System
open System.Runtime.CompilerServices

/// <summary>How much of something there is.</summary>
type Quantity = int

/// <summary>
/// A person, as this library thinks of one.
/// </summary>
/// <remarks>
/// Records arrive with their fields, and the fields carry what was written about them.
/// </remarks>
type Person =
    {
        /// <summary>What they answer to.</summary>
        Name: string
        /// <summary>Years since they were born.</summary>
        Age: int
    }

    /// <summary>A greeting, addressed to them.</summary>
    member this.Greeting: string = failwith "documentation only"

    /// <summary>Someone of that name, of unknown age.</summary>
    static member Named(name: string) : Person = failwith "documentation only"

/// <summary>Where something came from.</summary>
type Source =
    /// <summary>Read from a file at that path.</summary>
    | FromFile of path: string
    /// <summary>Made up on the spot.</summary>
    | Generated
    /// <summary>Nothing said where.</summary>
    | Unknown of reason: string * code: int

/// <summary>Anything that can be greeted.</summary>
type IGreetable =
    /// <summary>The name to greet.</summary>
    abstract Name: string
    /// <summary>Greet them in the given language.</summary>
    abstract Greet: language: string -> string

/// <summary>Things to do with people.</summary>
[<RequireQualifiedAccess>]
module People =

    /// <summary>
    /// Greet someone.
    /// </summary>
    /// <param name="greeting">The word to greet them with.</param>
    /// <param name="person">Who to greet.</param>
    /// <returns>The whole sentence.</returns>
    /// <example>
    /// <code lang="fsharp">
    /// People.greet "Hello" { Name = "Ada"; Age = 36 }
    /// </code>
    /// </example>
    let greet (greeting: string) (person: Person) : string = failwith "documentation only"

    /// <summary>Everyone old enough to vote.</summary>
    let adults (people: Person list) : Person list = failwith "documentation only"

    /// <summary>The old way of greeting.</summary>
    [<Obsolete("Use greet instead")>]
    let sayHello (person: Person) : string = failwith "documentation only"

    /// <summary>Tuples arrive as tuples, not as two arguments.</summary>
    let between (first: Person, second: Person) : string = failwith "documentation only"

    /// <summary>Match a person by whether they can vote.</summary>
    let (|Adult|Child|) (person: Person) : Choice<unit, unit> = failwith "documentation only"

/// <summary>A thing with a companion module, as F# libraries are written.</summary>
type Widget =
    {
        /// <summary>What it is called.</summary>
        Label: string
    }

/// <summary>Things to do with a widget.</summary>
[<RequireQualifiedAccess>]
module Widget =

    /// <summary>A widget with that label.</summary>
    let create (label: string) : Widget = failwith "documentation only"

    /// <summary>The same widget, labelled differently.</summary>
    let relabel (label: string) (widget: Widget) : Widget = failwith "documentation only"

/// <summary>A container whose nested type has a companion module, as a DSL is written.</summary>
[<RequireQualifiedAccess>]
module Codec =

    /// <summary>A pair, kept nested with its operations.</summary>
    type Pair =
        {
            /// <summary>The left part.</summary>
            Left: string
        }

    /// <summary>Things to do with a pair.</summary>
    [<RequireQualifiedAccess>]
    module Pair =

        /// <summary>A pair with that left part.</summary>
        let create (left: string) : Pair = failwith "documentation only"

    /// <summary>Two of a kind, as an abbreviation with a companion module.</summary>
    type Twin = Pair * Pair

    /// <summary>Things to do with twins.</summary>
    [<RequireQualifiedAccess>]
    module Twin =

        /// <summary>The same pair, twice.</summary>
        let double (pair: Pair) : Twin = failwith "documentation only"

/// <summary>Extensions this library adds to types it does not own.</summary>
[<AutoOpen>]
module Extensions =

    type System.String with

        /// <summary>Whether this is a name this library would accept.</summary>
        member this.IsUsableName: bool = failwith "documentation only"

/// <summary>The same thing, written the way C# callers need it.</summary>
[<Extension>]
type StringExtensions =

    /// <summary>Whether that name is one this library would accept.</summary>
    [<Extension>]
    static member IsAccepted(value: string) : bool = failwith "documentation only"

/// <summary>Extensions on a type this library owns.</summary>
[<Extension>]
type PersonExtensions =

    /// <summary>The initials of that person, uppercased.</summary>
    [<Extension>]
    static member Initials(person: Person) : string = failwith "documentation only"

/// <summary>Generic work, with the constraints spelled out.</summary>
module Constrained =

    /// <summary>The larger of two, whatever they are. Compares like <see cref="T:Fixture.Library.Person"/> does.</summary>
    /// <typeparam name="T">What is being compared.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">When either of them is null.</exception>
    let larger<'T when 'T: comparison> (first: 'T) (second: 'T) : 'T = failwith "documentation only"

/// <summary>Shapes that are easy to get wrong when writing a signature down.</summary>
type Awkward =

    /// <summary>Called with as much or as little as the caller has.</summary>
    static member Find(name: string, ?limit: int) : string list = failwith "documentation only"

    /// <summary>Answers, and says whether it had to look.</summary>
    static member TryFind(name: string, found: byref<string>) : bool = failwith "documentation only"

    /// <summary>What it returns is itself a function.</summary>
    static member Formatter(prefix: string) : string -> string = failwith "documentation only"

    /// <summary>Two things at once.</summary>
    static member Split(text: string) : string * int = failwith "documentation only"

    /// <summary>Takes something with no name of its own.</summary>
    static member Describe
        (value:
            {|
                Name: string
                Age: int
            |})
        : string
        =
        failwith "documentation only"

/// <summary>How loudly to greet.</summary>
type Volume =
    /// <summary>Barely audible.</summary>
    | Quiet = 0
    /// <summary>The usual.</summary>
    | Normal = 1
    /// <summary>From across the room.</summary>
    | Loud = 2

/// <summary>Operators this library defines.</summary>
[<AutoOpen>]
module Operators =

    /// <summary>
    /// Join two people into a greeting.
    /// </summary>
    /// <seealso cref="M:Fixture.Library.People.greet"/>
    let (=>) (first: Person) (second: Person) : string = failwith "documentation only"
