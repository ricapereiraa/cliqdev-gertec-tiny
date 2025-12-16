#!/bin/bash

# Script para executar Cliqdev Gertec e Tiny
# Este script é usado pelo ícone da área de trabalho e pelo serviço systemd

# Diretório do projeto
PROJECT_DIR="/home/ricardopereira/Área de trabalho/projetos/api-carol"
cd "$PROJECT_DIR" || exit 1

# Verifica se .NET está instalado
if ! command -v dotnet &> /dev/null; then
    echo "Erro: .NET não está instalado!"
    notify-send "Cliqdev Gertec e Tiny" "Erro: .NET não está instalado!" -u critical
    exit 1
fi

# Verifica se o arquivo .env existe
if [ ! -f ".env" ]; then
    echo "AVISO: Arquivo .env não encontrado!"
    notify-send "Cliqdev Gertec e Tiny" "AVISO: Arquivo .env não encontrado!" -u normal
fi

# Executa a aplicação
echo "Iniciando Cliqdev Gertec e Tiny..."
notify-send "Cliqdev Gertec e Tiny" "Iniciando aplicação..." -u normal

# Executa em background e redireciona logs
dotnet run --no-build > cliqdev-gertec-tiny.log 2>&1 &

# Salva o PID
echo $! > cliqdev-gertec-tiny.pid

echo "Cliqdev Gertec e Tiny iniciada! PID: $(cat cliqdev-gertec-tiny.pid)"
echo "Logs em: $PROJECT_DIR/cliqdev-gertec-tiny.log"
echo "Acesse: http://localhost:5000/painel.html"

notify-send "Cliqdev Gertec e Tiny" "Aplicação iniciada!\nAcesse: http://localhost:5000/painel.html" -u normal

