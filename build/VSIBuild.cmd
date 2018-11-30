@echo off

if "%1" == "-configuration" if "%3" == "-prepareMachine" (  
  mkdir "%~dp0..\artifacts\%2\tmp"
  mkdir "%~dp0..\artifacts\%2\log"
  
  echo Downloading dotnet ...  
  powershell -NoProfile -ExecutionPolicy Bypass -Command "((New-Object System.Net.WebClient).DownloadFile('https://download.microsoft.com/download/4/0/9/40920432-3302-47a8-b13c-bbc4848ad114/dotnet-sdk-2.1.302-win-x64.exe', '%~dp0..\artifacts\%2\tmp\dotnet-sdk-2.1.302-win-x64.exe'))"
  
  echo Installing dotnet ...
  "%~dp0..\artifacts\%2\tmp\dotnet-sdk-2.1.302-win-x64.exe" /install /quiet /norestart /log "%~dp0..\artifacts\%2\log\cli_install.log"
)

powershell -ExecutionPolicy ByPass %~dp0Build.ps1 -restore -build -integrationTest -ci -rootsuffix Exp %*
exit /b %ERRORLEVEL%
