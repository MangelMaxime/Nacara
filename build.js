#!node

import yargs from 'yargs';
import { hideBin } from 'yargs/helpers';
import shell from 'shelljs';
import chalk from 'chalk';

const info = chalk.blueBright
const warn = chalk.yellow
const error = chalk.red
const success = chalk.green
const log = console.log

// Crash script on error
shell.config.fatal = true;

const shellExecNacaraDir = (command) => {
    shell.exec(
        command,
        {
            cwd: "./src/Nacara"
        }
    )
}

const shellExecNacaraLayoutDir = (command) => {
    shell.exec(
        command,
        {
            cwd: "./src/Nacara.Layout.Standard"
        }
    )
}
const setupDevHandler = async () => {
    shell.exec("npm install")
    shell.exec("dotnet tool restore")
    shellExecNacaraDir("npm install")
    shellExecNacaraLayoutDir("npm install")
    shellExecNacaraDir("npm link")
    shellExecNacaraLayoutDir("npm link")
    shell.exec("npm link nacara nacara-layout-standard")
}

const unSetupDevHandler = async () => {
    shell.exec("npm unlink nacara nacara-layout-standard")
    shellExecNacaraDir("npm -g unlink")
    shellExecNacaraLayoutDir("npm -g unlink")
}

const cleanHandler = async () => {
    log(info("Cleaning..."));
    log(info("Cleaning Nacara..."));
    shell.rm("-rf", "./src/Nacara/dist")
    shell.rm("-rf", "./src/Nacara/Source/bin")
    shell.rm("-rf", "./src/Nacara/Source/obj")

    log(info("Cleaning Nacara.Layout.Standard..."));
    shell.rm("-rf", "./src/Nacara.Layout.Standard/dist")
    shell.rm("-rf", "./src/Nacara.Layout.Standard/Source/bin")
    shell.rm("-rf", "./src/Nacara.Layout.Standard/Source/obj")

    log(info("Cleaning Nacara.Core..."));
    shell.rm("-rf", "./src/Nacara.Core/dist")
    shell.rm("-rf", "./src/Nacara.Core/Source/bin")
    shell.rm("-rf", "./src/Nacara.Core/Source/obj")

    log(info("Cleaning Nacara.ApiGen..."));
    shell.rm("-rf", "./src/Nacara.ApiGen/**/bin")
    shell.rm("-rf", "./src/Nacara.ApiGen/**/obj")
    log(success("Cleaned!"));
}

yargs(hideBin(process.argv))
    .completion()
    .strict()
    .help()
    .alias("help", "h")
    .command(
        "setup-dev",
        "Setup npm link for local development",
        () => {},
        setupDevHandler
    )
    .command(
        "unsetup-dev",
        "Unsetup npm link for local development",
        () => {},
        unSetupDevHandler
    )
    .command(
        "clean",
        "Clean all build artifacts",
        () => {},
        cleanHandler
    )
    .version(false)
    .argv
