# Como Rodar Sem Docker

## Passo 1: Instalar .NET SDK 8.0

### Windows

1. **Baixe o instalador:**
   - Acesse: https://dotnet.microsoft.com/download/dotnet/8.0
   - Baixe o **SDK** (não apenas Runtime)
   - Escolha a versão para Windows (x64)

2. **Instale o arquivo baixado**

3. **Verifique a instalação:**
   ```cmd
   dotnet --version
   ```
   Deve mostrar: `8.0.x` ou superior

### Linux (Fedora/Ubuntu)

```bash
# Fedora
sudo dnf install dotnet-sdk-8.0

# Ubuntu/Debian
sudo apt-get update
sudo apt-get install dotnet-sdk-8.0

# Verificar
dotnet --version
```

## Passo 2: Baixar os Arquivos do Projeto

### Opção 1: Git (Recomendado)

```bash
# Clone o repositório
git clone <URL_DO_REPOSITORIO>
cd api-carol
```

### Opção 2: Download ZIP

1. Baixe o projeto como ZIP
2. Extraia em uma pasta (ex: `C:\projetos\api-carol`)
3. Abra o terminal nessa pasta

## Passo 3: Configurar o Arquivo .env

1. **Copie o arquivo de exemplo:**
   ```bash
   # Windows
   copy env.example .env
   
   # Linux
   cp env.example .env
   ```

2. **Edite o arquivo `.env`** e configure:
   ```env
   OLIST_API__TOKEN=f08598c71a1384a81527110a5dbf1d5fcb1773af
   GERTEC__IP_ADDRESS=192.168.1.57
   ASPNETCORE_ENVIRONMENT=Production
   ```

## Passo 4: Restaurar Dependências

```bash
dotnet restore
```

## Passo 5: Rodar a Aplicação

```bash
dotnet run
```

A aplicação iniciará e estará disponível em:
- **URL:** http://localhost:5000
- **Swagger:** http://localhost:5000/swagger (se Development)

## Comandos Completos (Copiar e Colar)

### Windows (CMD/PowerShell):

```cmd
REM 1. Verificar .NET
dotnet --version

REM 2. Ir para pasta do projeto
cd C:\caminho\para\api-carol

REM 3. Criar .env
copy env.example .env

REM 4. Editar .env (abra com notepad)
notepad .env

REM 5. Restaurar dependências
dotnet restore

REM 6. Rodar
dotnet run
```

### Linux:

```bash
# 1. Instalar .NET SDK
sudo dnf install dotnet-sdk-8.0  # Fedora
# ou
sudo apt-get install dotnet-sdk-8.0  # Ubuntu

# 2. Ir para pasta do projeto
cd ~/projetos/api-carol

# 3. Criar .env
cp env.example .env

# 4. Editar .env
nano .env

# 5. Restaurar dependências
dotnet restore

# 6. Rodar
dotnet run
```

## Rodar em Background (Linux)

```bash
# Rodar em background
nohup dotnet run > app.log 2>&1 &

# Ver logs
tail -f app.log

# Parar
pkill -f "dotnet run"
```

## Rodar como Serviço (Windows)

Use o script fornecido:

```powershell
# Execute como Administrador
.\install-windows-service.ps1
```

## Verificar se Está Rodando

### Testar API:

```bash
# Windows PowerShell
Invoke-WebRequest -Uri http://localhost:5000/api/integration/status

# Linux
curl http://localhost:5000/api/integration/status
```

### Ver Logs:

Os logs aparecem no console onde você executou `dotnet run`.

## Troubleshooting

### Erro: "dotnet: comando não encontrado"
- **Solução:** Instale o .NET SDK 8.0
- Verifique: `dotnet --version`

### Erro: "Token do Olist não configurado"
- **Solução:** Verifique se o arquivo `.env` existe e tem o token configurado

### Erro: "Falha ao conectar ao Gertec"
- **Solução:** Verifique o IP do Gertec no arquivo `.env`
- Verifique se o Gertec está ligado e na mesma rede

### Porta 5000 já em uso
- **Solução:** Pare outros serviços na porta 5000
- Ou altere a porta no `appsettings.json`

## Próximos Passos

1. ✅ Instalar .NET SDK 8.0
2. ✅ Baixar arquivos do projeto
3. ✅ Criar arquivo `.env`
4. ✅ Configurar token e IP do Gertec
5. ✅ Executar `dotnet restore`
6. ✅ Executar `dotnet run`

## Resumo Rápido

```bash
# 1. Instalar .NET SDK 8.0
# 2. Baixar projeto
# 3. Criar .env
cp env.example .env
# 4. Editar .env com suas configurações
# 5. Restaurar e rodar
dotnet restore
dotnet run
```

Pronto! A aplicação estará rodando em http://localhost:5000

