# Script para criar atalho na área de trabalho com ícone
# Execute como: powershell -ExecutionPolicy Bypass -File "Criar-Atalho-Area-Trabalho.ps1"

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$desktopPath = [Environment]::GetFolderPath("Desktop")
$shortcutPath = Join-Path $desktopPath "API Gertec - Sincronizador.lnk"
$batFile = Join-Path $scriptPath "Executar-API.bat"

Write-Host "Criando atalho na area de trabalho..." -ForegroundColor Green

# Cria o objeto WScript.Shell
$shell = New-Object -ComObject WScript.Shell

# Cria o atalho
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $batFile
$shortcut.WorkingDirectory = $scriptPath
$shortcut.Description = "API Gertec - Sincronizador Tiny ERP - Atualiza arquivo de produtos automaticamente"
$shortcut.WindowStyle = 1  # 1 = Normal, 3 = Maximized, 7 = Minimized

# Tenta usar ícone do .NET ou Windows
$dotnetPath = (Get-Command dotnet -ErrorAction SilentlyContinue).Source
if ($dotnetPath) {
    $dotnetDir = Split-Path -Parent $dotnetPath
    $iconPath = Join-Path $dotnetDir "dotnet.exe"
    if (Test-Path $iconPath) {
        $shortcut.IconLocation = "$iconPath,0"
    }
} else {
    # Usa ícone padrão do Windows
    $shortcut.IconLocation = "shell32.dll,137"  # Ícone de aplicativo
}

$shortcut.Save()

Write-Host "Atalho criado com sucesso em: $shortcutPath" -ForegroundColor Green
Write-Host "Voce pode arrastar o atalho para a area de trabalho se necessario." -ForegroundColor Yellow

