@if not defined _echo @echo off

REM Configures the build environment to be able to build the tree
REM
REM Downloads VSWhere, uses it to find a compatible Visual Studio and call a developer prompt to set the environment.

set RequiredVSVersion=16.0

REM Are we already in Developer Command Prompt?
if defined VSINSTALLDIR (
    exit /b 0
)

if not exist "%TEMP%\vswhere.exe" (
  echo Downloading VSWhere so that we can find Visual Studio...
  powershell -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest https://github.com/microsoft/vswhere/releases/download/2.6.7/vswhere.exe -OutFile $env:TEMP\vswhere.exe" || (
    echo Failed to download, check your internet connection.
    exit /b 1
  )
)

REM Find Visual Studio that suits our needs
FOR /F "tokens=* USEBACKQ" %%F IN (`%TEMP%\vswhere.exe -all -latest -prerelease -version %RequiredVSVersion% -property installationPath -requires Microsoft.Component.MSBuild`) DO (
  SET DeveloperCommandPrompt=%%F\Common7\Tools\VsDevCmd.bat
)

if not exist "%DeveloperCommandPrompt%" (
  echo To build this repository, Visual Studio %RequiredVSVersion% must be installed.
  echo.
  echo See https://github.com/dotnet/project-system/blob/master/docs/repo/getting-started.md for more information.
  exit /b 1
)

REM Turn off Developer Command Prompt logo
set __VSCMD_ARG_NO_LOGO=yes
call "%DeveloperCommandPrompt%"
