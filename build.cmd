@echo off
setlocal enabledelayedexpansion

set BatchFile=%0
set Root=%~dp0
set BuildConfiguration=Debug
set MSBuildBuildTarget=Build
set NodeReuse=true
set DeveloperCommandPrompt=%VS150COMNTOOLS%\VsDevCmd.bat
set MSBuildAdditionalArguments=/m
set RunTests=true
set DeployVsixExtension=true
set FileLoggerVerbosity=detailed
REM Turn on MSBuild async logging to speed up builds
set MSBUILDLOGASYNC=1 

:ParseArguments
if "%1" == "" goto :DoneParsing
if /I "%1" == "/?" call :Usage && exit /b 1
if /I "%1" == "/build" set MSBuildBuildTarget=Build&&shift&& goto :ParseArguments
if /I "%1" == "/rebuild" set MSBuildBuildTarget=Rebuild&&shift&& goto :ParseArguments
if /I "%1" == "/copy-artifacts" set CopyOutputArtifacts=true&&shift&& goto :ParseArguments
if /I "%1" == "/debug" set BuildConfiguration=Debug&&shift&& goto :ParseArguments
if /I "%1" == "/release" set BuildConfiguration=Release&&shift&& goto :ParseArguments
if /I "%1" == "/skiptests" set RunTests=false&&shift&& goto :ParseArguments
if /I "%1" == "/no-deploy-extension" set DeployVsixExtension=false&&shift&& goto :ParseArguments
if /I "%1" == "/no-node-reuse" set NodeReuse=false&&shift&& goto :ParseArguments
if /I "%1" == "/diagnostic" set FileLoggerVerbosity=diagnostic&&set MSBuildAdditionalArguments=&&shift&& goto :ParseArguments
call :Usage && exit /b 1
:DoneParsing

if not exist "%VS150COMNTOOLS%" (
  echo To build this repository, this script needs to be run from a Visual Studio 2017 RC developer command prompt.
  echo.
  echo If Visual Studio is not installed, visit this page to download:
  echo.
  echo https://www.visualstudio.com/vs/visual-studio-2017-rc/
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
if not exist "%BinariesDirectory%" mkdir "%BinariesDirectory%" || goto :BuildFailed

REM We build Restore, Build and BuildModernVsixPackages in different MSBuild processes.
REM Restore because we want to control the verbosity due to https://github.com/NuGet/Home/issues/4695.
REM BuildModernVsixPackages because under MicroBuild, it has a dependency on a dll with the same 
REM version but different contents than the legacy VSIX projects.
for %%T IN (Restore %MSBuildBuildTarget%, BuildModernVsixPackages) do (
  
  set LogFile=%BinariesDirectory%%%T.log
  set LogFiles=!LogFiles!!LogFile! 
  
  if "%%T" == "Restore" (
    set ConsoleLoggerVerbosity=quiet
    echo   Restoring packages for ProjectSystem (this may take some time^)
  ) else (
    set ConsoleLoggerVerbosity=minimal
  )

  set BuildCommand=msbuild /nologo /warnaserror /nodeReuse:%NodeReuse% /consoleloggerparameters:Verbosity=!ConsoleLoggerVerbosity! /fileLogger /fileloggerparameters:LogFile="!LogFile!";verbosity=%FileLoggerVerbosity% /t:"%%T" /p:Configuration="%BuildConfiguration%" /p:RunTests="%RunTests%" /p:DeployVsixExtension="%DeployVsixExtension%" "%Root%build\build.proj" %MSBuildAdditionalArguments%
  if "%FileLoggerVerbosity%" == "diagnostic" (
    echo !BuildCommand!
  )
  
  !BuildCommand!

  if ERRORLEVEL 1 (
    echo.
    call :PrintColor Red "Build failed, for full log see !LogFile!."
    exit /b 1
  )
)

REM Run copy as a final step after all the product components are built
if /I "%CopyOutputArtifacts%" == "true" (
  call build\Scripts\CopyOutput.cmd
)

echo.
call :PrintColor Green "Build completed successfully, for full logs see %LogFiles%"
exit /b 0

:Usage
echo Usage: %BatchFile% [/build^|/rebuild] [/debug^|/release] [/no-node-reuse] [/no-multi-proc] [/skiptests] [/no-deploy-extension]
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
echo     /copy-artifacts         Copy the nugets to CoreXT Nuget share and VS manifests to separate folder to enable vsdrop upload
echo     /diagnostic             Turns on diagnostic logging and turns off multi-proc build, useful for diagnosing build logs
echo     /no-node-reuse          Prevents MSBuild from reusing existing MSBuild instances,
echo                             useful for avoiding unexpected behavior on build machines
echo     /no-deploy-extension    Does not deploy the VSIX extension when building the solution
echo     /skiptests              Does not run unit tests
goto :eof

:BuildFailed
call :PrintColor Red "Build failed with ERRORLEVEL %ERRORLEVEL%"
exit /b 1

:PrintColor
"%Windir%\System32\WindowsPowerShell\v1.0\Powershell.exe" write-host -foregroundcolor %1 "'%2'"