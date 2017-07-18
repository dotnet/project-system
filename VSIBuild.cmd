@echo off
powershell -ExecutionPolicy ByPass %~dp0build\Build.ps1 -restore -deployDeps -build -deploy -integrationTest -ci %*
exit /b %ErrorLevel%
