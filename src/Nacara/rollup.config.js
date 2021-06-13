import externals from 'rollup-plugin-node-externals'
import { terser } from "rollup-plugin-terser"
import path from 'path'
// const path = require("path");

function resolve(filePath) {
    return path.join(__dirname, filePath)
}

const isProduction = process.env.BUILD === 'production';

export default {
    input: resolve('./fableBuild/Main.js'),
    output: {
        file: resolve('./dist/bundle.js'),
        format: 'cjs'
    },
    external: [
        "code-lightner",
        "front-matter",
        "live-server",
        "chokidar",
        "react-dom/server",
        "react",
        "chalk",
        "sass"
    ],
    plugins: [
        externals(),
        isProduction && terser()
    ].filter(Boolean)
};
