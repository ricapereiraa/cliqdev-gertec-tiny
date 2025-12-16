# Instalação no Windows - Cliqdev Gertec e Tiny

## Requisitos

- Windows 10/11 ou Windows Server 2016+
- .NET 8.0 Runtime ou SDK
- Permissões de Administrador

## Instalação Rápida

### Opção 1: Usando PowerShell (Recomendado)

1. **Abra o PowerShell como Administrador:**
   - Clique com botão direito no menu Iniciar
   - Selecione "Windows PowerShell (Admin)" ou "Terminal (Admin)"

2. **Navegue até a pasta do projeto:**
   ```powershell
   cd "C:\caminho\para\projeto"
   ```

3. **Execute o instalador:**
   ```powershell
   .\install-windows.ps1
   ```

4. **Se aparecer erro de política de execução:**
   ```powershell
   Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
   .\install-windows.ps1
   ```

### Opção 2: Usando Batch (.bat)

1. **Clique com botão direito em `install-windows.bat`**
2. **Selecione "Executar como administrador"**
3. **Aguarde a instalação concluir**

## O que o Instalador Faz

1. Compila a aplicação (se necessário)
2. Cria serviço do Windows com nome: `CliqdevGertecTiny`
3. Configura para iniciar automaticamente ao ligar o computador
4. Cria atalho na área de trabalho: **Cliqdev Gertec e Tiny**
5. Inicia o serviço automaticamente

## Verificar Instalação

### Verificar se o serviço está rodando:

```powershell
Get-Service -Name CliqdevGertecTiny
```

### Verificar status detalhado:

```powershell
sc query CliqdevGertecTiny
```

### Acessar o painel:

Abra o navegador e acesse: **http://localhost:5000/painel.html**

## Gerenciar o Serviço

### Parar o serviço:

```powershell
Stop-Service -Name CliqdevGertecTiny
```

### Iniciar o serviço:

```powershell
Start-Service -Name CliqdevGertecTiny
```

### Reiniciar o serviço:

```powershell
Restart-Service -Name CliqdevGertecTiny
```

### Ver logs do serviço:

```powershell
Get-EventLog -LogName Application -Source CliqdevGertecTiny -Newest 50
```

## Desinstalar

### Opção 1: PowerShell

```powershell
.\uninstall-windows.ps1
```

### Opção 2: Manualmente

```powershell
# Parar e remover serviço
Stop-Service -Name CliqdevGertecTiny
sc delete CliqdevGertecTiny

# Remover atalho
Remove-Item "$env:USERPROFILE\Desktop\Cliqdev Gertec e Tiny.lnk"
```

## Configuração Inicial

### 1. Configurar arquivo .env

Antes de iniciar, configure o arquivo `.env`:

```env
OLIST_API__TOKEN=seu_token_aqui
GERTEC__IP_ADDRESS=192.168.1.57
```

### 2. Acessar o painel

Após a instalação, acesse: **http://localhost:5000/painel.html**

No painel você pode:
- Configurar IP do Gertec
- Configurar Token da API
- Ver estatísticas
- Gerenciar conexões

## Ícone Personalizado

Para usar o ícone CLIQDEV:

1. Salve a logo como `icon.ico` na pasta do projeto
2. O instalador usará automaticamente o ícone
3. O atalho na área de trabalho terá o ícone personalizado

## Solução de Problemas

### Serviço não inicia

1. Verifique os logs:
   ```powershell
   Get-EventLog -LogName Application -Source CliqdevApiGertecTiny -Newest 10
   ```

2. Verifique se o arquivo .env existe e está configurado

3. Teste executar manualmente:
   ```powershell
   cd "C:\caminho\para\api-carol"
   .\OlistGertecIntegration.exe
   ```

### Porta já em uso

Se a porta 5000 estiver em uso, edite o `appsettings.json` ou configure via variáveis de ambiente.

### Firewall bloqueando

Adicione exceção no Firewall do Windows para a porta 5000.

## Estrutura de Arquivos

Após a instalação:

```
projeto/
├── OlistGertecIntegration.exe    (executável)
├── .env                          (configurações)
├── icon.ico                      (ícone - opcional)
├── install-windows.ps1           (instalador)
├── uninstall-windows.ps1         (desinstalador)
└── ...

Área de Trabalho/
└── Cliqdev Gertec e Tiny.lnk     (atalho)
```

## Notas Importantes

- O serviço precisa de permissões de Administrador para ser criado
- O arquivo `.env` deve estar configurado antes de iniciar
- O serviço inicia automaticamente ao ligar o computador
- O atalho na área de trabalho permite iniciar manualmente
- Todas as configurações são salvas no arquivo `.env`

