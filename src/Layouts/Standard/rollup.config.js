import externals from 'rollup-plugin-node-externals'
import { terser } from "rollup-plugin-terser"
import path from 'path'
// const path = require("path");

function resolve(filePath) {
    return path.join(__dirname, filePath)
}

const isProduction = process.env.BUILD === 'production';

export default {
    input: resolve('./fableBuild/Export.js'),
    output: {
        file: resolve('./dist/bundle.js'),
        format: 'cjs'
    },
    external: [
        'react',
        'slugify',
        'code-lightner',
        'chalk',
        'react-dom/server'
    ],
    plugins: [
        externals(),
        isProduction && terser()
    ].filter(Boolean)
};
