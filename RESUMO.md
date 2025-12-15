# Resumo da Solução - Integração Olist ERP com Gertec Busca Preço G2

## Visão Geral

Solução completa de integração em C# (.NET 8.0) que conecta o sistema Olist ERP com o equipamento Gertec Busca Preço G2, permitindo:

1. **Consulta de Preços em Tempo Real**: Quando um código de barras é escaneado no Gertec, a aplicação busca o produto no Olist ERP e exibe nome e preço no display.

2. **Monitoramento Automático de Preços**: Verifica periodicamente mudanças de preços no Olist ERP e atualiza o cache interno.

3. **API REST para Controle**: Endpoints para monitoramento, controle e testes da integração.

## Arquitetura

```
┌─────────────┐     ┌──────────────┐     ┌─────────────┐
│ Gertec  │◄──TCP──►│ Integração │◄──HTTP──►│ Olist ERP  │
│ Busca Preço│     │   API   │     │   API   │
│   G2   │     │  (C# .NET) │     │       │
└─────────────┘     └──────────────┘     └─────────────┘
```

## Componentes Principais

### 1. GertecProtocolService
- Gerencia conexão TCP/IP com o Gertec
- Implementa protocolo de comunicação do Gertec
- Escuta códigos de barras escaneados
- Envia respostas formatadas (nome + preço)

### 2. OlistApiService
- Cliente HTTP para API do Olist ERP
- Busca produtos por código de barras
- Lista todos os produtos (para monitoramento)
- Formata preços para exibição

### 3. IntegrationService
- Serviço em background que orquestra a integração
- Conecta ao Gertec na inicialização
- Processa códigos de barras recebidos
- Monitora mudanças de preços (opcional)

### 4. IntegrationController
- API REST para controle e monitoramento
- Endpoints para status, conexão, testes

## Fluxo de Funcionamento

### Consulta de Produto

1. Cliente escaneia código de barras no Gertec
2. Gertec envia `#codigo` via TCP/IP
3. `GertecProtocolService` recebe e dispara evento
4. `IntegrationService` busca produto no Olist ERP
5. Se encontrado: formata nome (80 bytes) + preço (20 bytes) e envia
6. Se não encontrado: envia `#nfound`
7. Gertec exibe informação no display

### Monitoramento de Preços

1. `IntegrationService` verifica produtos no Olist (a cada X minutos)
2. Compara preços com cache interno
3. Se houver mudança: atualiza cache e opcionalmente notifica Gertec

## Protocolo Gertec Implementado

### Comandos Recebidos
- `#codigo` - Código de barras escaneado

### Comandos Enviados
- `#nome|preco` - Resposta com produto encontrado
- `#nfound` - Produto não encontrado
- `#mesg` - Mensagem no display
- `#macaddr?` - Solicitar MAC Address

## Formato de Dados

### Resposta de Produto
```
# + [nome 80 bytes] + | + [preço 20 bytes]
```

- Nome: 4 linhas x 20 colunas = 80 bytes
- Preço: 1 linha x 20 colunas = 20 bytes
- Caractere `#` não permitido no preço

## Configuração

### appsettings.json

```json
{
 "OlistApi": {
  "BaseUrl": "https://api.tiny.com.br/api2",
  "Token": "SEU_TOKEN",
  "Format": "json"
 },
 "Gertec": {
  "IpAddress": "192.168.0.100",
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

## Endpoints da API

- `GET /api/integration/status` - Status da conexão
- `POST /api/integration/gertec/connect` - Conectar ao Gertec
- `POST /api/integration/gertec/disconnect` - Desconectar
- `POST /api/integration/gertec/message` - Enviar mensagem
- `GET /api/integration/gertec/macaddress` - Obter MAC Address
- `GET /api/integration/product/{barcode}` - Buscar produto
- `POST /api/integration/product/{barcode}/send` - Enviar produto ao Gertec

## Instalação Rápida

1. Configure `appsettings.json` com token do Olist e IP do Gertec
2. Execute `dotnet restore`
3. Execute `dotnet run`
4. Acesse `http://localhost:5000/swagger` para documentação

## Características

 **Fácil de Operar**: Interface REST simples e logs claros
 **Fácil de Reinstalar**: Scripts de instalação e documentação completa
 **Reconexão Automática**: Reconecta ao Gertec em caso de falha
 **Monitoramento**: Verifica mudanças de preços automaticamente
 **Robusto**: Tratamento de erros e logging completo

## Próximos Passos

1. Configure o `appsettings.json` com suas credenciais
2. Teste a conexão com o Gertec
3. Teste a busca de produtos
4. Configure como serviço do sistema (opcional)
5. Monitore os logs para garantir funcionamento

## Suporte

- Documentação completa: `README.md` e `INSTALACAO.md`
- Logs detalhados no console
- Swagger UI para testes da API
- Código comentado e organizado

