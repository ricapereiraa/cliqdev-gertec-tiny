# Configuração para Produção

## Configurações Aplicadas

### IP do Gertec Configurado

Baseado nas informações do display do Gertec:
- **IP do Gertec:** 192.168.1.57
- **IP do Servidor:** 192.168.1.100 (onde a API deve rodar)
- **Porta:** 6500

### Arquivos Atualizados

1. **env.example** - IP do Gertec atualizado para 192.168.1.57
2. **appsettings.json** - IP do Gertec atualizado para 192.168.1.57
3. **appsettings.Production.json** - Criado com configurações otimizadas para produção

## Configuração Final

### 1. Arquivo .env

Crie o arquivo `.env` na raiz do projeto:

```env
# API Olist/Tiny
OLIST_API__BASE_URL=https://api.tiny.com.br/api2
OLIST_API__TOKEN=SEU_TOKEN_REAL_AQUI
OLIST_API__FORMAT=json

# Gertec (IP real do equipamento)
GERTEC__IP_ADDRESS=192.168.1.57
GERTEC__PORT=6500
GERTEC__RECONNECT_INTERVAL_SECONDS=5
GERTEC__RESPONSE_TIMEOUT_MILLISECONDS=500

# Monitoramento
PRICE_MONITORING__ENABLED=true
PRICE_MONITORING__CHECK_INTERVAL_MINUTES=5

# Ambiente de Produção
ASPNETCORE_ENVIRONMENT=Production
LOG_LEVEL=Warning
```

### 2. Verificações Antes de Produção

- [ ] Token da API Olist configurado no .env
- [ ] IP do Gertec verificado (192.168.1.57)
- [ ] Servidor da API na mesma rede (192.168.1.100)
- [ ] Conectividade testada (ping e telnet)
- [ ] Firewall configurado (se necessário)
- [ ] Logs configurados para produção

### 3. Teste de Conectividade

**Do servidor onde a API rodará (192.168.1.100):**

```bash
# Teste ping
ping 192.168.1.57

# Teste porta TCP
telnet 192.168.1.57 6500
```

### 4. Executar em Produção

**Windows (Serviço):**
```powershell
.\install-windows-service.ps1
```

**Linux (systemd):**
```bash
sudo systemctl start olist-gertec
```

**Docker:**
```bash
docker-compose up -d
```

## Otimizações Aplicadas

### Logs
- Logs detalhados apenas em Development
- Production: apenas Warning e Information
- Logs de debug desabilitados em produção

### Performance
- Timeouts configurados no TcpClient
- Reconexão automática otimizada
- Cache de produtos implementado

### Segurança
- Swagger desabilitado em produção
- Logs não expõem informações sensíveis
- Variáveis de ambiente para credenciais

## Monitoramento

### Verificar Status

```bash
# Status da API
curl http://localhost:5000/api/integration/status

# Status do serviço (Windows)
Get-Service -Name OlistGertecIntegration
```

### Logs

**Windows:**
- `.\logs\service.log` - Logs gerais
- `.\logs\service-error.log` - Erros

**Linux:**
```bash
journalctl -u olist-gertec -f
```

**Docker:**
```bash
docker-compose logs -f
```

## Troubleshooting

### Gertec não conecta

1. Verificar IP: `192.168.1.57`
2. Testar conectividade: `ping 192.168.1.57`
3. Testar porta: `telnet 192.168.1.57 6500`
4. Verificar se estão na mesma rede (192.168.1.0/24)

### API não inicia

1. Verificar token no .env
2. Verificar logs de erro
3. Verificar se .NET Runtime está instalado

### Produtos não aparecem

1. Verificar token da API Olist
2. Verificar logs da aplicação
3. Testar busca manual: `curl http://localhost:5000/api/integration/product/CODIGO`

## Checklist de Deploy

- [ ] .env configurado com token real
- [ ] IP do Gertec verificado (192.168.1.57)
- [ ] Conectividade testada
- [ ] Serviço instalado e iniciado
- [ ] Logs verificados
- [ ] Teste de escaneamento realizado
- [ ] Monitoramento configurado

## Suporte

Em caso de problemas:
1. Verificar logs da aplicação
2. Verificar conectividade de rede
3. Verificar configurações no .env
4. Consultar documentação técnica

