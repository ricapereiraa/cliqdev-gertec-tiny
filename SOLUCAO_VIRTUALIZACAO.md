# Solução: Virtualization Not Supported Detected

## Problema

Erro ao iniciar Docker Desktop: "Virtualization not supported detected"

## Soluções por Sistema Operacional

### Windows

#### 1. Habilitar Virtualização no BIOS/UEFI

1. **Reinicie o computador** e entre no BIOS/UEFI
   - Geralmente: `F2`, `F10`, `F12`, `Del` ou `Esc` durante a inicialização
   - Varia conforme o fabricante

2. **Procure por:**
   - `Virtualization Technology` (Intel)
   - `AMD-V` (AMD)
   - `SVM Mode` (AMD)
   - `Intel Virtualization Technology (VT-x)`
   - `Hyper-V`

3. **Habilite a opção** e salve as alterações

#### 2. Habilitar Hyper-V no Windows

**Windows 10/11 Pro, Enterprise ou Education:**

```powershell
# Execute como Administrador
Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V -All
```

**Ou via Painel de Controle:**
1. Painel de Controle > Programas > Ativar ou desativar recursos do Windows
2. Marque `Hyper-V`
3. Reinicie o computador

#### 3. Habilitar Recursos do Windows

```powershell
# Execute como Administrador
dism.exe /Online /Enable-Feature:Microsoft-Windows-Subsystem-Linux /All /NoRestart
dism.exe /Online /Enable-Feature:VirtualMachinePlatform /All /NoRestart
```

#### 4. Verificar se Virtualização está Habilitada

```powershell
# Execute no PowerShell
systeminfo | findstr /C:"Hyper-V"
```

Se aparecer "Hyper-V Requirements: A hypervisor has been detected", está OK.

#### 5. Verificar no Gerenciador de Tarefas

1. Abra o Gerenciador de Tarefas (`Ctrl + Shift + Esc`)
2. Vá para a aba "Desempenho"
3. Clique em "CPU"
4. Verifique se "Virtualização" está **Habilitada**

### Linux (Fedora/Ubuntu/Debian)

#### 1. Verificar Suporte de Virtualização

```bash
# Verificar se CPU suporta virtualização
grep -E 'vmx|svm' /proc/cpuinfo
```

- Se aparecer `vmx` (Intel) ou `svm` (AMD), o hardware suporta
- Se não aparecer nada, o hardware não suporta ou está desabilitado no BIOS

#### 2. Habilitar no BIOS/UEFI

1. Reinicie e entre no BIOS/UEFI
2. Procure por:
   - `Virtualization Technology` (Intel)
   - `AMD-V` (AMD)
   - `SVM Mode` (AMD)
3. Habilite e salve

#### 3. Verificar Módulos do Kernel

```bash
# Verificar se módulos estão carregados
lsmod | grep kvm

# Carregar módulos (se necessário)
sudo modprobe kvm
sudo modprobe kvm_intel  # Para Intel
# ou
sudo modprobe kvm_amd    # Para AMD
```

#### 4. Verificar Permissões

```bash
# Adicionar usuário ao grupo libvirt (se necessário)
sudo usermod -aG libvirt $USER

# Verificar grupos
groups
```

### macOS

#### 1. Verificar Suporte

macOS geralmente suporta Docker Desktop sem problemas. Se houver erro:

1. **Verifique a versão do macOS:**
   - Docker Desktop requer macOS 10.15 ou superior

2. **Reinstale Docker Desktop:**
   - Baixe a versão mais recente
   - Instale novamente

## Soluções Alternativas

### Opção 1: Usar Docker sem Virtualização (Linux)

Se estiver no Linux e não conseguir habilitar virtualização, use Docker diretamente:

```bash
# Instalar Docker Engine (sem Docker Desktop)
sudo dnf install docker  # Fedora
# ou
sudo apt-get install docker.io  # Ubuntu/Debian

# Iniciar Docker
sudo systemctl start docker
sudo systemctl enable docker

# Adicionar usuário ao grupo docker
sudo usermod -aG docker $USER

# Fazer logout e login novamente
```

### Opção 2: Rodar Aplicação Diretamente (Sem Docker)

Se não conseguir usar Docker, rode a aplicação diretamente:

#### Windows:

```powershell
# Instalar .NET SDK 8.0
# Baixe de: https://dotnet.microsoft.com/download/dotnet/8.0

# Rodar aplicação
dotnet run
```

#### Linux:

```bash
# Instalar .NET SDK 8.0
sudo dnf install dotnet-sdk-8.0  # Fedora
# ou
sudo apt-get install dotnet-sdk-8.0  # Ubuntu/Debian

# Rodar aplicação
dotnet run
```

## Verificações Rápidas

### Windows

```powershell
# 1. Verificar virtualização
systeminfo | findstr /C:"Hyper-V"

# 2. Verificar no Gerenciador de Tarefas
# Abra Gerenciador de Tarefas > CPU > Verificar "Virtualização"
```

### Linux

```bash
# 1. Verificar suporte de hardware
grep -E 'vmx|svm' /proc/cpuinfo

# 2. Verificar módulos
lsmod | grep kvm

# 3. Verificar Docker
docker --version
```

## Checklist de Troubleshooting

- [ ] Virtualização habilitada no BIOS/UEFI
- [ ] Hyper-V habilitado (Windows Pro/Enterprise)
- [ ] Recursos do Windows habilitados
- [ ] Docker Desktop reinstalado
- [ ] Computador reiniciado após alterações
- [ ] Versão do sistema operacional compatível
- [ ] Hardware suporta virtualização

## Próximos Passos

1. **Tente habilitar virtualização no BIOS primeiro** (mais comum)
2. **Se não funcionar, use Docker Engine diretamente** (Linux)
3. **Ou rode a aplicação sem Docker** usando .NET SDK

## Ainda com Problemas?

- Verifique a documentação do Docker Desktop
- Consulte o fórum do Docker
- Verifique se seu hardware suporta virtualização

