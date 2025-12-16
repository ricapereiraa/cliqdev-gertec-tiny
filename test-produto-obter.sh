#!/bin/bash

# Teste da nova estratégia: GTIN -> ID -> produto.obter.php

TOKEN="f08598c71a1384a81527110a5dbf1d5fcb1773af"
GTIN="7898132989040"

echo "=========================================="
echo "Teste: GTIN -> ID -> produto.obter.php"
echo "=========================================="
echo ""

# Passo 1: Buscar ID pelo GTIN
echo "1. Buscando ID do produto pelo GTIN: $GTIN"
ID=$(curl -s -X POST "https://api.tiny.com.br/api2/produtos.pesquisa.php" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "token=${TOKEN}" \
  -d "formato=json" | jq -r ".retorno.produtos[] | select(.produto.gtin == \"${GTIN}\") | .produto.id" 2>/dev/null)

if [ -z "$ID" ] || [ "$ID" == "null" ]; then
    echo "Erro: Não foi possível encontrar ID para GTIN $GTIN"
    exit 1
fi

echo " ID encontrado: $ID"
echo ""

# Passo 2: Obter produto completo usando produto.obter.php
echo "2. Obtendo produto completo usando produto.obter.php (ID: $ID)"
PRODUTO=$(curl -s -X POST "https://api.tiny.com.br/api2/produto.obter.php" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "token=${TOKEN}" \
  -d "formato=json" \
  -d "id=${ID}")

STATUS=$(echo $PRODUTO | jq -r '.retorno.status' 2>/dev/null)

if [ "$STATUS" == "OK" ]; then
    echo " Produto obtido com sucesso!"
    echo ""
    echo "Dados do produto:"
    echo $PRODUTO | jq '.retorno.produto | {
        id: .id,
        nome: .nome,
        codigo: .codigo,
        gtin: .gtin,
        preco: .preco,
        preco_promocional: .preco_promocional,
        anexos: .anexos[0].anexo
    }' 2>/dev/null
    
    ANEXO=$(echo $PRODUTO | jq -r '.retorno.produto.anexos[0].anexo // empty' 2>/dev/null)
    if [ ! -z "$ANEXO" ]; then
        echo ""
        echo " Imagem encontrada: $ANEXO"
    else
        echo ""
        echo "  Nenhuma imagem encontrada"
    fi
else
    echo "Erro ao obter produto:"
    echo $PRODUTO | jq '.retorno.erros' 2>/dev/null
    exit 1
fi

echo ""
echo "=========================================="
echo " Teste concluído com sucesso!"
echo "=========================================="
echo ""
echo "Fluxo validado:"
echo "  GTIN ($GTIN) → ID ($ID) → produto.obter.php → Dados completos + Imagem"
echo ""

