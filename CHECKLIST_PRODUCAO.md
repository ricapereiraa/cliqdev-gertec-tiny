# Checklist de Deploy em Produção

## Configuração Inicial

### 1. Arquivo .env

Crie o arquivo `.env` na raiz do projeto:

```env
# API Olist/Tiny - CONFIGURE COM SEU TOKEN REAL
OLIST_API__BASE_URL=https://api.tiny.com.br/api2
OLIST_API__TOKEN=SEU_TOKEN_REAL_AQUI
OLIST_API__FORMAT=json

# Gertec - IP real do equipamento (192.168.1.57)
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

### 2. Verificação de Rede

**Do servidor onde a API rodará (192.168.1.100):**

```bash
# Teste ping
ping 192.168.1.57

# Teste porta TCP
telnet 192.168.1.57 6500
# ou no Windows PowerShell:
Test-NetConnection -ComputerName 192.168.1.57 -Port 6500
```

**Resultado esperado:** Conexão bem-sucedida

### 3. Instalação

**Windows (Recomendado):**
```powershell
# Execute como Administrador
.\install-windows-service.ps1
```

**Verificar instalação:**
```powershell
Get-Service -Name OlistGertecIntegration
```

## Validação Pós-Instalação

### Checklist de Testes

- [ ] Serviço iniciado e rodando
- [ ] Conexão com Gertec estabelecida
- [ ] Teste de escaneamento de código de barras
- [ ] Produto aparece no display do Gertec
- [ ] Produto não encontrado exibe corretamente
- [ ] Logs sem erros críticos
- [ ] API REST respondendo

### Testes Manuais

1. **Teste de Status:**
   ```bash
   curl http://localhost:5000/api/integration/status
   ```

2. **Teste de Conexão:**
   ```bash
   curl -X POST http://localhost:5000/api/integration/gertec/connect
   ```

3. **Teste de Busca:**
   ```bash
   curl http://localhost:5000/api/integration/product/7891234567890
   ```

4. **Teste Real:**
   - Escanear código de barras no Gertec
   - Verificar se produto aparece no display
   - Verificar logs da aplicação

## Monitoramento

### Logs a Verificar

**Windows:**
- `.\logs\service.log` - Logs gerais
- `.\logs\service-error.log` - Erros

**Linux:**
```bash
journalctl -u olist-gertec -f
```

### Métricas Importantes

- Taxa de sucesso de conexão
- Tempo de resposta da API Olist
- Taxa de produtos encontrados
- Reconexões automáticas

## Manutenção

### Atualização

1. Parar serviço
2. Atualizar código
3. Recompilar
4. Reiniciar serviço

### Backup

- Manter backup do arquivo `.env`
- Manter backup de configurações
- Documentar alterações

## Troubleshooting Rápido

### Gertec não conecta
- Verificar IP: 192.168.1.57
- Testar ping
- Verificar firewall
- Verificar se estão na mesma rede

### Produto não encontrado
- Verificar token da API
- Verificar se código está cadastrado no Olist
- Verificar logs da API

### Serviço não inicia
- Verificar .NET Runtime instalado
- Verificar logs de erro
- Verificar permissões

## Suporte

Para problemas:
1. Consultar logs
2. Verificar `PRODUCAO.md`
3. Verificar `ARQUITETURA_REDE.md`
4. Verificar `TROUBLESHOOTING.md`

