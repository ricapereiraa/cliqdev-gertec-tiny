# Teste de Variáveis de Ambiente

## Como Verificar se as Variáveis Estão Sendo Carregadas

### 1. Verificar no Console

Ao iniciar a aplicação, você verá:

```
Variáveis de ambiente carregadas do arquivo .env
Configurações carregadas:
  OlistApi:BaseUrl = https://api.tiny.com.br/api2
  OlistApi:Token = CONFIGURADO
  Gertec:IpAddress = 192.168.0.100
```

### 2. Verificar via API

```bash
# Verificar status (mostra se está conectado)
curl http://localhost:5000/api/integration/status
```

### 3. Teste Manual

Crie um arquivo `.env` na raiz do projeto:

```env
OLIST_API__TOKEN=seu_token_aqui
GERTEC__IP_ADDRESS=192.168.1.50
```

Execute:
```bash
dotnet run
```

Verifique no console se as variáveis foram carregadas.

## Formato Correto das Variáveis

### Formato Correto (dois underscores)

```env
OLIST_API__TOKEN=seu_token
OLIST_API__BASE_URL=https://api.tiny.com.br/api2
GERTEC__IP_ADDRESS=192.168.0.100
GERTEC__PORT=6500
```

### Formato Incorreto

```env
OLIST_API_TOKEN=seu_token     # ERRO: Falta um underscore
OLIST_API:TOKEN=seu_token     # ERRO: Não use dois pontos
OlistApi:Token=seu_token      # ERRO: Case sensitive, use maiúsculas
```

## Mapeamento

| appsettings.json | .env | Descrição |
|------------------|------|-----------|
| `OlistApi:Token` | `OLIST_API__TOKEN` | Token da API |
| `OlistApi:BaseUrl` | `OLIST_API__BASE_URL` | URL base da API |
| `OlistApi:Format` | `OLIST_API__FORMAT` | Formato (json) |
| `Gertec:IpAddress` | `GERTEC__IP_ADDRESS` | IP do Gertec |
| `Gertec:Port` | `GERTEC__PORT` | Porta do Gertec |
| `PriceMonitoring:Enabled` | `PRICE_MONITORING__ENABLED` | Habilitar monitoramento |

## Prioridade de Configuração

1. **Variáveis de ambiente do sistema** (maior prioridade)
2. **Arquivo .env** (carregado pelo DotNetEnv)
3. **appsettings.json** (fallback)

## Troubleshooting

### Problema: Variáveis não estão sendo carregadas

1. **Verifique se o arquivo .env existe:**
  ```bash
  ls -la .env
  ```

2. **Verifique o formato:**
  - Use dois underscores (`__`)
  - Use maiúsculas
  - Sem espaços ao redor do `=`

3. **Verifique os logs:**
  - Deve aparecer: "Variáveis de ambiente carregadas do arquivo .env"
  - Se aparecer "AVISO: Arquivo .env não encontrado", o arquivo não existe

### Problema: Token não está sendo usado

1. **Verifique se está no .env:**
  ```bash
  cat .env | grep TOKEN
  ```

2. **Verifique se o formato está correto:**
  ```env
  OLIST_API__TOKEN=seu_token_aqui
  ```

3. **Reinicie a aplicação:**
  ```bash
  dotnet run
  ```

## Exemplo Completo de .env

```env
# API Olist/Tiny
OLIST_API__BASE_URL=https://api.tiny.com.br/api2
OLIST_API__TOKEN=abc123xyz789seu_token_aqui
OLIST_API__FORMAT=json

# Gertec
GERTEC__IP_ADDRESS=192.168.1.50
GERTEC__PORT=6500
GERTEC__RECONNECT_INTERVAL_SECONDS=5
GERTEC__RESPONSE_TIMEOUT_MILLISECONDS=500

# Monitoramento
PRICE_MONITORING__ENABLED=true
PRICE_MONITORING__CHECK_INTERVAL_MINUTES=5

# Ambiente
ASPNETCORE_ENVIRONMENT=Production
LOG_LEVEL=Information
```

