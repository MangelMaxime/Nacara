# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased

## 1.6.0 - 2022-02-10

### Fixed

* Fix #157: Make the menu scrollable if it goes out of screen (by @64J0)

## 1.5.0 - 2021-12-07

### Fixed

* Fix #149: Fix anchor visibility and link display inside headers

## 1.4.2 - 2021-12-05

### Fixed

* Fix render of "Other" items category for the changelog

## 1.4.1 - 2021-12-05

### Fixed

* In the `standard` layout, center the content when on widescreen.

## 1.4.0 - 2021-12-02

### Added

* Add a `Improved` category to the changelog

### Fixed

* Fix #144: `toc: false` disabled the whole menu and not just the TOC
* Fix #145: Don't generate an empty `<li></li>` if the TOC contains no elements

### Improved

* Fix #148: Allow changelog items to be define outside of a category

    This allows support of RELEASE_NOTES files too.

* Optimize content size based on the display
* Don't generate empty line in the CHANGELOG page
* Improve the margin between the changelog items

### Changed

* Change the visual of the Changelog page

## 1.3.0 - 2021-11-30

### Added

* Addd `$nacara-navbar-dropdown-floating-max-width` SCSS variable

### Changed

* Make the floating dropdown able to return the text to new line automatically
* The dropdown description now accept an HTML string

### Removed

* Can't force new line using `\n` in the dropdown description

## 1.2.1 - 2021-11-09

### Fixed

* Make the menu-container scrollable on mobile

### Added

* Fix #108: If the dropdown correspond to the active page, we indicate it visually

## 1.2.0 - 2021-11-08

### Added

* We can disable the TOC generation from `standard` by setting `toc: false` in the front-matter

## 1.1.2 - 2021-11-08

### Fixed

* Fix missing `fable_modules` folder from `dist`

## 1.1.1 - 2021-11-07

### Fixed

* Fix the TOC parser to support minified HTML code too

## 1.1.0 - 2021-11-04

### Added

* Adapt to support Nacara 1.1.0

## 1.0.1 - 2021-10-28

### Changed

* Fix warning: Use of deprecated folder mapping "./" in the "exports"

## 1.0.0 - 2021-10-28

### Added

* Release v1.0.0
* Added ApiGen minimal style

## 1.0.0-beta-017 - 2021-10-26

### Fixed

* Add missing `has-footer` to the body element when a footer is added via the partials.

## 1.0.0-beta-016 - 2021-10-26

### Fixed

* Place correctly the footer even when there is not enought content to fill the whole page

### Changed

* In watch mode, general a local link for the brand item instead of redirecting the live website.

## 1.0.0-beta-015 - 2021-10-25

### Changed

* Simplify the `Page.Minimal.render` function because I was often forgetting to add the new arguments when using JavaScript
* Simplify the layout names

    * `nacara-standard` -> `standard`
    * `nacara-navbar-only` -> `navbar-only`
    * `nacara-changelog` -> `changelog`

### Added

* Add `api` layout

## 1.0.0-beta-014 - 2021-09-30

### Added

* Add support for the partial inside the dropdowns
* Add support for `toc` front-matter which allows to customize the Headers interval

### Fixed

* Improve the hover behavior when using a fullwidth dropdown. Before this fix, the user needed to be really quick to access the dropdown content because of the gap between the Navbar Item and the Dropdown body.

## 1.0.0-beta-013 - 2021-09-28

### Fixed

* Re-try fix exports so people can import the distributes files for their own custom layouts

## 1.0.0-beta-012 - 2021-09-28

### Added

* Improve reloading system to reload only if the change concern the current page.

    This avoid reloading the page before it is ready when regenerating a lot of pages.

### Fixed

* Force table cells to break words on mobile if needed allowing for a better display
* Try fix exports so people can import the distributes files for their own custom layouts

## 1.0.0-beta-011 - 2021-09-26

### Fixed

* Add files from `js` folder to the published package

## 1.0.0-beta-010 - 2021-09-26

### Added

* PR #87: Add breadcrumb on desktop (by @mabasic)
* PR #87: Add current section name in the navbar on mobile too
* Fix #90: Complete rework of the navbar behavior
    - Added support for `Dropdown`
    - Better mobile support
* Fix #96: Add partials support
* Fix #77: Add footer support

### Changed

* PR #86: Make the Edit button a bit more discrete (by @mabasic)
* PR #85: Change the Navigation buttons, to be styled as "Fat buttons" now (by @mabasic)
* Fix #91: The edit button is not aligned correctly with the breadcrumb text (by @mabasic)
* Fix #90: Us a different button style differentiate the navbar burger menu from the breadcrumb menu on mobile
* Fix #44: Move the "site metadata info" into a siteMetadata property in `nacara.config.json` (by @mabasic)
* Move the NPM package to be a pure ESM package
* Switch to `remark` and `rehype` for doing the markdown parsing

### Fixed

* Fix #112: If there is no menu or toc, center the page content

## 1.0.0-beta-009 - 2021-08-23

### Changed

* Fix typo in `date-disable-copy-button` attributes. It has been renamed to `data-disable-copy-button="true"`

## 1.0.0-beta-008 - 2021-08-21

### Added

* Fix #8: Generate a `favIcon` tag if the property is set in `nacara.config.json`

### Fixed

* Fix #62: Improve menu display when using a combination of item/section

## 1.0.0-beta-007 - 2021-08-19

### Changed

* Move the fix "margin of p element inside of list element" to the changelog layout only

## 1.0.0-beta-006 - 2021-08-18

### Changed

* Fix #53: Include `nacara/scripts/live-reload.js` when using Nacara in watch mode

## 1.0.0-beta-005 - 2021-08-05

### Fixed

* PR #56: Add responsive metadata so the website is rendered correctly on mobile

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

* Initial release as a standalone package
