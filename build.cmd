@if not defined _echo @echo off
setlocal enabledelayedexpansion

set BatchFile=%0
set Root=%~dp0

set BuildConfiguration=Debug
set PropRootSuffix=
set OptBuild=$true
set OptRebuild=$false
set OptDeploy=$true
set OptTest=$true
set OptIntegrationTest=$false
set OptLog=$false
set OptCI=$false
set OptPrepareMachine=$false
set OptSign=$false

:ParseArguments
if    "%1" == "" goto :DoneParsing
if /I "%1" == "/?" call :Usage && exit /b 1
if /I "%1" == "/build"                set OptBuild=$true  && set OptRebuild=$false  && shift && goto :ParseArguments
if /I "%1" == "/rebuild"              set OptBuild=$false && set OptRebuild=$true   && shift && goto :ParseArguments
if /I "%1" == "/test"                 set OptTest=$true                             && shift && goto :ParseArguments
if /I "%1" == "/no-test"              set OptTest=$false                            && shift && goto :ParseArguments
if /I "%1" == "/integration"          set OptIntegrationTest=$true                  && shift && goto :ParseArguments
if /I "%1" == "/no-integration"       set OptIntegrationTest=$false                 && shift && goto :ParseArguments
if /I "%1" == "/deploy"               set OptDeploy=$true                           && shift && goto :ParseArguments
if /I "%1" == "/no-deploy"            set OptDeploy=$false                          && shift && goto :ParseArguments
if /I "%1" == "/diagnostic"           set OptLog=$true                              && shift && goto :ParseArguments
if /I "%1" == "/no-diagnostic"        set OptLog=$false                             && shift && goto :ParseArguments
if /I "%1" == "/sign"                 set OptSign=$true                             && shift && goto :ParseArguments
if /I "%1" == "/no-sign"              set OptSign=$false                            && shift && goto :ParseArguments
if /I "%1" == "/ci"                   set OptCI=$true && set PrepareMachine=$true   && shift && goto :ParseArguments
if /I "%1" == "/no-ci"                set OptCI=$false && set PrepareMachine=$false && shift && goto :ParseArguments
if /I "%1" == "/rootsuffix"           set PropRootSuffix=/p:RootSuffix=%2           && shift && shift && goto :ParseArguments
if /I "%1" == "/configuration"        set BuildConfiguration=%2                     && shift && shift && goto :ParseArguments

call :Usage && exit /b 1

:DoneParsing

powershell -ExecutionPolicy ByPass -Command "& """%Root%build\Build.ps1""" -configuration %BuildConfiguration% -restore -pack:$true -build:%OptBuild% -rebuild:%OptRebuild% -deploy:%OptDeploy% -test:%OptTest% -integrationTest:%OptIntegrationTest% -log:%OptLog% %PropRootSuffix% -ci:%OptCI% -prepareMachine:%OptPrepareMachine% -sign:%OptSign%"

exit /b %ERRORLEVEL%

:Usage
echo Usage: %BatchFile% [options]
echo.
echo   Build targets:
echo     /build                    Perform a build (default)
echo     /rebuild                  Perform a clean, then build
echo.
echo   Test targets:
echo     /[no-]test                Run (default) or skip unit tests
echo     /[no-]integration         Run or skip (default) integration tests
echo.
echo   Build options:
echo     /[no-]diagnostic          Turns on or turns off (default) logging to a binlog
echo     /[no-]deploy              Deploy (default) or skip deploying Visual Studio extensions
echo     /[no-]sign                Sign (default) or skip signing build outputs
echo     /[no-]ci                  Turns on (default) or turns off a continuous integration build
echo     /rootsuffix ^<hive^>        Hive to use when deploying Visual Studio extensions (default is 'Exp')
echo     /configuration ^<config^>   Use Debug (default) or Release build configuration
goto :eof
