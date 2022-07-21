@if not defined _echo @echo off
setlocal enabledelayedexpansion

@REM Exit if not running within Developer Command Prompt
if not defined VSINSTALLDIR (
    echo This command must be run within Developer Command Prompt.
    exit /b 1
)

@REM Turn off dotnet CLI logo
set DOTNET_NOLOGO=true

@REM https://stackoverflow.com/a/16144756/294804
msbuild %~dp0eng\Build.proj /m /warnAsError /noLogo /clp:Summary /bl:%~dp0artifacts\log\Build.binlog %*
exit /b %ERRORLEVEL%