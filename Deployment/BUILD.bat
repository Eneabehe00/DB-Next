@echo off
echo ================================================================================
echo   DB-Next - Build e Deploy Automatico
echo ================================================================================
echo.

REM 1. PULIZIA (opzionale)
echo [1/4] Pulizia build precedenti...
if exist publish rmdir /S /Q publish
if exist Deployment rmdir /S /Q Deployment
dotnet clean --configuration Release >nul 2>&1
echo       OK - Pulizia completata
echo.

REM 2. BUILD
echo [2/4] Compilazione progetto...
dotnet build --configuration Release
if errorlevel 1 (
    echo.
    echo ERRORE: Compilazione fallita!
    pause
    exit /b 1
)
echo       OK - Compilazione completata
echo.

REM 3. PUBLISH
echo [3/4] Creazione eseguibili...
dotnet publish src\DBNext\DBNext.csproj -c Release -o publish --self-contained false
if errorlevel 1 (
    echo.
    echo ERRORE: Publish fallito!
    pause
    exit /b 1
)

dotnet publish src\DBNextConfig\DBNextConfig.csproj -c Release -o publish --self-contained false
dotnet publish src\DBNextCLI\DBNextCLI.csproj -c Release -o publish --self-contained false
dotnet publish src\DBNextOperator\DBNextOperator.csproj -c Release -o publish --self-contained false
echo       OK - Eseguibili creati
echo.

REM 4. DEPLOY
echo [4/4] Copia file in Deployment...
if not exist Deployment mkdir Deployment
xcopy /Y publish\*.exe Deployment\ >nul
xcopy /Y publish\*.dll Deployment\ >nul
xcopy /Y publish\*.pdb Deployment\ >nul
xcopy /Y publish\*.json Deployment\ >nul
robocopy publish\libvlc Deployment\libvlc /E /NFL /NDL >nul
copy config.ini Deployment\ >nul 2>&1
copy *.bat Deployment\ >nul 2>&1
copy *.sql Deployment\ >nul 2>&1
copy *.md Deployment\ >nul 2>&1
if exist src\Resources (
    if not exist Deployment\Resources mkdir Deployment\Resources
    copy src\Resources\*.* Deployment\Resources\ >nul
)
if not exist Deployment\logs mkdir Deployment\logs
echo       OK - Deploy completato
echo.

echo ================================================================================
echo   BUILD COMPLETATO CON SUCCESSO!
echo ================================================================================
echo.
echo File pronti in: Deployment\
echo   - DB-Next.exe          (Applicazione principale)
echo   - DB-NextConfig.exe    (Configuratore)
echo   - DB-NextCLI.exe       (Tool command-line)
echo   - DBNextOperator.exe   (Impostazioni Operatore)
echo.
echo NOTA: Se le icone non vengono visualizzate correttamente:
echo   1. Riavvia Explorer (Win+R, digitare: explorer)
echo   2. Oppure riavvia il computer
echo.
echo Puoi ora eseguire: Deployment\DB-Next.exe
echo.
pause

