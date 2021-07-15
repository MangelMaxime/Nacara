#!/usr/bin/env node

// Configure babel to parse further `require` files
// This is needed in order to support JSX syntax if the user use it in their config

// require('core-js');
// require('@babel/register')({
//   babelrc: false,
// //   only: [__dirname, `${process.cwd()}/core`],
//   plugins: [
//     // require('./server/translate-plugin.js'),
//     require('@babel/plugin-proposal-class-properties').default,
//     require('@babel/plugin-proposal-object-rest-spread').default,
//     // require('@babel/plugin-transform-typescript').default,
//   ],
//   presets: [
//     require('@babel/preset-react').default,
//     require('@babel/preset-env').default,
//   ],
// });

require = require("esm")(module/*, options*/)
var nacara = require('./dist/Main.js');
nacara.start();
