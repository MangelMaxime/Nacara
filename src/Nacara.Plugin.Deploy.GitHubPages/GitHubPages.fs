namespace Nacara.Plugins

open System
open System.IO
open System.Text.Json
open FsToolkit.ErrorHandling
open Nacara.Core
open Nacara.Plugins.Internal

/// <summary>Options of the GitHub Pages plugin.</summary>
type GitHubPagesOptions =
    {
        /// The branch GitHub Pages serves.
        Branch: string
        /// The remote it is pushed to.
        Remote: string
    }

/// <summary>
/// Publishes a build to the branch GitHub Pages serves.
/// </summary>
/// <remarks>
/// <para>A version is a build of its own, deployed into a directory of its own, so publishing one
/// must leave the others as they were. This writes the new tree from the published one rather than
/// checking it out: the current version becomes the root and the other versions' directories are
/// carried across, or a version's directory is replaced and nothing else is touched.</para>
/// <para>Which versions exist comes from the <c>versions.json</c> the build wrote.</para>
/// </remarks>
[<RequireQualifiedAccess>]
module GitHubPages =

    /// <summary>The tree with nothing in it, which is what an unpublished branch holds.</summary>
    [<Literal>]
    let private EmptyTree = "4b825dc642cb6eb9a060e54bf8d69288fbee4904"

    let defaults =
        {
            Branch = "gh-pages"
            Remote = "origin"
        }

    /// <summary>One version, as the manifest describes it.</summary>
    type private PublishedVersion =
        {
            Prefix: string
            Current: bool
        }

    let private readVersionManifest (output: string) =
        let path = Path.Combine(output, "versions.json")

        if not (File.Exists path) then
            []
        else

            use document = JsonDocument.Parse(File.ReadAllText path)

            [
                for entry in document.RootElement.EnumerateArray() do
                    {
                        Prefix = entry.GetProperty("prefix").GetString()
                        Current =
                            match entry.TryGetProperty "current" with
                            | true, value -> value.GetBoolean()
                            | _ -> false
                    }
            ]

    /// <summary>The tree to publish: what was built, in its place, and the rest as it stands.</summary>
    let private tree
        (root: string)
        (branch: string)
        (published: bool)
        (output: string)
        (prefix: string)
        =
        let index = Git.temporaryIndex ()
        let staging = Git.temporaryIndex ()

        let git arguments = Git.exec root (Some index) arguments
        let gitRead arguments = Git.read root (Some index) arguments

        let others =
            readVersionManifest output
            |> List.filter (fun version -> version.Prefix <> "" && version.Prefix <> prefix)
            |> List.map _.Prefix

        try
            result {
                do!
                    Git.exec
                        root
                        (Some staging)
                        [
                            "--work-tree"
                            output
                            "add"
                            "--all"
                        ]

                let! build = Git.read root (Some staging) [ "write-tree" ]

                if prefix = "" then
                    // The build is the root, so anything the last one left behind goes with it.
                    do!
                        git
                            [
                                "read-tree"
                                build
                            ]

                    for other in others do
                        if published then
                            do!
                                git
                                    [
                                        "read-tree"
                                        $"--prefix=%s{other}/"
                                        $"%s{branch}:%s{other}"
                                    ]
                else
                    // This version owns one directory, and only that one is replaced.
                    if published then
                        do!
                            git
                                [
                                    "read-tree"
                                    branch
                                ]

                    do!
                        git
                            [
                                "rm"
                                "-r"
                                "--cached"
                                "--quiet"
                                "--ignore-unmatch"
                                prefix
                            ]

                    do!
                        git
                            [
                                "read-tree"
                                $"--prefix=%s{prefix}/"
                                build
                            ]

                return! gitRead [ "write-tree" ]
            }
        finally
            for path in
                [
                    index
                    staging
                ] do
                if File.Exists path then
                    File.Delete path

    let private deploy (options: GitHubPagesOptions) (context: CommandContext) =
        let dryRun = List.contains "--dry-run" context.Arguments
        let root = AbsolutePath.value context.ProjectRoot
        let output = AbsolutePath.value context.OutputDirectory

        if not (Directory.Exists output) then
            Log.error $"'%s{output}' is not there - build the site first"
            1
        else

            let branch = $"%s{options.Remote}/%s{options.Branch}"

            Log.info $"Fetching %s{options.Branch} from %s{options.Remote}"

            // A branch nobody has published to yet has nothing to carry across.
            let published =
                let fetched =
                    Git.run
                        root
                        None
                        [
                            "fetch"
                            options.Remote
                            options.Branch
                        ]

                fetched.Succeeded

            let currentPrefix =
                readVersionManifest output
                |> List.tryFind _.Current
                |> Option.map _.Prefix
                |> Option.defaultValue ""

            let plan =
                result {
                    Log.info "Reading the build into a tree"
                    let! written = tree root branch published output currentPrefix

                    let where =
                        if currentPrefix = "" then
                            "the root"
                        else
                            $"%s{currentPrefix}/"

                    if dryRun then
                        Log.info
                            $"%s{written} is what %s{options.Branch} would hold, with this build at %s{where}"

                        let! changes =
                            Git.read
                                root
                                None
                                [
                                    "diff"
                                    "--name-status"
                                    (if published then
                                         $"%s{branch}^{{tree}}"
                                     else
                                         EmptyTree)
                                    written
                                ]

                        if changes = "" then
                            Log.success "Nothing to publish: the branch already holds this build"
                        else
                            printfn "%s" changes
                    else
                        let! head =
                            Git.read
                                root
                                None
                                [
                                    "rev-parse"
                                    "HEAD"
                                ]

                        let unchanged =
                            published
                            && Git.read
                                root
                                None
                                [
                                    "rev-parse"
                                    $"%s{branch}^{{tree}}"
                                ] = Ok written

                        if unchanged then
                            Log.success "Nothing to publish: the branch already holds this build"
                        else
                            // git needs someone to attribute the commit to, and a runner has
                            // nobody configured.
                            let identity =
                                match
                                    Git.read
                                        root
                                        None
                                        [
                                            "config"
                                            "user.email"
                                        ]
                                with
                                | Ok email when email <> "" -> []
                                | _ ->
                                    [
                                        "-c"
                                        "user.name=Nacara"
                                        "-c"
                                        "user.email=nacara@users.noreply.github.com"
                                    ]

                            let! commit =
                                Git.read
                                    root
                                    None
                                    (identity
                                     @ [
                                         "commit-tree"
                                         written
                                         "-m"
                                         $"deploy: %s{head}"
                                     ]
                                     @ (if published then
                                            [
                                                "-p"
                                                branch
                                            ]
                                        else
                                            []))

                            Log.info $"Pushing %s{commit} to %s{options.Remote}/%s{options.Branch}"

                            do!
                                Git.exec
                                    root
                                    None
                                    [
                                        "push"
                                        options.Remote
                                        $"%s{commit}:refs/heads/%s{options.Branch}"
                                    ]

                            Log.success
                                $"Published this build at %s{where} of %s{options.Branch}, as %s{commit}"
                }

            match plan with
            | Ok() -> 0
            | Error message ->
                Log.error message
                1

    type private GitHubPagesPlugin(options: GitHubPagesOptions) =
        interface IPlugin with
            member _.Name = "deploy-github-pages"

            member _.Configure registry =
                registry
                |> Registry.command (
                    PluginCommand.create
                        "gh-pages"
                        "Publish the build to the branch GitHub Pages serves"
                        (deploy options)
                    |> PluginCommand.help
                        """deploy - publish the build to the branch GitHub Pages serves

USAGE
    gh-pages [--dry-run]

Publishes what the last build wrote. The version it was built as goes to its own
directory, and every other version stays as it was published:

    dotnet run -- build
    dotnet run -- gh-pages --dry-run
    dotnet run -- gh-pages

It reads versions.json to know which version this build is and which others exist,
so build before deploying."""
                )

    /// <summary>The branch GitHub Pages serves. Defaults to <c>gh-pages</c>.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let branch value (options: GitHubPagesOptions) =
        { options with
            Branch = value
        }

    /// <summary>The remote it is pushed to. Defaults to <c>origin</c>.</summary>
    /// <param name="value">The value to use.</param>
    /// <param name="options">The options so far.</param>
    let remote value (options: GitHubPagesOptions) =
        { options with
            Remote = value
        }

    let create () = GitHubPagesPlugin defaults :> IPlugin

    /// <summary>Ready to register, configured.</summary>
    /// <param name="configure">Given the defaults, the options to use.</param>
    let createWith (configure: GitHubPagesOptions -> GitHubPagesOptions) =
        GitHubPagesPlugin(configure defaults) :> IPlugin

    /// <summary>Add publishing to GitHub Pages to a site.</summary>
    /// <param name="site">The site to add it to.</param>
    let register (site: Site) = Site.plugin (create ()) site

    /// <summary>Add publishing to GitHub Pages to a site, configured.</summary>
    /// <param name="configure">Given the defaults, the options to use.</param>
    /// <param name="site">The site to add it to.</param>
    let registerWith (configure: GitHubPagesOptions -> GitHubPagesOptions) (site: Site) =
        Site.plugin (createWith configure) site
