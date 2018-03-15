@echo off
powershell -ExecutionPolicy ByPass -Command "& """%~dp0build\Build.ps1""" -test %*"
exit /b %ErrorLevel%