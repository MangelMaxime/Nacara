import { runWatch } from '../dist/Main.js';
import yargs from 'yargs';
import { hideBin } from "yargs/helpers";
import afterCleanOptions from './afterClean-options.js';

// Yargs to parse the arguments
// We don't need to be complete or strict here because
// arguments have already been check by the cli.js
// Here we re-parse the arguments because we are in a new instance of Nacara
// started from Nodemon
const res =
    yargs(hideBin(process.argv))
        .option(
            "afterClean",
            afterCleanOptions
        )
        .argv

runWatch(res);
