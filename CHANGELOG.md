# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased

### Added

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

```
{
    default: standard.Default,
    changelog: standard.Changelog
}
```

* Responsive mode is now implemented supported in the standard layout

<img style="width: 75%; margin-left: 12.5%;" src="/Nacara/public/changelog/0_2_0/desktop_preview.png" alt="Desktop preview">
<br/>
<br/>
<div class="has-text-weight-bold has-text-centered">Desktop preview</div>
<br/>

<img style="width: 75%; margin-left: 12.5%;" src="/Nacara/public/changelog/0_2_0/touch_preview.gif" alt="Touchscreen preview">
<br/>
<br/>
<div class="has-text-weight-bold has-text-centered">Desktop preview</div>
<br/>

* Markdown plugins are now configurable via `plugins.markdown` in `nacara.js`

It take an array of object, the properties are:

```
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

```
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
* Previoux and Next navigation button are added at the bottom of the page
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
