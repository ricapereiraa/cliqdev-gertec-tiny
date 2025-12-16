# Como Criar Atalho na Área de Trabalho

## Windows

### Opção 1: Usando o Script PowerShell (Recomendado)

1. Abra o PowerShell como Administrador (opcional, mas recomendado)
2. Navegue até a pasta do projeto:
   ```powershell
   cd "C:\caminho\para\projeto\api-carol"
   ```
3. Execute o script:
   ```powershell
   powershell -ExecutionPolicy Bypass -File "Criar-Atalho-Area-Trabalho.ps1"
   ```
4. Um atalho será criado na sua área de trabalho com o nome "API Gertec - Sincronizador.lnk"

### Opção 2: Criar Manualmente

1. Clique com o botão direito na área de trabalho
2. Selecione "Novo" > "Atalho"
3. Clique em "Procurar..." e navegue até o arquivo `Executar-API.bat`
4. Clique em "Avançar"
5. Digite o nome: "API Gertec - Sincronizador"
6. Clique em "Concluir"
7. Clique com o botão direito no atalho criado > "Propriedades"
8. Na aba "Atalho", clique em "Alterar ícone"
9. Escolha um ícone ou use um arquivo .ico personalizado

### Opção 3: Usar o Arquivo VBS (Execução Silenciosa)

1. Clique com o botão direito no arquivo `Executar-API.vbs`
2. Selecione "Criar atalho"
3. Arraste o atalho para a área de trabalho
4. Renomeie para "API Gertec - Sincronizador"

## Linux

### Opção 1: Copiar Arquivo .desktop

1. Copie o arquivo `api-gertec-sincronizador.desktop` para a área de trabalho:
   ```bash
   cp api-gertec-sincronizador.desktop ~/Área\ de\ trabalho/
   ```
2. Torne o arquivo executável:
   ```bash
   chmod +x ~/Área\ de\ trabalho/api-gertec-sincronizador.desktop
   ```
3. Clique duas vezes no ícone para executar

### Opção 2: Criar Manualmente

1. Clique com o botão direito na área de trabalho
2. Selecione "Criar um novo atalho" ou "Criar Launcher"
3. Preencha:
   - **Nome**: API Gertec - Sincronizador
   - **Comando**: `dotnet run`
   - **Diretório de trabalho**: `/caminho/para/projeto/api-carol`
   - **Ícone**: Escolha um ícone (pode usar `application-x-executable`)
4. Salve

## Personalizar Ícone

### Windows
- Baixe um ícone .ico de sites como [IconArchive](https://www.iconarchive.com/)
- No atalho, clique com botão direito > Propriedades > Alterar ícone
- Selecione o arquivo .ico

### Linux
- Coloque um arquivo `icon.png` na pasta do projeto
- Edite o arquivo `.desktop` e altere a linha `Icon=` para o caminho do ícone

## Executar em Segundo Plano (Windows)

Para executar sem mostrar a janela de console:

1. Use o arquivo `Executar-API-Silencioso.bat` ao criar o atalho
2. Ou edite o atalho e altere o destino para `Executar-API-Silencioso.bat`

## Verificar se Está Funcionando

Após executar o atalho, você deve ver:
- Uma janela de console com mensagens de inicialização
- Logs indicando que a API está rodando
- Mensagem "Monitoramento automático iniciado - atualizando arquivo a cada 1 minuto"

