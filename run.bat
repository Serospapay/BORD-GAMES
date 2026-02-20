@echo off
cd /d "%~dp0"

REM Спочатку збираємо проєкт
echo Збірка проєкту...
dotnet build BoardGamesLibrary.Desktop\BoardGamesLibrary.Desktop.csproj -c Release
if errorlevel 1 (
    echo Помилка збірки.
    pause
    exit /b 1
)

REM Шлях до exe: self-contained збірка йде в win-x64
REM Пріоритет: щойно зібраний bin\Release > publish (щоб не запускати застарілий exe)
set "EXE="
if exist "BoardGamesLibrary.Desktop\bin\Release\net8.0-windows\win-x64\BoardGamesLibrary.Desktop.exe" set "EXE=BoardGamesLibrary.Desktop\bin\Release\net8.0-windows\win-x64\BoardGamesLibrary.Desktop.exe"
if "%EXE%"=="" if exist "publish\BoardGamesLibrary.Desktop.exe" set "EXE=publish\BoardGamesLibrary.Desktop.exe"

if "%EXE%"=="" (
    echo EXE не знайдено. Запустіть: dotnet publish BoardGamesLibrary.Desktop\BoardGamesLibrary.Desktop.csproj -c Release -o publish
    pause
    exit /b 1
)

start "" "%EXE%"
