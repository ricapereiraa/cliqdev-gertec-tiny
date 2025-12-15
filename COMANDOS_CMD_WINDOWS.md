# Comandos CMD - Windows (Passo a Passo)

## Passo 1: Verificar se .NET está Instalado

Abra o **CMD** (Prompt de Comando) e digite:

```cmd
dotnet --version
```

**Se aparecer erro:**
- Baixe e instale: https://dotnet.microsoft.com/download/dotnet/8.0
- Escolha: **.NET SDK 8.0** para Windows
- Instale o arquivo baixado
- Feche e abra o CMD novamente

**Se aparecer `8.0.x` ou superior:** ✅ Pronto, continue!

## Passo 2: Ir para a Pasta do Projeto

```cmd
cd C:\projetos\api-carol
```

**Ou se estiver em outra pasta:**
```cmd
cd C:\caminho\completo\para\api-carol
```

**Para ver onde está:**
```cmd
cd
```

## Passo 3: Criar Arquivo .env

```cmd
copy env.example .env
```

## Passo 4: Editar Arquivo .env

```cmd
notepad .env
```

**No Notepad, configure:**

```env
OLIST_API__BASE_URL=https://api.tiny.com.br/api2
OLIST_API__TOKEN=f08598c71a1384a81527110a5dbf1d5fcb1773af
OLIST_API__FORMAT=json

GERTEC__IP_ADDRESS=192.168.1.57
GERTEC__PORT=6500
GERTEC__RECONNECT_INTERVAL_SECONDS=5
GERTEC__RESPONSE_TIMEOUT_MILLISECONDS=500

PRICE_MONITORING__ENABLED=true
PRICE_MONITORING__CHECK_INTERVAL_MINUTES=1

ASPNETCORE_ENVIRONMENT=Production
LOG_LEVEL=Warning
```

**Salve:** `Ctrl+S` e feche o Notepad (`Alt+F4`)

## Passo 5: Restaurar Dependências

```cmd
dotnet restore
```

Aguarde até terminar. Pode demorar alguns minutos na primeira vez.

## Passo 6: Rodar a Aplicação

```cmd
dotnet run
```

A aplicação iniciará e você verá mensagens como:

```
info: OlistGertecIntegration.Services.IntegrationService[0]
      Serviço de integração iniciado
info: OlistGertecIntegration.Services.GertecProtocolService[0]
      Conectando ao Gertec em 192.168.1.57:6500
```

**A aplicação estará rodando em:** http://localhost:5000

**Para parar:** Pressione `Ctrl+C`

## Comandos Completos (Copiar e Colar)

```cmd
REM 1. Verificar .NET
dotnet --version

REM 2. Ir para pasta do projeto
cd C:\projetos\api-carol

REM 3. Criar .env
copy env.example .env

REM 4. Editar .env
notepad .env

REM 5. Restaurar dependências
dotnet restore

REM 6. Rodar aplicação
dotnet run
```

## Verificar se Está Funcionando

Abra outro CMD e digite:

```cmd
curl http://localhost:5000/api/integration/status
```

**Ou no navegador:** http://localhost:5000/api/integration/status

## Rodar em Background (Como Serviço)

Para rodar como serviço do Windows:

```cmd
REM Execute como Administrador
powershell -ExecutionPolicy Bypass -File install-windows-service.ps1
```

## Comandos Úteis

### Ver arquivos na pasta:
```cmd
dir
```

### Ver conteúdo de arquivo:
```cmd
type .env
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

## Troubleshooting

### Erro: "dotnet não é reconhecido"
**Solução:**
1. Instale .NET SDK 8.0: https://dotnet.microsoft.com/download/dotnet/8.0
2. Feche e abra o CMD novamente
3. Verifique: `dotnet --version`

### Erro: "Token do Olist não configurado"
**Solução:**
1. Verifique se `.env` existe: `dir .env`
2. Se não existir: `copy env.example .env`
3. Edite: `notepad .env`
4. Verifique se `OLIST_API__TOKEN` está configurado

### Erro: "Falha ao conectar ao Gertec"
**Solução:**
1. Verifique IP no `.env`: `type .env | findstr GERTEC`
2. Teste conectividade: `ping 192.168.1.57`
3. Verifique se o Gertec está ligado

### Porta 5000 já em uso
**Solução:**
1. Encontre processo: `netstat -ano | findstr :5000`
2. Pare o processo ou altere a porta no `appsettings.json`

## Checklist Rápido

```cmd
REM 1. Verificar .NET
dotnet --version

REM 2. Ir para pasta
cd C:\projetos\api-carol

REM 3. Verificar arquivos
dir

REM 4. Criar .env
copy env.example .env

REM 5. Verificar .env foi criado
dir .env

REM 6. Restaurar
dotnet restore

REM 7. Rodar
dotnet run
```

## Resumo dos Comandos

| Ação | Comando |
|------|---------|
| Verificar .NET | `dotnet --version` |
| Ir para pasta | `cd C:\projetos\api-carol` |
| Criar .env | `copy env.example .env` |
| Editar .env | `notepad .env` |
| Restaurar | `dotnet restore` |
| Rodar | `dotnet run` |
| Parar | `Ctrl+C` |
| Ver arquivos | `dir` |
| Ver conteúdo | `type arquivo.txt` |

## Próximos Passos

Após rodar com sucesso:

1. ✅ Deixe o CMD aberto (aplicação precisa estar rodando)
2. ✅ Teste escanear código de barras no Gertec
3. ✅ Configure como serviço para rodar automaticamente (opcional)

## Nota Importante

**A aplicação precisa estar rodando** para funcionar. Se fechar o CMD, a aplicação para.

Para rodar em background, use o script de serviço do Windows:
```cmd
powershell -ExecutionPolicy Bypass -File install-windows-service.ps1
```

