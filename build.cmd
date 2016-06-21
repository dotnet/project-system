@echo off
setlocal enabledelayedexpansion

set BatchFile=%0
set Root=%~dp0
set BuildConfiguration=Debug
set MSBuildTarget=Build
set NodeReuse=true
set DeveloperCommandPrompt=%VS150COMNTOOLS%\VsDevCmd.bat
set MSBuildAdditionalArguments=/m

:ParseArguments
if "%1" == "" goto :DoneParsing
if /I "%1" == "/?" call :Usage && exit /b 1
if /I "%1" == "/debug" set BuildConfiguration=Debug&&shift&& goto :ParseArguments
if /I "%1" == "/release" set BuildConfiguration=Release&&shift&& goto :ParseArguments
if /I "%1" == "/rebuild" set MSBuildTarget=Rebuild&&shift&& goto :ParseArguments
if /I "%1" == "/no-node-reuse" set NodeReuse=false&&shift&& goto :ParseArguments
if /I "%1" == "/no-multi-proc" set MSBuildAdditionalArguments=&&shift&& goto :ParseArguments
call :Usage && exit /b 1
:DoneParsing

if not exist "%DeveloperCommandPrompt%" (
  echo In order to build this repository, you need Visual Studio "15" Preview installed.
  echo.
  echo Visit this page to download:
  echo.
  echo http://go.microsoft.com/fwlink/?LinkId=746567
  exit /b 1
)

if not exist "%VSSDK150Install%" (
  echo In order to build this repository, you need to modify your Visual Studio installation to include "Visual Studio Extensibility Tools".
  exit /b 1
)

call "%DeveloperCommandPrompt%" || goto :BuildFailed

set BinariesDirectory=%Root%bin\%BuildConfiguration%\
set LogFile=%BinariesDirectory%Build.log
if not exist "%BinariesDirectory%" mkdir "%BinariesDirectory%" || goto :BuildFailed

msbuild /nologo /nodeReuse:%NodeReuse% /consoleloggerparameters:Verbosity=minimal /fileLogger /fileloggerparameters:LogFile="%LogFile%";verbosity=detailed /t:"%MSBuildTarget%" /p:Configuration="%BuildConfiguration%" "%Root%build\build.proj" %MSBuildAdditionalArguments%
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
echo   /no-node-reuse     Run msbuild with /nodeReuse=false, which affects performance
echo   /no-multi-proc     No multi-proc build, useful for diagnosing build logs
goto :eof

:BuildFailed
echo Build failed with ERRORLEVEL %ERRORLEVEL%
exit /b 1
