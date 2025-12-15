# Erro Docker: Virtualization Not Supported

## Mensagem de Erro

```
Virtualization not supported detected
```

## Solução Rápida

### Passo 1: Habilitar Virtualização no BIOS

1. **Reinicie o computador**
2. **Entre no BIOS/UEFI:**
   - Durante a inicialização, pressione: `F2`, `F10`, `F12`, `Del` ou `Esc`
   - Varia conforme o fabricante (Dell, HP, Lenovo, etc.)

3. **Procure e Habilite:**
   - `Virtualization Technology` (Intel)
   - `AMD-V` ou `SVM Mode` (AMD)
   - `Intel VT-x`
   - `Virtualization` (geral)

4. **Salve e saia** (geralmente `F10`)

5. **Reinicie o computador**

### Passo 2: Verificar no Windows

1. Abra **Gerenciador de Tarefas** (`Ctrl + Shift + Esc`)
2. Vá para a aba **"Desempenho"**
3. Clique em **"CPU"**
4. Verifique se **"Virtualização"** está **Habilitada**

Se não estiver habilitada, volte ao Passo 1.

### Passo 3: Habilitar Recursos do Windows (Windows Pro/Enterprise)

```powershell
# Execute PowerShell como Administrador
Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V -All
```

Depois, **reinicie o computador**.

### Passo 4: Reinstalar Docker Desktop

1. Desinstale o Docker Desktop atual
2. Baixe a versão mais recente: https://www.docker.com/products/docker-desktop
3. Instale novamente
4. Reinicie o computador

## Alternativa: Rodar Sem Docker

Se não conseguir habilitar virtualização, rode a aplicação diretamente:

### Windows

1. **Instalar .NET SDK 8.0:**
   - Baixe: https://dotnet.microsoft.com/download/dotnet/8.0
   - Instale o SDK (não apenas Runtime)

2. **Rodar aplicação:**
   ```powershell
   cd "C:\caminho\para\api-carol"
   dotnet run
   ```

### Linux

```bash
# Instalar .NET SDK
sudo dnf install dotnet-sdk-8.0  # Fedora
# ou
sudo apt-get install dotnet-sdk-8.0  # Ubuntu

# Rodar aplicação
cd ~/projetos/api-carol
dotnet run
```

## Verificação Rápida

### Windows PowerShell:

```powershell
# Verificar virtualização
systeminfo | findstr /C:"Hyper-V"
```

Se aparecer "A hypervisor has been detected", está OK.

### Linux:

```bash
# Verificar suporte de hardware
grep -E 'vmx|svm' /proc/cpuinfo
```

Se aparecer `vmx` (Intel) ou `svm` (AMD), o hardware suporta.

## Checklist

- [ ] Virtualização habilitada no BIOS
- [ ] Computador reiniciado após alterações no BIOS
- [ ] Hyper-V habilitado (Windows Pro/Enterprise)
- [ ] Docker Desktop reinstalado
- [ ] Virtualização aparece como "Habilitada" no Gerenciador de Tarefas

## Ainda Não Funciona?

1. **Verifique se seu processador suporta virtualização:**
   - Intel: Processadores Core i3/i5/i7/i9, Xeon
   - AMD: Processadores Ryzen, EPYC, FX

2. **Alguns processadores antigos não suportam** - nesse caso, use a alternativa sem Docker

3. **Considere usar Docker Engine** (sem Docker Desktop) no Linux

## Próximos Passos

1. **Tente habilitar no BIOS primeiro** (resolve 90% dos casos)
2. **Se não funcionar, rode sem Docker** usando .NET SDK diretamente
3. **A aplicação funciona igual** com ou sem Docker

