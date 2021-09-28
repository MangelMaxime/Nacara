import path from "node:path"
import chalk from "chalk"
import shell from "shelljs"

const log = console.log

import { release } from "./release-core.js"

// Check that we have enough arguments
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
