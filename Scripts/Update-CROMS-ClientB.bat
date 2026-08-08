@echo off
REM ================================================================
REM  Update-CROMS-ClientB.bat  (double-click this)
REM  Keep it in the SAME folder as Find-And-Update-CROMS.ps1.
REM  Auto-finds the server on the current Wi-Fi (no IP to type),
REM  copies the latest CROMS build, and launches it.
REM ================================================================
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Find-And-Update-CROMS.ps1"
