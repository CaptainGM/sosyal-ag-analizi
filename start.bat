@echo off
echo Makale Atif Agi Analizi
echo ===================================
echo.

REM PATH'e dotnet ekle
set PATH=C:\Program Files (x86)\dotnet;C:\Program Files\dotnet;%PATH%

REM Proje klasorune git
cd /d "%~dp0"

REM data.json dosyasini kopyala
if not exist "bin\Debug\net9.0-windows\Data" mkdir "bin\Debug\net9.0-windows\Data"
copy /Y "Data\data.json" "bin\Debug\net9.0-windows\Data\data.json" >nul 2>&1

REM Uygulamayi calistir
echo Uygulama baslatiliyor...
dotnet run

pause
