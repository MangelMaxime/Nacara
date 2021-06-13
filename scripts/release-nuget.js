const path = require('path')
const chalk = require('chalk')
const shell = require('shelljs')

const log = console.log

const release = require("./release-core").release

const getEnvVariable = function (varName) {
    const value = process.env[varName];
    if (value === undefined) {
        log(error(`Missing environnement variable ${varName}`))
        process.exit(1)
    } else {
        return value;
    }
}

// Check that we have enought arguments
if (process.argv.length < 4) {
    log(chalk.red("Missing arguments"))
    process.exit(1)
}

const cwd = process.cwd()
const baseDirectory = path.resolve(cwd, process.argv[2])
const projectFileName = process.argv[3]

const NUGET_KEY = getEnvVariable("NUGET_KEY")

release({
    baseDirectory: baseDirectory,
    projectFileName: projectFileName,
    versionRegex: /(^\s*<Version>)(.*)(<\/Version>\s*$)/gmi,
    publishFn: async (versionInfo) => {

        const packResult =
            shell.exec(
                "dotnet pack -c Release",
                {
                    cwd: baseDirectory
                }
            )

        if (packResult.code !== 0) {
            throw "Dotnet pack failed"
        }

        const fileName = path.basename(projectFileName, ".fsproj")

        const pushNugetResult =
            shell.exec(
                `dotnet nuget push bin/Release/${fileName}.${versionInfo.version}.nupkg -s nuget.org -k ${NUGET_KEY}`,
                {
                    cwd: baseDirectory
                }
            )

        if (pushNugetResult.code !== 0) {
            throw "Dotnet push failed"
        }
    }
})
