#!/usr/bin/env bash

# Set up the different project links
pushd src/Nacara
npm link
popd
pushd src/Layouts/Standard
npm link
popd

# Set up the link in at the root project
npm link nacara nacara-layout-standard
