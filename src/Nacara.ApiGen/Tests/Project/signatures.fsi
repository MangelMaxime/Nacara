



namespace PureNamespace
    
    [<Class>]
    type Empty

namespace NamespaceWithModule
    
    module SubModule =
        
        val a: int


/// This module is the main module where syntax are being tested.
/// This is because, having everything in a single module make
/// it easier to find the type we want to test against
module Fixtures

val thisIsAVariable: int

/// Test that function with type parameters are correctly generated.
val add: a: int -> b: int -> int

/// Test that function without type parameters are correctly generated.
val sub: a: int -> b: int -> int

/// Test that function partially typed are correctly generated.
val mul: a: float -> b: float -> float

type SimpleRecord =
    {
      Firstname: string
      Age: int
    }


/// This is a global module
module GlobalModuleA

type User =
    { Name: string }


/// This is a second global module.
module GlobalModuleB

[<Struct>]
type Number = int


module ModuleWithFunctionsAndValues

/// <summary>
/// Simple addition
/// </summary>
/// <param name="a">Fisrt number</param>
/// <param name="b">Second number</param>
/// <returns>Sum of the <c>a+b</c></returns>
val add: a: int -> b: int -> int

/// The answer to the ultimate question of life, the universe, and everything.
val answer: int


module ModuleWithRecord

/// <summary>
/// Simple User type
/// </summary>
type User =
    {
      
      /// <summary>
      /// Unique Id of the user
      /// </summary>
      Id: int
      
      /// <summary>
      /// Name of the user
      /// </summary>
      Name: string
      
      /// <summary>
      /// Email of the user
      /// </summary>
      Email: string
    }
    
    /// <summary>
    /// Create a new user
    /// </summary>
    /// <param name="id">Unique Id of the user</param>
    /// <param name="name">Name of the user</param>
    /// <param name="email">Email of the user</param>
    /// <returns>
    /// Returns the newly created user
    /// </returns>
    static member Create: id: int * name: string * ?email: string -> User
    
    member GetterAndSetter: string
    
    /// <summary>
    /// This is a getter only property
    /// </summary>
    member GetterOnly: string
    
    member SetterOnly: string with set

