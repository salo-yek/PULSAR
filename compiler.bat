@echo off
setlocal EnableDelayedExpansion

set "RUNTIMES=win-x64 linux-x64 linux-arm64"

if "%~1"=="" (
    set /p "PROJECT_DIR=Source directory: "
) else (
    set "PROJECT_DIR=%~1"
)

if "%~2"=="" (
    set /p "OUTPUT_DIR=Output directory: "
) else (
    set "OUTPUT_DIR=%~2"
)

if not exist "%PROJECT_DIR%\" (
    >&2 echo Error: '%PROJECT_DIR%' is not a directory
    exit /b 1
)

if not exist "%OUTPUT_DIR%\" mkdir "%OUTPUT_DIR%"

for %%I in ("%PROJECT_DIR%") do set "PROJECT_NAME=%%~nxI"

set "FAILED=0"
set "FAILED_LIST="

for %%R in (%RUNTIMES%) do (
    echo Publishing %%R ...
    dotnet publish -c Release -r "%%R" --self-contained true ^
        /p:PublishSingleFile=true /p:PublishTrimmed=true ^
        -o "%OUTPUT_DIR%\%%R" "%PROJECT_DIR%"
    if !errorlevel! neq 0 (
        set "FAILED=1"
        if defined FAILED_LIST (set "FAILED_LIST=!FAILED_LIST!, %%R") else (set "FAILED_LIST=%%R")
    ) else (
        echo   Done: %OUTPUT_DIR%\%%R
    )
)

if !FAILED! neq 0 (
    >&2 echo Failed targets: !FAILED_LIST!
    exit /b 1
)

echo All targets published successfully.
exit /b 0
