# Instalação no Fedora - Guia Rápido

## Instalar .NET SDK 8.0

No Fedora, a forma mais simples é usar o gerenciador de pacotes DNF:

```bash
sudo dnf install dotnet-sdk-8.0
```

Ou use o script fornecido:

```bash
./install-dotnet.sh
```

## Verificar Instalação

```bash
dotnet --version
```

Deve mostrar algo como: `8.0.xxx`

## Executar a Aplicação

Após instalar o .NET SDK:

```bash
./run.sh
```

## Alternativa: Instalação Manual

Se preferir instalar manualmente sem sudo:

1. Baixe o .NET SDK 8.0:
  - Acesse: https://dotnet.microsoft.com/download/dotnet/8.0
  - Baixe o instalador para Linux x64

2. Extraia e configure:
```bash
mkdir -p $HOME/dotnet
tar zxf dotnet-sdk-8.0.*-linux-x64.tar.gz -C $HOME/dotnet
export DOTNET_ROOT=$HOME/dotnet
export PATH=$PATH:$HOME/dotnet
```

3. Adicione ao seu `~/.bashrc` ou `~/.zshrc`:
```bash
export DOTNET_ROOT=$HOME/dotnet
export PATH=$PATH:$HOME/dotnet
```

## Verificar Pacotes Disponíveis

Para ver todas as versões do .NET disponíveis no Fedora:

```bash
dnf search dotnet-sdk
```

## Solução de Problemas

### Erro: "dotnet: comando não encontrado"

1. Verifique se o .NET está instalado:
```bash
rpm -qa | grep dotnet
```

2. Se não estiver, instale:
```bash
sudo dnf install dotnet-sdk-8.0
```

3. Verifique o PATH:
```bash
echo $PATH
```

### Erro: "Versão não encontrada"

O Fedora pode ter versões ligeiramente diferentes. Verifique qual versão está disponível:

```bash
dnf info dotnet-sdk-8.0
```

E ajuste o `OlistGertecIntegration.csproj` se necessário (qualquer versão 8.0.x funcionará).

