#!/bin/bash

# Script de execução da Integração Olist ERP com Gertec Busca Preço G2

echo "=========================================="
echo "Integração Olist ERP com Gertec Busca Preço G2"
echo "=========================================="
echo ""

# Verifica se o .NET está instalado
if ! command -v dotnet &> /dev/null; then
    echo "ERRO: .NET SDK não encontrado!"
    echo "Por favor, instale o .NET 8.0 SDK: https://dotnet.microsoft.com/download"
    exit 1
fi

echo "Versão do .NET:"
dotnet --version
echo ""

# Verifica se o appsettings.json existe
if [ ! -f "appsettings.json" ]; then
    echo "AVISO: appsettings.json não encontrado!"
    echo "Copiando appsettings.Example.json para appsettings.json..."
    cp appsettings.Example.json appsettings.json
    echo "Por favor, edite o appsettings.json com suas configurações antes de continuar."
    exit 1
fi

# Restaura dependências
echo "Restaurando dependências..."
dotnet restore
if [ $? -ne 0 ]; then
    echo "ERRO: Falha ao restaurar dependências"
    exit 1
fi

echo ""

# Compila o projeto
echo "Compilando projeto..."
dotnet build
if [ $? -ne 0 ]; then
    echo "ERRO: Falha na compilação"
    exit 1
fi

echo ""

# Executa a aplicação
echo "Iniciando aplicação..."
echo "Pressione Ctrl+C para parar"
echo ""
dotnet run

