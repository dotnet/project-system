@echo off
setlocal enabledelayedexpansion

set BatchFile=%0
set Root=%~dp0
set BuildConfiguration=Debug
set MSBuildTarget=Build
set NodeReuse=true
set DeveloperCommandPrompt=%VS150COMNTOOLS%\VsDevCmd.bat
set MSBuildAdditionalArguments=/m
set DeployVsixExtension=true
set Solution=%Root%\ProjectSystemTools.sln
set OnlyRestore=false
set SignType=public
set OfficialBuild=false

:ParseArguments
if "%1" == "" goto :DoneParsing
if /I "%1" == "/?" call :Usage && exit /b 1
if /I "%1" == "/debug" set BuildConfiguration=Debug&&shift&& goto :ParseArguments
if /I "%1" == "/release" set BuildConfiguration=Release&&shift&& goto :ParseArguments
if /I "%1" == "/rebuild" set MSBuildTarget=Rebuild&&shift&& goto :ParseArguments
if /I "%1" == "/restore" set OnlyRestore=true&&shift&& goto :ParseArguments
if /I "%1" == "/no-deploy-extension" set DeployVsixExtension=false&&shift&& goto :ParseArguments
if /I "%1" == "/no-node-reuse" set NodeReuse=false&&shift&& goto :ParseArguments
if /I "%1" == "/no-multi-proc" set MSBuildAdditionalArguments=&&shift&& goto :ParseArguments
if /I "%1" == "/official-build" set SignType=real&&set OfficialBuild=true&&shift&& goto :ParseArguments
call :Usage && exit /b 1
:DoneParsing

if not exist "%VS150COMNTOOLS%" (
  echo To build this repository, this script needs to be run from a Visual Studio 2017 developer command prompt.
  echo.
  echo If Visual Studio is not installed, visit this page to download:
  echo.
  echo https://www.visualstudio.com/vs/visual-studio-2017/
  exit /b 1
)

if not exist "%VSSDK150Install%" (
  echo To build this repository, you need to modify your Visual Studio installation to include the "Visual Studio extension development" workload.
  exit /b 1
)

if "%VisualStudioVersion%" == "" (
  REM In Jenkins and MicroBuild, we set VS150COMNTOOLS and VSSDK150Install to point to where VS is installed but don't launch in a developer prompt
  call "%DeveloperCommandPrompt%" || goto :BuildFailed
)

set BinariesDirectory=%Root%bin\%BuildConfiguration%\
set RestoreLogFile=%BinariesDirectory%Restore.log
set LogFile=%BinariesDirectory%Build.log
if not exist "%BinariesDirectory%" mkdir "%BinariesDirectory%" || goto :BuildFailed

echo.
echo Restoring packages
msbuild /nologo /nodeReuse:%NodeReuse% /consoleloggerparameters:Verbosity=minimal /fileLogger /fileloggerparameters:LogFile="%RestoreLogFile%";verbosity=diagnostic /t:Restore /p:Configuration="%BuildConfiguration%" %Solution% %MSBuildAdditionalArguments%
if ERRORLEVEL 1 (
    echo.
    call :PrintColor Red "Restore failed, for full log see %RestoreLogFile%."
    exit /b 1
)

echo.
call :PrintColor Green "Restore completed successfully, for full log see %RestoreLogFile%"

if "%OnlyRestore%" == "true" (
    exit /b 0
)

echo.
echo Building solution
msbuild /nologo /nodeReuse:%NodeReuse% /consoleloggerparameters:Verbosity=minimal /fileLogger /fileloggerparameters:LogFile="%LogFile%";verbosity=diagnostic /t:"%MSBuildTarget%" /p:Configuration="%BuildConfiguration%" /p:DeployVsixExtension="%DeployVsixExtension%" /p:SignType="%SignType%" %Solution% %MSBuildAdditionalArguments%
if ERRORLEVEL 1 (
    echo.
    call :PrintColor Red "Build failed, for full log see %LogFile%."
    exit /b 1
)

echo.
call :PrintColor Green "Build completed successfully, for full log see %LogFile%"

:Signing
if NOT "%OfficialBuild%" == "true" (
    exit /b 0
)

echo.
echo Signing binaries

REM Respect the %NUGET_PACKAGES% environment variable if set, as that's where nuget will restore to
set NuGetHome=%NUGET_PACKAGES%
if "%NuGetHome%" == "" (
    REM If it's not set, the nuget cache is in the user's home dir
    set NuGetHome=%UserProfile%\.nuget\packages
)
set SignTool="%NuGetHome%\roslyntools.microsoft.signtool\0.3.1-beta\tools\SignTool.exe"

%SignTool% -config "%Root%build\Signing\SignToolConfig.json" -msbuildPath "%VS150COMNTOOLS%..\..\MSBuild\15.0\Bin\msbuild.exe" "%Root%bin\%BuildConfiguration%"
if ERRORLEVEL 1 (
    echo.
    call :PrintColor Red "Signing failed"
    exit /b 1
)

call :PrintColor Green "Signing completed. See output Binaries in %Root%build\%BuildConfiguration%"
exit /b 0

:Usage
echo Usage: %BatchFile% [/rebuild^|/restore^|/modernvsixonly] [/debug^|/release] [/no-node-reuse] [/no-multi-proc] [/no-deploy-extension]
echo.
echo   Build targets:
echo     /rebuild                 Perform a clean, then build
echo     /restore                 Only restore NuGet packages
echo.
echo   Build options:
echo     /debug                   Perform debug build (default)
echo     /release                 Perform release build
echo     /no-node-reuse           Prevents MSBuild from reusing existing MSBuild instances,
echo                              useful for avoiding unexpected behavior on build machines
echo     /no-multi-proc           No multi-proc build, useful for diagnosing build logs
echo     /no-deploy-extension     Does not deploy the VSIX extension when building the solution
echo     /official-build          Run the full signed build.
goto :eof

:BuildFailed
call :PrintColor Red "Build failed with ERRORLEVEL %ERRORLEVEL%"
exit /b 1

:PrintColor
"%Windir%\System32\WindowsPowerShell\v1.0\Powershell.exe" write-host -foregroundcolor %1 "'%2'"