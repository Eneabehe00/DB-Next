@echo off
REM ============================================
REM Installazione Schema DB-Next su sys_datos
REM ============================================

echo.
echo ========================================
echo   DB-Next - Installazione Database
echo ========================================
echo.
echo Questo script installera' le tabelle DB-Next
echo nel database esistente 'sys_datos'
echo.
echo Credenziali:
echo   Database: sys_datos
echo   User: user
echo   Password: dibal
echo   Host: 192.168.1.56
echo   Port: 3306
echo.
pause

echo.
echo Esecuzione schema.sql...
mysql -u user -pdibal -h 192.168.1.56 -P 3306 sys_datos < install.sql

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERRORE] Installazione fallita!
    echo.
    echo Possibili cause:
    echo - MySQL non e' in esecuzione
    echo - Credenziali errate
    echo - Database sys_datos non esiste
    echo.
    pause
    exit /b 1
)

echo.
echo [OK] Schema installato con successo!
echo.

REM Chiedi se eseguire i test
echo.
set /p TEST="Vuoi eseguire i test dello schema? (S/N): "
if /i "%TEST%"=="S" (
    echo.
    echo Esecuzione test...
    mysql -u user -pdibal -h 192.168.1.56 -P 3306 sys_datos < test_schema.sql
    
    if %ERRORLEVEL% NEQ 0 (
        echo.
        echo [AVVISO] Test completati con alcuni errori
        echo.
    ) else (
        echo.
        echo [OK] Test completati con successo!
        echo.
    )
)

echo.
echo ========================================
echo   Installazione Completata
echo ========================================
echo.
echo Prossimi passi:
echo 1. Copia config.ini.example in config.ini
echo 2. Esegui: dotnet build --configuration Release
echo 3. Avvia DB-Next.exe
echo.
pause

