module ModuleWithRecord

/// <summary>
/// Simple User type
/// </summary>
type User =
    {
        /// <summary>
        /// Unique Id of the user
        /// </summary>
        Id : int
        /// <summary>
        /// Name of the user
        /// </summary>
        Name : string
        /// <summary>
        /// Email of the user
        /// </summary>
        Email : string
    }

    /// <summary>
    /// This is a getter only property
    /// </summary>
    member this.GetterOnly
        with get () =
            "This is a getter only property"

    member this.SetterOnly
        with set (value : string) =
            ()

    member this.GetterAndSetter
        with get () =
            "This is a getter and setter property"
        and set (value : string) =
            ()

    /// <summary>
    /// Create a new user
    /// </summary>
    /// <param name="id">Unique Id of the user</param>
    /// <param name="name">Name of the user</param>
    /// <param name="email">Email of the user</param>
    /// <returns>
    /// Returns the newly created user
    /// </returns>
    static member Create(id : int, name : string, ?email : string) : User =
        {
            Id = id
            Name = name
            Email = ""
        }
