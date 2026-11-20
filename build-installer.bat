@echo off
rem Double-click this file to build the installer. The results appear in the "Release" folder.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build-installer.ps1" %*
echo.
pause
