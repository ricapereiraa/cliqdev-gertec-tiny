#!/bin/bash

# Script para configurar o arquivo .env

echo "=========================================="
echo "Configuração do arquivo .env"
echo "=========================================="
echo ""

# Verifica se .env já existe
if [ -f ".env" ]; then
    echo "AVISO: O arquivo .env já existe!"
    read -p "Deseja sobrescrever? (s/N): " resposta
    if [[ ! $resposta =~ ^[Ss]$ ]]; then
        echo "Operação cancelada."
        exit 0
    fi
fi

# Copia o exemplo
cp env.example .env

echo "Arquivo .env criado a partir do env.example"
echo ""
echo "Por favor, edite o arquivo .env e configure:"
echo "  - OLIST_API__TOKEN (OBRIGATÓRIO)"
echo "  - GERTEC__IP_ADDRESS (ajuste o IP do seu Gertec)"
echo ""
echo "Para editar:"
echo "  nano .env"
echo "  ou"
echo "  vim .env"
echo ""

