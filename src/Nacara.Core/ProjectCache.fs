namespace Nacara.Core

open System.IO

/// <summary>Where a build keeps what it worked out from the project's own files.</summary>
/// <remarks>
/// <para>Put an entry here when its key mentions the project - a file's contents, a source's
/// timestamp, a path. Deleting <c>.nacara</c> costs a rebuild and nothing else.</para>
/// <para>Something fetched rather than derived - a pinned tool, a grammar at a commit - is the same
/// for every project, and belongs in <see cref="T:Nacara.Core.Tool" />'s cache instead.</para>
/// <para>The directory carries a <c>.gitignore</c> covering itself, so a project needs no rule of
/// its own.</para>
/// </remarks>
[<RequireQualifiedAccess>]
module ProjectCache =

    /// <summary>What the directory is called, at the root of the project.</summary>
    [<Literal>]
    let PROJECT_CACHE_DIR_NAME = ".nacara"

    let private ignoreEverything =
        "# Automatically created by Nacara.\n\
         **/*\n"

    /// <summary>The cache directory itself, made ready to be written to.</summary>
    /// <param name="projectRoot">The root of the project being built.</param>
    let private root (projectRoot: AbsolutePath) =
        let directory = AbsolutePath.combine projectRoot [ PROJECT_CACHE_DIR_NAME ]
        let path = AbsolutePath.value directory

        Directory.CreateDirectory path |> ignore

        let gitignore = Path.Combine(path, ".gitignore")

        if not (File.Exists gitignore) then
            File.WriteAllText(gitignore, ignoreEverything)

        directory

    /// <summary>Where one entry goes, made ready to be written to.</summary>
    /// <param name="projectRoot">The root of the project being built.</param>
    /// <param name="group">What kind of entry it is. One per job, so that
    /// <see cref="M:Nacara.Core.ProjectCache.forgetOthers" /> can tidy a job's entries without
    /// touching anyone else's.</param>
    /// <param name="entry">What tells this entry from the others in its group, usually a hash of
    /// whatever it was worked out from.</param>
    let directory (projectRoot: AbsolutePath) (group: string) (entry: string) =
        let directory =
            AbsolutePath.combine
                (root projectRoot)
                [
                    group
                    entry
                ]

        Directory.CreateDirectory(AbsolutePath.value directory) |> ignore

        directory

    /// <summary>Remove the cache, so the next build works everything out again.</summary>
    /// <param name="projectRoot">The root of the project being built.</param>
    let clear (projectRoot: AbsolutePath) =
        let path = Path.Combine(AbsolutePath.value projectRoot, PROJECT_CACHE_DIR_NAME)

        if Directory.Exists path then
            Directory.Delete(path, true)

    /// <summary>Drop the entries in a group that this build did not ask for.</summary>
    /// <remarks>An entry is keyed by what it was worked out from, so changing a source mints a new
    /// one and orphans the last.</remarks>
    /// <param name="projectRoot">The root of the project being built.</param>
    /// <param name="group">The group to tidy.</param>
    /// <param name="kept">The entries this build is using.</param>
    let forgetOthers (projectRoot: AbsolutePath) (group: string) (kept: string list) =
        let path =
            Path.Combine(AbsolutePath.value projectRoot, PROJECT_CACHE_DIR_NAME, group)

        if Directory.Exists path then
            for candidate in Directory.EnumerateDirectories path do
                if not (List.contains (Path.GetFileName candidate) kept) then
                    try
                        Directory.Delete(candidate, true)
                    with _ ->
                        ()
