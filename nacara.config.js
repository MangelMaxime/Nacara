const standardLayouts = require("nacara-layout-standard");

const mdMessage = (level) => {

    return {
        validate: function (params) {
            return params.trim() === level;
        },

        render: function (tokens, idx) {
            if (tokens[idx].nesting === 1) {
                // opening tag
                return `<article class="message is-${level}">
                <div class="message-body">`;


            } else {
                // closing tag
                return '</div>\n</article>\n';
            }
        }
    }
}

const path = require('path');

module.exports = {
    githubURL: "https://github.com/MangelMaxime/Nacara",
    url: "https://mangelmaxime.github.io",
    baseUrl: "/Nacara/",
    editUrl : "https://github.com/MangelMaxime/Nacara/edit/master/docsrc",
    source: "docs",
    output: "temp",
    title: "Nacara",
    debug: true,
    changelog: "CHANGELOG.md",
    version: "0.4.0",
    serverPort: 8081,
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
            "API/page-attributes",
            "API/nacara-config-json"
        ]
    },
    lightner: {
        backgroundColor: "#FAFAFA",
        textColor: "",
        themeFile: "./lightner/themes/OneLight.json",
        grammars: [
            "./lightner/grammars/fsharp.json",
            "./lightner/grammars/json.json"
        ]
    },
    layouts: {
        default: standardLayouts.standard,
        changelog: standardLayouts.changelog
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
            }
        ]
    }
};
