module Nacara.Commands.Clean

open Nacara
open Nacara.Core
open Nacara.Evaluator
open System.IO

let execute () =

    let context = Shared.createContext()
    use fsi = EvaluatorHelpers.fsi context

    Shared.loadConfigOrExit fsi context

    Directory.Delete(AbsolutePath.value context.OutputPath, true)

    0
