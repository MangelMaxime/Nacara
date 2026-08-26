/// <summary>Declarations that are easy to get wrong, one of each kind.</summary>
namespace Fixture.Library.Shapes

open System

/// <summary>A record kept on the stack.</summary>
[<Struct>]
type Size =
    {
        Width: float
        Height: float
    }

/// <summary>A type whose name cannot be its slug.</summary>
type ``Type With Spaces`` =
    {
        /// <summary>A field whose name also has spaces.</summary>
        ``Field With Spaces``: string
    }

/// <summary>How far, in metres.</summary>
[<Measure>]
type m

/// <summary>Something went wrong, and it says where.</summary>
exception Refused of message: string * position: int

/// <summary>Two of a kind, into one.</summary>
type Combine<'T> = delegate of 'T * 'T -> 'T

/// <summary>What anything that can be named answers.</summary>
type INamed =
    /// <summary>What it is called.</summary>
    abstract member Name: string
    /// <summary>How it describes itself.</summary>
    abstract member Describe: unit -> string

/// <summary>What every shape starts from.</summary>
[<AbstractClass>]
type Shape(id: int) =

    /// <summary>What tells it apart.</summary>
    member _.Id: int = failwith "documentation only"

    /// <summary>Left to whoever inherits it.</summary>
    abstract member Describe: unit -> string

/// <summary>
/// Inherits <see cref="T:Fixture.Library.Shapes.Shape"/> and implements
/// <see cref="T:Fixture.Library.Shapes.INamed"/>.
/// </summary>
type Circle(id: int, radius: float) =
    inherit Shape(id)

    /// <summary>How wide it is from the middle.</summary>
    member _.Radius: float = failwith "documentation only"

    override _.Describe() : string = failwith "documentation only"

    interface INamed with
        member _.Name: string = failwith "documentation only"
        member _.Describe() : string = failwith "documentation only"

/// <summary>One of every kind of member.</summary>
type Counter(label: string) =

    let mutable count = 0
    let changed = Event<int>()

    /// <summary>Started without a label.</summary>
    new() = Counter("default")

    /// <summary>What it is called.</summary>
    member _.Label: string = failwith "documentation only"

    /// <summary>How far it has counted, which the caller may set.</summary>
    member _.Count
        with get (): int = count
        and set (value: int) = count <- value

    /// <summary>Written out plainly.</summary>
    member _.Format() : string = failwith "documentation only"

    /// <summary>Written out to a width, which shares a name with the one above.</summary>
    /// <param name="width">How wide to pad it.</param>
    member _.Format(width: int) : string = failwith "documentation only"

    /// <summary>Raised whenever it counts.</summary>
    [<CLIEvent>]
    member _.Changed = changed.Publish

    /// <summary>Counts one more.</summary>
    member _.Increment() : unit = failwith "documentation only"

    /// <summary>A name that has to be quoted.</summary>
    member _.``Reset Everything``() : unit = failwith "documentation only"

    /// <summary>The one everybody shares.</summary>
    static member Shared: Counter = failwith "documentation only"

    /// <summary>Two counters, added.</summary>
    static member (+)(left: Counter, right: Counter) : int = failwith "documentation only"

/// <summary>Asked for by position, and asked to be fast.</summary>
type Values(values: int list) =

    /// <summary>What is at that position.</summary>
    /// <param name="index">Counting from zero.</param>
    member _.Item
        with get (index: int): int = failwith "documentation only"

    /// <summary>The larger of two, without a call.</summary>
    member inline _.Larger(first: 'T, second: 'T) : 'T = failwith "documentation only"

/// <summary>Matches a | b, pipes and all.</summary>
type Piped =
    {
        Value: string
    }

/// <summary>Values whose value is part of what they mean.</summary>
module Constants =

    /// <summary>How many times to try again.</summary>
    [<Literal>]
    let Retries = 3

    /// <summary>What to call something with no name.</summary>
    [<Literal>]
    let Unnamed = "unnamed"

    /// <summary>How far it went.</summary>
    let travelled (value: float) : float<m> = failwith "documentation only"
