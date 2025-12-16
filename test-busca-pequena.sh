#!/bin/bash

# Script para testar busca pequena de produtos

echo "=========================================="
echo "TESTE: Busca Pequena de Produtos"
echo "=========================================="
echo ""

# Limpa arquivo de produtos para teste
ARQUIVO_PRODUTOS="gertec_produtos.txt"
if [ -f "$ARQUIVO_PRODUTOS" ]; then
    echo "1. Removendo arquivo antigo para teste..."
    rm -f "$ARQUIVO_PRODUTOS"
    echo "   ✓ Arquivo removido"
else
    echo "1. Arquivo não existe, será criado durante o teste"
fi

echo ""
echo "2. Iniciando API em background..."
cd "/home/ricardopereira/Área de trabalho/projetos/api-carol"
dotnet run --no-build > api-test.log 2>&1 &
API_PID=$!

echo "   API iniciada (PID: $API_PID)"
echo "   Aguardando 5 segundos para inicialização..."
sleep 5

echo ""
echo "3. Testando endpoint de busca pequena (10 produtos)..."
echo "   Chamando: POST http://localhost:5000/api/integration/gertec/datafile/test?limit=10"
echo ""

RESPONSE=$(curl -s -X POST "http://localhost:5000/api/integration/gertec/datafile/test?limit=10" \
  -H "Content-Type: application/json")

echo "Resposta da API:"
echo "$RESPONSE" | python3 -m json.tool 2>/dev/null || echo "$RESPONSE"
echo ""

echo "4. Aguardando 3 segundos para processamento..."
sleep 3

echo ""
echo "5. Verificando arquivo gerado..."
if [ -f "$ARQUIVO_PRODUTOS" ]; then
    TAMANHO=$(stat -f%z "$ARQUIVO_PRODUTOS" 2>/dev/null || stat -c%s "$ARQUIVO_PRODUTOS" 2>/dev/null)
    LINHAS=$(wc -l < "$ARQUIVO_PRODUTOS" 2>/dev/null || echo "0")
    
    echo "   ✓ Arquivo existe: $ARQUIVO_PRODUTOS"
    echo "   Tamanho: $TAMANHO bytes"
    echo "   Linhas: $LINHAS"
    
    if [ "$TAMANHO" -gt 0 ]; then
        echo ""
        echo "   Primeiras 5 linhas do arquivo:"
        head -n 5 "$ARQUIVO_PRODUTOS" | sed 's/^/      /'
        echo ""
        echo "   ✓ SUCESSO: Arquivo contém dados!"
    else
        echo "   ⚠ Arquivo está vazio"
    fi
else
    echo "   ✗ Arquivo não foi criado"
fi

echo ""
echo "6. Parando API..."
kill $API_PID 2>/dev/null
wait $API_PID 2>/dev/null
echo "   ✓ API parada"

echo ""
echo "=========================================="
echo "TESTE CONCLUÍDO"
echo "=========================================="

