#!/bin/bash

# Script de instalação do .NET SDK 8.0 no Fedora

echo "=========================================="
echo "Instalação do .NET SDK 8.0 no Fedora"
echo "=========================================="
echo ""

# Verifica se já está instalado
if command -v dotnet &> /dev/null; then
    echo "Verificando versão do .NET instalada..."
    dotnet --version
    echo ""
    echo "Se a versão for 8.0 ou superior, você pode pular a instalação."
    echo ""
    read -p "Deseja continuar com a instalação mesmo assim? (s/N): " resposta
    if [[ ! $resposta =~ ^[Ss]$ ]]; then
        echo "Instalação cancelada."
        exit 0
    fi
fi

echo "Instalando .NET SDK 8.0..."
echo "Será necessário inserir sua senha de administrador."
echo ""

sudo dnf install -y dotnet-sdk-8.0

if [ $? -eq 0 ]; then
    echo ""
    echo "=========================================="
    echo ".NET SDK 8.0 instalado com sucesso!"
    echo "=========================================="
    echo ""
    echo "Verificando instalação..."
    dotnet --version
    echo ""
    echo "Agora você pode executar: ./run.sh"
else
    echo ""
    echo "ERRO: Falha na instalação do .NET SDK"
    echo ""
    echo "Tente executar manualmente:"
    echo "  sudo dnf install dotnet-sdk-8.0"
    exit 1
fi

