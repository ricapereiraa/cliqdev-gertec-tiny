# Arquitetura de Rede - Integração Olist ERP com Gertec

## Requisitos de Rede

### Não é Necessário Estar na Mesma Máquina

O equipamento Gertec Busca Preço G2 **NÃO precisa estar na mesma máquina física** onde a API está rodando. A comunicação é feita via **TCP/IP na rede local**.

## Arquitetura de Rede

```
┌─────────────────┐         ┌──────────────────┐         ┌─────────────┐
│   Gertec Busca  │         │  Servidor com    │         │  Olist ERP  │
│   Preço G2      │◄──TCP──►│  API (Windows)   │◄──HTTP──►│   (Cloud)   │
│                 │         │                  │         │             │
│  IP: 192.168.1.50│         │  IP: 192.168.1.100│         │  Internet   │
└─────────────────┘         └──────────────────┘         └─────────────┘
     Porta 6500                  Porta 5000
```

## Requisitos de Conectividade

### 1. Mesma Rede Local (Recomendado)

**Cenário Ideal:**
- Gertec e servidor da API na mesma rede local (LAN)
- Exemplo: Ambos na rede `192.168.1.0/24`
- Comunicação direta via TCP/IP

**Vantagens:**
- Baixa latência
- Maior confiabilidade
- Sem necessidade de configuração de firewall complexa

### 2. Redes Diferentes (Com Roteamento)

**Cenário Alternativo:**
- Gertec em uma rede, API em outra
- Requer roteamento configurado
- Pode precisar de configuração de firewall

**Exemplo:**
- Gertec: `192.168.1.50` (Rede A)
- API: `192.168.2.100` (Rede B)
- Roteador configurado para permitir comunicação

### 3. VPN (Para Ambientes Remotos)

**Cenário Avançado:**
- Gertec em local físico diferente
- API em servidor remoto
- Conectados via VPN

**Requisitos:**
- VPN configurada entre os locais
- Roteamento adequado
- Porta 6500 acessível via VPN

## Configuração da API

### Configurar IP do Gertec

No arquivo `.env` ou `appsettings.json`:

```env
GERTEC__IP_ADDRESS=192.168.1.50
GERTEC__PORT=6500
```

**Importante:** Use o IP do Gertec na rede, não `localhost` ou `127.0.0.1`.

## Verificação de Conectividade

### Teste de Conectividade

**Do servidor onde a API está rodando:**

```bash
# Teste ping
ping 192.168.1.50

# Teste porta TCP
telnet 192.168.1.50 6500
# ou
nc -zv 192.168.1.50 6500
```

### Teste via API

```bash
# Verificar status da conexão
curl http://localhost:5000/api/integration/status

# Tentar conectar
curl -X POST http://localhost:5000/api/integration/gertec/connect
```

## Cenários de Implantação

### Cenário 1: Loja Física (Recomendado)

```
Loja Física
├── Gertec Busca Preço G2 (192.168.1.50)
├── Servidor/PC com API (192.168.1.100)
└── Roteador/Switch Local
    └── Internet (para API Olist)
```

**Vantagens:**
- Tudo na mesma rede local
- Comunicação rápida e confiável
- Fácil manutenção

### Cenário 2: Múltiplas Lojas

```
Loja 1                    Loja 2                    Servidor Central
├── Gertec (192.168.1.50) ├── Gertec (192.168.2.50) ├── API (10.0.0.100)
└── Rede Local            └── Rede Local            └── VPN/Rede Corporativa
```

**Configuração:**
- Cada loja tem seu próprio Gertec
- API centralizada em servidor
- Comunicação via VPN ou rede corporativa
- Múltiplas instâncias da API (uma por Gertec) ou API única gerenciando múltiplas conexões

### Cenário 3: Cloud/Remoto

```
Gertec (Loja)              Internet/VPN              Servidor Cloud
├── IP: 192.168.1.50  ───►  ───►  ───►  ├── API (Cloud)
└── Porta 6500                            └── IP Público/VPN
```

**Requisitos:**
- VPN entre loja e cloud
- Porta 6500 roteada via VPN
- IP estático ou DNS para o Gertec

## Firewall e Segurança

### Portas Necessárias

**No servidor da API:**
- Porta 5000 (API REST) - Opcional, apenas para monitoramento
- Porta 6500 - **NÃO precisa estar aberta** (API conecta no Gertec, não recebe conexões)

**No Gertec:**
- Porta 6500 - Deve aceitar conexões TCP do servidor da API

### Regras de Firewall

**Servidor da API (Windows Firewall):**
```powershell
# Permitir conexões de saída para porta 6500 (geralmente já permitido)
# Não precisa abrir porta 6500 para entrada
```

**Roteador/Switch:**
- Permitir comunicação TCP entre IP da API e IP do Gertec
- Porta 6500 deve estar acessível na rede local

## Troubleshooting de Rede

### Problema: Não conecta ao Gertec

1. **Verificar conectividade básica:**
   ```bash
   ping IP_DO_GERTEC
   ```

2. **Verificar porta:**
   ```bash
   telnet IP_DO_GERTEC 6500
   ```

3. **Verificar se estão na mesma rede:**
   ```bash
   # No servidor da API
   ipconfig  # Windows
   ifconfig  # Linux
   
   # Verificar se o IP do Gertec está na mesma sub-rede
   ```

4. **Verificar firewall:**
   - Windows Firewall não deve bloquear conexões de saída
   - Firewall de rede deve permitir comunicação entre os IPs

### Problema: Timeout na Conexão

- Verificar se o Gertec está ligado
- Verificar se o IP está correto
- Verificar se há firewall bloqueando
- Verificar se estão na mesma rede ou há roteamento

### Problema: Conexão Intermitente

- Verificar qualidade da rede
- Verificar se o cabo de rede está bem conectado (se usar Ethernet)
- Verificar sinal Wi-Fi (se usar Wi-Fi)
- Verificar logs da aplicação para padrões de desconexão

## Resumo

**Pergunta:** O Gertec precisa estar na mesma máquina da API?

**Resposta:** **NÃO**

**Requisitos:**
- Mesma rede local OU
- Roteamento configurado OU
- VPN configurada

**Comunicação:**
- TCP/IP na porta 6500
- API conecta no Gertec (não o contrário)
- Não precisa abrir porta 6500 no servidor da API

**Configuração:**
- Configure o IP do Gertec no arquivo `.env`
- Certifique-se de que há conectividade de rede entre os dois

