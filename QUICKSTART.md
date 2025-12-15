# Quick Start - Início Rápido

## 1. Instalar .NET SDK 8.0

```bash
# Fedora
sudo dnf install dotnet-sdk-8.0

# Ou use o script
./install-dotnet.sh
```

## 2. Configurar Variáveis de Ambiente

```bash
# Criar arquivo .env
./setup-env.sh

# Editar com suas credenciais
nano .env
```

**Configure pelo menos:**
- `OLIST_API__TOKEN` - Seu token da API do Olist
- `GERTEC__IP_ADDRESS` - IP do seu equipamento Gertec

## 3. Executar

```bash
./run.sh
```

## Exemplo de .env

```env
OLIST_API__TOKEN=seu_token_aqui
GERTEC__IP_ADDRESS=192.168.1.50
```

Pronto! A aplicação está rodando.

## Verificar

- Acesse: http://localhost:5000/swagger
- Verifique os logs no console
- Teste escanear um código de barras no Gertec

## Documentação Completa

- `README.md` - Documentação principal
- `VARIAVEIS_AMBIENTE.md` - Guia completo de variáveis de ambiente
- `INSTALACAO.md` - Guia detalhado de instalação

