# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased

### Added

* Add support for the partial inside the dropdowns

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
