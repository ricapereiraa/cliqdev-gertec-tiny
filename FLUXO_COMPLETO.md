# Fluxo Completo da Integração Gertec + Tiny ERP

## Visão Geral

Este documento detalha o fluxo completo de funcionamento da integração entre o terminal Gertec Busca Preço G2 S e o Tiny ERP (Olist).

---

##  Fluxo de Consulta de Preço (Código de Barras)

### 1. Leitura do Código de Barras no Terminal

```
[Terminal Gertec] → Lê código de barras → Envia: #1234567890
```

**Protocolo:** Conforme manual Gertec, o terminal envia o código de barras no formato:
- `#` + código de barras (ex: `#7891234567890`)

### 2. Recepção no Servidor

**Arquivo:** `Services/GertecProtocolService.cs`
- Método: `ListenForMessagesAsync()`
- Detecta mensagens que começam com `#` e não são comandos do protocolo
- Extrai o código de barras removendo o `#` inicial
- Dispara evento `BarcodeReceived`

### 3. Resposta Imediata (Evita "Conexão Falhou")

**Arquivo:** `Services/IntegrationService.cs`
- Método: `OnBarcodeReceived()`
- **Ação imediata:** Envia mensagem `#mesg` com "Consultando... Aguarde"
- **Tempo de exibição:** 2 segundos
- **Objetivo:** Informar ao terminal que o código foi recebido e está sendo processado

**Protocolo #mesg:**
```
#mesg + tamanho_linha1(byte+48) + "Consultando..." + tamanho_linha2(byte+48) + "Aguarde" + tempo(byte+48) + reservado(48)
```

### 4. Busca no Tiny ERP

**Arquivo:** `Services/OlistApiService.cs`
- Método: `GetProductByBarcodeAsync(barcode)`
- **Endpoint:** `https://api.tiny.com.br/api2/produtos.pesquisa.php`
- **Método:** POST
- **Parâmetros:**
  - `token`: Token de autenticação
  - `formato`: "json"
  - `pesquisa`: Código de barras

**Resposta da API:**
```json
{
  "retorno": {
    "status": "OK",
    "produtos": [
      {
        "produto": {
          "id": 123,
          "codigo": "7891234567890",
          "nome": "Produto Exemplo",
          "preco": "29.90",
          "preco_promocional": "24.90",
          "gtin": "7891234567890",
          "imagem": "https://exemplo.com/imagem.jpg"
        }
      }
    ]
  }
}
```

### 5. Envio de Imagem (Se Disponível)

**Arquivo:** `Services/GertecProtocolService.cs`
- Método: `SendImageAsync()` ou `SendImageFromFileAsync()`
- **Comando:** `#gif`
- **Quando:** Se o produto tiver campo `imagem` ou `imagem_principal` preenchido

**Protocolo #gif:**
```
#gif + índice(2 bytes hex ASCII) + loops(2 bytes hex ASCII) + tempo(2 bytes hex ASCII) + 
tamanho(6 bytes hex ASCII) + checksum(4 bytes hex ASCII) + ETB(0x17) + dados_imagem
```

**Parâmetros:**
- `indice`: 0 (exibição imediata)
- `numeroLoops`: 1
- `tempoExibicao`: 5 segundos
- `tamanho`: Tamanho da imagem em bytes (hex)
- `checksum`: "0000" (não validado pelo equipamento)

**Limites:**
- Máximo: 124KB (modelo com áudio) ou 192KB (modelo sem áudio)
- Formato: GIF animado ou imagem estática

**Resposta do Terminal:**
- `#gif_ok00` → Sucesso
- `#img_error` → Erro

### 6. Envio de Nome e Preço

**Arquivo:** `Services/GertecProtocolService.cs`
- Método: `SendProductInfoAsync(nome, preco)`

**Formatação do Nome:**
- 4 linhas × 20 colunas = **80 bytes exatos**
- Dividido automaticamente em até 4 linhas de 20 caracteres
- Preenchido com espaços se necessário

**Formatação do Preço:**
- 1 linha × 20 colunas = **20 bytes exatos**
- Formato: `R$ XX,XX`
- Caractere `#` removido (não permitido no preço)

**Protocolo:**
```
# + nome(80 bytes) + | + preço(20 bytes)
```

**Exemplo:**
```
#Produto Exemplo        |R$ 24,90
```

### 7. Produto Não Encontrado

**Arquivo:** `Services/GertecProtocolService.cs`
- Método: `SendProductNotFoundAsync()`

**Protocolo:**
```
#nfound
```

O terminal exibe mensagem padrão de "Produto não cadastrado".

---

##  Fluxo de Atualização Automática de Preços

### 1. Monitoramento Contínuo

**Arquivo:** `Services/IntegrationService.cs`
- Método: `MonitorPriceChangesAsync()`
- **Intervalo:** Configurável via `PriceMonitoring:CheckIntervalMinutes` (padrão: 1 minuto)
- **Execução:** Thread separada que roda continuamente

### 2. Busca de Todos os Produtos

**Arquivo:** `Services/OlistApiService.cs`
- Método: `GetAllProductsAsync()`
- **Endpoint:** `https://api.tiny.com.br/api2/produtos.pesquisa.php`
- **Parâmetros:**
  - `token`: Token de autenticação
  - `formato`: "json"
  - (sem parâmetro `pesquisa` = retorna todos)

### 3. Comparação com Cache

**Cache em Memória:**
- `Dictionary<string, Produto> _productCache`
- Chave: Código do produto ou GTIN
- Valor: Objeto `Produto` completo

**Verificações:**
1. **Produto existe no cache?**
   - Se sim: Compara preço e preço promocional
   - Se mudou: Atualiza no Gertec
   - Se não mudou: Ignora
2. **Produto não existe no cache?**
   - Adiciona ao cache
   - Log de "novo produto detectado"

### 4. Detecção de Mudança de Preço

**Critério:**
```csharp
bool precoMudou = produtoCache.Preco != produto.Preco || 
                 produtoCache.PrecoPromocional != produto.PrecoPromocional;
```

### 5. Atualização no Gertec

Quando detecta mudança de preço:

1. **Atualiza cache:** `_productCache[chaveProduto] = produto;`
2. **Formata dados:** Nome (80 bytes) + Preço (20 bytes)
3. **Envia ao Gertec:** `SendProductInfoAsync(nomeFormatado, precoFormatado)`
4. **Log:** Registra atualização bem-sucedida

**Observação:** A atualização automática só envia nome e preço. Imagens não são atualizadas automaticamente (apenas na consulta por código de barras).

---

##  Diagrama de Fluxo

```
┌─────────────────┐
│ Terminal Gertec │
│  Lê Código      │
└────────┬────────┘
         │ #123456
         ▼
┌─────────────────────────┐
│ GertecProtocolService   │
│ ListenForMessagesAsync()│
└────────┬────────────────┘
         │ BarcodeReceived Event
         ▼
┌─────────────────────────┐
│ IntegrationService       │
│ OnBarcodeReceived()      │
└────────┬────────────────┘
         │
         ├─► Envia #mesg "Consultando..."
         │
         ▼
┌─────────────────────────┐
│ OlistApiService         │
│ GetProductByBarcodeAsync()│
└────────┬────────────────┘
         │
         ├─► POST /produtos.pesquisa.php
         │
         ▼
┌─────────────────────────┐
│ Tiny ERP API            │
│ Retorna Produto         │
└────────┬────────────────┘
         │
         ▼
┌─────────────────────────┐
│ IntegrationService       │
│ Processa Resposta       │
└────────┬────────────────┘
         │
         ├─► Se tem imagem: Envia #gif
         │
         ├─► Formata nome (80 bytes)
         │
         ├─► Formata preço (20 bytes)
         │
         ▼
┌─────────────────────────┐
│ GertecProtocolService   │
│ SendProductInfoAsync()  │
└────────┬────────────────┘
         │
         ▼
┌─────────────────┐
│ Terminal Gertec │
│  Exibe Produto  │
└─────────────────┘
```

---

## Configurações

### appsettings.json

```json
{
  "OlistApi": {
    "BaseUrl": "https://api.tiny.com.br/api2",
    "Token": "seu_token_aqui",
    "Format": "json"
  },
  "Gertec": {
    "IpAddress": "192.168.1.57",
    "Port": 6500,
    "ReconnectIntervalSeconds": 5,
    "ResponseTimeoutMilliseconds": 500,
    "ConnectionTimeoutMilliseconds": 15000
  },
  "PriceMonitoring": {
    "Enabled": true,
    "CheckIntervalMinutes": 1
  }
}
```

---

##  Checklist de Funcionamento

### Consulta de Preço
- [x] Terminal envia código de barras (`#codigo`)
- [x] Servidor recebe e processa
- [x] Resposta imediata com `#mesg` (evita "conexão falhou")
- [x] Busca no Tiny ERP via API
- [x] Envia imagem se disponível (`#gif`)
- [x] Envia nome e preço (`#nome|preço`)
- [x] Envia `#nfound` se não encontrado

### Atualização Automática
- [x] Monitoramento contínuo a cada X minutos
- [x] Busca todos os produtos do Tiny ERP
- [x] Compara com cache em memória
- [x] Detecta mudanças de preço
- [x] Atualiza automaticamente no Gertec
- [x] Log de produtos atualizados

### Protocolo Gertec
- [x] Comando `#mesg` implementado
- [x] Comando `#gif` implementado
- [x] Comando `#nome|preço` implementado
- [x] Comando `#nfound` implementado
- [x] Formato exato conforme manual (80 bytes nome, 20 bytes preço)

---

## 🐛 Troubleshooting

### "Conexão Falhou" no Terminal
-  **Corrigido:** Resposta imediata com `#mesg` antes de buscar na API
- Verifique se o token está configurado corretamente
- Verifique logs para erros na API

### Produto Não Aparece
- Verifique se o código de barras está cadastrado no Tiny ERP
- Verifique logs: `GetProductByBarcodeAsync`
- Teste a API diretamente com curl

### Preços Não Atualizam
- Verifique se `PriceMonitoring:Enabled` está `true`
- Verifique o intervalo: `CheckIntervalMinutes`
- Verifique logs: `MonitorPriceChangesAsync`
- Verifique se há mudanças reais de preço no Tiny ERP

### Imagens Não Aparecem
- Verifique se o produto tem campo `imagem` ou `imagem_principal` na API
- Verifique tamanho da imagem (máx 124KB)
- Verifique formato (GIF recomendado)
- Verifique logs: `SendImageAsync`

---

##  Logs Importantes

```
info: Processando código de barras: 7891234567890
info: Consultando... Aguarde (enviado ao Gertec)
info: Busca produto no Tiny ERP...
info: Produto encontrado: Nome do Produto
info: Enviando imagem do produto: https://...
info: Imagem enviada com sucesso ao Gertec
info: Produto enviado ao Gertec: Nome do Produto - R$ 24,90
```

```
info: Verificando mudanças de preços no Tiny ERP...
info: Preço alterado para produto X - Preço anterior: 29.90, Novo preço: 24.90
info: Produto X atualizado no Gertec com sucesso
info: Monitoramento concluído: 5 produtos atualizados, 2 produtos novos
```

---

## 🔗 Referências

- Manual Gertec Busca Preço G2 S - Desenvolvedor
- API Tiny ERP: https://tiny.com.br/api-docs
- Documentação do Protocolo: `ARQUITETURA_REDE.md`

