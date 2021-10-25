/// Additional operations on Array
module Array

// Copied from F# plus
//https://github.com/fsprojects/FSharpPlus/blob/327cdfcff9d7a209bf934218a5067301ef44e35d/src/FSharpPlus/Extensions/Array.fs#L100-100

/// <summary>
/// Creates two arrays by applying the mapper function to each element in the array
/// and classifies the transformed values depending on whether they were wrapped with Choice1Of2 or Choice2Of2.
/// </summary>
/// <returns>
/// A tuple with both resulting arrays.
/// </returns>
let partitionMap (mapper: 'T -> Choice<'T1,'T2>) (source: array<'T>) =
    let (x, y) = ResizeArray (), ResizeArray ()
    Array.iter (mapper >> function Choice1Of2 e -> x.Add e | Choice2Of2 e -> y.Add e) source
    x.ToArray (), y.ToArray ()
