import path from "node:path"
import chalk from "chalk"
import shell from "shelljs"

const log = console.log

import { release } from "./release-core.js"

const getEnvVariable = function (varName) {
    const value = process.env[varName];
    if (value === undefined) {
        log(chalk.red(`Missing environnement variable ${varName}`))
        process.exit(1)
    } else {
        return value;
    }
}

// Check that we have enough arguments
if (process.argv.length < 4) {
    log(chalk.red("Missing arguments"))
    process.exit(1)
}

const cwd = process.cwd()

const relativePathToFsproj = process.argv[3]
const baseDirectory = path.resolve(cwd, process.argv[2])
const fullPathToFsproj = path.resolve(baseDirectory, relativePathToFsproj)
const fsprojDirectory = path.dirname(fullPathToFsproj)
const projectName = path.basename(fullPathToFsproj, ".fsproj")

const NUGET_KEY = getEnvVariable("NUGET_KEY")

release({
    baseDirectory: baseDirectory,
    projectFileName: relativePathToFsproj,
    versionRegex: /(^\s*<Version>)(.*)(<\/Version>\s*$)/gmi,
    publishFn: async (versionInfo) => {

        const packResult =
            shell.exec(
                "dotnet pack -c Release",
                {
                    cwd: fsprojDirectory
                }
            )

        if (packResult.code !== 0) {
            throw "Dotnet pack failed"
        }

        const pushNugetResult =
            shell.exec(
                `dotnet nuget push bin/Release/${projectName}.${versionInfo.version}.nupkg -s nuget.org -k ${NUGET_KEY}`,
                {
                    cwd: fsprojDirectory
                }
            )

        if (pushNugetResult.code !== 0) {
            throw "Dotnet push failed"
        }
    }
})
