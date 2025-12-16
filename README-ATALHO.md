# Executar API com Atalho na Area de Trabalho

## Arquivos Criados

1. **Executar-API.bat** - Script principal para executar a API (Windows)
2. **Criar-Atalho-Area-Trabalho.ps1** - Script PowerShell para criar atalho automaticamente
3. **Executar-API.vbs** - Versao silenciosa (sem janela de console)
4. **Executar-API-Silencioso.bat** - Executa em segundo plano
5. **api-gertec-sincronizador.desktop** - Atalho para Linux

## Windows - Metodo Rapido

### Passo 1: Criar o Atalho

**Opcao A - Automatico (Recomendado):**
```powershell
powershell -ExecutionPolicy Bypass -File "Criar-Atalho-Area-Trabalho.ps1"
```

**Opcao B - Manual:**
1. Clique com botao direito na area de trabalho
2. Novo > Atalho
3. Navegue ate `Executar-API.bat`
4. Nome: "API Gertec - Sincronizador"
5. Concluir

### Passo 2: Personalizar Icone (Opcional)

1. Clique com botao direito no atalho > Propriedades
2. Aba "Atalho" > Botao "Alterar icone"
3. Escolha um icone ou use um arquivo .ico personalizado

## Linux

```bash
# Copiar arquivo .desktop para area de trabalho
cp api-gertec-sincronizador.desktop ~/Área\ de\ trabalho/

# Tornar executavel
chmod +x ~/Área\ de\ trabalho/api-gertec-sincronizador.desktop
```

## Verificar se Funcionou

Ao executar o atalho, voce deve ver:

```
========================================
  API Gertec - Sincronizador Tiny ERP
========================================

[OK] .NET SDK encontrado: 8.0.x
[OK] Executando API...
```

E depois:
```
Monitoramento automatico iniciado - atualizando arquivo a cada 1 minuto
```

## Solucao de Problemas

### Erro: ".NET SDK nao encontrado"
- Instale o .NET 8.0 SDK: https://dotnet.microsoft.com/download
- Reinicie o computador apos instalar

### Erro: "Arquivo .env nao encontrado"
- Nao e critico, a API usara `appsettings.json`
- Crie um arquivo `.env` se quiser usar variaveis de ambiente

### Atalho nao executa
- Verifique se o caminho do projeto esta correto
- Tente executar `Executar-API.bat` diretamente para ver erros

## Notas

- O atalho executa `dotnet run` na pasta do projeto
- A API ficara rodando ate voce pressionar Ctrl+C
- Para executar em segundo plano, use `Executar-API-Silencioso.bat`

