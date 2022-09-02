module Nacara.Commands.Version

open System.Reflection

let execute () =
    let assembly =
        Assembly.GetExecutingAssembly()

    let version =
        assembly.GetCustomAttributes<AssemblyVersionAttribute>()
        |> Seq.tryHead
        |> Option.map (fun attribute -> attribute.Version)
        |> Option.defaultValue "development"

    printfn $"%s{version}"

    0

