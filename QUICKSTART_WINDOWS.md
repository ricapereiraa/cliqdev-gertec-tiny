# Quick Start - Windows

## Instalação Rápida como Serviço do Windows

### 1. Instalar .NET Runtime

Baixe e instale o **ASP.NET Core Runtime 8.0** (não precisa do SDK):
https://dotnet.microsoft.com/download/dotnet/8.0

### 2. Configurar

```powershell
# Criar arquivo .env
Copy-Item env.example .env

# Editar com suas credenciais
notepad .env
```

**Configure:**
- `OLIST_API__TOKEN` - Seu token da API do Olist
- `GERTEC__IP_ADDRESS` - IP do seu Gertec

### 3. Instalar Serviço

**Execute PowerShell como Administrador:**

```powershell
.\install-windows-service.ps1
```

### 4. Pronto!

O serviço está instalado e iniciará automaticamente com o Windows.

## Gerenciar

```powershell
# Ver status
Get-Service -Name OlistGertecIntegration

# Iniciar
Start-Service -Name OlistGertecIntegration

# Parar
Stop-Service -Name OlistGertecIntegration

# Ver logs
Get-Content .\logs\service.log -Tail 50
```

## Testar

Abra no navegador:
- http://localhost:5000/swagger

## Desinstalar

```powershell
# Execute como Administrador
.\uninstall-windows-service.ps1
```

## Documentação Completa

Veja `DEPLOY_WINDOWS.md` para guia completo.

