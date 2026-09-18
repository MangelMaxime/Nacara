namespace Fixture.Dependent

open Fixture.Dependency

/// <summary>Its signature names a type of another assembly.</summary>
type Reader =
    abstract member Read: unit -> Payload

/// <summary>Its signature names types of this assembly only.</summary>
type Plain =
    abstract member Size: float
