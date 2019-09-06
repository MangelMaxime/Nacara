const standard = require('./dist/layouts/standard/Export').default;
const mdMessage = require('./src/Nacara/js/utils').mdMessage;
const path = require('path');

module.exports = {
    githubURL: "https://github.com/MangelMaxime/Nacara",
    url: "https://mangelmaxime.github.io",
    baseUrl: "/Nacara/",
    editUrl : "https://github.com/MangelMaxime/Nacara/edit/master/docsrc",
    title: "Nacara",
    debug: true,
    changelog: "CHANGELOG.md",
    version: "0.2.1",
    navbar: {
        showVersion: true,
        links: [
            {
                href: "/Nacara/index.html",
                label: "Documentation",
                icon: "fas fa-book"
            },
            {
                href: "/Nacara/changelog.html",
                label: "Changelog",
                icon: "fas fa-tasks"
            },
            {
                href: "https://gitter.im/fable-compiler/Fable",
                label: "Support",
                icon: "fab fa-gitter",
                isExternal: true
            },
            {
                href: "https://github.com/MangelMaxime/Nacara",
                icon: "fab fa-github",
                isExternal: true
            },
            {
                href: "https://twitter.com/MangelMaxime",
                icon: "fab fa-twitter",
                isExternal: true,
                color: "#55acee"
            }
        ]
    },
    menu: {
        "Getting Started": [
            "index"
        ],
        API: [
            "API/nacara-config-json",
            "API/page-attributes"
        ]
    },
    lightner: {
        backgroundColor: "#FAFAFA",
        textColor: "",
        themeFile: "./paket-files/akamud/vscode-theme-onelight/themes/OneLight.json",
        grammars: [
            "./paket-files/ionide/ionide-fsgrammar/grammar/fsharp.json",
            "./paket-files/Microsoft/vscode/extensions/json/syntaxes/JSON.tmLanguage.json"
        ]
    },
    layouts: {
        default: standard.Default,
        changelog: standard.Changelog
    },
    plugins: {
        markdown: [
            {
                path: 'markdown-it-container',
                args: [
                    'warning',
                    mdMessage("warning")
                ]
            },
            {
                path: 'markdown-it-container',
                args: [
                    'info',
                    mdMessage("info")
                ]
            },
            {
                path: 'markdown-it-container',
                args: [
                    'success',
                    mdMessage("success")
                ]
            },
            {
                path: 'markdown-it-container',
                args: [
                    'danger',
                    mdMessage("danger")
                ]
            },
            {
                path: path.join(__dirname, './src/Nacara/js/markdown-it-anchored.js')
            },
            {
                path: path.join(__dirname, './src/Nacara/js/markdown-it-toc.js')
            }
        ],
        layout: [
            // {
            //     name: "table-of-content",
            //     apply: standard.Default.
            // }
        ]

    }
};
