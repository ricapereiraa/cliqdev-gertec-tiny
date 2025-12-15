@echo off
REM Script de execução da Integração Olist ERP com Gertec Busca Preço G2 (Windows)

echo ==========================================
echo Integração Olist ERP com Gertec Busca Preço G2
echo ==========================================
echo.

REM Verifica se o .NET está instalado
where dotnet >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo ERRO: .NET SDK não encontrado!
    echo Por favor, instale o .NET 8.0 SDK: https://dotnet.microsoft.com/download
    exit /b 1
)

echo Versão do .NET:
dotnet --version
echo.

REM Verifica se o appsettings.json existe
if not exist "appsettings.json" (
    echo AVISO: appsettings.json não encontrado!
    echo Copiando appsettings.Example.json para appsettings.json...
    copy appsettings.Example.json appsettings.json
    echo Por favor, edite o appsettings.json com suas configurações antes de continuar.
    exit /b 1
)

REM Restaura dependências
echo Restaurando dependências...
dotnet restore
if %ERRORLEVEL% NEQ 0 (
    echo ERRO: Falha ao restaurar dependências
    exit /b 1
)

echo.

REM Compila o projeto
echo Compilando projeto...
dotnet build
if %ERRORLEVEL% NEQ 0 (
    echo ERRO: Falha na compilação
    exit /b 1
)

echo.

REM Executa a aplicação
echo Iniciando aplicação...
echo Pressione Ctrl+C para parar
echo.
dotnet run

