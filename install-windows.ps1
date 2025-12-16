# Instalador para Windows - Cliqdev Gertec e Tiny
# Execute como Administrador: powershell -ExecutionPolicy Bypass -File install-windows.ps1

param(
    [switch]$Uninstall
)

$ErrorActionPreference = "Stop"

# Configurações
$ServiceName = "CliqdevGertecTiny"
$DisplayName = "Cliqdev Gertec e Tiny"
$Description = "API de Integração Gertec e Tiny ERP - Cliqdev"
$ProjectPath = $PSScriptRoot
$ExePath = Join-Path $ProjectPath "OlistGertecIntegration.exe"
$DesktopPath = [Environment]::GetFolderPath("Desktop")
$ShortcutPath = Join-Path $DesktopPath "Cliqdev Gertec e Tiny.lnk"
$IconPath = Join-Path $ProjectPath "icon.ico"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Instalador Cliqdev Gertec e Tiny" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verifica se está executando como Administrador
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERRO: Este script precisa ser executado como Administrador!" -ForegroundColor Red
    Write-Host "Clique com botão direito e selecione 'Executar como administrador'" -ForegroundColor Yellow
    pause
    exit 1
}

if ($Uninstall) {
    Write-Host "Desinstalando $DisplayName..." -ForegroundColor Yellow
    
    # Para e remove o serviço
    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($service) {
        if ($service.Status -eq "Running") {
            Stop-Service -Name $ServiceName -Force
            Write-Host "Serviço parado." -ForegroundColor Green
        }
        sc.exe delete $ServiceName | Out-Null
        Write-Host "Serviço removido." -ForegroundColor Green
    }
    
    # Remove atalho da área de trabalho
    if (Test-Path $ShortcutPath) {
        Remove-Item $ShortcutPath -Force
        Write-Host "Atalho removido da área de trabalho." -ForegroundColor Green
    }
    
    Write-Host ""
    Write-Host "Desinstalação concluída!" -ForegroundColor Green
    pause
    exit 0
}

Write-Host "Instalando $DisplayName..." -ForegroundColor Yellow
Write-Host ""

# Verifica se o executável existe
if (-not (Test-Path $ExePath)) {
    Write-Host "AVISO: Executável não encontrado em: $ExePath" -ForegroundColor Yellow
    Write-Host "Compilando aplicação..." -ForegroundColor Yellow
    
    # Tenta compilar
    Push-Location $ProjectPath
    try {
        dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
        $ExePath = Join-Path $ProjectPath "bin\Release\net8.0\win-x64\publish\OlistGertecIntegration.exe"
        
        if (-not (Test-Path $ExePath)) {
            Write-Host "ERRO: Não foi possível compilar a aplicação!" -ForegroundColor Red
            Write-Host "Certifique-se de que o .NET SDK está instalado." -ForegroundColor Yellow
            pause
            exit 1
        }
        Write-Host "Compilação concluída!" -ForegroundColor Green
    }
    catch {
        Write-Host "ERRO ao compilar: $_" -ForegroundColor Red
        pause
        exit 1
    }
    finally {
        Pop-Location
    }
}

# Verifica se o arquivo .env existe
$envFile = Join-Path $ProjectPath ".env"
if (-not (Test-Path $envFile)) {
    Write-Host "AVISO: Arquivo .env não encontrado!" -ForegroundColor Yellow
    Write-Host "Criando arquivo .env a partir do exemplo..." -ForegroundColor Yellow
    
    $envExample = Join-Path $ProjectPath "env.example"
    if (Test-Path $envExample) {
        Copy-Item $envExample $envFile
        Write-Host "Arquivo .env criado. Por favor, configure-o antes de iniciar o serviço." -ForegroundColor Yellow
    }
}

# Remove serviço existente se houver
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "Removendo serviço existente..." -ForegroundColor Yellow
    if ($existingService.Status -eq "Running") {
        Stop-Service -Name $ServiceName -Force
    }
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
}

# Cria o serviço do Windows
Write-Host "Criando serviço do Windows..." -ForegroundColor Yellow

$serviceArgs = @{
    Name = $ServiceName
    DisplayName = $DisplayName
    Description = $Description
    BinaryPathName = "`"$ExePath`""
    StartupType = "Automatic"
    ErrorControl = "Normal"
}

try {
    # Cria o serviço usando sc.exe
    $result = sc.exe create $ServiceName binPath= "`"$ExePath`"" DisplayName= "$DisplayName" start= auto
    if ($LASTEXITCODE -ne 0) {
        throw "Falha ao criar serviço"
    }
    
    # Configura descrição
    sc.exe description $ServiceName "$Description"
    
    Write-Host "Serviço criado com sucesso!" -ForegroundColor Green
}
catch {
    Write-Host "ERRO ao criar serviço: $_" -ForegroundColor Red
    pause
    exit 1
}

# Cria atalho na área de trabalho
Write-Host "Criando atalho na área de trabalho..." -ForegroundColor Yellow

try {
    $WshShell = New-Object -ComObject WScript.Shell
    $Shortcut = $WshShell.CreateShortcut($ShortcutPath)
    $Shortcut.TargetPath = $ExePath
    $Shortcut.WorkingDirectory = $ProjectPath
    $Shortcut.Description = $Description
    
    # Se o ícone existir, usa ele
    if (Test-Path $IconPath) {
        $Shortcut.IconLocation = $IconPath
    }
    else {
        # Usa o ícone do executável
        $Shortcut.IconLocation = "$ExePath,0"
    }
    
    $Shortcut.Save()
    Write-Host "Atalho criado na área de trabalho!" -ForegroundColor Green
}
catch {
    Write-Host "AVISO: Não foi possível criar atalho: $_" -ForegroundColor Yellow
}

# Inicia o serviço
Write-Host ""
Write-Host "Iniciando serviço..." -ForegroundColor Yellow
try {
    Start-Service -Name $ServiceName
    Write-Host "Serviço iniciado com sucesso!" -ForegroundColor Green
}
catch {
    Write-Host "AVISO: Não foi possível iniciar o serviço automaticamente." -ForegroundColor Yellow
    Write-Host "Você pode iniciá-lo manualmente usando: Start-Service -Name $ServiceName" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Instalacao Concluida!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Servico: $DisplayName" -ForegroundColor Cyan
Write-Host "Status: $((Get-Service -Name $ServiceName).Status)" -ForegroundColor Cyan
Write-Host "Atalho: $ShortcutPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "A aplicacao iniciara automaticamente quando o computador ligar." -ForegroundColor Green
Write-Host "Acesse: http://localhost:5000/painel.html" -ForegroundColor Green
Write-Host ""
Write-Host "Comandos úteis:" -ForegroundColor Yellow
Write-Host "  - Parar: Stop-Service -Name $ServiceName" -ForegroundColor White
Write-Host "  - Iniciar: Start-Service -Name $ServiceName" -ForegroundColor White
Write-Host "  - Status: Get-Service -Name $ServiceName" -ForegroundColor White
Write-Host "  - Desinstalar: .\install-windows.ps1 -Uninstall" -ForegroundColor White
Write-Host ""

pause

