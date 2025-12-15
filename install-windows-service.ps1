# Script PowerShell para instalar como serviço do Windows
# Execute como Administrador

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Instalação como Serviço do Windows" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Verifica se está executando como administrador
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERRO: Este script precisa ser executado como Administrador!" -ForegroundColor Red
    Write-Host "Clique com botão direito e selecione 'Executar como administrador'" -ForegroundColor Yellow
    pause
    exit 1
}

# Verifica se o .NET está instalado
$dotnetPath = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnetPath) {
    Write-Host "ERRO: .NET SDK não encontrado!" -ForegroundColor Red
    Write-Host "Instale o .NET 8.0 SDK: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
    pause
    exit 1
}

Write-Host "Versão do .NET:" -ForegroundColor Green
dotnet --version
Write-Host ""

# Caminho do projeto
$projectPath = $PSScriptRoot
$serviceName = "OlistGertecIntegration"
$displayName = "Integração Olist ERP com Gertec Busca Preço G2"
$description = "Serviço de integração entre Olist ERP e Gertec Busca Preço G2"

Write-Host "Caminho do projeto: $projectPath" -ForegroundColor Green
Write-Host ""

# Verifica se o arquivo .env existe
if (-not (Test-Path "$projectPath\.env")) {
    Write-Host "AVISO: Arquivo .env não encontrado!" -ForegroundColor Yellow
    Write-Host "Criando a partir do env.example..." -ForegroundColor Yellow
    Copy-Item "$projectPath\env.example" "$projectPath\.env"
    Write-Host "Por favor, edite o arquivo .env antes de continuar!" -ForegroundColor Yellow
    Write-Host ""
    $continue = Read-Host "Deseja continuar mesmo assim? (S/N)"
    if ($continue -ne "S" -and $continue -ne "s") {
        exit 0
    }
}

# Compila o projeto
Write-Host "Compilando projeto..." -ForegroundColor Green
Set-Location $projectPath
dotnet publish -c Release -o "$projectPath\publish"

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERRO: Falha na compilação!" -ForegroundColor Red
    pause
    exit 1
}

Write-Host "Compilação concluída!" -ForegroundColor Green
Write-Host ""

# Verifica se o NSSM está disponível
$nssmPath = Get-Command nssm -ErrorAction SilentlyContinue
if (-not $nssmPath) {
    Write-Host "NSSM não encontrado. Baixando NSSM..." -ForegroundColor Yellow
    
    $nssmUrl = "https://nssm.cc/release/nssm-2.24.zip"
    $nssmZip = "$env:TEMP\nssm.zip"
    $nssmDir = "$env:TEMP\nssm"
    
    try {
        Invoke-WebRequest -Uri $nssmUrl -OutFile $nssmZip
        Expand-Archive -Path $nssmZip -DestinationPath $nssmDir -Force
        $nssmExe = Get-ChildItem -Path $nssmDir -Filter "nssm.exe" -Recurse | Select-Object -First 1
        $nssmPath = $nssmExe.FullName
        Write-Host "NSSM baixado com sucesso!" -ForegroundColor Green
    }
    catch {
        Write-Host "ERRO: Não foi possível baixar o NSSM automaticamente." -ForegroundColor Red
        Write-Host "Por favor, baixe manualmente de: https://nssm.cc/download" -ForegroundColor Yellow
        Write-Host "Extraia e adicione ao PATH, ou coloque nssm.exe na pasta do projeto." -ForegroundColor Yellow
        pause
        exit 1
    }
}
else {
    $nssmPath = $nssmPath.Source
}

Write-Host "Usando NSSM: $nssmPath" -ForegroundColor Green
Write-Host ""

# Remove serviço existente se houver
$existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "Serviço existente encontrado. Removendo..." -ForegroundColor Yellow
    if ($existingService.Status -eq 'Running') {
        Stop-Service -Name $serviceName -Force
    }
    & $nssmPath remove $serviceName confirm
    Start-Sleep -Seconds 2
}

# Instala o serviço
Write-Host "Instalando serviço..." -ForegroundColor Green

$dotnetExe = (Get-Command dotnet).Source
$dllPath = "$projectPath\publish\OlistGertecIntegration.dll"

& $nssmPath install $serviceName $dotnetExe "$dllPath"
& $nssmPath set $serviceName AppDirectory "$projectPath\publish"
& $nssmPath set $serviceName DisplayName "$displayName"
& $nssmPath set $serviceName Description "$description"
& $nssmPath set $serviceName Start SERVICE_AUTO_START
& $nssmPath set $serviceName AppStdout "$projectPath\logs\service.log"
& $nssmPath set $serviceName AppStderr "$projectPath\logs\service-error.log"

# Cria diretório de logs
$logsDir = "$projectPath\logs"
if (-not (Test-Path $logsDir)) {
    New-Item -ItemType Directory -Path $logsDir | Out-Null
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Serviço instalado com sucesso!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Nome do serviço: $serviceName" -ForegroundColor Green
Write-Host "Caminho: $dllPath" -ForegroundColor Green
Write-Host ""
Write-Host "Para iniciar o serviço:" -ForegroundColor Yellow
Write-Host "  Start-Service -Name $serviceName" -ForegroundColor White
Write-Host ""
Write-Host "Para parar o serviço:" -ForegroundColor Yellow
Write-Host "  Stop-Service -Name $serviceName" -ForegroundColor White
Write-Host ""
Write-Host "Para verificar status:" -ForegroundColor Yellow
Write-Host "  Get-Service -Name $serviceName" -ForegroundColor White
Write-Host ""
Write-Host "Logs:" -ForegroundColor Yellow
Write-Host "  $logsDir\service.log" -ForegroundColor White
Write-Host "  $logsDir\service-error.log" -ForegroundColor White
Write-Host ""

$startNow = Read-Host "Deseja iniciar o serviço agora? (S/N)"
if ($startNow -eq "S" -or $startNow -eq "s") {
    Start-Service -Name $serviceName
    Start-Sleep -Seconds 2
    $service = Get-Service -Name $serviceName
    if ($service.Status -eq 'Running') {
        Write-Host "Serviço iniciado com sucesso!" -ForegroundColor Green
    }
    else {
        Write-Host "ERRO: Falha ao iniciar o serviço. Verifique os logs." -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Pressione qualquer tecla para continuar..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

