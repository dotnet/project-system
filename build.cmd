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

:ParseArguments
if    "%1" == "" goto :DoneParsing
if /I "%1" == "/?" call :Usage && exit /b 1
if /I "%1" == "/build"                set OptBuild=$true  && set OptRebuild=$false && shift && goto :ParseArguments
if /I "%1" == "/rebuild"              set OptBuild=$false && set OptRebuild=$true  && shift && goto :ParseArguments
if /I "%1" == "/test"                 set OptTest=$true                            && shift && goto :ParseArguments
if /I "%1" == "/no-test"              set OptTest=$false                           && shift && goto :ParseArguments
if /I "%1" == "/integration"          set OptIntegrationTest=$true                 && shift && goto :ParseArguments
if /I "%1" == "/no-integration"       set OptIntegrationTest=$false                && shift && goto :ParseArguments
if /I "%1" == "/debug"                set BuildConfiguration=Debug                 && shift && goto :ParseArguments
if /I "%1" == "/release"              set BuildConfiguration=Release               && shift && goto :ParseArguments
if /I "%1" == "/deploy-extension"     set OptDeploy=$true                          && shift && goto :ParseArguments
if /I "%1" == "/no-deploy-extension"  set OptDeploy=$false                         && shift && goto :ParseArguments
if /I "%1" == "/diagnostic"           set OptLog=$true                             && shift && goto :ParseArguments
if /I "%1" == "/ci"                   set OptCI=$true && set PrepareMachine=$true  && shift && goto :ParseArguments
if /I "%1" == "/rootsuffix"           set PropRootSuffix=/p:RootSuffix=%2          && shift && shift && goto :ParseArguments
call :Usage && exit /b 1
:DoneParsing

powershell -ExecutionPolicy ByPass -Command "& """%Root%build\Build.ps1""" -configuration %BuildConfiguration% -restore -pack:$true -build:%OptBuild% -rebuild:%OptRebuild% -deploy:%OptDeploy% -test:%OptTest% -integrationTest:%OptIntegrationTest% -log:%OptLog% %PropRootSuffix% -ci:%OptCI% -prepareMachine:%OptPrepareMachine%"
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
echo   Configurations:
echo     /debug                    Perform debug build (default)
echo     /release                  Perform release build
echo.
echo   Build options:
echo     /diagnostic               Turns on logging to a binlog
echo     /rootsuffix ^<hive^>        Hive to use when deploying Visual Studio extensions (default is 'Exp')
echo     /[no-]deploy-extension    Deploy (default) or avoid deploying Visual Studio extensions
echo     /ci                       Configures a continuous integration build
goto :eof
