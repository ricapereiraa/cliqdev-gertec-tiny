# Passo a Passo: Rodar no Windows (CMD) - Sem Docker

## Repositório Docker Hub
https://hub.docker.com/repositories/ricapereiraa

## Passo 1: Instalar .NET SDK 8.0

### 1.1. Baixar o Instalador

1. Abra o navegador e acesse: **https://dotnet.microsoft.com/download/dotnet/8.0**
2. Clique em **"Download .NET SDK 8.0"**
3. Baixe o instalador para **Windows x64**
4. Salve o arquivo (ex: `dotnet-sdk-8.0.x-win-x64.exe`)

### 1.2. Instalar

1. Execute o arquivo baixado
2. Siga as instruções na tela
3. Marque todas as opções durante a instalação
4. Aguarde a instalação concluir

### 1.3. Verificar Instalação

Abra o **CMD** (Prompt de Comando) e digite:

```cmd
dotnet --version
```

**Se aparecer:** `8.0.x` ou superior  **Pronto!**

**Se aparecer erro:** 
- Feche e abra o CMD novamente
- Se ainda não funcionar, reinicie o computador
- Verifique se instalou o SDK (não apenas Runtime)

## Passo 2: Baixar os Arquivos do Projeto

### Opção 1: Se tiver repositório Git

```cmd
REM Instalar Git (se não tiver)
REM Baixe: https://git-scm.com/download/win

REM Clone o repositório
git clone <URL_DO_REPOSITORIO>
cd api-carol
```

### Opção 2: Baixar como ZIP

1. Acesse o repositório Git (GitHub, GitLab, etc.)
2. Clique em **"Download ZIP"** ou **"Code" > "Download ZIP"**
3. Extraia o ZIP em uma pasta (ex: `C:\projetos\api-carol`)
4. Abra o CMD e vá para essa pasta:

```cmd
cd C:\projetos\api-carol
```

### Opção 3: Extrair da Imagem Docker (Alternativa)

Se você só tiver acesso à imagem Docker:

```cmd
REM Criar container temporário
docker create --name temp-container ricapereiraa/carol-api:latest

REM Copiar arquivos do container
docker cp temp-container:/app ./extraido

REM Remover container
docker rm temp-container
```

## Passo 3: Ir para a Pasta do Projeto

```cmd
cd C:\projetos\api-carol
```

**Ou ajuste o caminho conforme sua pasta:**

```cmd
cd C:\caminho\completo\para\api-carol
```

**Para ver onde está:**
```cmd
cd
```

**Para ver arquivos na pasta:**
```cmd
dir
```

## Passo 4: Criar Arquivo .env

```cmd
copy env.example .env
```

**Verificar se foi criado:**
```cmd
dir .env
```

## Passo 5: Editar Arquivo .env

```cmd
notepad .env
```

**No Notepad que abrir, configure:**

```env
# API Olist/Tiny
OLIST_API__BASE_URL=https://api.tiny.com.br/api2
OLIST_API__TOKEN=f08598c71a1384a81527110a5dbf1d5fcb1773af
OLIST_API__FORMAT=json

# Gertec (IP real do equipamento)
GERTEC__IP_ADDRESS=192.168.1.57
GERTEC__PORT=6500
GERTEC__RECONNECT_INTERVAL_SECONDS=5
GERTEC__RESPONSE_TIMEOUT_MILLISECONDS=500

# Monitoramento
PRICE_MONITORING__ENABLED=true
PRICE_MONITORING__CHECK_INTERVAL_MINUTES=1

# Ambiente
ASPNETCORE_ENVIRONMENT=Production
LOG_LEVEL=Warning
```

**Salvar:**
- Pressione `Ctrl+S`
- Feche o Notepad (`Alt+F4` ou clique no X)

## Passo 6: Restaurar Dependências

```cmd
dotnet restore
```

**Aguarde até terminar.** Pode demorar alguns minutos na primeira vez.

Você verá mensagens como:
```
Restoring packages for C:\projetos\api-carol\OlistGertecIntegration.csproj...
```

## Passo 7: Rodar a Aplicação

```cmd
dotnet run
```

**A aplicação iniciará e você verá mensagens como:**

```
info: OlistGertecIntegration.Services.IntegrationService[0]
      Serviço de integração iniciado
info: OlistGertecIntegration.Services.GertecProtocolService[0]
      Conectando ao Gertec em 192.168.1.57:6500
info: OlistGertecIntegration.Services.GertecProtocolService[0]
      Conectado ao Gertec com sucesso - IP: 192.168.1.57:6500
```

**A aplicação estará rodando em:** **http://localhost:5000**

**Para parar a aplicação:** Pressione `Ctrl+C`

## Passo 8: Verificar se Está Funcionando

### Opção 1: No Navegador

Abra o navegador e acesse:
```
http://localhost:5000/api/integration/status
```

Deve retornar um JSON com status da conexão.

### Opção 2: No CMD (outra janela)

Abra **outro CMD** e digite:

```cmd
curl http://localhost:5000/api/integration/status
```

**Ou use PowerShell:**
```powershell
Invoke-WebRequest -Uri http://localhost:5000/api/integration/status
```

## Comandos Completos (Copiar e Colar)

```cmd
REM 1. Verificar .NET
dotnet --version

REM 2. Ir para pasta do projeto
cd C:\projetos\api-carol

REM 3. Ver arquivos
dir

REM 4. Criar .env
copy env.example .env

REM 5. Verificar .env foi criado
dir .env

REM 6. Editar .env
notepad .env

REM 7. Restaurar dependências
dotnet restore

REM 8. Rodar aplicação
dotnet run
```

## Rodar em Background (Como Serviço)

Para rodar como serviço do Windows (inicia automaticamente):

### Opção 1: Usar Script PowerShell

```cmd
REM Execute como Administrador
powershell -ExecutionPolicy Bypass -File install-windows-service.ps1
```

### Opção 2: Rodar Manualmente em Background

```cmd
REM Rodar em nova janela
start cmd /k "cd C:\projetos\api-carol && dotnet run"
```

## Comandos Úteis

### Ver arquivos na pasta:
```cmd
dir
```

### Ver conteúdo de arquivo:
```cmd
type .env
type appsettings.json
```

### Ver logs em tempo real:
Os logs aparecem no mesmo CMD onde você rodou `dotnet run`

### Parar aplicação:
```cmd
Ctrl+C
```

### Limpar e recompilar:
```cmd
dotnet clean
dotnet build
```

### Ver processos rodando:
```cmd
tasklist | findstr dotnet
```

## Troubleshooting

### Erro: "dotnet não é reconhecido como comando"

**Solução:**
1. Verifique se .NET SDK está instalado: `dotnet --version`
2. Se não funcionar, reinstale o .NET SDK 8.0
3. Feche e abra o CMD novamente
4. Se ainda não funcionar, reinicie o computador

### Erro: "Token do Olist não configurado"

**Solução:**
1. Verifique se `.env` existe: `dir .env`
2. Se não existir: `copy env.example .env`
3. Edite: `notepad .env`
4. Verifique se `OLIST_API__TOKEN` está configurado
5. Salve o arquivo (`Ctrl+S`)

### Erro: "Falha ao conectar ao Gertec"

**Solução:**
1. Verifique IP no `.env`: `type .env | findstr GERTEC`
2. Teste conectividade: `ping 192.168.1.57`
3. Verifique se o Gertec está ligado
4. Verifique se estão na mesma rede

### Erro: "Porta 5000 já está em uso"

**Solução:**
1. Encontre processo usando a porta:
```cmd
netstat -ano | findstr :5000
```
2. Pare o processo ou altere a porta no `appsettings.json`

### Erro: "Não foi possível restaurar"

**Solução:**
1. Verifique conexão com internet
2. Tente novamente: `dotnet restore`
3. Limpe cache: `dotnet nuget locals all --clear`
4. Tente novamente: `dotnet restore`

## Checklist Completo

- [ ] .NET SDK 8.0 instalado (`dotnet --version` funciona)
- [ ] Código do projeto baixado
- [ ] Na pasta correta do projeto (`cd C:\projetos\api-carol`)
- [ ] Arquivo `.env` criado (`copy env.example .env`)
- [ ] Arquivo `.env` editado e configurado (`notepad .env`)
- [ ] Token da API configurado no `.env`
- [ ] IP do Gertec configurado no `.env`
- [ ] Dependências restauradas (`dotnet restore`)
- [ ] Aplicação rodando (`dotnet run`)
- [ ] API respondendo em http://localhost:5000

## Resumo Rápido

```cmd
REM 1. Verificar .NET
dotnet --version

REM 2. Ir para pasta
cd C:\projetos\api-carol

REM 3. Criar .env
copy env.example .env

REM 4. Editar .env
notepad .env

REM 5. Restaurar
dotnet restore

REM 6. Rodar
dotnet run
```

## Próximos Passos

Após rodar com sucesso:

1.  Deixe o CMD aberto (aplicação precisa estar rodando)
2.  Teste escanear um código de barras no Gertec
3.  Verifique se o produto aparece no display
4.  Configure para rodar como serviço (opcional)
5.  Configure inicialização automática (opcional)

## Nota Importante

**A aplicação precisa estar rodando** para funcionar. Se fechar o CMD, a aplicação para.

Para rodar em background permanente, use o script de serviço do Windows:
```cmd
powershell -ExecutionPolicy Bypass -File install-windows-service.ps1
```

## Estrutura de Arquivos Esperada

```
C:\projetos\api-carol\
├── .env                    (criado por você)
├── env.example             (template)
├── appsettings.json
├── Program.cs
├── OlistGertecIntegration.csproj
├── Services\
├── Models\
└── Controllers\
```

## Verificar Estrutura

```cmd
REM Ver estrutura de pastas
tree /F

REM Ou ver apenas arquivos principais
dir *.json
dir *.csproj
dir .env
```

## Sucesso!

Se você vê mensagens como:
```
info: OlistGertecIntegration.Services.IntegrationService[0]
      Serviço de integração iniciado
```

E a API responde em http://localhost:5000, **está funcionando!** 
