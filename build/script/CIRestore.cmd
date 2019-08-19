@echo off
powershell -ExecutionPolicy ByPass %~dp0Build.ps1 -restore -build -ci %*
exit /b %ErrorLevel%
