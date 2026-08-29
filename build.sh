#!/usr/bin/env sh
# Everything this repository is built with. `./build.sh --help` lists it.
cd "$(dirname "$0")" || exit 1
dotnet tool restore
exec dotnet run --project build -- "$@"
