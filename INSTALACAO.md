# Guia de Instalação - Integração Olist ERP com Gertec Busca Preço G2

## Pré-requisitos

1. **.NET 8.0 SDK** instalado
  - Verificar instalação: `dotnet --version`
  - Download: https://dotnet.microsoft.com/download/dotnet/8.0

2. **Token da API do Olist ERP**
  - Obtenha o token em: https://tiny.com.br/api-docs/api
  - Você precisará de acesso administrativo ao Olist ERP

3. **Informações do Gertec Busca Preço G2**
  - Endereço IP do equipamento na rede
  - Porta de comunicação (padrão: 6500)
  - Equipamento ligado e conectado à mesma rede

## Passo a Passo da Instalação

### 1. Preparar o Ambiente

```bash
# Navegue até o diretório do projeto
cd /caminho/para/api-carol

# Verifique se o .NET está instalado
dotnet --version
```

### 2. Configurar a Aplicação

1. Copie o arquivo de exemplo de configuração:
```bash
cp appsettings.Example.json appsettings.json
```

2. Edite o arquivo `appsettings.json` e configure:

```json
{
 "OlistApi": {
  "BaseUrl": "https://api.tiny.com.br/api2",
  "Token": "COLE_SEU_TOKEN_AQUI",
  "Format": "json"
 },
 "Gertec": {
  "IpAddress": "192.168.0.100", // IP do seu Gertec
  "Port": 6500,
  "ReconnectIntervalSeconds": 5,
  "ResponseTimeoutMilliseconds": 500
 },
 "PriceMonitoring": {
  "Enabled": true,
  "CheckIntervalMinutes": 5
 }
}
```

**Importante:**
- Substitua `COLE_SEU_TOKEN_AQUI` pelo token real da API do Olist
- Substitua `192.168.0.100` pelo IP real do equipamento Gertec na sua rede
- Ajuste o intervalo de monitoramento conforme necessário

### 3. Instalar Dependências

```bash
dotnet restore
```

### 4. Compilar o Projeto

```bash
dotnet build
```

### 5. Executar a Aplicação

#### Modo Desenvolvimento (com Swagger)
```bash
dotnet run
```

A aplicação estará disponível em:
- HTTP: http://localhost:5000
- HTTPS: https://localhost:5001
- Swagger UI: http://localhost:5000/swagger

#### Modo Produção

```bash
dotnet publish -c Release -o ./publish
cd publish
dotnet OlistGertecIntegration.dll
```

### 6. Verificar Funcionamento

1. **Verificar Status da Conexão:**
```bash
curl http://localhost:5000/api/integration/status
```

2. **Testar Conexão com Gertec:**
```bash
curl -X POST http://localhost:5000/api/integration/gertec/connect
```

3. **Testar Consulta de Produto:**
```bash
curl http://localhost:5000/api/integration/product/7891234567890
```

## Configuração como Serviço do Windows (Opcional)

### Usando NSSM (Non-Sucking Service Manager)

1. Baixe o NSSM: https://nssm.cc/download

2. Instale o serviço:
```bash
nssm install OlistGertecIntegration "C:\Program Files\dotnet\dotnet.exe" "C:\caminho\para\api-carol\OlistGertecIntegration.dll"
```

3. Configure o diretório de trabalho:
```bash
nssm set OlistGertecIntegration AppDirectory "C:\caminho\para\api-carol"
```

4. Inicie o serviço:
```bash
nssm start OlistGertecIntegration
```

## Configuração como Serviço do Linux (systemd)

1. Crie o arquivo de serviço:
```bash
sudo nano /etc/systemd/system/olist-gertec.service
```

2. Adicione o conteúdo:
```ini
[Unit]
Description=Integração Olist ERP com Gertec Busca Preço G2
After=network.target

[Service]
Type=notify
WorkingDirectory=/caminho/para/api-carol
ExecStart=/usr/bin/dotnet /caminho/para/api-carol/OlistGertecIntegration.dll
Restart=always
RestartSec=10
User=seu-usuario
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

3. Habilite e inicie o serviço:
```bash
sudo systemctl enable olist-gertec.service
sudo systemctl start olist-gertec.service
sudo systemctl status olist-gertec.service
```

## Verificação Pós-Instalação

### Checklist

- [ ] Aplicação inicia sem erros
- [ ] Conexão com Gertec estabelecida (verificar logs)
- [ ] API responde corretamente (testar endpoints)
- [ ] Código de barras escaneado no Gertec retorna produto
- [ ] Monitoramento de preços funcionando (se habilitado)

### Logs

Os logs são exibidos no console. Para produção, configure redirecionamento:

```bash
dotnet OlistGertecIntegration.dll > logs.txt 2>&1
```

## Troubleshooting

### Erro: "Token do Olist não configurado"
- Verifique se o token está correto no `appsettings.json`
- Certifique-se de que o arquivo não tem erros de sintaxe JSON

### Erro: "Falha ao conectar ao Gertec"
- Verifique se o IP está correto
- Verifique se o Gertec está ligado
- Teste conectividade: `ping 192.168.0.100`
- Verifique firewall/porta 6500

### Erro: "Produto não encontrado"
- Verifique se o código de barras existe no Olist ERP
- Teste a API do Olist diretamente
- Verifique os logs para detalhes do erro

### Aplicação não inicia
- Verifique se o .NET 8.0 está instalado
- Execute `dotnet restore` novamente
- Verifique permissões de arquivo

## Reinstalação

Para reinstalar após mudanças ou falhas:

1. **Parar a aplicação:**
```bash
# Se estiver rodando como serviço
sudo systemctl stop olist-gertec # Linux
# ou pare manualmente se estiver rodando no terminal
```

2. **Fazer backup da configuração:**
```bash
cp appsettings.json appsettings.json.backup
```

3. **Atualizar código (se necessário):**
```bash
git pull # se usar git
# ou copiar novos arquivos
```

4. **Restaurar dependências:**
```bash
dotnet restore
```

5. **Recompilar:**
```bash
dotnet build
```

6. **Restaurar configuração:**
```bash
cp appsettings.json.backup appsettings.json
# Edite se necessário
```

7. **Reiniciar:**
```bash
dotnet run
# ou
sudo systemctl start olist-gertec
```

## Suporte

Para problemas ou dúvidas:
- Verifique os logs da aplicação
- Consulte a documentação da API do Olist: https://tiny.com.br/api-docs/api
- Consulte o manual do Gertec: https://www.gertec.com.br/download-center/

