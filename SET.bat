@echo off
:: DB-Next - Imposta numero manualmente
cd /d "%~dp0"

if "%1"=="" (
    set /p NUM="Inserisci numero (0-99): "
) else (
    set NUM=%1
)

DB-NextCLI.exe set %NUM%

