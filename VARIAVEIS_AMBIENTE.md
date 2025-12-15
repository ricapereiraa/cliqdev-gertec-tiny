# Configuração de Variáveis de Ambiente

Este projeto suporta configuração através de arquivo `.env` para facilitar o gerenciamento de credenciais e configurações sensíveis.

## Como Usar

### 1. Criar arquivo .env

Copie o arquivo de exemplo:

```bash
cp env.example .env
```

### 2. Editar o arquivo .env

Abra o arquivo `.env` e configure as variáveis:

```env
# Token da API do Olist (OBRIGATÓRIO)
OLIST_API__TOKEN=seu_token_aqui

# IP do Gertec (ajuste conforme sua rede)
GERTEC__IP_ADDRESS=192.168.0.100
```

### 3. Formato das Variáveis

No .NET, para mapear variáveis de ambiente para a estrutura hierárquica do `appsettings.json`, use **dois underscores (`__`)** para representar dois pontos (`:`):

- `OlistApi:Token` no appsettings.json → `OLIST_API__TOKEN` no .env
- `Gertec:IpAddress` no appsettings.json → `GERTEC__IP_ADDRESS` no .env
- `PriceMonitoring:Enabled` no appsettings.json → `PRICE_MONITORING__ENABLED` no .env

### 4. Variáveis Disponíveis

#### API do Olist ERP
- `OLIST_API__BASE_URL` - URL base da API (padrão: https://api.tiny.com.br/api2)
- `OLIST_API__TOKEN` - **OBRIGATÓRIO** - Token de autenticação da API
- `OLIST_API__FORMAT` - Formato de resposta (padrão: json)

#### Gertec Busca Preço G2
- `GERTEC__IP_ADDRESS` - Endereço IP do equipamento (padrão: 192.168.0.100)
- `GERTEC__PORT` - Porta de comunicação (padrão: 6500)
- `GERTEC__RECONNECT_INTERVAL_SECONDS` - Intervalo de reconexão (padrão: 5)
- `GERTEC__RESPONSE_TIMEOUT_MILLISECONDS` - Timeout de resposta (padrão: 500)

#### Monitoramento de Preços
- `PRICE_MONITORING__ENABLED` - Habilitar monitoramento (padrão: true)
- `PRICE_MONITORING__CHECK_INTERVAL_MINUTES` - Intervalo de verificação (padrão: 5)

#### Ambiente
- `ASPNETCORE_ENVIRONMENT` - Ambiente de execução (Development/Production)
- `LOG_LEVEL` - Nível de log (Debug/Information/Warning/Error)

## Prioridade de Configuração

As configurações são carregadas na seguinte ordem (última sobrescreve):

1. `appsettings.json` (valores padrão)
2. `appsettings.{Environment}.json` (valores por ambiente)
3. Variáveis de ambiente do sistema
4. Arquivo `.env` (carregado primeiro, mas pode ser sobrescrito)

## Segurança

AVISO: **IMPORTANTE**: 

- O arquivo `.env` contém informações sensíveis e **NÃO deve ser commitado** no Git
- O arquivo `.env` já está no `.gitignore`
- Use `env.example` como template para documentação
- Em produção, use variáveis de ambiente do sistema ou um gerenciador de segredos

## Exemplo Completo

```env
# API Olist
OLIST_API__BASE_URL=https://api.tiny.com.br/api2
OLIST_API__TOKEN=abc123xyz789token
OLIST_API__FORMAT=json

# Gertec
GERTEC__IP_ADDRESS=192.168.1.50
GERTEC__PORT=6500
GERTEC__RECONNECT_INTERVAL_SECONDS=10
GERTEC__RESPONSE_TIMEOUT_MILLISECONDS=1000

# Monitoramento
PRICE_MONITORING__ENABLED=true
PRICE_MONITORING__CHECK_INTERVAL_MINUTES=10

# Ambiente
ASPNETCORE_ENVIRONMENT=Production
LOG_LEVEL=Warning
```

## Verificação

Para verificar se as variáveis estão sendo carregadas, execute a aplicação e verifique os logs. Você verá:

```
Variáveis de ambiente carregadas do arquivo .env
```

## Troubleshooting

### Variáveis não estão sendo carregadas

1. Verifique se o arquivo `.env` existe no diretório raiz do projeto
2. Verifique se está usando dois underscores (`__`) na nomenclatura
3. Verifique se não há espaços ao redor do sinal de igual (`=`)
4. Verifique os logs da aplicação

### Erro: "Token do Olist não configurado"

Certifique-se de que `OLIST_API__TOKEN` está definido no `.env` ou no `appsettings.json`.

### Valores não estão sendo aplicados

Lembre-se que variáveis de ambiente do sistema têm prioridade sobre o arquivo `.env`. Verifique se não há variáveis de ambiente definidas no sistema que estejam sobrescrevendo.

