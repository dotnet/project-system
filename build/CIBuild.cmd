@echo off
if "%1" == "-configuration" if "%3" == "-prepareMachine" (  
  mkdir "%~dp0..\artifacts\%2\tmp"
  mkdir "%~dp0..\artifacts\%2\log"
)

powershell -ExecutionPolicy ByPass %~dp0Build.ps1 -restore -build -test -pack -ci %*
exit /b %ErrorLevel%
