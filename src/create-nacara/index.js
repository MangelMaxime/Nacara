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

const nacaraConfigJson = (options) => {
    return `
{
    "url": "${options.url}",
    "baseUrl": "${options.baseUrl}",
    "editUrl" : "https://github.com/${options.organizationName}/${options.projectName}/edit/master/docsrc",
    "title": "${options.title}",
    "navbar": {
        "start": [
            {
                "section": "documentation",
                "url": "${options.baseUrl}documentation/index.html",
                "label": "Documentation"
            }
        ],
        "end": [
            {
                "url": "https://github.com/${options.organizationName}/${options.projectName}",
                "icon": "fab fa-github"
            }
        ]
    },
    "lightner": {
        "backgroundColor": "#FAFAFA",
        "textColor": "",
        "themeFile": "./lightner/themes/OneLight.json",
        "grammars": [
            "./lightner/grammars/fsharp.json",
            "./lightner/grammars/json.json",
            "./lightner/grammars/JavaScript.tmLanguage.json"
        ]
    },
    "layouts": [
        "nacara-layout-standard"
    ]
}`.trim();

}

const run = async () => {

    const response = await prompt(
        [
            {
                type: 'input',
                name: 'url',
                message: 'Public URL',
                initial: 'https://mangelmaxime.github.io'
            },
            {
                type: 'input',
                name: 'title',
                message: 'Website title',
                initial: 'My Site'
            },
            {
                type: 'input',
                name: 'baseUrl',
                message: 'Base URL',
                initial: '/'
            },
            {
                type: 'input',
                name: 'organizationName',
                message: 'Organization name',
                initial: 'MyOrganization'
            },
            {
                type: 'input',
                name: 'projectName',
                message: 'Project name',
                initial: 'MyProject'
            },
            {
                type: 'select',
                name: 'styleProcessor',
                message: 'Choose how you want to style your application',
                choices: [
                    'scss',
                    'sass'
                ],
                initial: 'scss'
            },
            {
                type: 'confirm',
                name: 'setupBabelAndReact',
                message: 'Do you want to setup Babel and react? Useful, only if you are going to write custom layout with JSX',
                initial: false
            }
        ]
    );

    console.log('Generating nacara.config.json file...');
    await fs.writeFile('nacara.config.json', nacaraConfigJson(response));

    console.log('Setting up minimal site...');
    shell.cp('-R', resolve('./templates/docs'), 'docs');
    if (response.styleProcessor === 'scss') {
        // shell.mkdir('-p', 'docs/scss');
        shell.cp(resolve('./templates/style.scss'), 'docs/style.scss');
    } else if (response.styleProcessor === 'sass') {
        // shell.mkdir('-p', 'docs/sass');
        shell.cp(resolve('./templates/style.sass'), 'docs/style.sass');
    }
    shell.cp('-R', resolve('./templates/lightner'), 'lightner');
    shell.cp(resolve('./templates/.gitignore'), '.gitignore');

    const styleExtension = (response.styleProcessor === 'scss') ? 'scss' : 'sass';

    // Replace placeholders with values coming from user response
    console.log('Adapting the site to your needs...');
    shell.ls([
        'docs/**/*.md',
        'package.json'
    ]).forEach(function (file) {
        shell.sed('-i', '{{REPLACE_WITH_MY_SITE}}', response.title, file);
        shell.sed('-i', '{{REPLACE_WITH_BASE_URL}}', response.baseUrl, file);
        shell.sed('-i', '{{REPLACE_WITH_STYLE_EXTENSION}}', styleExtension, file);
        shell.sed('-i', '{{REPLACE_WITH_SITE_TITLE}}', response.title, file);
    });

    console.log('Installing NPM dependencies...');
    await fs.copyFile(resolve('./templates/package.json'), './package.json');
    execSync('npm install --save-dev nacara nacara-layout-standard', { cwd: '.', stdio: 'inherit' });

    console.log('Configuring Babel and React for JSX support');
    await fs.copyFile(resolve('./templates/babel.config.json'), './babel.config.json');
    execSync('npm install --save-dev @babel/register @babel/preset-react', { cwd: '.', stdio: 'inherit' });

    console.log(chalk.green('Your site is ready!'));
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
