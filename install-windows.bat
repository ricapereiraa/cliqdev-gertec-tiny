@echo off
REM Instalador para Windows - Cliqdev Gertec e Tiny
REM Execute como Administrador

echo ========================================
echo   Instalador Cliqdev Gertec e Tiny
echo ========================================
echo.

REM Verifica se está executando como Administrador
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERRO: Este script precisa ser executado como Administrador!
    echo Clique com botao direito e selecione "Executar como administrador"
    pause
    exit /b 1
)

set SERVICE_NAME=CliqdevGertecTiny
set DISPLAY_NAME=Cliqdev Gertec e Tiny
set DESCRIPTION=API de Integracao Gertec e Tiny ERP - Cliqdev
set PROJECT_PATH=%~dp0
set EXE_PATH=%PROJECT_PATH%OlistGertecIntegration.exe
set DESKTOP_PATH=%USERPROFILE%\Desktop
set SHORTCUT_PATH=%DESKTOP_PATH%\Cliqdev Gertec e Tiny.lnk

echo Instalando %DISPLAY_NAME%...
echo.

REM Verifica se o executavel existe
if not exist "%EXE_PATH%" (
    echo AVISO: Executavel nao encontrado em: %EXE_PATH%
    echo Compilando aplicacao...
    cd /d "%PROJECT_PATH%"
    dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
    set EXE_PATH=%PROJECT_PATH%bin\Release\net8.0\win-x64\publish\OlistGertecIntegration.exe
    
    if not exist "%EXE_PATH%" (
        echo ERRO: Nao foi possivel compilar a aplicacao!
        echo Certifique-se de que o .NET SDK esta instalado.
        pause
        exit /b 1
    )
    echo Compilacao concluida!
)

REM Remove servico existente se houver
sc query %SERVICE_NAME% >nul 2>&1
if %errorLevel% equ 0 (
    echo Removendo servico existente...
    sc stop %SERVICE_NAME% >nul 2>&1
    timeout /t 2 >nul
    sc delete %SERVICE_NAME% >nul 2>&1
    timeout /t 2 >nul
)

REM Cria o servico do Windows
echo Criando servico do Windows...
sc create %SERVICE_NAME% binPath= "%EXE_PATH%" DisplayName= "%DISPLAY_NAME%" start= auto
sc description %SERVICE_NAME% "%DESCRIPTION%"

if %errorLevel% neq 0 (
    echo ERRO ao criar servico!
    pause
    exit /b 1
)

echo Servico criado com sucesso!

REM Cria atalho na area de trabalho usando PowerShell
echo Criando atalho na area de trabalho...
powershell -Command "$WshShell = New-Object -ComObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut('%SHORTCUT_PATH%'); $Shortcut.TargetPath = '%EXE_PATH%'; $Shortcut.WorkingDirectory = '%PROJECT_PATH%'; $Shortcut.Description = '%DESCRIPTION%'; $IconPath = '%PROJECT_PATH%icon.ico'; if (Test-Path $IconPath) { $Shortcut.IconLocation = $IconPath } else { $Shortcut.IconLocation = '%EXE_PATH%,0' }; $Shortcut.Save()"

if %errorLevel% equ 0 (
    echo Atalho criado na area de trabalho!
) else (
    echo AVISO: Nao foi possivel criar atalho.
)

REM Inicia o servico
echo.
echo Iniciando servico...
sc start %SERVICE_NAME%

if %errorLevel% equ 0 (
    echo Servico iniciado com sucesso!
) else (
    echo AVISO: Nao foi possivel iniciar o servico automaticamente.
    echo Voce pode inicia-lo manualmente usando: sc start %SERVICE_NAME%
)

echo.
echo ========================================
echo   Instalacao Concluida!
echo ========================================
echo.
echo Servico: %DISPLAY_NAME%
echo Atalho: %SHORTCUT_PATH%
echo.
echo A aplicacao iniciara automaticamente quando o computador ligar.
echo Acesse: http://localhost:5000/painel.html
echo.
echo Comandos uteis:
echo   - Parar: sc stop %SERVICE_NAME%
echo   - Iniciar: sc start %SERVICE_NAME%
echo   - Status: sc query %SERVICE_NAME%
echo.

pause

