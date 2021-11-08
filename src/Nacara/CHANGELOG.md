# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased

## 1.2.0 - 2021-11-08

### Fixed

* Detect the Node.JS version and use `fs.rm` or `fs.rmdir` depending on the version.

    This avoid warning about `fs.rmdir` being deprecated in the future.

* Fix missing `fable_modules` folder from `dist`

## 1.1.0 - 2021-11-04

### Changed

* Provide the `relativePath` to `unified`.

    This is required for `remark-code-import` plugins to work.

## 1.0.0 - 2021-10-28

### Added

* Release v1.0.0

## 1.0.0-beta-023 - 2021-10-28

### Fixed

* Check if a directory exist before executing `Directory.rmdir`. Since Node.js v16 it generate an error if the directory doesn't exist

## 1.0.0-beta-022 - 2021-10-26

### Fixed

* Fix detection of `nacara.config.json` it was a relicat from a test

## 1.0.0-beta-021 - 2021-10-26

### Added

* Support both `nacara.config.json` and `nacara.config.js` as the config file.

    The main goal is to have access to comments makking it easier for people to use the template.

## 1.0.0-beta-020 - 2021-09-30

### Fixed

* Force to display `nacara` as the script name when displaying help
* Fix dynamic load of module from absolute and relative path for Windows

## 1.0.0-beta-019 - 2021-09-30

### Added

* Add support for the partial inside the dropdowns

## 1.0.0-beta-018 - 2021-09-28

### Added

* Fix #120: Add support for watching layout files changes

### Fixed

* Fix live reload for the index page it was never reloading

## 1.0.0-beta-017 - 2021-09-28

### Added

* Fix #105: Re-add support for JS/JSX for both partials and layouts

## 1.0.0-beta-016 - 2021-09-26

### Fixed

* Add a default command so if Nacara is run without a command, it build the website

## 1.0.0-beta-015 - 2021-09-26

### Changed

* Fix #44: Move the "site metadata info" into a siteMetadata property in `nacara.config.json` (by @mabasic)
* Move the NPM package to be a pure ESM package

## 1.0.0-beta-014 - 2021-08-23

### Fixed

* Fix #80: Fix pages cache update, depending on the pages order, it could erase the "found" boolean

## 1.0.0-beta-013 - 2021-08-21

### Added

* Add `clean` command
* Fix #8: Add `favIcon` property to `nacara.config.json`
* Fix #70: Generate a .nojekyll file on production build
* Fix #96: Add partials support
* Fix #77: Add footer support
* Fix #2: Restart Nacara when changes to `nacara.config.json` are detected

### Changed

* Clean the output folder before `build` and `watch`

### Fixed

* Fix #22: Make navigation buttons display on top of each other when on mobile display
* Fix #67: Nacara crash if a folder under the source folder is empty

## 1.0.0-beta-012 - 2021-08-21

### Added

* Fix #68: Add `serve` command
* Add `--version` argument

### Changed

* Fix #69: Change the default source folder from `docsrc` to `docs`
* Fix #71: Change the default output folder from `docs` to `docs_deploy`
* Change `--watch` argument to `watch` command

## 1.0.0-beta-011 - 2021-08-21

### Added

* Load `@babel/register` if a `babel.config.json` is found.

    It is up to you to install `@babel/register` and the required presets.

### Changed

* Start WebServer after setting up the websocket

## 1.0.0-beta-010 - 2021-08-19

### Fixed

* Answer on websocket connection, because it was causing the websocket to be really slow to connect

## 1.0.0-beta-009 - 2021-08-19

### Changed

* Reword category to section to have it consistent everywhere.

    You need to replace `category` with `section` in both `menu.json` and `nacara.config.json`

* Wait only `200ms` instead of `2s` before considering a file stable. This makes Nacara detect changes faster and improve responsivenes

### Removed

* Remove `--verbose` option

## 1.0.0-beta-008 - 2021-08-18

### Fixed

* Include the `scripts` folder into the published package

## 1.0.0-beta-007 - 2021-08-18

### Added

* Add `$menu-list-spacing` SCSS variable

### Changed

* Make chokidar wait for stable file before notifying a change
* Fix #53: Remove live-server dependency instead use express and a custom implementation

### Fixed

* Fix #62: Rework menu alignment and margin to have a better display

## 1.0.0-beta-006 - 2021-08-01

### Changed

* Relax Nacara requirements on npm engine from `7.13.0` to `7.0.0`

## 1.0.0-beta-005 - 2021-08-01

## 1.0.0-beta-004 - 2021-07-30

### Changed

* Publish `.fable` folder

## 1.0.0-beta-003 - 2021-07-30

### Changed

* Publish `.fable` folder

## 1.0.0-beta-002 - 2021-07-30

### Changed

* Publish `.fable` folder

## 1.0.0-beta-001 - 2021-07-29

### Added

* Add `excludeFromNavigation` property to all the layout allowing to opt-out a page from the Next / Previous button generation
* Recompute the known pages if the attributes of a page change.

This ensure that the information using the attributes like the menu or the next / previous buttons are up to date on all the page

* Add copy button to code blocks
* Add `$textual-steps-color` color SCSS variable
* Create a NuGet package `Nacara.Core` which shares the type and some helpers between Nacara and the layouts project
* Add section support, a section is defined by being a folder under the root folder

    For example, this structure defined 2 sections.

    ```
    docsrc
    ├── changelogs
    │   ├── file.md
    ├── docs
    │   ├── file.md
    ├── index.md
    ```

* Add support for `menu.json` which allows to configure the menu per section

### Changed

* Change config file back to `nacara.config.json`
* Generate Next / Previous button on site compilation instead of using JavaScript at runtime
* Move the TOC inside the menu and remove the need for `[[toc]]` tag
* Upgrade to Bulma 0.9.3
* Make the layout as a standalone npm package `nacara-layout-standard`
* Move the `Changelog.fs` from `Nacara` to the `Layout` project and rename it `ChangelogParser.fs`
* Upgrade to Fable 3

    Change `Model.DocFiles` to use `JS.Map` instead of `Map` because it seems like Fable 3 does something different and break the `Map` usage from the layout project

* Remove the material like button
* Unknown files are now copied to the output folder making it easier to add static assets like images
* Generate compressed CSS from SCSS/SASS files when in build mode
* Rewrite the base-url-middleware to redirect on exact match and use temporary redirection to avoid caching from the browser
* Rewrite the base-url-middleware to redirect on exact match and use temporary redirection to avoid caching from the browser
* Changed `version` in `paket.dependencies` to `5.258.1` so that it matches the version in `.config/dotnet-tools.json`. This fixes the issue which required having .NET DSK version `'2.1.0`.
* Use paket from CLI tool

### Fixed

* Changelog parser and layout now correctly understand items indeed under a list item

    Before, this text would have been displayed as quoted or you would have had to un-indent to force Nacara to display it "correctly" in the browser.

### Removed

* Removed files `fake.cmd` and `fake.sh` because they are no longer needed.
* Removed unused code from `build.fsx`.
* Remove fake and use a Makefile instead.
* Remove plugins property from the config file. Now the layouts can extends their own version of markdown-it to meet their needs
* Remove the `menu` property from the config file

Right now Windows user needs to install make or use Gitpod. In the future, a `make.bat` will be available but I don't have time to add it right now.



## 0.4.1 - 2021-05-10

### Fixed

* When Nacara encounter an unknown file in build mode skip it and trigger the next process.

It was stopping the whole generation causing problem is the user hosted some PNG files in the source folder for example.

### Changed

* Make the text in the textual steps use a normal `font-weight`. It was making it hard to read the text when on a white background
* Change the font-size to `16px` to improve readibility and accessibility


## 0.4.0 - 2021-05-02

### Changed

* Change the config file name from `nacara.js` is now `nacara.config.js`

    It seems like the new version of `npm exec` and `npx` execute/open `nacara.js` when executing `npx nacara`. Probably because the file as the same name as the package 🤷‍♂️

### Fixed

* Make the `pre` element horizontal scrollable. This avoid to have the whole page having an horizontal scroll when a code snippet is a bit large

## 0.3.0 - 2021-04-29

### Fixed

* Fix #21: Make sure that the user can't click on the material like button when they are hidden

## 0.2.1 - 2019-09-06

### Added

* User can now click on the navbar brand to go "index" page.

    The "index" page is calculated as follow `config.url + config.baseUrl`

### Fixed

* If no menu found on the page, hide the Next & Previous button
* Secure access to `.mobile-menu .menu-trigger` to avoid error in the console if no menu found
* Fix menu scroll on touch display

## 0.2.0 - 2019-09-05

### Added

* Layout system has been added

    User can add `layouts` node to `nacara.js`, it takes an object.

    Example:

    ```js
    {
        default: standard.Default,
        changelog: standard.Changelog
    }
    ```

* Responsive mode is now implemented supported in the standard layout

<img style="width: 75%; margin-left: 12.5%;" src="/Nacara/assets/changelog/0_2_0/desktop_preview.png" alt="Desktop preview">
<br/>
<br/>
<div class="has-text-weight-bold has-text-centered">Desktop preview</div>
<br/>

<img style="width: 75%; margin-left: 12.5%;" src="/Nacara/assets/changelog/0_2_0/touch_preview.gif" alt="Touchscreen preview">
<br/>
<br/>
<div class="has-text-weight-bold has-text-centered">Desktop preview</div>
<br/>

* Markdown plugins are now configurable via `plugins.markdown` in `nacara.js`

    It take an array of object, the properties are:

    ```js
    {
        // Path to pass to `require` function can be:
        // - a npm module
        // - a file path (local plugin)
        path: 'markdown-it-container',
        // Optional array of arguments
        args: [
            'warning',
            mdMessage("warning")
        ]
    }
    ```

    Example:

    ```js
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
                path: path.join(__dirname, './src/markdown-it-anchored.js')
            }
        ]
    }
    ```

* Build mode has been added to Nacara it active by default. You can start in watch mode by adding `--watch` or `-w` to the CLI
* Port server can be configured via `serverPort` in `nacara.js`
* Current section is now shown in the Table of Content
* Previous and Next navigation button are added at the bottom of the page
* Add a button to scroll to the top, this button is only displayed the page is scrolled
* Add *material like* menu when displayed on touchscreen (mobile & tablet)
* Make the anchors elements less visible
* Add possibility to have an **Edit** button at the top of the page

    Turn it on, by setting `editUrl` in `nacara.js`. The url should be the start of the url, the file path will be added when generating the page.

    Example: `https://github.com/MangelMaxime/Nacara/edit/master/docsrc`

### Changed

* Change config file format, `nacara.json` is now `nacara.js`
* Improve the navbar responsive support
* Transform the left menu into a breadcumb when on touchscreen

    Items with only icons will stay at the top of the navbar, while items with text (and icon) are displayed under the burger menu.

### Fixed

* If a grammar is not found it's possible that some of the snippet in a valid grammar failed

## 0.1.6 - 2019-04-19

### Fixed

* Fix generated URL for Windows

## 0.1.5 - 2019-04-19

### Fixed

* Make page id independant from the OS

## 0.1.4 - 2019-04-18

### Changed

* Remove `is-primary` class from the navbar.

    Please use the variable `$navbar-background-color` in order to customize it

## 0.1.3 - 2019-04-18

### Fixed

* Fix `nacara.scss`, user needs to provide Bulma in is own `style.scss` file

## 0.1.2 - 2019-04-17

### Added

* Add `cli.js` so nacara can be used as a CLI tool

## 0.1.1 - 2019-04-17

### Added

* Make `nacara` a "CLI" package

## 0.1.0 - 2019-04-17

### Added

* Initial release
