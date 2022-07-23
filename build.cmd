@if not defined _echo @echo off
setlocal enabledelayedexpansion

@REM Turn off dotnet CLI logo
set DOTNET_NOLOGO=true

@REM Sets the output of GetMSBuildPath.ps1 to the MSBuildPath environment variable
@REM https://stackoverflow.com/a/3417728/294804
FOR /F "usebackq delims=" %%v IN (`powershell -NonInteractive -NoLogo -NoProfile -ExecutionPolicy Unrestricted -File "%~dp0eng\scripts\GetMSBuildPath.ps1" -versionJsonPath "%~dp0version.json"`) DO set "MSBuildPath=%%v"

if not defined MSBuildPath (
    echo Visual Studio must be installed to allow building via MSBuild.
    exit /b 1
)

@REM https://stackoverflow.com/a/16144756/294804
"%MSBuildPath%" %~dp0eng\Build.proj /m /warnAsError /noLogo /clp:Summary /bl:%~dp0artifacts\log\Build.binlog %*
exit /b %ERRORLEVEL%