namespace Nacara.Evaluator

open FsToolkit.ErrorHandling
open Nacara.Core
open System.Reflection

[<RequireQualifiedAccess>]
module LoaderEvaluator =

    let private checkSignature (func: MethodInfo) =
        let parameters = func.GetParameters()

        result {
            do! EvaluatorHelpers.checkParameterType 1 parameters[0] Typeof.context

            if func.ReturnType = Typeof.unit then
                return! Ok()
            else
                return! Error $"Loader function should return '{Typeof.unit.Name}'"
        }

    let tryEvaluate (loaderPath: AbsolutePath.AbsolutePath) (context: Context) =
        use fsi = EvaluatorHelpers.fsi context

        result {
            do! EvaluatorHelpers.tryLoad fsi loaderPath
            do! EvaluatorHelpers.tryOpen fsi loaderPath

            let! loaderFunc =
                EvaluatorHelpers.tryEvaluateCode fsi loaderPath "<@@ fun a -> loader a @@>"

            let loaderExpr = EvaluatorHelpers.compileExpression loaderFunc
            let loaderType = loaderExpr.GetType()

            let loaderInvokeFunc =
                loaderType.GetMethods()
                |> Array.filter (fun method ->
                    method.Name = "Invoke" && method.GetParameters().Length = 1
                )
                |> Array.head

            // Valid the signature of the loader function
            do! checkSignature loaderInvokeFunc

            // Execute the loader
            loaderInvokeFunc.Invoke(
                loaderExpr,
                [|
                    box context
                |]
            )
            |> ignore

            return! Ok()
        }
        |> Result.mapError (fun error ->
            [
                $"Invalid loader: %s{AbsolutePath.value loaderPath}"
                $"Error: {error}"
                """
Example:

open FSharp.Static.Core

let loader (projectRoot: ProjectRoot.ProjectRoot) (context: Context) =
    // Your code goes here
    ()"""
            ]
            |> String.concat "\n"
        )
