# Como Descobrir o IP do Gertec Busca Preço G2

## Métodos para Descobrir o IP

### Método 1: Através do Display do Gertec (Recomendado)

1. **Acesse o menu de configuração do Gertec:**
  - No display do equipamento, navegue até as configurações de rede
  - Procure por "Configurações de Rede", "Network Settings" ou "IP"

2. **Verifique as informações de rede:**
  - O IP será exibido no display
  - Anote o IP, máscara de sub-rede e gateway

### Método 2: Através do Roteador/Modem

1. **Acesse o painel administrativo do roteador:**
  - Geralmente: `http://192.168.1.1` ou `http://192.168.0.1`
  - Verifique no manual do seu roteador

2. **Procure por "Dispositivos Conectados" ou "DHCP Clients":**
  - Procure por um dispositivo com nome similar a "Gertec" ou "BPG2"
  - O IP será listado junto com o nome do dispositivo

### Método 3: Usando Comandos de Rede

#### No Linux/Mac:

```bash
# Escaneia a rede local para encontrar dispositivos
nmap -sn 192.168.1.0/24

# Ou use arp para ver dispositivos conhecidos
arp -a | grep -i gertec

# Verifica dispositivos na rede
ip neigh show
```

#### No Windows:

```cmd
# Ver dispositivos na rede
arp -a

# Escanear rede (requer ferramentas adicionais)
# Use ferramentas como Advanced IP Scanner
```

### Método 4: Usando o Comando #macaddr? (Se já tiver conexão)

Se você já conseguiu conectar uma vez, pode usar a API para obter o MAC Address e depois procurar no roteador:

```bash
# Teste a conexão e obtenha o MAC Address
curl http://localhost:5000/api/integration/gertec/macaddress
```

Depois procure esse MAC Address na lista de dispositivos do roteador.

### Método 5: Verificar Logs do Sistema

Se o Gertec já tentou se conectar ou foi configurado anteriormente:

1. Verifique logs de DHCP do roteador
2. Verifique logs de firewall
3. Verifique configurações de rede salvas em outros sistemas

## Verificar se o IP está Correto

### Teste de Conectividade

```bash
# Ping no IP do Gertec
ping 192.168.0.100

# Teste de porta (porta padrão: 6500)
telnet 192.168.0.100 6500
# ou
nc -zv 192.168.0.100 6500
```

### Usando a API da Aplicação

Após configurar o IP, teste a conexão:

```bash
# Verificar status
curl http://localhost:5000/api/integration/status

# Tentar conectar
curl -X POST http://localhost:5000/api/integration/gertec/connect
```

## Configuração no Arquivo .env

Depois de descobrir o IP, configure no arquivo `.env`:

```env
GERTEC__IP_ADDRESS=192.168.0.100
```

**Substitua `192.168.0.100` pelo IP real do seu equipamento.**

## Troubleshooting

### Problema: Não consigo encontrar o IP

1. **Verifique se o Gertec está ligado e conectado à rede:**
  - Verifique os LEDs de rede no equipamento
  - Verifique se o cabo de rede está conectado (se usar Ethernet)

2. **Verifique se está na mesma rede:**
  - O computador e o Gertec devem estar na mesma rede local
  - Verifique a máscara de sub-rede

3. **Verifique o DHCP:**
  - O Gertec pode estar configurado para obter IP automaticamente (DHCP)
  - Verifique no roteador quais IPs foram atribuídos

### Problema: IP muda constantemente

Se o IP muda a cada reinicialização (DHCP dinâmico):

1. **Configure IP estático no Gertec:**
  - Acesse as configurações de rede do Gertec
  - Configure um IP fixo (ex: 192.168.0.100)
  - Configure máscara e gateway

2. **Ou configure reserva de IP no roteador:**
  - No roteador, configure uma reserva de IP baseada no MAC Address
  - Assim o Gertec sempre receberá o mesmo IP

### Problema: Não consigo conectar mesmo com IP correto

1. **Verifique a porta:**
  - Porta padrão: 6500
  - Verifique se não está bloqueada pelo firewall

2. **Verifique firewall:**
  ```bash
  # Linux - verificar regras
  sudo iptables -L
  
  # Permitir porta 6500 (se necessário)
  sudo firewall-cmd --add-port=6500/tcp --permanent
  sudo firewall-cmd --reload
  ```

3. **Teste com telnet/nc:**
  ```bash
  telnet 192.168.0.100 6500
  # Se conectar, o IP e porta estão corretos
  ```

## Exemplo de Configuração Completa

```env
# IP do Gertec (descoberto usando um dos métodos acima)
GERTEC__IP_ADDRESS=192.168.1.50

# Porta padrão do Gertec
GERTEC__PORT=6500

# Outras configurações
GERTEC__RECONNECT_INTERVAL_SECONDS=5
GERTEC__RESPONSE_TIMEOUT_MILLISECONDS=500
```

## Dicas

- **Anote o IP:** Depois de descobrir, anote em local seguro
- **Use IP estático:** Configure IP fixo no Gertec para evitar mudanças
- **Documente:** Mantenha um registro dos IPs dos equipamentos da rede
- **Teste regularmente:** Verifique periodicamente se o IP ainda está correto

## Ferramentas Úteis

- **nmap** - Escaneamento de rede (Linux/Mac)
- **Advanced IP Scanner** - Escaneamento de rede (Windows)
- **Angry IP Scanner** - Escaneamento de rede (Multiplataforma)
- **Wireshark** - Análise de rede (avançado)

