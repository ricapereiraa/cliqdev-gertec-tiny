# Como Publicar como Executável (.exe)

## Windows

### Opção 1: Publicação como Single File (Recomendado)

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

O executável será gerado em:
```
bin/Release/net8.0/win-x64/publish/OlistGertecIntegration.exe
```

### Opção 2: Publicação com Runtime Incluído

```bash
dotnet publish -c Release -r win-x64 --self-contained true
```

### Opção 3: Publicação Portável (requer .NET instalado)

```bash
dotnet publish -c Release
```

## Linux

### Publicação para Linux

```bash
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true
```

## Estrutura de Arquivos Necessários

Após a publicação, você precisa copiar os seguintes arquivos junto com o executável:

- `appsettings.json` (ou `appsettings.Production.json`)
- `.env` (se estiver usando variáveis de ambiente)
- `wwwroot/` (pasta com o painel web)

## Executar o Aplicativo

### Windows

1. Execute `OlistGertecIntegration.exe`
2. Acesse `http://localhost:5000` ou `https://localhost:5001`
3. Acesse o painel em `http://localhost:5000/painel.html`

### Linux

1. Execute `./OlistGertecIntegration`
2. Acesse `http://localhost:5000`

## Configuração Inicial

1. Abra o navegador e acesse: `http://localhost:5000/painel.html`
2. Configure o IP do Gertec
3. Configure o token da API Olist
4. Clique em "Reconectar" para testar a conexão

## Notas Importantes

- O executável é auto-contido (não precisa do .NET instalado)
- O tamanho do executável será maior (~70-100MB) porque inclui o runtime
- Para produção, considere usar `appsettings.Production.json`
- O painel web está disponível em `/painel.html`

## Script de Publicação Rápida

Crie um arquivo `publish.bat` (Windows) ou `publish.sh` (Linux):

### Windows (publish.bat)

```batch
@echo off
echo Publicando aplicacao...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
echo.
echo Publicacao concluida!
echo Executavel em: bin\Release\net8.0\win-x64\publish\OlistGertecIntegration.exe
pause
```

### Linux (publish.sh)

```bash
#!/bin/bash
echo "Publicando aplicação..."
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true
echo ""
echo "Publicação concluída!"
echo "Executável em: bin/Release/net8.0/linux-x64/publish/OlistGertecIntegration"
```

