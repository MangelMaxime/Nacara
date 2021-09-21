#!/usr/bin/env node

// TODO: When the refactor of the start of the application is done with yargs,
// handle nodemon support in a better way
// For v1 release, we will do it in a "hacky" way
import nodemon from 'nodemon';
import { start } from './dist/Main.js';

import { fileURLToPath } from 'node:url'
import path from 'node:path';
import { info } from './dist/Nacara.Core/Log.js';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Application will be started in watch mode
// So we use nodemon to start it
if (process.argv.indexOf("watch") !== -1) {
    nodemon({
        script: path.join(__dirname, "./js/nodemon-watch.js"),
        args: process.argv.slice(2),
        watch: [
            "nacara.config.json"
        ],
        delay: 200
    })
    // Inform the user that the application is restarting
    .on("restart", (x) => {
        info("Nacara configuration file changed, restarting Nacara...")
    })
    // Allow the user to kill the application with a single CTRL+C
    // Without this the user need to do it twice
    .once('quit', function () {
        process.exit();
    });
} else {
    start();
}
