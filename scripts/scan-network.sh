#!/bin/bash

# Script para escanear a rede e encontrar possíveis IPs do Gertec

echo "=========================================="
echo "Escaneamento de Rede - Busca do Gertec"
echo "=========================================="
echo ""

# Detecta o IP da máquina atual
CURRENT_IP=$(ip route get 8.8.8.8 2>/dev/null | awk '{print $7; exit}' || hostname -I | awk '{print $1}')

if [ -z "$CURRENT_IP" ]; then
    echo "ERRO: Não foi possível detectar o IP da máquina"
    exit 1
fi

echo "IP da máquina atual: $CURRENT_IP"
echo ""

# Extrai a rede (assumindo /24)
NETWORK=$(echo $CURRENT_IP | cut -d'.' -f1-3)
echo "Escaneando rede: $NETWORK.0/24"
echo ""
echo "Isso pode levar alguns minutos..."
echo ""

# Verifica se nmap está instalado
if command -v nmap &> /dev/null; then
    echo "Usando nmap para escanear..."
    nmap -sn $NETWORK.0/24 | grep -E "Nmap scan report|MAC Address" | grep -B1 -i "gertec\|unknown" || echo "Nenhum dispositivo Gertec encontrado automaticamente"
    echo ""
    echo "Listando todos os IPs encontrados:"
    nmap -sn $NETWORK.0/24 | grep "Nmap scan report" | awk '{print $5}'
else
    echo "nmap não está instalado. Tentando método alternativo..."
    echo ""
    echo "IPs na rede (usando ping):"
    
    for i in {1..254}; do
        IP="$NETWORK.$i"
        if ping -c 1 -W 1 $IP &> /dev/null; then
            echo "  $IP - Ativo"
            # Tenta verificar se é a porta 6500 (porta do Gertec)
            if timeout 1 bash -c "echo > /dev/tcp/$IP/6500" 2>/dev/null; then
                echo "    *** Possível Gertec (porta 6500 aberta) ***"
            fi
        fi
    done
fi

echo ""
echo "=========================================="
echo "Próximos passos:"
echo "1. Verifique os IPs listados acima"
echo "2. Teste cada IP com: ping IP"
echo "3. Teste a porta 6500: telnet IP 6500"
echo "4. Configure o IP correto no arquivo .env"
echo "=========================================="

