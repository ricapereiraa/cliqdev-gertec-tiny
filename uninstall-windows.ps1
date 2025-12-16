# Desinstalador para Windows - Cliqdev Gertec e Tiny
# Execute como Administrador: powershell -ExecutionPolicy Bypass -File uninstall-windows.ps1

$ErrorActionPreference = "Stop"

$ServiceName = "CliqdevGertecTiny"
$DisplayName = "Cliqdev Gertec e Tiny"
$DesktopPath = [Environment]::GetFolderPath("Desktop")
$ShortcutPath = Join-Path $DesktopPath "Cliqdev Gertec e Tiny.lnk"

Write-Host "========================================" -ForegroundColor Yellow
Write-Host "  Desinstalador Cliqdev Gertec e Tiny" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""

# Verifica se está executando como Administrador
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERRO: Este script precisa ser executado como Administrador!" -ForegroundColor Red
    Write-Host "Clique com botão direito e selecione 'Executar como administrador'" -ForegroundColor Yellow
    pause
    exit 1
}

Write-Host "Desinstalando $DisplayName..." -ForegroundColor Yellow
Write-Host ""

# Para e remove o serviço
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($service) {
    Write-Host "Parando serviço..." -ForegroundColor Yellow
    if ($service.Status -eq "Running") {
        Stop-Service -Name $ServiceName -Force
        Start-Sleep -Seconds 2
        Write-Host "Serviço parado." -ForegroundColor Green
    }
    
    Write-Host "Removendo serviço..." -ForegroundColor Yellow
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
    Write-Host "Serviço removido." -ForegroundColor Green
}
else {
    Write-Host "Serviço não encontrado." -ForegroundColor Yellow
}

# Remove atalho da área de trabalho
if (Test-Path $ShortcutPath) {
    Write-Host "Removendo atalho da área de trabalho..." -ForegroundColor Yellow
    Remove-Item $ShortcutPath -Force
    Write-Host "Atalho removido." -ForegroundColor Green
}
else {
    Write-Host "Atalho não encontrado." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Desinstalação Concluída!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "A aplicação foi removida do sistema." -ForegroundColor Green
Write-Host "Os arquivos do projeto não foram removidos." -ForegroundColor Yellow
Write-Host ""

pause

