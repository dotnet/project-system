@if not defined _echo @echo off
setlocal enabledelayedexpansion

set BatchFile=%0
set Root=%~dp0

set BuildConfiguration=Debug
set RootSuffixCmdLine=
set OptBuild=true
set OptDiagnostic=false
set OptRebuild=false
set OptTest=true
set OptCI=false
set OptNodeReuse=true

:ParseArguments
if    "%1" == ""                                                                                    goto :DoneParsing
if /I "%1" == "/?"                                                                                  call :Usage && exit /b 1
if /I "%1" == "/build"                set "OptBuild=true"  && set "OptRebuild=false"                && shift && goto :ParseArguments
if /I "%1" == "/no-build"             set "OptBuild=false" && set "OptRebuild=false"                && shift && goto :ParseArguments
if /I "%1" == "/rebuild"              set "OptBuild=false" && set "OptRebuild=true"                 && shift && goto :ParseArguments
if /I "%1" == "/test"                 set "OptTest=true"                                            && shift && goto :ParseArguments
if /I "%1" == "/no-test"              set "OptTest=false"                                           && shift && goto :ParseArguments
if /I "%1" == "/diagnostic"           set "OptDiagnostic=true"                                      && shift && goto :ParseArguments
if /I "%1" == "/no-diagnostic"        set "OptDiagnostic=false"                                     && shift && goto :ParseArguments
if /I "%1" == "/ci"                   set "OptCI=true"     && set "OptNodeReuse=false"              && shift && goto :ParseArguments
if /I "%1" == "/no-ci"                set "OptCI=false"    && set "OptNodeReuse=true"               && shift && goto :ParseArguments
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

call "%Root%\eng\scripts\SetVSEnvironment.cmd" || exit /b 1

set MSBuildDebugEngine=1
msbuild %Root%eng\Build.proj /m /warnaserror /nologo /clp:Summary /nodeReuse:%OptNodeReuse% /p:Configuration=%BuildConfiguration% /p:Build=%OptBuild% /p:Rebuild=%OptRebuild% /p:Test=%OptTest% /p:CIBuild=%OptCI% %LogCmdLine% %RootSuffixCmdLine%
set MSBuildErrorLevel=%ERRORLEVEL%

REM Runs the binlog after build.
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
echo.
echo   Build options:
echo     /[no-]diagnostic          Turns on or turns off (default) logging to a binlog
echo     /[no-]ci                  Turns on or turns off (default) a continuous integration build
echo     /rootsuffix ^<hive^>        Hive to use when deploying Visual Studio extensions (default is 'Exp')
echo     /configuration ^<config^>   Use Debug (default) or Release build configuration
goto :eof
