module Utils

open Expecto

module Expect =

    let inline equal actual expected =
        Expect.equal actual expected ""
