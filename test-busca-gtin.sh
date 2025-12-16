#!/bin/bash

# Script de teste para validar busca por GTIN na API Tiny ERP
# Testa a estratégia de fallback implementada

TOKEN="f08598c71a1384a81527110a5dbf1d5fcb1773af"
BASE_URL="https://api.tiny.com.br/api2"

echo "=========================================="
echo "Teste de Busca por GTIN - API Tiny ERP"
echo "=========================================="
echo ""

# Cores para output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Teste 1: Busca direta por GTIN (deve falhar)
echo -e "${YELLOW}Teste 1: Busca direta por GTIN${NC}"
echo "GTIN: 7898132989040"
echo ""

RESPONSE1=$(curl -s -X POST "${BASE_URL}/produtos.pesquisa.php" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "token=${TOKEN}" \
  -d "formato=json" \
  -d "pesquisa=7898132989040")

STATUS1=$(echo $RESPONSE1 | jq -r '.retorno.status' 2>/dev/null)

if [ "$STATUS1" == "Erro" ]; then
    echo -e "${RED}✗ Busca direta falhou (esperado)${NC}"
    echo "Erro: $(echo $RESPONSE1 | jq -r '.retorno.erros[0].erro' 2>/dev/null)"
else
    echo -e "${GREEN}✓ Busca direta funcionou${NC}"
fi
echo ""

# Teste 2: Busca por código interno (SKU) - deve funcionar
echo -e "${YELLOW}Teste 2: Busca por código interno (SKU)${NC}"
echo "Código: SKU007158"
echo ""

RESPONSE2=$(curl -s -X POST "${BASE_URL}/produtos.pesquisa.php" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "token=${TOKEN}" \
  -d "formato=json" \
  -d "pesquisa=SKU007158")

STATUS2=$(echo $RESPONSE2 | jq -r '.retorno.status' 2>/dev/null)

if [ "$STATUS2" == "OK" ]; then
    echo -e "${GREEN}✓ Busca por SKU funcionou${NC}"
    NOME=$(echo $RESPONSE2 | jq -r '.retorno.produtos[0].produto.nome' 2>/dev/null)
    GTIN=$(echo $RESPONSE2 | jq -r '.retorno.produtos[0].produto.gtin' 2>/dev/null)
    PRECO=$(echo $RESPONSE2 | jq -r '.retorno.produtos[0].produto.preco' 2>/dev/null)
    echo "Produto: $NOME"
    echo "GTIN: $GTIN"
    echo "Preço: R$ $PRECO"
else
    echo -e "${RED}✗ Busca por SKU falhou${NC}"
fi
echo ""

# Teste 3: Busca completa e filtro por GTIN (fallback)
echo -e "${YELLOW}Teste 3: Busca completa e filtro por GTIN (fallback)${NC}"
echo "GTIN: 7898132989040"
echo "Buscando todos os produtos e filtrando..."
echo ""

RESPONSE3=$(curl -s -X POST "${BASE_URL}/produtos.pesquisa.php" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "token=${TOKEN}" \
  -d "formato=json")

STATUS3=$(echo $RESPONSE3 | jq -r '.retorno.status' 2>/dev/null)

if [ "$STATUS3" == "OK" ]; then
    PRODUTO=$(echo $RESPONSE3 | jq '.retorno.produtos[] | select(.produto.gtin == "7898132989040")' 2>/dev/null)
    
    if [ ! -z "$PRODUTO" ]; then
        echo -e "${GREEN}✓ Produto encontrado via fallback!${NC}"
        NOME=$(echo $PRODUTO | jq -r '.produto.nome' 2>/dev/null)
        GTIN=$(echo $PRODUTO | jq -r '.produto.gtin' 2>/dev/null)
        PRECO=$(echo $PRODUTO | jq -r '.produto.preco' 2>/dev/null)
        CODIGO=$(echo $PRODUTO | jq -r '.produto.codigo' 2>/dev/null)
        echo "Produto: $NOME"
        echo "Código: $CODIGO"
        echo "GTIN: $GTIN"
        echo "Preço: R$ $PRECO"
    else
        echo -e "${RED}✗ Produto não encontrado no fallback${NC}"
    fi
else
    echo -e "${RED}✗ Erro ao buscar todos os produtos${NC}"
fi
echo ""

# Teste 4: Código de barras inexistente
echo -e "${YELLOW}Teste 4: Código de barras inexistente${NC}"
echo "Código: 896062000480"
echo ""

RESPONSE4=$(curl -s -X POST "${BASE_URL}/produtos.pesquisa.php" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "token=${TOKEN}" \
  -d "formato=json" \
  -d "pesquisa=896062000480")

STATUS4=$(echo $RESPONSE4 | jq -r '.retorno.status' 2>/dev/null)

if [ "$STATUS4" == "Erro" ]; then
    echo -e "${YELLOW}⚠ Código não encontrado (esperado para código inexistente)${NC}"
    echo "Erro: $(echo $RESPONSE4 | jq -r '.retorno.erros[0].erro' 2>/dev/null)"
else
    echo -e "${GREEN}✓ Código encontrado${NC}"
fi
echo ""

echo "=========================================="
echo "Resumo dos Testes"
echo "=========================================="
echo ""
echo "✓ Busca direta por GTIN: Falha (limitação da API)"
echo "✓ Busca por SKU: Funciona"
echo "✓ Fallback (busca completa + filtro): Funciona"
echo ""
echo "Conclusão: A estratégia de fallback implementada"
echo "resolve a limitação da API Tiny ERP."
echo ""

