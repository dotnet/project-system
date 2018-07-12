@if not defined _echo @echo off
setlocal enabledelayedexpansion

set BatchFile=%0
set Root=%~dp0

set BuildConfiguration=Debug
set PropRootSuffix=
set OptBuild=$true
set OptRebuild=$false
set OptDeploy=$true
set OptDeployDeps=$false
set OptTest=$true
set OptIntegrationTest=$false
set OptLog=$false

:ParseArguments
if "%1" == "" goto :DoneParsing
if /I "%1" == "/?" call :Usage && exit /b 1
if /I "%1" == "/build" set OptBuild=$true&&set OptRebuild=$false&&shift&& goto :ParseArguments
if /I "%1" == "/rebuild" set OptBuild=$false&&set OptRebuild=$true&&shift&& goto :ParseArguments
if /I "%1" == "/debug" set BuildConfiguration=Debug&&shift&& goto :ParseArguments
if /I "%1" == "/release" set BuildConfiguration=Release&&shift&& goto :ParseArguments
if /I "%1" == "/skiptests" set OptTest=$false&&shift&& goto :ParseArguments
if /I "%1" == "/restore-only" set OptBuild=$false&&set OptDeploy=$false&&set OptTest=$false&&shift&& goto :ParseArguments
if /I "%1" == "/no-deploy-extension" set OptDeploy=$false&&shift&& goto :ParseArguments
if /I "%1" == "/diagnostic" set OptLog=$true&&shift&& goto :ParseArguments
if /I "%1" == "/integrationtests" set OptDeployDeps=$true&&set OptIntegrationTest=$true&&shift&& goto :ParseArguments
if /I "%1" == "/rootsuffix" set PropRootSuffix=/p:RootSuffix=%2&&shift&&shift&& goto :ParseArguments
call :Usage && exit /b 1
:DoneParsing

powershell -ExecutionPolicy ByPass %Root%build\Build.ps1 -configuration %BuildConfiguration% -restore -deployDeps:%OptDeployDeps% -build:%OptBuild% -rebuild:%OptRebuild% -deploy:%OptDeploy% -test:%OptTest% -integrationTest:%OptIntegrationTest% -log:%OptLog% %PropRootSuffix%
exit /b %ERRORLEVEL%

:Usage
echo Usage: %BatchFile% [/build^|/rebuild] [/debug^|/release] [/skiptests] [/no-deploy-extension] [/diagnostic] [/integrationtests] [/rootsuffix hive]
echo.
echo   Build targets:
echo     /build                  Perform a build (default)
echo     /rebuild                Perform a clean, then build
echo.
echo   Configurations:
echo     /debug                  Perform debug build (default)
echo     /release                Perform release build
echo.
echo   Build options:
echo     /restore-only           Restore dependencies only
echo     /skiptests              Does not run unit tests
echo     /diagnostic             Turns on logging to a binlog
echo     /rootsuffix             Visual Studio hive to deploy VSIX extensions to (default is ProjectSystem)
echo     /no-deploy-extension    Does not deploy VSIX extensions when building the solution
echo     /integrationtests       Runs integration tests
goto :eof
