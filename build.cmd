@echo off
setlocal enabledelayedexpansion

set BatchFile=%0
set Root=%~dp0
set BuildConfiguration=Debug
set DeveloperCommandPrompt=%ProgramFiles(x86)%\Microsoft Visual Studio 14.0\Common7\Tools\VsDevCmd.bat

:ParseArguments
if "%1" == "" goto :DoneParsing
if /I "%1" == "/?" call :Usage && exit /b 1
if /I "%1" == "/debug" set BuildConfiguration=Debug&&shift&& goto :ParseArguments
if /I "%1" == "/release" set BuildConfiguration=Release&&shift&& goto :ParseArguments
call :Usage && exit /b 1
:DoneParsing

if not exist "%DeveloperCommandPrompt%" (
  echo In order to build this respository, you need Visual Studio 2015 installed.
  echo.
  echo Visit this page to download:
  echo.
  echo http://www.visualstudio.com/en-us/downloads/visual-studio-2015-downloads-vs
  exit /b 1
)

call "%DeveloperCommandPrompt%" || goto :BuildFailed

set BinariesDirectory=%Root%binaries\%BuildConfiguration%\
set LogFile=%BinariesDirectory%Build.log
if not exist "%BinariesDirectory%" mkdir "%BinariesDirectory%" || goto :BuildFailed

msbuild /nologo /m /consoleloggerparameters:Verbosity=minimal /fileLogger /fileloggerparameters:LogFile="%LogFile%";verbosity=detailed /p:Configuration="%BuildConfiguration%" "%Root%build\build.proj"
if ERRORLEVEL 1 (
    echo.
    echo Build failed, for full log see %LogFile%.
    exit /b 1
)

echo.
echo Build completed sucessfully, for full log see %LogFile%
exit /b 0

:Usage
echo Usage: %BatchFile% [/debug^|/release]
echo.
echo   /debug   Perform debug build (default)
echo   /release Perform release build
goto :eof

:BuildFailed
echo Build failed with ERRORLEVEL %ERRORLEVEL%
exit /b 1
