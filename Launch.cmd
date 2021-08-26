@echo off
setlocal EnableDelayedExpansion

set BatchFile=%0
set Root=%~dp0
if "%ROOTSUFFIX%"=="" (
   set ROOTSUFFIX=Exp
)

set VisualStudioXamlRulesDir=%Root%\artifacts\Debug\VSSetup\Rules\
set VisualBasicDesignTimeTargetsPath=%VisualStudioXamlRulesDir%Microsoft.VisualBasic.DesignTime.targets
set FSharpDesignTimeTargetsPath=%VisualStudioXamlRulesDir%Microsoft.FSharp.DesignTime.targets
set CSharpDesignTimeTargetsPath=%VisualStudioXamlRulesDir%Microsoft.CSharp.DesignTime.targets
set CPS_DiagnosticRuntime=1
set CPS_MetricsCollection=1

:ParseArguments
if "%1" == "" goto :DoneParsing
if /I "%1" == "/?" call :Usage && exit /b 1
if /I "%1" == "/rootsuffix" set ROOTSUFFIX=%2&&shift&&shift&& goto :ParseArguments
call :Usage && exit /b 1
:DoneParsing

for /f "tokens=*" %%i in ('where devenv.exe') do set DevEnvPath=%%i

echo Launching Visual Studio under the '!ROOTSUFFIX!' hive
echo %DevEnvPath%
devenv /rootsuffix !ROOTSUFFIX!
exit /b %ERRORLEVEL%

:Usage
echo Usage: %BatchFile% [/rootsuffix hive]
echo.
echo     /rootsuffix             Visual Studio hive to use when when launching (default is 'Exp')
goto :eof
