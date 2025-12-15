# Integração Olist ERP com Gertec Busca Preço G2

Solução de integração em C# que conecta o sistema Olist ERP com o equipamento Gertec Busca Preço G2, permitindo consulta de preços em tempo real e sincronização automática de alterações de preços.

> **Validação Técnica:** A implementação foi validada contra as documentações oficiais. Veja `ANALISE_TECNICA.md` e `RESUMO_VALIDACAO.md` para detalhes.

## Funcionalidades

- Conexão TCP/IP com o Gertec Busca Preço G2
- Consulta de produtos por código de barras via API do Olist ERP
- Exibição de nome e preço no display do Gertec
- Monitoramento automático de mudanças de preços
- API REST para controle e monitoramento
- Reconexão automática em caso de falha

## Requisitos

- .NET 8.0 SDK
- Acesso à API do Olist ERP (token)
- Equipamento Gertec Busca Preço G2 na mesma rede local (ou com roteamento/VPN configurado)
- Conectividade TCP/IP entre servidor da API e Gertec (porta 6500)
- **NOTA:** O Gertec NÃO precisa estar na mesma máquina física da API, apenas na mesma rede ou com roteamento adequado

## Instalação

### Windows - Instalação como Serviço (Recomendado)

Para rodar como serviço do Windows com inicialização automática:

1. **Instale o .NET 8.0 Runtime:**
  - Download: https://dotnet.microsoft.com/download/dotnet/8.0
  - Instale o "ASP.NET Core Runtime 8.0"

2. **Configure o .env:**
  ```powershell
  Copy-Item env.example .env
  notepad .env
  ```

3. **Instale como serviço (Execute como Administrador):**
  ```powershell
  .\install-windows-service.ps1
  ```

O serviço iniciará automaticamente com o Windows!

**Guia completo:** Veja `DEPLOY_WINDOWS.md`

### Docker (Alternativa)

**Linux/Mac/Windows:**
```bash
docker-compose up -d
```

**Nota:** Use `docker-compose.yml` (padrão). Se tiver problemas no Windows, use `docker-compose -f docker-compose.windows.yml up -d`

### Instalar .NET SDK 8.0 (Desenvolvimento)

#### Fedora/RHEL:
```bash
sudo dnf install dotnet-sdk-8.0
# ou use o script: ./install-dotnet.sh
```

#### Ubuntu/Debian:
```bash
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0
```

#### Outros Linux:
Baixe de: https://dotnet.microsoft.com/download/dotnet/8.0

#### Windows:
Baixe o instalador de: https://dotnet.microsoft.com/download/dotnet/8.0

### Configurar o Projeto

1. Clone ou baixe o projeto
2. Configure as variáveis de ambiente:

**Opção 1: Usar arquivo .env (Recomendado)**
```bash
./setup-env.sh # Cria o arquivo .env
# Edite o arquivo .env com suas configurações
nano .env
```

**Opção 2: Editar appsettings.json diretamente**
Configure o arquivo `appsettings.json`:

```json
{
 "OlistApi": {
  "BaseUrl": "https://api.tiny.com.br/api2",
  "Token": "SEU_TOKEN_DO_OLIST",
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

**Nota:** Para mais informações sobre variáveis de ambiente, consulte `VARIAVEIS_AMBIENTE.md`

3. Restaure as dependências:
```bash
dotnet restore
```

4. Execute o projeto:
```bash
dotnet run
```

## Configuração

### OlistApi
- **BaseUrl**: URL base da API do Olist ERP
- **Token**: Token de autenticação da API
- **Format**: Formato de resposta (json)

### Gertec
- **IpAddress**: Endereço IP do equipamento Gertec na rede
 - **Como descobrir?** Veja `COMO_DESCOBRIR_IP.md` ou execute `./scripts/scan-network.sh`
- **Port**: Porta de comunicação (padrão: 6500)
- **ReconnectIntervalSeconds**: Intervalo de tentativas de reconexão
- **ResponseTimeoutMilliseconds**: Timeout para respostas

### PriceMonitoring
- **Enabled**: Habilita/desabilita monitoramento de preços
- **CheckIntervalMinutes**: Intervalo de verificação de mudanças (em minutos)

## Uso

### Iniciar a integração

A aplicação inicia automaticamente e:
1. Conecta ao Gertec Busca Preço G2
2. Escuta códigos de barras escaneados
3. Busca produtos no Olist ERP
4. Exibe nome e preço no display do Gertec

### API REST

A aplicação expõe uma API REST para controle:

#### Status da conexão
```
GET /api/integration/status
```

#### Conectar ao Gertec
```
POST /api/integration/gertec/connect
```

#### Desconectar do Gertec
```
POST /api/integration/gertec/disconnect
```

#### Enviar mensagem ao display
```
POST /api/integration/gertec/message
Body: {
 "linha1": "Mensagem linha 1",
 "linha2": "Mensagem linha 2",
 "tempoSegundos": 5
}
```

#### Obter MAC Address do Gertec
```
GET /api/integration/gertec/macaddress
```

#### Buscar produto por código de barras
```
GET /api/integration/product/{barcode}
```

#### Enviar produto específico ao Gertec
```
POST /api/integration/product/{barcode}/send
```

### Swagger UI

Quando executando em modo Development, acesse:
```
http://localhost:5000/swagger
```

## Protocolo Gertec

A aplicação implementa os seguintes comandos do protocolo Gertec:

- `#` - Recebe código de barras e responde com nome/preço
- `#mesg` - Envia mensagem ao display
- `#macaddr?` - Solicita MAC Address
- `#nfound` - Resposta quando produto não encontrado

## Formato de Dados

### Resposta de Produto ao Gertec

Formato: `#` + nome (80 bytes) + `|` + preço (20 bytes)

- Nome: 4 linhas x 20 colunas = 80 bytes
- Preço: 1 linha x 20 colunas = 20 bytes
- O caractere `#` não é permitido no campo preço

## Monitoramento de Preços

Quando habilitado, o sistema:
1. Verifica periodicamente mudanças de preços no Olist ERP
2. Atualiza o cache interno
3. Pode enviar notificações ao Gertec sobre atualizações

## Troubleshooting

### Problema: Não conecta ao Gertec

- Verifique se o IP está correto no `appsettings.json`
- Verifique se o Gertec está ligado e na mesma rede
- Verifique se a porta 6500 está aberta no firewall
- Verifique os logs da aplicação

### Problema: Produto não encontrado

- Verifique se o código de barras está cadastrado no Olist ERP
- Verifique se o token da API está correto
- Verifique os logs para erros de comunicação com a API

### Problema: Preço não aparece corretamente

- Verifique se o formato do preço está correto no Olist
- Verifique se o nome do produto não excede 80 caracteres

## Logs

Os logs são exibidos no console e incluem:
- Conexões/desconexões do Gertec
- Códigos de barras recebidos
- Consultas à API do Olist
- Erros e exceções

## Manutenção

### Reinstalação

1. Pare a aplicação
2. Atualize o `appsettings.json` se necessário
3. Execute `dotnet restore` se houver mudanças nas dependências
4. Execute `dotnet run` novamente

### Backup de Configuração

Mantenha uma cópia do `appsettings.json` em local seguro para facilitar reinstalação.

## Arquitetura de Rede

**Importante:** O Gertec Busca Preço G2 NÃO precisa estar na mesma máquina física da API. A comunicação é feita via TCP/IP na rede local.

**Requisitos:**
- Mesma rede local (recomendado) OU
- Roteamento configurado OU  
- VPN configurada

Para mais detalhes sobre arquitetura de rede, consulte `ARQUITETURA_REDE.md`.

## Suporte

Para mais informações sobre:
- API do Olist ERP: https://tiny.com.br/api-docs/api
- Gertec Busca Preço G2: https://www.gertec.com.br/download-center/

## Licença

Este projeto foi desenvolvido para integração específica entre Olist ERP e Gertec Busca Preço G2.

