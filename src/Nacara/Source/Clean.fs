module Clean

open Nacara.Core.Types
open Node

let clean (config : Config) =
    promise {
        do! Directory.rmdir config.DestinationFolder
    }
