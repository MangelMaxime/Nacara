namespace Nacara.Evaluator

open FsToolkit.ErrorHandling
open Nacara.Core
open FSharp.Compiler.Interactive.Shell

[<RequireQualifiedAccess>]
module ConfigEvaluator =

    let tryEvaluate (fsi : FsiEvaluationSession) (context: Context) =
        result {
            do! EvaluatorHelpers.tryLoad fsi context.ConfigPath
            do! EvaluatorHelpers.tryOpen fsi context.ConfigPath

            let! configValue = EvaluatorHelpers.tryEvaluateCode fsi context.ConfigPath "config"

            if configValue.ReflectionType <> Typeof.config then
                return! Error """Invalid config type detected. Please make sure that your 'nacara.fsx' file contains a 'config' variable of type 'FSharp.Static.Core.Config'

Example:
#r "./src/FSharp.Static/bin/Debug/net6.0/FSharp.Static.Core.dll"

open FSharp.Static.Core

let config =
    {
        Directory =
            {
                Input = "docsrc"
                Output = "public"
                Generators = "generators"
                Loaders = "loaders"
            }
    }
                """

            else
                return! Ok (configValue.ReflectionValue :?> Config)
        }
