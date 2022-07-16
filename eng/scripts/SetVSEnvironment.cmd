@if not defined _echo @echo off

REM Configures the build environment to be able to build the tree
REM
REM Downloads VSWhere, uses it to find a compatible Visual Studio and call a developer prompt to set the environment.

FOR /F "USEBACKQ delims=" %%i IN (`powershell -NonInteractive -NoLogo -NoProfile -Command "([xml](Get-Content %~dp0\..\import\Versions.props)).Project.PropertyGroup.MinimumRequiredVSVersion"`) DO SET RequiredVSVersion=%%i

REM Are we already in Developer Command Prompt?
if defined VSINSTALLDIR (
    exit /b 0
)

if not exist "%TEMP%\vswhere.exe" (
  echo Downloading VSWhere so that we can find Visual Studio...
  powershell -NonInteractive -NoLogo -NoProfile -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; $pp = $ProgressPreference; $ProgressPreference = 'SilentlyContinue'; Invoke-WebRequest https://github.com/microsoft/vswhere/releases/download/2.8.4/vswhere.exe -OutFile $env:TEMP\vswhere.exe; $ProgressPreference = $pp" || (
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
  echo See https://github.com/dotnet/project-system/blob/main/docs/repo/getting-started.md for more information.
  exit /b 1
)

REM Turn off Developer Command Prompt logo
set __VSCMD_ARG_NO_LOGO=yes
call "%DeveloperCommandPrompt%"
