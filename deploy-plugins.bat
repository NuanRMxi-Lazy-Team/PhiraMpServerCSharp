@echo off
REM Plugin deployment script for Windows
REM This script copies plugin DLLs to the server's plugins directory

set SERVER_DIR=PhiraMp.Server\bin\Debug\net10.0
set PLUGINS_DIR=%SERVER_DIR%\plugins

echo Deploying plugins...

REM Create plugins directory if it doesn't exist
if not exist "%PLUGINS_DIR%" mkdir "%PLUGINS_DIR%"

REM Copy CommandPlugin
if exist "PhiraMp.Plugins.CommandPlugin\bin\Debug\net10.0\PhiraMp.Plugins.CommandPlugin.dll" (
    copy /Y "PhiraMp.Plugins.CommandPlugin\bin\Debug\net10.0\PhiraMp.Plugins.CommandPlugin.dll" "%PLUGINS_DIR%\"
    echo [OK] CommandPlugin deployed
) else (
    echo [FAIL] CommandPlugin not found - build the solution first
)

REM Copy CycleVotingPlugin
if exist "PhiraMp.Plugins.CycleVoting\bin\Debug\net10.0\PhiraMp.Plugins.CycleVoting.dll" (
    copy /Y "PhiraMp.Plugins.CycleVoting\bin\Debug\net10.0\PhiraMp.Plugins.CycleVoting.dll" "%PLUGINS_DIR%\"
    echo [OK] CycleVotingPlugin deployed
) else (
    echo [FAIL] CycleVotingPlugin not found - build the solution first
)

REM Copy SinglePlayerPreventionPlugin
if exist "PhiraMp.Plugins.SinglePlayerPrevention\bin\Debug\net10.0\PhiraMp.Plugins.SinglePlayerPrevention.dll" (
    copy /Y "PhiraMp.Plugins.SinglePlayerPrevention\bin\Debug\net10.0\PhiraMp.Plugins.SinglePlayerPrevention.dll" "%PLUGINS_DIR%\"
    echo [OK] SinglePlayerPreventionPlugin deployed
) else (
    echo [FAIL] SinglePlayerPreventionPlugin not found - build the solution first
)

echo.
echo Plugin deployment complete!
echo Plugins directory: %PLUGINS_DIR%
echo.
echo To run the server with plugins:
echo   cd %SERVER_DIR%
echo   dotnet PhiraMp.Server.dll
pause
