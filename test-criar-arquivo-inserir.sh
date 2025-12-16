#!/bin/bash

# Script para testar criação do arquivo e inserção de produtos da API Tiny

echo "=========================================="
echo "TESTE: Criação de Arquivo e Inserção de Produtos"
echo "=========================================="
echo ""

# Caminho do arquivo de produtos
ARQUIVO_PRODUTOS="gertec_produtos.txt"
ARQUIVO_PRODUTOS_ABS=$(realpath "$ARQUIVO_PRODUTOS")

echo "1. Verificando se arquivo existe..."
if [ -f "$ARQUIVO_PRODUTOS" ]; then
    echo "   Arquivo já existe: $ARQUIVO_PRODUTOS_ABS"
    echo "   Tamanho atual: $(stat -f%z "$ARQUIVO_PRODUTOS" 2>/dev/null || stat -c%s "$ARQUIVO_PRODUTOS" 2>/dev/null) bytes"
    echo "   Backup sendo criado..."
    cp "$ARQUIVO_PRODUTOS" "${ARQUIVO_PRODUTOS}.backup.$(date +%Y%m%d_%H%M%S)"
    echo "   Removendo arquivo antigo para teste..."
    rm -f "$ARQUIVO_PRODUTOS"
else
    echo "   Arquivo não existe, será criado durante o teste"
fi

echo ""
echo "2. Criando arquivo vazio ANTES de executar API..."
touch "$ARQUIVO_PRODUTOS"
if [ -f "$ARQUIVO_PRODUTOS" ]; then
    echo "   ✓ Arquivo criado com sucesso: $ARQUIVO_PRODUTOS_ABS"
    echo "   Tamanho: $(stat -f%z "$ARQUIVO_PRODUTOS" 2>/dev/null || stat -c%s "$ARQUIVO_PRODUTOS" 2>/dev/null) bytes"
else
    echo "   ✗ ERRO: Não foi possível criar o arquivo"
    exit 1
fi

echo ""
echo "3. Verificando configuração da API..."
if [ ! -f "appsettings.json" ]; then
    echo "   ✗ ERRO: appsettings.json não encontrado"
    exit 1
fi

TOKEN=$(grep -o '"Token":\s*"[^"]*"' appsettings.json | cut -d'"' -f4)
if [ -z "$TOKEN" ]; then
    echo "   ✗ ERRO: Token não encontrado no appsettings.json"
    exit 1
fi

echo "   ✓ Token encontrado: ${TOKEN:0:8}..."
echo ""

echo "4. Compilando projeto..."
dotnet build --no-restore > /dev/null 2>&1
if [ $? -ne 0 ]; then
    echo "   Restaurando dependências primeiro..."
    dotnet restore
    dotnet build
    if [ $? -ne 0 ]; then
        echo "   ✗ ERRO: Falha na compilação"
        exit 1
    fi
fi
echo "   ✓ Compilação OK"
echo ""

echo "5. Executando API em background (timeout de 60 segundos)..."
echo "   A API irá:"
echo "   - Criar o arquivo se não existir"
echo "   - Buscar produtos da API Tiny"
echo "   - Registrar produtos no arquivo"
echo ""

# Executa a API em background com timeout
timeout 60 dotnet run --no-build 2>&1 | tee api-output.log &
API_PID=$!

# Aguarda alguns segundos para a API inicializar
sleep 10

echo ""
echo "6. Verificando status do arquivo após 10 segundos..."
if [ -f "$ARQUIVO_PRODUTOS" ]; then
    TAMANHO=$(stat -f%z "$ARQUIVO_PRODUTOS" 2>/dev/null || stat -c%s "$ARQUIVO_PRODUTOS" 2>/dev/null)
    LINHAS=$(wc -l < "$ARQUIVO_PRODUTOS" 2>/dev/null || echo "0")
    echo "   Arquivo existe: $ARQUIVO_PRODUTOS_ABS"
    echo "   Tamanho: $TAMANHO bytes"
    echo "   Linhas: $LINHAS"
    
    if [ "$TAMANHO" -gt 0 ]; then
        echo "   ✓ Arquivo contém dados!"
        echo ""
        echo "   Primeiras 5 linhas do arquivo:"
        head -n 5 "$ARQUIVO_PRODUTOS" | sed 's/^/      /'
    else
        echo "   ⚠ Arquivo está vazio"
    fi
else
    echo "   ✗ ERRO: Arquivo não existe"
fi

echo ""
echo "7. Aguardando mais 20 segundos para processamento completo..."
sleep 20

echo ""
echo "8. Verificação final do arquivo..."
if [ -f "$ARQUIVO_PRODUTOS" ]; then
    TAMANHO=$(stat -f%z "$ARQUIVO_PRODUTOS" 2>/dev/null || stat -c%s "$ARQUIVO_PRODUTOS" 2>/dev/null)
    LINHAS=$(wc -l < "$ARQUIVO_PRODUTOS" 2>/dev/null || echo "0")
    echo "   Arquivo: $ARQUIVO_PRODUTOS_ABS"
    echo "   Tamanho final: $TAMANHO bytes"
    echo "   Linhas finais: $LINHAS"
    
    if [ "$TAMANHO" -gt 0 ]; then
        echo "   ✓ SUCESSO: Arquivo contém $LINHAS produtos!"
        echo ""
        echo "   Primeiras 10 linhas do arquivo:"
        head -n 10 "$ARQUIVO_PRODUTOS" | sed 's/^/      /'
        echo ""
        echo "   Últimas 5 linhas do arquivo:"
        tail -n 5 "$ARQUIVO_PRODUTOS" | sed 's/^/      /'
    else
        echo "   ✗ FALHA: Arquivo ainda está vazio após processamento"
        echo ""
        echo "   Verificando logs da API..."
        if [ -f "api-output.log" ]; then
            echo "   Últimas 20 linhas do log:"
            tail -n 20 "api-output.log" | sed 's/^/      /'
        fi
    fi
else
    echo "   ✗ ERRO: Arquivo não existe"
fi

echo ""
echo "9. Parando API..."
kill $API_PID 2>/dev/null
wait $API_PID 2>/dev/null
echo "   ✓ API parada"

echo ""
echo "=========================================="
echo "TESTE CONCLUÍDO"
echo "=========================================="

