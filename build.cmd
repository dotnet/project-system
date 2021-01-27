@if not defined _echo @echo off
setlocal enabledelayedexpansion

set BatchFile=%0
set Root=%~dp0

set BuildConfiguration=Debug
set RootSuffixCmdLine=
set OptBuild=true
set OptDiagnostic=false
set OptRebuild=false
set OptDeploy=true
set OptTest=true
set OptIntegrationTest=false
set OptCI=false
set OptSign=false
set OptIbc=false
set OptNodeReuse=true
set OptClearNuGetCache=false

:ParseArguments
if    "%1" == ""                                                                                    goto :DoneParsing
if /I "%1" == "/?"                                                                                  call :Usage && exit /b 1
if /I "%1" == "/build"                set "OptBuild=true"  && set "OptRebuild=false"                && shift && goto :ParseArguments
if /I "%1" == "/no-build"             set "OptBuild=false" && set "OptRebuild=false"                && shift && goto :ParseArguments
if /I "%1" == "/rebuild"              set "OptBuild=false" && set "OptRebuild=true"                 && shift && goto :ParseArguments
if /I "%1" == "/test"                 set "OptTest=true"                                            && shift && goto :ParseArguments
if /I "%1" == "/no-test"              set "OptTest=false"                                           && shift && goto :ParseArguments
if /I "%1" == "/integration"          set "OptIntegrationTest=true"                                 && shift && goto :ParseArguments
if /I "%1" == "/no-integration"       set "OptIntegrationTest=false"                                && shift && goto :ParseArguments
if /I "%1" == "/deploy"               set "OptDeploy=true"                                          && shift && goto :ParseArguments
if /I "%1" == "/no-deploy"            set "OptDeploy=false"                                         && shift && goto :ParseArguments
if /I "%1" == "/diagnostic"           set "OptDiagnostic=true"                                      && shift && goto :ParseArguments
if /I "%1" == "/no-diagnostic"        set "OptDiagnostic=false"                                     && shift && goto :ParseArguments
if /I "%1" == "/sign"                 set "OptSign=true"                                            && shift && goto :ParseArguments
if /I "%1" == "/no-sign"              set "OptSign=false"                                           && shift && goto :ParseArguments
if /I "%1" == "/ci"                   set "OptCI=true"     && set "OptNodeReuse=false"              && shift && goto :ParseArguments
if /I "%1" == "/no-ci"                set "OptCI=false"    && set "OptNodeReuse=true"               && shift && goto :ParseArguments
if /I "%1" == "/ibc"                  set "OptIbc=true"                                             && shift && goto :ParseArguments
if /I "%1" == "/no-ibc"               set "OptIbc=false"                                            && shift && goto :ParseArguments
if /I "%1" == "/clearnugetcache"      set "OptClearNuGetCache=true"                                 && shift && goto :ParseArguments
if /I "%1" == "/no-clearnugetcache"   set "OptClearNuGetCache=false"                                && shift && goto :ParseArguments
if /I "%1" == "/rootsuffix"           set "RootSuffixCmdLine=/p:RootSuffix=%2"                      && shift && shift && goto :ParseArguments
if /I "%1" == "/configuration"        set "BuildConfiguration=%2"                                   && shift && shift && goto :ParseArguments

call :Usage && exit /b 1
:DoneParsing

set LogFile=%Root%artifacts\%BuildConfiguration%\log\Build.binlog

REM The logging command-line needs to factor in build configuration, so calculate it after that's been determined
if "%OptDiagnostic%" == "true" (
    set LogCmdLine=/v:normal /bl:%LogFile%
) else (
    set LogCmdLine=/v:minimal
)

call "%Root%\build\script\SetVSEnvironment.cmd" || exit /b 1

msbuild %Root%build\proj\Build.proj /m /warnaserror /nologo /clp:Summary /nodeReuse:%OptNodeReuse% /p:Configuration=%BuildConfiguration% /p:Build=%OptBuild% /p:Rebuild=%OptRebuild% /p:Deploy=%OptDeploy% /p:Test=%OptTest% /p:IntegrationTest=%OptIntegrationTest% /p:Sign=%OptSign% /p:CIBuild=%OptCI% /p:EnableIbc=%OptIbc% /p:ClearNuGetCache=%OptClearNuGetCache% %LogCmdLine% %RootSuffixCmdLine%
set MSBuildErrorLevel=%ERRORLEVEL%

if "%OptDiagnostic%" == "true" if "%OptCI%" == "false" (
    start %LogFile%
)

exit /b %MSBuildErrorLevel%

:Usage
echo Usage: %BatchFile% [options]
echo.
echo   Build targets:
echo     /rebuild                  Perform a clean, then build
echo     /[no]-build               Perform a build (default) or not
echo.
echo   Test targets:
echo     /[no-]test                Run (default) or skip unit tests
echo     /[no-]integration         Run or skip (default) integration tests
echo.
echo   Build options:
echo     /[no-]diagnostic          Turns on or turns off (default) logging to a binlog
echo     /[no-]deploy              Deploy (default) or skip deploying Visual Studio extensions
echo     /[no-]sign                Sign (default) or skip signing build outputs
echo     /[no-]ci                  Turns on or turns off (default) a continuous integration build
echo     /[no-]ibc                 Turns on or turns off (default) IBC (OptProf) optimization data usage
echo     /[no-]clearnugetcache     Clears or skips clearing (default) NuGet package cache
echo     /rootsuffix ^<hive^>        Hive to use when deploying Visual Studio extensions (default is 'Exp')
echo     /configuration ^<config^>   Use Debug (default) or Release build configuration
goto :eof
