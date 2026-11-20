---
last_commit_released: 6602c4d64e2751e5457356f852a3fe963e7ec169
---

# Changelog

All notable changes to this project will be documented in this file.

This project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

This changelog is generated using [EasyBuild.ShipIt](https://github.com/easybuild-org/EasyBuild.ShipIt).

⚠ Only edit the front matter metadata at the top of this file. All other changes will be overwritten when a new release is created.

## 2.0.0-beta.7 - 2026-11-20

### 🚀 Features

* *(core)* Derived collections and site files ([7991142](https://github.com/MangelMaxime/Nacara/commit/7991142f0c0230e18bd4072098621d3a11aae286))
* *(core)* Default the source, output and 404 route ([43699e1](https://github.com/MangelMaxime/Nacara/commit/43699e14b311ee7fb45aabc98a625009d6477a63))
* *(core)* Typed pages, paging and taxonomies ([0561723](https://github.com/MangelMaxime/Nacara/commit/056172339e3ade93debf337c0af99e8664a01feb))
* *(core)* Leave drafts out of a build ([bfc625d](https://github.com/MangelMaxime/Nacara/commit/bfc625d4fd6b356bf52ebe0033862a0c9bf02503))
* *(core)* Page lookup and per-page layouts ([b0d72c9](https://github.com/MangelMaxime/Nacara/commit/b0d72c9cecf8662598e8459e594b36fd4c14a977))
* *(core)* Show build problems in the browser ([0561219](https://github.com/MangelMaxime/Nacara/commit/0561219380d0c1a4d15ba9efcabad72588e41812))
* *(core)* Tell a plugin which build it configures ([8e17783](https://github.com/MangelMaxime/Nacara/commit/8e177838cfa65af348fb5cb4f836a3defbe13363))
* *(markdown)* Size images and load them lazily ([fba46ec](https://github.com/MangelMaxime/Nacara/commit/fba46ec355b57ccd9f568fa943e36f421abbf749))

### 🐞 Bug Fixes

* *(core)* Show problem locations with forward slashes ([6602c4d](https://github.com/MangelMaxime/Nacara/commit/6602c4d64e2751e5457356f852a3fe963e7ec169))

<strong><small>[View changes on Github](https://github.com/MangelMaxime/Nacara/compare/4df579722848356abcfd265a61967830b991af33..6602c4d64e2751e5457356f852a3fe963e7ec169)</small></strong>

## 2.0.0-beta.6 - 2026-09-16

### 🚀 Features

* *(core)* Cap a code block's height with maxHeight= ([4df5797](https://github.com/MangelMaxime/Nacara/commit/4df579722848356abcfd265a61967830b991af33))

<strong><small>[View changes on Github](https://github.com/MangelMaxime/Nacara/compare/09a68e254780ca78d3c8463b489a11124920e50c..4df579722848356abcfd265a61967830b991af33)</small></strong>

## 2.0.0-beta.5 - 2026-09-12

### 🐞 Bug Fixes

* *(core)* Key the transform cache on the page data too ([09a68e2](https://github.com/MangelMaxime/Nacara/commit/09a68e254780ca78d3c8463b489a11124920e50c))

<strong><small>[View changes on Github](https://github.com/MangelMaxime/Nacara/compare/353cf65737afd57d96bee68d5236b20bfedad32a..09a68e254780ca78d3c8463b489a11124920e50c)</small></strong>

## 2.0.0-beta.4 - 2026-09-02

### 🚀 Features

* *(live-example)* Say what the compiler was given and asked for under --verbose ([353cf65](https://github.com/MangelMaxime/Nacara/commit/353cf65737afd57d96bee68d5236b20bfedad32a))

### 🐞 Bug Fixes

* *(link-validator)* Check the links the layout adds, not only the body ([052fba0](https://github.com/MangelMaxime/Nacara/commit/052fba037f7deae1c4c0dc39f4bcec10c0a54f4c))

<strong><small>[View changes on Github](https://github.com/MangelMaxime/Nacara/compare/a76c8db8191075ddab193d526b0a6f11e5435746..353cf65737afd57d96bee68d5236b20bfedad32a)</small></strong>

## 2.0.0-beta.3 - 2026-09-02

### 🚀 Features

* Re-release Nacore.Core to expose Decode.oneOf API ([a76c8db](https://github.com/MangelMaxime/Nacara/commit/a76c8db8191075ddab193d526b0a6f11e5435746))

<strong><small>[View changes on Github](https://github.com/MangelMaxime/Nacara/compare/6e538f915a2693829ce44463ce7444bd568b60d5..a76c8db8191075ddab193d526b0a6f11e5435746)</small></strong>

## 2.0.0-beta.2 - 2026-09-01

### 🚀 Features

* *(theme-default)* Let front matter put attributes on `main` ([30e042d](https://github.com/MangelMaxime/Nacara/commit/30e042d97e83f437dfae30a1a3cab384f481f089))
* *(theme-default)* Make `layout: bare` a canvas and expose `toc: false` ([33b041d](https://github.com/MangelMaxime/Nacara/commit/33b041d85926df96c7b971b541e2a9fb0ca9dd92))

### 🐞 Bug Fixes

* Normalize Path even more `./` and `/` normalize to the same RelativePath ([71b36c1](https://github.com/MangelMaxime/Nacara/commit/71b36c1a1ba23772d36707c58a266482f6a4b8ad))
* *(core)* Take a free port when the one asked for is busy on either loopback ([77a1ec3](https://github.com/MangelMaxime/Nacara/commit/77a1ec39ca22c887a84791bfae59bd02f46a1894))

<strong><small>[View changes on Github](https://github.com/MangelMaxime/Nacara/compare/e8ed7c5a308808771fe5062cef3dd9ea2179fcf9..6e538f915a2693829ce44463ce7444bd568b60d5)</small></strong>

## 2.0.0-beta.1 - 2026-08-28

### 🏗️ Breaking changes

* Nacara v3 ([9cb78aa](https://github.com/MangelMaxime/Nacara/commit/9cb78aace4a660da18bac00d4a19f45e1cd8e15a))
* *(core)* Hand a plugin command the site it runs in ([c913a6e](https://github.com/MangelMaxime/Nacara/commit/c913a6e43801df531971cc620383887370a81f4f))

### 🚀 Features

* *(core)* Clear the shared tool cache with clean --global ([5ba0791](https://github.com/MangelMaxime/Nacara/commit/5ba0791b1103ba98623db9cbf022c97e337ace4c))
* *(core)* Empty the output directory before a build ([96c8f18](https://github.com/MangelMaxime/Nacara/commit/96c8f18f58f7d54d86285a7eed9e71ae9a70b8a5))
* *(core)* Say what a build is spending its time on ([00299e1](https://github.com/MangelMaxime/Nacara/commit/00299e19691be931d1fc84279ba2edb8f58ae59d))

### 🐞 Bug Fixes

* *(core)* Write what is generated in LF, wherever it was generated ([d7efb67](https://github.com/MangelMaxime/Nacara/commit/d7efb670bfceaaf8466da1c34ce827032f8962aa))
* *(core)* Stop a build's own dot-directory caches from triggering it ([5b9d6c6](https://github.com/MangelMaxime/Nacara/commit/5b9d6c625988de405d0e6d0d755b170fb33ccb70))
* *(core)* Re-render a page when the assets it links change ([f70734f](https://github.com/MangelMaxime/Nacara/commit/f70734f95d8ade29c8d2a69902911a6584b9b85f))

<strong><small>[View changes on Github](https://github.com/MangelMaxime/Nacara/compare/448dbdc8db3f53f81e320099e9c992b69e336395..e8ed7c5a308808771fe5062cef3dd9ea2179fcf9)</small></strong>

## 0.0.0
