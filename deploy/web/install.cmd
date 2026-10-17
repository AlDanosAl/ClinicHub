@echo off
rem Double-click to install the clinic website. Optional: install.cmd 8081  (another port)
set PORT=%1
if "%PORT%"=="" set PORT=8080
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1" -Port %PORT%
