module Demo.RandomUser

open System
open Thoth.Json.Core

/// <summary>Whoever the API answered with.</summary>
type Gender =
    | Male
    | Female

    static member Decoder =
        Decode.string
        |> Decode.andThen (
            function
            | "male" -> Decode.succeed Male
            | "female" -> Decode.succeed Female
            | invalid -> Decode.fail $"`%s{invalid}` isn't a valid value for Gender"
        )

/// <summary>A person, flattened out of the shape the API sends.</summary>
type User =
    {
        Gender: Gender
        FullName: string
        Email: string
        CellPhone: string
        OfficePhone: string
        Age: int
        Birthday: DateTime
        Picture: string
    }

    /// <summary>Reads a user out of the API's own shape.</summary>
    /// <remarks>The JSON nests the name, the age and the picture; nothing says a decoder has to
    /// keep that shape, so this reaches in and puts what it finds at the top of the record.</remarks>
    static member Decoder =
        Decode.object (fun get ->
            let firstname =
                get.Required.At
                    [
                        "name"
                        "first"
                    ]
                    Decode.string

            let lastname =
                get.Required.At
                    [
                        "name"
                        "last"
                    ]
                    Decode.string

            {
                Gender = get.Required.Field "gender" Gender.Decoder
                FullName = firstname + " " + lastname
                Email = get.Required.Field "email" Decode.string
                CellPhone = get.Required.Field "cell" Decode.string
                OfficePhone = get.Required.Field "phone" Decode.string
                Age =
                    get.Required.At
                        [
                            "dob"
                            "age"
                        ]
                        Decode.int
                Birthday =
                    get.Required.At
                        [
                            "dob"
                            "date"
                        ]
                        Decode.datetimeUtc
                Picture =
                    get.Required.At
                        [
                            "picture"
                            "large"
                        ]
                        Decode.string
            }
        )

    /// <summary>The one the API hands back, which is the first of a list of one.</summary>
    static member FromApi = Decode.field "results" (Decode.index 0 User.Decoder)
