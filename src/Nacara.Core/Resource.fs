namespace Nacara.Core

open System.IO
open System.Reflection

/// <summary>Files a plugin ships inside its own assembly.</summary>
/// <remarks>
/// Embed them under an <c>assets</c> folder and they answer to
/// <c>&lt;assembly&gt;.assets.&lt;name&gt;</c>, which is what this reads.
/// </remarks>
[<RequireQualifiedAccess>]
module Resource =

    /// <summary>Read one, as text.</summary>
    /// <remarks>
    /// The assembly is passed in rather than asked for: asked for here, it would be
    /// <c>Nacara.Core</c> rather than the plugin that embedded the file.
    /// </remarks>
    /// <param name="assembly">The assembly holding it, usually
    /// <c>Assembly.GetExecutingAssembly()</c>.</param>
    /// <param name="name">What it is called under <c>assets</c>.</param>
    let text (assembly: Assembly) (name: string) =
        let prefix = assembly.GetName().Name
        let path = $"%s{prefix}.assets.%s{name}"

        match assembly.GetManifestResourceStream path with
        | null -> failwith $"'%s{prefix}' embeds no '%s{path}'"
        | stream ->
            use stream = stream
            use reader = new StreamReader(stream)
            reader.ReadToEnd()
