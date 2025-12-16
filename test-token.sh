#!/bin/bash

# Script para testar o token da API Tiny ERP (Olist)
# Substitua SEU_TOKEN_AQUI pelo seu token real

TOKEN="f08598c71a1384a81527110a5dbf1d5fcb1773af"

echo "=== Teste 1: Verificar token (listar produtos) ==="
curl -X POST "https://api.tiny.com.br/api2/produtos.pesquisa.php" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "token=${TOKEN}" \
  -d "formato=json" \
  | jq '.' 2>/dev/null || cat

echo -e "\n\n=== Teste 2: Buscar produto por código de barras (exemplo: 7891234567890) ==="
curl -X POST "https://api.tiny.com.br/api2/produtos.pesquisa.php" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "token=${TOKEN}" \
  -d "formato=json" \
  -d "pesquisa=7891234567890" \
  | jq '.' 2>/dev/null || cat

echo -e "\n\n=== Teste 3: Verificar apenas status do token (formato compacto) ==="
curl -s -X POST "https://api.tiny.com.br/api2/produtos.pesquisa.php" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "token=${TOKEN}" \
  -d "formato=json" \
  | jq '.retorno.status' 2>/dev/null || echo "Erro ao processar resposta"

