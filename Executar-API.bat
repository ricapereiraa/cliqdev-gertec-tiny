@echo off
title API Gertec - Sincronizador Tiny ERP
color 0A

echo ========================================
echo   API Gertec - Sincronizador Tiny ERP
echo ========================================
echo.
echo Iniciando API...
echo.

REM Navega para o diretório do projeto
cd /d "%~dp0"

REM Verifica se o .NET está instalado
echo Verificando .NET SDK...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo.
    echo [ERRO] .NET SDK nao encontrado!
    echo Por favor, instale o .NET 8.0 SDK
    echo Download: https://dotnet.microsoft.com/download
    echo.
    pause
    exit /b 1
)

for /f "tokens=*" %%i in ('dotnet --version') do set DOTNET_VERSION=%%i
echo [OK] .NET SDK encontrado: %DOTNET_VERSION%
echo.

REM Verifica se o arquivo .env existe
if not exist ".env" (
    echo [AVISO] Arquivo .env nao encontrado!
    echo A API usara appsettings.json como configuracao
    echo.
)

REM Executa a API
echo ========================================
echo   Executando API...
echo   Pressione Ctrl+C para parar
echo ========================================
echo.
dotnet run

echo.
echo API encerrada.
pause

