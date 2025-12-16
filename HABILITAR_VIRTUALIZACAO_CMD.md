# Habilitar Virtualização pelo CMD e Rodar Docker

##  IMPORTANTE

**A virtualização no BIOS NÃO pode ser ativada pelo CMD.** Ela precisa ser habilitada manualmente no BIOS.

**O que podemos fazer pelo CMD:**
- Habilitar recursos do Windows (Hyper-V, WSL2)
- Verificar se virtualização está habilitada
- Configurar Docker após virtualização estar ativa

## Passo 1: Habilitar Virtualização no BIOS (OBRIGATÓRIO)

**Isso NÃO pode ser feito pelo CMD. Precisa ser manual:**

1. **Reinicie o computador**
2. **Entre no BIOS/UEFI:**
   - Durante a inicialização, pressione: `F2`, `F10`, `F12`, `Del` ou `Esc`
   - Varia conforme o fabricante

3. **Procure e Habilite:**
   - `Virtualization Technology` (Intel)
   - `AMD-V` ou `SVM Mode` (AMD)
   - `Intel VT-x`
   - `Virtualization` (geral)

4. **Salve e saia** (geralmente `F10`)

5. **Reinicie o computador**

## Passo 2: Verificar Virtualização pelo CMD

Abra **CMD como Administrador** e execute:

```cmd
systeminfo | findstr /C:"Hyper-V"
```

**Se aparecer:** "A hypervisor has been detected"  Virtualização está OK

**Se não aparecer:** Volte ao Passo 1 e habilite no BIOS

## Passo 3: Habilitar Recursos do Windows pelo CMD

Abra **PowerShell como Administrador** (não CMD):

```powershell
# Habilitar Hyper-V (Windows Pro/Enterprise/Education)
Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V -All

# Habilitar WSL2 (necessário para Docker Desktop)
dism.exe /Online /Enable-Feature:Microsoft-Windows-Subsystem-Linux /All /NoRestart
dism.exe /Online /Enable-Feature:VirtualMachinePlatform /All /NoRestart
```

**Ou pelo CMD como Administrador:**

```cmd
dism.exe /Online /Enable-Feature:Microsoft-Hyper-V /All /NoRestart
dism.exe /Online /Enable-Feature:Microsoft-Windows-Subsystem-Linux /All /NoRestart
dism.exe /Online /Enable-Feature:VirtualMachinePlatform /All /NoRestart
```

## Passo 4: Reiniciar o Computador

```cmd
shutdown /r /t 0
```

**OU reinicie manualmente**

## Passo 5: Verificar no Gerenciador de Tarefas

1. Abra **Gerenciador de Tarefas** (`Ctrl + Shift + Esc`)
2. Vá para aba **"Desempenho"**
3. Clique em **"CPU"**
4. Verifique se **"Virtualização"** está **Habilitada**

**Se não estiver habilitada:** Volte ao Passo 1 (BIOS)

## Passo 6: Instalar Docker Desktop

1. **Baixe Docker Desktop:**
   - https://www.docker.com/products/docker-desktop
   - Baixe a versão para Windows

2. **Instale o arquivo baixado**

3. **Reinicie o computador** (se solicitado)

## Passo 7: Verificar Docker pelo CMD

```cmd
docker --version
```

**Deve mostrar:** `Docker version x.x.x`

## Passo 8: Baixar e Rodar a Imagem

```cmd
REM Baixar imagem do Docker Hub
docker pull ricapereiraa/carol-api:latest

REM Criar arquivo .env (se ainda não tiver)
copy env.example .env

REM Editar .env
notepad .env

REM Rodar container
docker run -d --name carol-api -p 5000:80 --env-file .env --restart unless-stopped ricapereiraa/carol-api:latest
```

## Passo 9: Verificar se Está Rodando

```cmd
REM Ver containers rodando
docker ps

REM Ver logs
docker logs carol-api

REM Testar API
curl http://localhost:5000/api/integration/status
```

## Comandos Completos (Copiar e Colar)

### 1. Verificar Virtualização:
```cmd
systeminfo | findstr /C:"Hyper-V"
```

### 2. Habilitar Recursos (PowerShell como Admin):
```powershell
Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V -All
dism.exe /Online /Enable-Feature:Microsoft-Windows-Subsystem-Linux /All /NoRestart
dism.exe /Online /Enable-Feature:VirtualMachinePlatform /All /NoRestart
```

### 3. Reiniciar:
```cmd
shutdown /r /t 0
```

### 4. Verificar Docker:
```cmd
docker --version
```

### 5. Baixar e Rodar:
```cmd
docker pull ricapereiraa/carol-api:latest
docker run -d --name carol-api -p 5000:80 --env-file .env --restart unless-stopped ricapereiraa/carol-api:latest
```

## Troubleshooting

### Erro: "Virtualization not supported detected"

**Causa:** Virtualização não habilitada no BIOS

**Solução:**
1. Entre no BIOS (Passo 1)
2. Habilite virtualização
3. Reinicie
4. Verifique no Gerenciador de Tarefas

### Erro: "Hyper-V não pode ser habilitado"

**Causa:** Windows Home não suporta Hyper-V

**Solução:**
1. Use WSL2 (habilitado no Passo 3)
2. Ou use Docker sem Hyper-V
3. Ou atualize para Windows Pro

### Erro: "docker: comando não encontrado"

**Solução:**
1. Instale Docker Desktop
2. Reinicie o computador
3. Verifique: `docker --version`

### Erro: "WSL 2 installation is incomplete"

**Solução:**
```cmd
wsl --install
```

Depois reinicie o computador.

## Checklist Completo

- [ ] Virtualização habilitada no BIOS
- [ ] Computador reiniciado após BIOS
- [ ] Hyper-V ou WSL2 habilitado (PowerShell como Admin)
- [ ] Computador reiniciado após habilitar recursos
- [ ] Virtualização aparece como "Habilitada" no Gerenciador de Tarefas
- [ ] Docker Desktop instalado
- [ ] Docker funcionando (`docker --version`)
- [ ] Imagem baixada (`docker pull`)
- [ ] Container rodando (`docker ps`)

## Resumo dos Comandos

| Ação | Comando |
|------|---------|
| Verificar virtualização | `systeminfo \| findstr /C:"Hyper-V"` |
| Habilitar Hyper-V | `Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V -All` |
| Habilitar WSL2 | `dism.exe /Online /Enable-Feature:Microsoft-Windows-Subsystem-Linux /All` |
| Reiniciar | `shutdown /r /t 0` |
| Verificar Docker | `docker --version` |
| Baixar imagem | `docker pull ricapereiraa/carol-api:latest` |
| Rodar container | `docker run -d --name carol-api -p 5000:80 --env-file .env ricapereiraa/carol-api:latest` |
| Ver containers | `docker ps` |
| Ver logs | `docker logs carol-api` |

## Nota Importante

**A virtualização no BIOS PRECISA ser habilitada manualmente.** Os comandos do CMD apenas habilitam recursos do Windows, mas não alteram o BIOS.

Se você não conseguir habilitar no BIOS, use a alternativa sem Docker (rode com `dotnet run` diretamente).

