# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased

### Added

* Add support for the `Partial` property to the `DropdownInfo`

## 1.0.0-beta-003 - 2021-09-28

### Fixed

* Attach `MarkdownToHtml` to `RendererContext` class

## 1.0.0-beta-002 - 2021-09-26

### Changed

* Fix #44: Move the "site metadata info" into a siteMetadata property in `nacara.config.json` (by @mabasic)
* Add `remarkPlugins` property to `nacara.config.json`
* Add `rehypePlugins` property to `nacara.config.json`

### Added

* `Navbar.tryFindWebsiteSectionLabelForPage` function which return the label of the navbar item corresponding to the given page
* Rework the `NavbarConfig`
    - The start section of the navbar now support both `LabelLink` and `Dropdown`
    - The end section of the navbar can now only contains links with label/icon intended for Github, Twitter, etc links.
* Fix #96: Add partials support

### Removed

* Remove `lightner` property from `nacara.config.json`

## 1.0.0-beta-001 - 2021-07-29

### Added

* Initial release
