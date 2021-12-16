// This module is the main module where syntax are being tested.
module Fixtures

let thisIsAVariable = 1

// Test that function with type parameters are correctly generated.
let add (a : int) (b : int) = a + b

// Test that function without type parameters are correctly generated.
let sub a b = a -b

// Test that function partial typed are correctly generated.
let mul (a : float) b = a * b

type SimpleRecord =
    {
        Firstname : string
        Age : int
    }
