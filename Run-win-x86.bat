@echo off
setlocal
cd /d "%~dp0"
echo Restoring Creation Studio 2026 for win-x86...
dotnet restore -r win-x86
if errorlevel 1 (
    echo.
    echo Restore failed. Check that the .NET 8 SDK is installed and internet is available.
    pause
    exit /b 1
)
echo.
echo Starting Creation Studio 2026...
dotnet run -r win-x86
if errorlevel 1 (
    echo.
    echo The application could not start. Read the error above.
    pause
    exit /b 1
)
endlocal
