@echo off
setlocal EnableDelayedExpansion
set DesignTimeBuildOutputPane=1
set BatchFile=%0

if not exist "%VSINSTALLDIR%" (
  echo This script needs to be run from an elevated Visual Studio 2017 developer command prompt.
  exit /b 1
)

:ParseArguments
if "%1" == "" goto :DoneParsing
if /I "%1" == "/?" call :Usage && exit /b 1
if /I "%1" == "enable" set DesignTimeBuildOutputPane=1&&shift&& goto :ParseArguments
if /I "%1" == "disable" set DesignTimeBuildOutputPane=0&&shift&& goto :ParseArguments
call :Usage && exit /b 1
:DoneParsing

if "%DesignTimeBuildOutputPane%" == "1" (echo Enabling design-time build logging...)
if "%DesignTimeBuildOutputPane%" == "0" (echo Disabling design-time build logging...)

set InstalledVSInstances=%ProgramData%\Microsoft\VisualStudio\Packages\_Instances
for /F %%d in ('dir /B /D "%InstalledVSInstances%"') do (

    for %%x in (%%d %%dExp %%dRoslynDev) do (

        set VSInstance=%VisualStudioVersion%_%%x%
        set VSRegistryHive=%LOCALAPPDATA%\Microsoft\VisualStudio\!VSInstance!\privateregistry.bin

        if exist "!VSRegistryHive!" (
    
            echo    !VSInstance!

            REM Import this VS instance's private registry into HKLM so that we can manipulate it
            reg load HKLM\VS !VSRegistryHive! > nul || goto :Fail

            REM Set the registry key
            reg add HKLM\VS\Software\Microsoft\VisualStudio\!VSInstance!\CPS\ /v "Design-time Build Logging" /t REG_DWORD /d %DesignTimeBuildOutputPane% /f > nul || goto :Fail

            REM Make sure we unload it, otherwise, VS will never start again
            reg unload HKLM\VS > nul || goto :Fail
        )
    )
)
echo.
echo Done.
exit /b 0

:Fail
echo Unable to open Visual Studio registry. Make sure you have all VS instances closed, and you are running from an elevated command prompt.
exit /b 1

:Usage
echo Usage: 
echo %BatchFile% [enable^|disable]
echo.
echo enable		Turns on the design-time build output pane (default)
echo disable		Turns off the design-time build output pane
goto :eof
