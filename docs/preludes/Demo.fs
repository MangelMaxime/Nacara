module Demo

/// A tiny stand-in for a library a site documents. Anything defined here is in scope in every
/// snippet that asks for this preset, and the type-checker answers for it: hover `distance` in a
/// snippet and it reports this signature.
type Point =
    {
        X: float
        Y: float
    }

let distance (a: Point) (b: Point) =
    sqrt ((a.X - b.X) ** 2.0 + (a.Y - b.Y) ** 2.0)

let fizzbuzz n =
    match n % 3, n % 5 with
    | 0, 0 -> "FizzBuzz"
    | 0, _ -> "Fizz"
    | _, 0 -> "Buzz"
    | _ -> string n
