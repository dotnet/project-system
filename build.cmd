@if not defined _echo @echo off
setlocal enabledelayedexpansion

@REM Show the command line arguments available if /? is provided.
if /I "%1" == "/?" call :Usage && exit /b 1

@REM Turn off dotnet CLI logo
set DOTNET_NOLOGO=true

@REM Sets the output of GetMSBuildPath.ps1 to the MSBuildPath environment variable.
@REM https://stackoverflow.com/a/3417728/294804
FOR /F "usebackq delims=" %%v IN (`powershell -NonInteractive -NoLogo -NoProfile -ExecutionPolicy Unrestricted -File "%~dp0eng\scripts\GetMSBuildPath.ps1"`) DO set "MSBuildPath=%%~v"

if not defined MSBuildPath (
    echo Visual Studio must be installed to allow building via MSBuild.
    exit /b 1
)

@REM The configuration is not known at this point. It could either use the default from Build.proj or be passed in as an MSBuild argument.
set "BinlogPath=%~dp0artifacts\Build.binlog"
@REM https://stackoverflow.com/a/16144756/294804
"%MSBuildPath%" "%~dp0eng\Build.proj" /m /warnAsError /noLogo /clp:Summary /bl:"%BinlogPath%" %*
set MSBuildErrorLevel=%ERRORLEVEL%

@REM Move the binlog into the appropriate configuration directory.
if exist "%BinlogPath%" (
    @REM Finds the last modified directory in the artifacts folder.
    @REM https://stackoverflow.com/a/36545652/294804
    for /f "delims=" %%a in ('dir /b /a:d /o:-d /t:w "%~dp0artifacts\"') do (
        set "ConfigurationDirectory=%%~a"
        goto :break
    )
    :break
    set "LogDirectory=%~dp0artifacts\%ConfigurationDirectory%\log\"
    if not exist "%LogDirectory%" (
        mkdir "%LogDirectory%"
    )
    move "%BinlogPath%" "%LogDirectory%"
)

exit /b %MSBuildErrorLevel%

:Usage
echo Usage: build.cmd [options]
echo.
echo   For Projects:
echo     /p:SrcProjects=[true or false]     Includes the projects within the src directory. Default: true
echo     /p:TestProjects=[true or false]    Includes the projects within the tests directory. Default: true
echo     /p:SetupProjects=[true or false]   Includes the projects within the setup directory. Default: true
echo.
echo   For Targets:
echo     /p:Restore=[true or false]         Runs the Restore target to acquire project dependencies. Default: true
echo     /p:Build=[true or false]           Runs the Build target to compile the projects into assemblies. Default: true
echo     /p:Rebuild=[true or false]         Runs the Rebuild target which cleans and builds the projects. Default: false
echo     /p:Test=[true or false]            Runs the Test target to execute the xUnit test projects. Default: true
echo     /p:Pack=[true or false]            Runs the Pack target to bundle the projects into NuGet packages. Default: true
goto :eof