@echo off
powershell -ExecutionPolicy ByPass %~dp0Build.ps1 -restore -build -test -pack -ci %*
exit /b %ErrorLevel%
