const path = require('path')
const chalk = require('chalk')
const shell = require('shelljs')

const log = console.log

const release = require("./release-core").release

// Check that we have enought arguments
if (process.argv.length < 3) {
    log(chalk.red("Missing the path arguments"))
    process.exit(1)
}

const cwd = process.cwd()
const baseDirectory = path.resolve(cwd, process.argv[2])

release({
    baseDirectory: baseDirectory,
    projectFileName: "package.json",
    versionRegex: /(^\s*"version":\s*")(.+)(",\s*$)/gmi,
    publishFn: async () => {
        const publishResult =
            shell.exec(
                "npm publish",
                {
                    cwd: baseDirectory
                }
            )

        // If published failed revert the file change
        if (publishResult.code !== 0) {
            throw "Npm publish failed"
        }
    }
})
