@echo off
echo ================================================================================
echo   DB-Next - Build e Deploy Automatico
echo ================================================================================
echo.

REM 1. PULIZIA (opzionale)
echo [1/4] Pulizia build precedenti...
if exist publish rmdir /S /Q publish
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
echo       OK - Eseguibili creati
echo.

REM 4. DEPLOY
echo [4/4] Copia file in Deployment...
xcopy /Y publish\*.exe Deployment\ >nul
xcopy /Y publish\*.dll Deployment\ >nul
xcopy /Y publish\*.pdb Deployment\ >nul
xcopy /Y publish\*.json Deployment\ >nul
echo       OK - Deploy completato
echo.

echo ================================================================================
echo   BUILD COMPLETATO CON SUCCESSO!
echo ================================================================================
echo.
echo File pronti in: Deployment\
echo   - DB-Next.exe       (Applicazione principale)
echo   - DB-NextConfig.exe (Configuratore)
echo   - DB-NextCLI.exe    (Tool command-line)
echo.
echo Puoi ora eseguire: Deployment\DB-Next.exe
echo.
pause

