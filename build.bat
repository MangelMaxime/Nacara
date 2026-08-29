@echo off
rem Everything this repository is built with. `build.bat --help` lists it.
cd /d "%~dp0"
dotnet tool restore
dotnet run --project build -- %*
exit /b %errorlevel%
