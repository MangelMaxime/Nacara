#!/usr/bin/env bash

# Set up nacara link
pushd src/Nacara
npm unlink
popd
npm unlink --no-save nacara

# Set up nacara-layout-standard link
pushd src/Layouts/Standard
npm unlink
popd
npm unlink --no-save nacara-layout-standard
