# Script PowerShell para remover o serviço do Windows
# Execute como Administrador

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Remoção do Serviço do Windows" -ForegroundColor Cyan
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

$serviceName = "OlistGertecIntegration"

# Verifica se o serviço existe
$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if (-not $service) {
    Write-Host "Serviço '$serviceName' não encontrado." -ForegroundColor Yellow
    pause
    exit 0
}

Write-Host "Serviço encontrado: $serviceName" -ForegroundColor Green
Write-Host "Status: $($service.Status)" -ForegroundColor Green
Write-Host ""

# Para o serviço se estiver rodando
if ($service.Status -eq 'Running') {
    Write-Host "Parando o serviço..." -ForegroundColor Yellow
    Stop-Service -Name $serviceName -Force
    Start-Sleep -Seconds 2
}

# Remove o serviço usando NSSM
$nssmPath = Get-Command nssm -ErrorAction SilentlyContinue
if ($nssmPath) {
    Write-Host "Removendo serviço com NSSM..." -ForegroundColor Yellow
    & $nssmPath remove $serviceName confirm
}
else {
    Write-Host "NSSM não encontrado. Tentando remover via sc.exe..." -ForegroundColor Yellow
    sc.exe delete $serviceName
}

Start-Sleep -Seconds 2

# Verifica se foi removido
$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if (-not $service) {
    Write-Host "Serviço removido com sucesso!" -ForegroundColor Green
}
else {
    Write-Host "AVISO: Serviço ainda existe. Pode ser necessário reiniciar o computador." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Pressione qualquer tecla para continuar..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

