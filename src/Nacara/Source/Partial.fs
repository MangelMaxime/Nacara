module Partial

#nowarn "52"

open Nacara.Core.Types
open Fable.Core.JsInterop
open Node

let babel : obj =
    import "default" "@babel/core"

let loadFromJavaScript (config : Config) (partialPath : string) =
    promise {
        // Cache busting is useful for development, but not for production.
        let cacheBusting =
            System.DateTime.UtcNow.ToString("O")

        let fullPath =
            path.join(config.WorkingDirectory, config.SourceFolder, partialPath)

        let! m = importDynamic (fullPath + "?" + cacheBusting)

        let res : Partial =
            {
                Id = getPartialId partialPath
                Module = m
            }

        return res
    }

let loadPartialFromJsx (config : Config) (partialPath : string) =
    promise {
        let fullPath =
            path.join(config.WorkingDirectory, config.SourceFolder, partialPath)

        let! res = babel?transformFileAsync(fullPath)

        let destination =
            path.join(config.WorkingDirectory, ".nacara", partialPath)
            |> File.changeExtension "js"

        do! File.write destination res?code

        // Cache busting is useful for development, but not for production.
        let cacheBusting =
            System.DateTime.UtcNow.ToString("O")

        let bustedPath =
            destination + "?" + cacheBusting

        let! m =
            importDynamic bustedPath

        let res : Partial =
            {
                Id = getPartialId partialPath
                Module = m
            }

        return res
    }
