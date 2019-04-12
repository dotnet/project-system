@echo off
powershell -ExecutionPolicy ByPass %~dp0%eng\common\Build.ps1 -test %*
exit /b %ErrorLevel%