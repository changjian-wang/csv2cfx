@echo off
setlocal enabledelayedexpansion

echo === CSV2CFX Auto Release Script ===

REM Save the script directory (solution directory)
set "script_dir=%cd%"
echo Script Directory: %script_dir%

REM Find the project directory
set "project_dir="
set "project_name=CSV2CFX"

if exist "%project_name%.csproj" (
    set "project_dir=%cd%"
    echo Found project in current directory: %cd%
) else (
    for /d %%i in (*) do (
        if exist "%%i\%project_name%.csproj" (
            set "project_dir=%cd%\%%i"
            echo Found project in subdirectory: !project_dir!
            goto :found_project
        )
    )
    
    echo Could not find %project_name%.csproj
    echo Please ensure this script is in the solution directory
    pause
    exit /b 1
)

:found_project

REM Generate version based on CURRENT UTC time
for /f "tokens=1-6 delims=/-: " %%a in ('powershell -command "Get-Date ([DateTime]::UtcNow) -Format 'yyyy-MM-dd-HH-mm-ss'"') do (
    set year=%%a
    set month=%%b
    set day=%%c
    set hour=%%d
    set minute=%%e
    set second=%%f
)

set version=1.0.%month%%day%.%hour%%minute%
set zipname=CSV2CFX-v%version%.zip
set user=changjian-wang
set repo=lc-materials

echo.
echo Current UTC Time: %year%-%month%-%day% %hour%:%minute%:%second%
echo Generated Version: %version%
echo Filename: %zipname%
echo User: %user%
echo Project Directory: %project_dir%
echo Output Directory: %script_dir%

echo.
echo [1/5] Cleaning and building project...
cd /d "%project_dir%"
dotnet clean
if %errorlevel% neq 0 goto :error

dotnet restore
if %errorlevel% neq 0 goto :error

dotnet build -c Release
if %errorlevel% neq 0 goto :error

echo.
echo [2/5] Publishing project...
if exist "publish" rmdir /s /q "publish"
dotnet publish -c Release -r win-x64 --self-contained false -o publish
if %errorlevel% neq 0 goto :error

echo.
echo [3/5] Creating release package in script directory...
REM Switch back to script directory for ZIP creation
cd /d "%script_dir%"

REM Remove old ZIP if exists
if exist "%zipname%" del "%zipname%"

REM Create ZIP from project's publish folder
powershell -command "Compress-Archive -Path '%project_dir%\publish\*' -DestinationPath '%script_dir%\%zipname%' -CompressionLevel Optimal"
if %errorlevel% neq 0 goto :error

echo.
echo [4/5] Calculating file hash...
for /f "tokens=*" %%i in ('powershell -command "(Get-Item '%zipname%').Length"') do set filesize=%%i
for /f "tokens=*" %%i in ('powershell -command "(Get-FileHash '%zipname%' -Algorithm SHA256).Hash"') do set hash=%%i
set /a filesize_mb=%filesize%/1024/1024

echo File Size: %filesize_mb% MB
echo SHA256: %hash%

echo.
echo [5/5] Creating updates.xml in script directory...
(
echo ^<?xml version="1.0" encoding="UTF-8"?^>
echo ^<item^>
echo   ^<version^>%version%^</version^>
echo   ^<url^>https://github.com/%user%/%repo%/releases/download/v%version%/%zipname%^</url^>
echo   ^<changelog^>https://github.com/%user%/%repo%/releases/tag/v%version%^</changelog^>
echo   ^<mandatory^>false^</mandatory^>
echo   ^<checksum algorithm="SHA256"^>%hash%^</checksum^>
echo   ^<args^>/SILENT^</args^>
echo ^</item^>
) > "%script_dir%\updates.xml"

echo.
echo === Release Complete! ===
echo ================================
echo Build Time (UTC): %year%-%month%-%day% %hour%:%minute%:%second%
echo Version: %version%
echo ZIP File: %script_dir%\%zipname%
echo XML File: %script_dir%\updates.xml
echo File Size: %filesize_mb% MB
echo SHA256: %hash%
echo ================================

echo.
echo Files created in script directory:
dir "%script_dir%\%zipname%" "%script_dir%\updates.xml" 2>nul

echo.
echo Next Steps:
echo 1. Upload %zipname% to GitHub Release
echo 2. Commit updates.xml to repository main branch
echo 3. Create GitHub Release tag: v%version%

echo.
echo GitHub Release URL: https://github.com/%user%/%repo%/releases/new

echo.
set /p openGitHub="Open GitHub Release page? (y/N): "
if /i "%openGitHub%"=="y" start https://github.com/%user%/%repo%/releases/new

echo.
set /p openFolder="Open script folder? (y/N): "
if /i "%openFolder%"=="y" start explorer "%script_dir%"

pause
goto :end

:error
echo.
echo Build failed! Error code: %errorlevel%
echo Current directory: %cd%
pause

:end

REM Clean up project publish folder
if exist "%project_dir%\publish" (
    echo Cleaning up project publish folder...
    rmdir /s /q "%project_dir%\publish"
)