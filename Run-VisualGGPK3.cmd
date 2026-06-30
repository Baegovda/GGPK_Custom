@echo off
chcp 65001 >nul
powershell -ExecutionPolicy Bypass -NoProfile -File "%~dp0build-run.ps1"
if errorlevel 1 pause
