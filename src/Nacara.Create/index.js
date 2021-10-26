#!/usr/bin/env node

const { Confirm, prompt } = require('enquirer');
const fs = require('fs').promises;
const path = require('path');
const execSync = require('child_process').execSync;
const chalk = require('chalk');
const shell = require('shelljs');

const resolve = (relativePath) => {
    return path.join(__dirname, relativePath);
};

const nacaraConfig = (options) => {
    return `
// For more information about the config, please visit:
// https://mangelmaxime.github.io/Nacara/nacara/configuration.html
export default {
    "siteMetadata": {
        "url": "https://your-nacara-test-site.com",
        "baseUrl": "/",
        // Please change this to your repo
        "editUrl" : "https://github.com/MangelMaxime/Nacara/edit/master/docs",
        "title": "${options.title}"
    },
    "navbar": {
        "start": [
            {
                "section": "documentation",
                "url": "/documentation/introduction.html",
                "label": "Documentation"
            }
        ],
        "end": [
            // Please change it your repo or delete the item if you don't need it
            {
                "url": "https://github.com/MangelMaxime/Nacara",
                "icon": "fab fa-github",
                "label": "Github"
            }
        ]
    },
    "remarkPlugins": [
        {
            "resolve": "gatsby-remark-vscode",
            "property": "remarkPlugin",
            "options": {
                "theme": "Atom One Light",
                "extensions": [
                    "vscode-theme-onelight"
                ]
            }
        }
    ],
    "layouts": [
        "nacara-layout-standard"
    ]
}
    `.trim();

}

const run = async () => {

    const response = await prompt(
        [
            {
                type: 'input',
                name: 'title',
                message: 'What would you like to call your site?',
                initial: 'My Nacara Site'
            },
            {
                type: 'input',
                name: 'destination',
                message: 'What would you like to name the folder where your site will be created?',
                initial: 'my-site'
            }
        ]
    );

    const destination = (relativePath) =>{
        return path.join(response.destination, relativePath)
    }

    shell.rm("-rf", response.destination);
    shell.mkdir(response.destination);

    console.log('Generating nacara.config.json file...');
    await fs.writeFile(destination('nacara.config.js'), nacaraConfig(response));

    console.log('Setting up minimal site...');
    shell.cp('-R', resolve('./templates/docs'), destination('docs'));

    shell.cp(resolve('./templates/style.scss'), destination('docs/style.scss'));

    shell.cp(resolve('./templates/.gitignore'), destination('.gitignore'));
    shell.cp(resolve('./templates/package.json'), destination('./package.json'));

    shell.ls([
        `${response.destination}/docs/**/*.md`,
        `${response.destination}/package.json`
    ]).forEach(function (file) {
        shell.sed('-i', /{{REPLACE_WITH_SITE_TITLE}}/g, response.title, file);
    });

    await fs.copyFile(resolve('./templates/babel.config.json'), destination('./babel.config.json'));

    console.log('Installing NPM dependencies...');

    const npmArgs =
        [
            "nacara",
            "nacara-layout-standard",
            "@babel/register",
            "@babel/preset-react",
            "akamud/vscode-theme-onelight ",
            "gatsby-remark-vscode",
            // We need to force unified to be of the latest version
            // because of gatsby...
            // In the future, I will create a standalone version of gatsby-remark-vscode
            "unified@latest"
        ].join(" ")

    execSync(
        `npm install --silent --save-dev ${npmArgs}`,
        {
            cwd: response.destination,
            stdio: 'inherit'
        }
    );

    console.log(chalk.green('Your site is ready!'));

    console.log(``)
}

(async () => {
    try {
        await run();
    }
    catch (e) {
        console.error(chalk.red("An error occurred:"));
        console.error(chalk.red(e));
    }
})();
