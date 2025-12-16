# Instalação Completa via CMD - Windows

Guia passo a passo para instalar tudo e rodar a aplicação usando apenas o Prompt de Comando (CMD).

## Passo 1: Verificar se .NET está Instalado

Abra o **CMD** (Prompt de Comando) e digite:

```cmd
dotnet --version
```

**Se aparecer um número (ex: 8.0.101):**  .NET já está instalado, pule para o Passo 2.

**Se aparecer erro "não é reconhecido":** Continue no Passo 1.1.

---

## Passo 1.1: Instalar .NET 8.0 SDK

### Opção A: Download Manual (Recomendado)

1. **Abra o navegador e acesse:**
   ```
   https://dotnet.microsoft.com/download/dotnet/8.0
   ```

2. **Baixe o instalador:**
   - Procure por: **".NET SDK 8.0"** (não Runtime)
   - Clique em **"Download .NET SDK x64"** (ou x86 se seu Windows for 32-bit)
   - O arquivo será algo como: `dotnet-sdk-8.0.xxx-win-x64.exe`

3. **Execute o instalador:**
   - Clique duas vezes no arquivo baixado
   - Siga as instruções (Next, Next, Install)
   - Aguarde a instalação terminar
   - Clique em "Finish"

4. **Feche e abra o CMD novamente** (importante!)

5. **Verifique se instalou:**
   ```cmd
   dotnet --version
   ```
   Deve aparecer algo como: `8.0.101` ou superior.

### Opção B: Via Chocolatey (Se tiver instalado)

```cmd
choco install dotnet-sdk -y
```

### Opção C: Via winget (Windows 10/11)

```cmd
winget install Microsoft.DotNet.SDK.8
```

---

## Passo 2: Ir para a Pasta do Projeto

```cmd
cd "C:\caminho\completo\para\api-carol"
```

**Exemplo:**
```cmd
cd "C:\Users\SeuUsuario\Desktop\api-carol"
```

**Para descobrir onde está a pasta:**
1. Abra o Explorador de Arquivos
2. Navegue até a pasta `api-carol`
3. Clique na barra de endereço e copie o caminho
4. No CMD, digite: `cd "cole-o-caminho-aqui"`

**Para ver onde você está:**
```cmd
cd
```

---

## Passo 3: Verificar Arquivos do Projeto

```cmd
dir
```

Você deve ver arquivos como:
- `OlistGertecIntegration.csproj`
- `Program.cs`
- `env.example`
- `appsettings.json` ou `appsettings.Example.json`

---

## Passo 4: Criar Arquivo de Configuração .env

```cmd
copy env.example .env
```

**Verificar se foi criado:**
```cmd
dir .env
```

---

## Passo 5: Editar Arquivo .env

```cmd
notepad .env
```

**No Notepad que abrir, configure:**

```env
OLIST_API__BASE_URL=https://api.tiny.com.br/api2
OLIST_API__TOKEN=SEU_TOKEN_AQUI
OLIST_API__FORMAT=json

GERTEC__IP_ADDRESS=192.168.1.57
GERTEC__PORT=6500
GERTEC__RECONNECT_INTERVAL_SECONDS=5
GERTEC__RESPONSE_TIMEOUT_MILLISECONDS=500

PRICE_MONITORING__ENABLED=true
PRICE_MONITORING__CHECK_INTERVAL_MINUTES=1

ASPNETCORE_ENVIRONMENT=Development
LOG_LEVEL=Information
```

**IMPORTANTE:**
- Substitua `SEU_TOKEN_AQUI` pelo seu token real da API do Olist
- Substitua `192.168.1.57` pelo IP real do seu equipamento Gertec

**Salvar:**
- Pressione `Ctrl+S`
- Feche o Notepad: `Alt+F4` ou clique no X

---

## Passo 6: Restaurar Dependências do Projeto

```cmd
dotnet restore
```

**Aguarde terminar.** Pode demorar alguns minutos na primeira vez (baixa pacotes do NuGet).

**Se der erro:**
- Verifique sua conexão com internet
- Tente novamente: `dotnet restore`

---

## Passo 7: Compilar o Projeto

```cmd
dotnet build
```

**Aguarde terminar.** Deve aparecer "Build succeeded" no final.

**Se der erro:**
- Verifique se o Passo 6 foi concluído com sucesso
- Tente: `dotnet clean` e depois `dotnet build` novamente

---

## Passo 8: Executar a Aplicação

```cmd
dotnet run
```

**A aplicação iniciará e você verá mensagens como:**

```
info: OlistGertecIntegration.Services.IntegrationService[0]
      Serviço de integração iniciado
info: OlistGertecIntegration.Services.GertecProtocolService[0]
      Conectando ao Gertec em 192.168.1.57:6500
Now listening on: http://localhost:5000
```

**A aplicação está rodando!** 

**Para parar:** Pressione `Ctrl+C`

---

## Passo 9: Testar se Está Funcionando

**Abra outro CMD** (deixe o primeiro aberto rodando) e digite:

```cmd
curl http://localhost:5000/api/integration/status
```

**Ou abra no navegador:**
- http://localhost:5000/swagger (interface visual da API)
- http://localhost:5000/api/integration/status (status da conexão)

---

## Comandos Completos (Copiar e Colar)

Aqui está a sequência completa de comandos:

```cmd
REM 1. Verificar .NET
dotnet --version

REM 2. Ir para pasta do projeto (AJUSTE O CAMINHO!)
cd "C:\caminho\para\api-carol"

REM 3. Verificar arquivos
dir

REM 4. Criar .env
copy env.example .env

REM 5. Editar .env (configure suas credenciais)
notepad .env

REM 6. Restaurar dependências
dotnet restore

REM 7. Compilar
dotnet build

REM 8. Rodar aplicação
dotnet run
```

---

## Resumo Rápido

| Passo | Comando | O que faz |
|-------|---------|-----------|
| 1 | `dotnet --version` | Verifica se .NET está instalado |
| 2 | `cd "caminho"` | Vai para pasta do projeto |
| 3 | `dir` | Lista arquivos |
| 4 | `copy env.example .env` | Cria arquivo de configuração |
| 5 | `notepad .env` | Edita configurações |
| 6 | `dotnet restore` | Baixa dependências |
| 7 | `dotnet build` | Compila o projeto |
| 8 | `dotnet run` | Executa a aplicação |

---

## Troubleshooting

### Erro: "dotnet não é reconhecido"

**Solução:**
1. Instale o .NET SDK 8.0: https://dotnet.microsoft.com/download/dotnet/8.0
2. **Feche e abra o CMD novamente** (muito importante!)
3. Verifique: `dotnet --version`

### Erro: "Token do Olist não configurado"

**Solução:**
```cmd
REM Verificar se .env existe
dir .env

REM Se não existir, criar
copy env.example .env

REM Editar
notepad .env
```

Verifique se `OLIST_API__TOKEN` está preenchido no arquivo `.env`.

### Erro: "Falha ao conectar ao Gertec"

**Solução:**
```cmd
REM Verificar IP no .env
type .env | findstr GERTEC

REM Testar conectividade
ping 192.168.1.57
```

- Verifique se o IP está correto
- Verifique se o Gertec está ligado
- Verifique se estão na mesma rede

### Erro: "Porta 5000 já em uso"

**Solução:**
```cmd
REM Ver qual processo está usando a porta
netstat -ano | findstr :5000

REM Parar processo (substitua PID pelo número que aparecer)
taskkill /PID [PID] /F
```

### Erro: "Não foi possível restaurar"

**Solução:**
```cmd
REM Limpar cache
dotnet nuget locals all --clear

REM Tentar novamente
dotnet restore
```

### Erro: "Build failed"

**Solução:**
```cmd
REM Limpar build anterior
dotnet clean

REM Restaurar novamente
dotnet restore

REM Compilar novamente
dotnet build
```

---

## Próximos Passos

Após rodar com sucesso:

1.  **Deixe o CMD aberto** - A aplicação precisa estar rodando
2.  **Teste escanear código de barras** no Gertec
3.  **Acesse http://localhost:5000/swagger** para ver a API
4.  **Configure como serviço** (opcional) para rodar automaticamente

---

## Rodar como Serviço (Opcional)

Para que a aplicação rode automaticamente com o Windows:

1. **Abra PowerShell como Administrador:**
   - Clique com botão direito no menu Iniciar
   - Selecione "Windows PowerShell (Admin)" ou "Terminal (Admin)"

2. **Execute:**
   ```powershell
   cd "C:\caminho\para\api-carol"
   .\install-windows-service.ps1
   ```

3. **Pronto!** O serviço iniciará automaticamente com o Windows.

---

## Notas Importantes

-  **A aplicação precisa estar rodando** para funcionar
-  **Não feche o CMD** enquanto a aplicação estiver rodando
-  **Configure o .env** com suas credenciais reais antes de rodar
-  **Verifique o IP do Gertec** antes de iniciar

---

## Checklist Final

Antes de rodar, verifique:

- [ ] .NET 8.0 SDK instalado (`dotnet --version` funciona)
- [ ] Está na pasta correta do projeto
- [ ] Arquivo `.env` foi criado
- [ ] Arquivo `.env` foi configurado com token e IP corretos
- [ ] `dotnet restore` executado com sucesso
- [ ] `dotnet build` executado com sucesso
- [ ] Gertec está ligado e na mesma rede

---

## Ajuda Adicional

Se ainda tiver problemas:

1. Verifique os logs no CMD onde está rodando `dotnet run`
2. Consulte `COMANDOS_CMD_WINDOWS.md` para mais comandos
3. Consulte `DEPLOY_WINDOWS.md` para instalação como serviço
4. Verifique `TROUBLESHOOTING.md` (se existir)

