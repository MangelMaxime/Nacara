namespace Nacara.Plugins.Internal

open System.IO
open System.IO.Compression
open System.Reflection
open System.Text
open Nacara.Core

/// <summary>
/// The browser side of this plugin: where its files go, and where they come from.
/// </summary>
/// <remarks>
/// <para>A build colours a code block with the native runtime. The browser cannot use that one, so
/// a site that colours code it only has at runtime ships the wasm build of tree-sitter, the
/// grammars it names, and the script that defines the element.</para>
/// <para>Emitted into the site rather than loaded from a CDN, so a site works offline and behind a
/// proxy.</para>
/// </remarks>
[<RequireQualifiedAccess>]
module Browser =

    /// <summary>Where all of it is served from.</summary>
    let Directory = "assets/tree-sitter"

    /// <summary>The script that defines the element, under <c>Directory</c>.</summary>
    let Script = "highlight.js"

    /// <summary>The worker the grammars are loaded in, under <c>Directory</c>.</summary>
    /// <remarks>A browser refuses synchronous WebAssembly instantiation above 8 MB on the main
    /// thread, and the F# grammar is 9.7 MB inflated.</remarks>
    let Worker = "highlight-worker.js"

    /// <summary>The wasm build of tree-sitter, at the version the native side pins.</summary>
    /// <remarks>A grammar is compiled against a tree-sitter ABI, so this is the same version as the
    /// native runtime or a browser cannot load the grammar a site already ships.</remarks>
    let package =
        {
            Name = "web-tree-sitter"
            Version = Runtime.Version
            Url =
                $"https://registry.npmjs.org/web-tree-sitter/-/web-tree-sitter-%s{Runtime.Version}.tgz"
            Archive = TarGzip
            Files = [ "package.json" ]
            Executable = []
            Checksum = None
        }

    let private readResource = Resource.text (Assembly.GetExecutingAssembly())

    let private worker = lazy readResource "highlight-worker.js"

    let private element = lazy readResource "highlight.js"

    let private at (name: string) =
        RelativePath.create $"%s{Directory}/%s{name}"

    /// <summary>Whether these bytes are gzipped already.</summary>
    let private gzipped (bytes: byte array) =
        bytes.Length > 1 && bytes[0] = 0x1fuy && bytes[1] = 0x8buy

    /// <summary>The grammar as the worker reads it: gzipped, whatever it was given.</summary>
    /// <remarks>Shipped gzipped so the transfer does not depend on what the host does about
    /// Content-Encoding, and a grammar this plugin built or ships is stored that way too.</remarks>
    let private packed (bytes: byte array) =
        if gzipped bytes then
            bytes
        else
            use memory = new MemoryStream()

            (use stream = new GZipStream(memory, CompressionLevel.SmallestSize)
             stream.Write(bytes, 0, bytes.Length))

            memory.ToArray()

    /// <summary>The runtime, fetched once per machine.</summary>
    /// <returns>The two files to emit, or why they could not be had.</returns>
    let runtime () =
        match Tool.resolve package with
        | Error message -> Error message
        | Ok directory ->
            let file name =
                AbsolutePath.create (Path.Combine(directory, "package", name))

            Ok
                [
                    CopyFile(file "web-tree-sitter.js", at "web-tree-sitter.js")
                    CopyFile(file "web-tree-sitter.wasm", at "web-tree-sitter.wasm")
                ]

    /// <summary>What one language takes, beside the runtime.</summary>
    /// <param name="language">The name the browser asks for it by.</param>
    /// <param name="wasm">Its parse tables, gzipped or not.</param>
    /// <param name="queries">The highlights query saying what its nodes mean.</param>
    /// <param name="captures">Every capture those queries use, paired with the class the build
    /// would give it, as json.</param>
    let grammar (language: string) (wasm: byte array) (queries: byte array) (captures: string) =
        [
            WriteBytes(packed wasm, at $"grammars/%s{language}/grammar.wasm.gz")
            WriteBytes(queries, at $"grammars/%s{language}/highlights.scm")
            WriteText(captures, at $"grammars/%s{language}/captures.json")
        ]

    /// <summary>The worker the grammars are loaded in.</summary>
    /// <remarks>Emitted as bytes, so no asset transform reads it: it is an ES module.</remarks>
    let workerAsset () =
        WriteBytes(Encoding.UTF8.GetBytes worker.Value, at Worker)

    /// <summary>The script that defines the element.</summary>
    /// <param name="prelude">What it has to know before a page uses the element.</param>
    let script (prelude: string) =
        WriteText(prelude + element.Value, at Script)

    /// <summary>Adds the assets nothing is already writing.</summary>
    /// <remarks>A site that colours its blocks and its live snippets with tree-sitter has two
    /// plugins asking for the same runtime and the same grammars. Whoever asks first writes them,
    /// so the site ships one copy rather than two.</remarks>
    /// <param name="assets">What to emit.</param>
    /// <param name="registry">What the plugins have asked for so far.</param>
    let add (assets: Asset list) (registry: Registry) =
        let taken =
            registry.Assets
            |> List.map (fun asset -> RelativePath.value asset.Destination)
            |> Set.ofList

        (registry, assets)
        ||> List.fold (fun acc asset ->
            if Set.contains (RelativePath.value asset.Destination) taken then
                acc
            else
                Registry.asset asset acc
        )
