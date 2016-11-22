@echo off
setlocal enabledelayedexpansion

set BatchFile=%0
set Root=%~dp0
set BuildConfiguration=Debug
set MSBuildTarget=Build
set NodeReuse=true
set MSBuildAdditionalArguments=/m

:ParseArguments
if "%1" == "" goto :DoneParsing
if /I "%1" == "/?" call :Usage && exit /b 1
if /I "%1" == "/debug" set BuildConfiguration=Debug&&shift&& goto :ParseArguments
if /I "%1" == "/release" set BuildConfiguration=Release&&shift&& goto :ParseArguments
if /I "%1" == "/rebuild" set MSBuildTarget=Rebuild&&shift&& goto :ParseArguments
if /I "%1" == "/restore" set MSBuildTarget=RestorePackages&&shift&& goto :ParseArguments
if /I "%1" == "/modernvsixonly" set MSBuildTarget=BuildModernVsixPackages&&shift&& goto :ParseArguments
if /I "%1" == "/no-node-reuse" set NodeReuse=false&&shift&& goto :ParseArguments
if /I "%1" == "/no-multi-proc" set MSBuildAdditionalArguments=&&shift&& goto :ParseArguments
call :Usage && exit /b 1
:DoneParsing

if not exist "%VSINSTALLDIR%" (
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

set BinariesDirectory=%Root%bin\%BuildConfiguration%\
set LogFile=%BinariesDirectory%Build.log
if not exist "%BinariesDirectory%" mkdir "%BinariesDirectory%" || goto :BuildFailed

msbuild /nologo /nodeReuse:%NodeReuse% /consoleloggerparameters:Verbosity=minimal /fileLogger /fileloggerparameters:LogFile="%LogFile%";verbosity=diagnostic /t:"%MSBuildTarget%" /p:Configuration="%BuildConfiguration%" "%Root%build\build.proj" %MSBuildAdditionalArguments%
if ERRORLEVEL 1 (
    echo.
    echo Build failed, for full log see %LogFile%.
    exit /b 1
)

echo.
echo Build completed successfully, for full log see %LogFile%
exit /b 0

:Usage
echo Usage: %BatchFile% [/debug^|/release] [/rebuild]
echo.
echo   /debug             Perform debug build (default)
echo   /release           Perform release build
echo   /rebuild           Perform a clean, then build
echo   /restore           Only restore nuget packages
echo   /no-node-reuse     Run msbuild with /nodeReuse=false, which affects performance
echo   /no-multi-proc     No multi-proc build, useful for diagnosing build logs
echo   /modernvsixonly    Only build modern vsman vsixes.
goto :eof

:BuildFailed
echo Build failed with ERRORLEVEL %ERRORLEVEL%
exit /b 1
