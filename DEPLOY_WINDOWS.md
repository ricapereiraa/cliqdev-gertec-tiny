# Deploy no Windows - Guia Completo

Este guia explica como instalar e configurar a aplicação para rodar como serviço do Windows, iniciando automaticamente com o sistema.

## Opções de Instalação

### Opção 1: Serviço do Windows (Recomendado para Produção)

A aplicação roda como serviço do Windows, iniciando automaticamente com o sistema.

### Opção 2: Docker (Alternativa)

A aplicação roda em container Docker, também com inicialização automática.

## Opção 1: Instalação como Serviço do Windows

### Pré-requisitos

1. **Windows 10/11 ou Windows Server**
2. **.NET 8.0 Runtime** instalado
  - Download: https://dotnet.microsoft.com/download/dotnet/8.0
  - Instale o "ASP.NET Core Runtime 8.0" (não precisa do SDK)

3. **PowerShell** (já incluído no Windows)

### Passo a Passo

#### 1. Preparar o Ambiente

```powershell
# Navegue até a pasta do projeto
cd C:\caminho\para\api-carol
```

#### 2. Configurar Variáveis de Ambiente

```powershell
# Criar arquivo .env
Copy-Item env.example .env

# Editar o arquivo .env
notepad .env
```

Configure pelo menos:
- `OLIST_API__TOKEN` - Token da API do Olist
- `GERTEC__IP_ADDRESS` - IP do equipamento Gertec

#### 3. Instalar como Serviço

**Execute como Administrador:**

```powershell
# Clique com botão direito no PowerShell e selecione "Executar como administrador"
# Execute o script de instalação
.\install-windows-service.ps1
```

O script irá:
- Verificar dependências
- Compilar o projeto
- Baixar NSSM (se necessário)
- Instalar o serviço
- Configurar inicialização automática

#### 4. Verificar Instalação

```powershell
# Verificar status do serviço
Get-Service -Name OlistGertecIntegration

# Ver logs
Get-Content .\logs\service.log -Tail 50
```

#### 5. Gerenciar o Serviço

```powershell
# Iniciar serviço
Start-Service -Name OlistGertecIntegration

# Parar serviço
Stop-Service -Name OlistGertecIntegration

# Reiniciar serviço
Restart-Service -Name OlistGertecIntegration

# Ver status
Get-Service -Name OlistGertecIntegration
```

### Gerenciar via Interface Gráfica

1. Abra **"Serviços"** (services.msc)
2. Procure por **"Integração Olist ERP com Gertec Busca Preço G2"**
3. Clique com botão direito para:
  - Iniciar
  - Parar
  - Reiniciar
  - Propriedades (configurar inicialização automática)

### Desinstalar o Serviço

```powershell
# Execute como Administrador
.\uninstall-windows-service.ps1
```

## Opção 2: Docker (Alternativa)

### Pré-requisitos

1. **Docker Desktop** instalado
  - Download: https://www.docker.com/products/docker-desktop

### Passo a Passo

#### 1. Configurar .env

```powershell
Copy-Item env.example .env
notepad .env
```

#### 2. Construir e Executar

```powershell
# Construir a imagem
docker-compose build

# Executar em background
docker-compose up -d

# Ver logs
docker-compose logs -f
```

#### 3. Configurar Inicialização Automática

O Docker Desktop pode ser configurado para iniciar com o Windows:
1. Abra Docker Desktop
2. Settings > General
3. Marque "Start Docker Desktop when you log in"

O container será iniciado automaticamente com o Docker.

#### 4. Gerenciar Container

```powershell
# Parar
docker-compose stop

# Iniciar
docker-compose start

# Reiniciar
docker-compose restart

# Ver status
docker-compose ps

# Ver logs
docker-compose logs -f
```

## Configuração de Firewall

Se necessário, permita a porta 5000 no firewall:

```powershell
# Permitir porta 5000
New-NetFirewallRule -DisplayName "Olist Gertec Integration" -Direction Inbound -LocalPort 5000 -Protocol TCP -Action Allow
```

## Verificação

### Testar a API

```powershell
# Status da conexão
Invoke-WebRequest -Uri http://localhost:5000/api/integration/status

# Ou no navegador
# http://localhost:5000/swagger
```

### Verificar Logs

**Serviço do Windows:**
```powershell
Get-Content .\logs\service.log -Tail 50
Get-Content .\logs\service-error.log -Tail 50
```

**Docker:**
```powershell
docker-compose logs -f
```

## Troubleshooting

### Serviço não inicia

1. **Verifique os logs:**
  ```powershell
  Get-Content .\logs\service-error.log
  ```

2. **Verifique o .env:**
  - Certifique-se de que o arquivo existe
  - Verifique se as variáveis estão corretas

3. **Verifique permissões:**
  - O serviço precisa de permissão para acessar a rede
  - Verifique firewall

4. **Teste manualmente:**
  ```powershell
  cd .\publish
  dotnet OlistGertecIntegration.dll
  ```

### Erro: "Token do Olist não configurado"

- Verifique se `OLIST_API__TOKEN` está no arquivo `.env`
- Certifique-se de que o arquivo `.env` está na pasta raiz do projeto

### Erro: "Falha ao conectar ao Gertec"

- Verifique se `GERTEC__IP_ADDRESS` está correto
- Teste conectividade: `ping IP_DO_GERTEC`
- Verifique se a porta 6500 está acessível

### Serviço para após alguns minutos

- Verifique os logs de erro
- Verifique se há problemas de memória
- Verifique conexão de rede

## Atualização

### Atualizar Serviço do Windows

1. **Parar o serviço:**
  ```powershell
  Stop-Service -Name OlistGertecIntegration
  ```

2. **Atualizar código:**
  - Substitua os arquivos do projeto
  - Execute `dotnet restore` se necessário

3. **Recompilar:**
  ```powershell
  dotnet publish -c Release -o .\publish
  ```

4. **Reiniciar serviço:**
  ```powershell
  Start-Service -Name OlistGertecIntegration
  ```

### Atualizar Docker

```powershell
# Reconstruir e reiniciar
docker-compose build
docker-compose up -d
```

## Backup

### Configuração

Mantenha backup do arquivo `.env`:
```powershell
Copy-Item .env .env.backup
```

### Logs

Os logs são salvos em:
- `.\logs\service.log` - Logs gerais
- `.\logs\service-error.log` - Erros

## Segurança

### Recomendações

1. **Arquivo .env:**
  - Não compartilhe o arquivo `.env`
  - Mantenha backup seguro
  - Use permissões restritas

2. **Serviço:**
  - Execute com conta de serviço dedicada (opcional)
  - Configure permissões mínimas necessárias

3. **Firewall:**
  - Permita apenas portas necessárias
  - Considere restringir acesso à rede local

## Monitoramento

### Verificar Status Regularmente

```powershell
# Status do serviço
Get-Service -Name OlistGertecIntegration

# Últimas linhas do log
Get-Content .\logs\service.log -Tail 20
```

### Verificar API

```powershell
# Status da API
$response = Invoke-WebRequest -Uri http://localhost:5000/api/integration/status
$response.Content
```

## Suporte

Para problemas:
1. Verifique os logs
2. Consulte `README.md` e `TROUBLESHOOTING.md`
3. Verifique se todas as dependências estão instaladas

