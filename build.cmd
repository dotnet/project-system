@echo off
setlocal enabledelayedexpansion

set BatchFile=%0
set Root=%~dp0
set BuildConfiguration=Debug

:ParseArguments
if "%1" == "" goto :DoneParsing
if /I "%1" == "/?" call :Usage && exit /b 1
if /I "%1" == "/debug" set BuildConfiguration=Debug&&shift&& goto :ParseArguments
if /I "%1" == "/release" set BuildConfiguration=Release&&shift&& goto :ParseArguments
call :Usage && exit /b 1
:DoneParsing

set BinariesDirectory=%Root%binaries\%BuildConfiguration%\
if not exist "%BinariesDirectory%" mkdir "%BinariesDirectory%" || goto :BuildFailed

call "%ProgramFiles(x86)%\Microsoft Visual Studio 14.0\Common7\Tools\VsDevCmd.bat" || :BuildFailed

msbuild /nologo /m /consoleloggerparameters:Verbosity=minimal /fileLogger /fileloggerparameters:LogFile="%BinariesDirectory%Build.log";verbosity=detailed /p:Configuration="%BuildConfiguration%" "%Root%build\build.proj" || :BuildFailed

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
