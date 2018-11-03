@echo off
if "%1" == "-configuration" if "%3" == "-prepareMachine" (  
  mkdir "%~dp0..\artifacts\%2\tmp"
  mkdir "%~dp0..\artifacts\%2\log"
  
  echo Downloading dotnet ...  
  powershell -NoProfile -ExecutionPolicy Bypass -Command "((New-Object System.Net.WebClient).DownloadFile('https://download.visualstudio.microsoft.com/download/pr/45f93081-cdb4-41c1-8d8d-e6c3bbf2872b/62d6a598956fdfe585acb1f15268d930/dotnet-sdk-2.1.403-win-x64.exe', '%~dp0..\artifacts\%2\tmp\dotnet-sdk-2.1.403-win-x64.exe'))"
  
  echo Installing dotnet ...
  "%~dp0..\artifacts\%2\tmp\dotnet-sdk-2.1.403-win-x64.exe" /install /quiet /norestart /log "%~dp0..\artifacts\%2\log\cli_install.log"
)

powershell -ExecutionPolicy ByPass %~dp0Build.ps1 -restore -build -test -pack -ci %*
exit /b %ErrorLevel%
