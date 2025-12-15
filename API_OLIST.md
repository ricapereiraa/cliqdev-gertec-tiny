# Informações sobre a API do Olist ERP

## Endpoints Utilizados

### Buscar Produto por Código de Barras

**Endpoint:** `POST /api2/produto.pesquisa.php`

**Parâmetros:**
- `token`: Token de autenticação
- `formato`: Formato de resposta (json)
- `pesquisa`: Código de barras ou nome do produto

**Resposta de Exemplo:**
```json
{
 "status": "OK",
 "retorno": {
  "produto": {
   "id": "123456",
   "codigo": "7891234567890",
   "nome": "Produto Exemplo",
   "preco": "29.90",
   "precoPromocional": "24.90",
   "descricao": "Descrição do produto",
   "estoque": "100"
  }
 }
}
```

### Listar Todos os Produtos

**Endpoint:** `POST /api2/produtos.pesquisa.php`

**Parâmetros:**
- `token`: Token de autenticação
- `formato`: Formato de resposta (json)

**Resposta de Exemplo:**
```json
{
 "status": "OK",
 "retorno": {
  "produtos": [
   {
    "id": "123456",
    "codigo": "7891234567890",
    "nome": "Produto 1",
    "preco": "29.90",
    "precoPromocional": "",
    "descricao": "",
    "estoque": "100"
   }
  ]
 }
}
```

## Obtenção do Token

1. Acesse o painel administrativo do Olist ERP
2. Vá em Configurações > API
3. Gere ou copie o token de acesso
4. Cole no arquivo `appsettings.json`

## Formato de Preço

A API do Olist retorna preços como strings no formato decimal (ex: "29.90").

A aplicação formata para exibição: `R$ 29,90`

## Código de Barras

O campo `codigo` na resposta da API contém o código de barras do produto.

Este é o campo usado para:
- Buscar produto quando código é escaneado no Gertec
- Identificar produtos no cache de monitoramento

## Tratamento de Erros

Se a API retornar `status: "Erro"`, a aplicação:
1. Registra o erro nos logs
2. Envia `#nfound` ao Gertec
3. Continua funcionando normalmente

## Limitações Conhecidas

- A API pode ter limites de requisições por minuto
- Alguns produtos podem não ter código de barras cadastrado
- Preços promocionais podem estar vazios

## Documentação Completa

Para mais informações, consulte:
https://tiny.com.br/api-docs/api

