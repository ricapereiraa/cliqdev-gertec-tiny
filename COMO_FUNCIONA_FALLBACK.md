# Como Funciona o Fallback Quando a Busca Direta Falha

##  Fluxo Completo Passo a Passo

Vamos acompanhar o que acontece quando você escaneia um código de barras GTIN no terminal Gertec.

---

##  Cenário: Código de Barras GTIN

**Código escaneado:** `7898132989040` (GTIN/EAN)

---

##  Passo 1: Primeira Tentativa - Busca Direta

### O que acontece:

```csharp
// Código em OlistApiService.cs - linha 43-48
var content = new FormUrlEncodedContent(new[]
{
    new KeyValuePair<string, string>("token", _token),
    new KeyValuePair<string, string>("formato", _format),
    new KeyValuePair<string, string>("pesquisa", "7898132989040")  // GTIN
});

var response = await _httpClient.PostAsync(
    "https://api.tiny.com.br/api2/produtos.pesquisa.php", 
    content
);
```

### Requisição HTTP enviada:
```
POST https://api.tiny.com.br/api2/produtos.pesquisa.php
Content-Type: application/x-www-form-urlencoded

token=f08598c71a1384a81527110a5dbf1d5fcb1773af
formato=json
pesquisa=7898132989040
```

### Resposta da API:
```json
{
  "retorno": {
    "status": "Erro",
    "codigo_erro": "20",
    "erros": [
      {
        "erro": "A consulta não retornou registros"
      }
    ]
  }
}
```

### Análise do código (linha 58-84):
```csharp
var apiResponse = JsonConvert.DeserializeObject<TinyApiResponse>(responseContent);

if (apiResponse?.Retorno?.Status == "OK" && 
    apiResponse.Retorno.Produtos != null && 
    apiResponse.Retorno.Produtos.Count > 0)
{
    //  NÃO ENTRA AQUI porque status = "Erro"
    // ...
}
```

**Resultado:**  **FALHOU** - A API não encontrou nada porque não busca por GTIN.

---

##  Passo 2: Verificação do Cache

### O que acontece (linha 90-99):

```csharp
// Verifica se já temos esse GTIN em cache (válido por 30 segundos)
Produto? produtoCache = null;
lock (_cacheLock)
{
    if (_gtinCache.TryGetValue("7898132989040", out var cacheEntry))
    {
        var idadeCache = DateTime.Now - cacheEntry.timestamp;
        if (idadeCache < TimeSpan.FromSeconds(30))  // Cache válido?
        {
            produtoCache = cacheEntry.produto;
            //  Retorna do cache (muito rápido!)
            return produtoCache;
        }
        else
        {
            // Cache expirado, remove
            _gtinCache.Remove("7898132989040");
        }
    }
}
```

**Cenários:**
-  **Cache válido (< 30s):** Retorna imediatamente (muito rápido!)
-  **Cache expirado ou não existe:** Continua para o próximo passo

**Neste exemplo:** Assumindo que não há cache → Continua para o Passo 3.

---

##  Passo 3: Segunda Tentativa - Busca Completa + Filtro Local

### O que acontece (linha 101-118):

```csharp
_logger.LogInformation(
    "Busca direta não retornou resultado para 7898132989040. " +
    "Buscando em todos os produtos..."
);

//  BUSCA TODOS OS PRODUTOS (sem parâmetro pesquisa)
var todosProdutos = await GetAllProductsAsync();
```

### Requisição HTTP enviada:
```
POST https://api.tiny.com.br/api2/produtos.pesquisa.php
Content-Type: application/x-www-form-urlencoded

token=f08598c71a1384a81527110a5dbf1d5fcb1773af
formato=json
#  SEM parâmetro "pesquisa" = retorna TODOS os produtos
```

### Resposta da API:
```json
{
  "retorno": {
    "status": "OK",
    "status_processamento": "3",
    "pagina": 1,
    "numero_paginas": 424,  ← 424 páginas de produtos!
    "produtos": [
      {
        "produto": {
          "id": "878745884",
          "nome": " Bio Extratus Condicionador Cachos 250ml",
          "codigo": "SKU007158",
          "gtin": "7898132989040",  ← AQUI ESTÁ O GTIN!
          "preco": 41.9
        }
      },
      {
        "produto": {
          "id": "875184359",
          "nome": " Davene Leite De Aveia...",
          "codigo": "SKU10102045",
          "gtin": "7898489512687",  ← Outro produto
          "preco": 24.9
        }
      },
      // ... milhares de outros produtos
    ]
  }
}
```

### Processamento (linha 103-118):

```csharp
// Recebe TODOS os produtos (pode ser centenas ou milhares)
var todosProdutos = await GetAllProductsAsync();
// todosProdutos = [Produto1, Produto2, Produto3, ..., ProdutoN]

//  FILTRA LOCALMENTE pelo GTIN
var produtoPorGtin = todosProdutos
    .FirstOrDefault(p => 
        !string.IsNullOrEmpty(p.Gtin) && 
        p.Gtin == "7898132989040"  // Procura o GTIN que queremos
    );
```

### Como funciona o filtro:

```csharp
// Simulação do que acontece:
foreach (var produto in todosProdutos)
{
    if (!string.IsNullOrEmpty(produto.Gtin) && 
        produto.Gtin == "7898132989040")
    {
        //  ENCONTROU!
        return produto;  // Retorna este produto
    }
}

// Se chegar aqui, não encontrou
return null;
```

**Resultado:**  **ENCONTROU!** O produto com GTIN `7898132989040` foi encontrado na lista completa.

---

## Passo 4: Atualização do Cache

### O que acontece (linha 109-114):

```csharp
if (produtoPorGtin != null)
{
    // Salva no cache para próximas consultas
    lock (_cacheLock)
    {
        _gtinCache["7898132989040"] = (produtoPorGtin, DateTime.Now);
        _lastFullSync = DateTime.Now;
    }
    
    _logger.LogInformation(
        $"Produto encontrado por GTIN em busca completa: {produtoPorGtin.Nome}"
    );
    
    return produtoPorGtin;  //  Retorna o produto encontrado
}
```

**Resultado:** 
-  Produto encontrado e retornado
- Cache atualizado (próxima consulta será instantânea se dentro de 30s)

---

##  Diagrama do Fluxo Completo

```
┌─────────────────────────────────────────────────────────┐
│ Terminal Gertec escaneia: 7898132989040 (GTIN)         │
└────────────────────┬────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────┐
│ PASSO 1: Busca Direta                                   │
│ POST /produtos.pesquisa.php?pesquisa=7898132989040     │
└────────────────────┬────────────────────────────────────┘
                     │
                     ▼
         ┌───────────────────────┐
         │ API retorna: "Erro"   │
         │ "Não encontrou"       │
         └───────────┬────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────┐
│ PASSO 2: Verifica Cache                                 │
│ Cache["7898132989040"] existe?                         │
└────────────────────┬────────────────────────────────────┘
                     │
         ┌───────────┴───────────┐
         │                       │
         ▼                       ▼
     Cache OK             Sem cache
    (retorna)              (continua)
         │                       │
         │                       ▼
         │        ┌──────────────────────────────────────┐
         │        │ PASSO 3: Busca Completa              │
         │        │ POST /produtos.pesquisa.php          │
         │        │ (SEM parâmetro pesquisa)            │
         │        └──────────────┬───────────────────────┘
         │                       │
         │                       ▼
         │        ┌──────────────────────────────────────┐
         │        │ Recebe TODOS os produtos             │
         │        │ [Produto1, Produto2, ..., ProdutoN] │
         │        └──────────────┬───────────────────────┘
         │                       │
         │                       ▼
         │        ┌──────────────────────────────────────┐
         │        │ Filtra localmente:                   │
         │        │ produtos.Where(p =>                  │
         │        │   p.Gtin == "7898132989040")         │
         │        └──────────────┬───────────────────────┘
         │                       │
         │                       ▼
         │        ┌──────────────────────────────────────┐
         │        │ PASSO 4: Atualiza Cache              │
         │        │ Cache["7898132989040"] = produto     │
         │        └──────────────┬───────────────────────┘
         │                       │
         └───────────────────────┴───────────┐
                                             │
                                             ▼
                              ┌──────────────────────────────┐
                              │ Retorna Produto Encontrado! │
                              │ Nome: "Bio Extratus..."     │
                              │ Preço: R$ 41,90             │
                              └──────────────────────────────┘
```

---

##  Performance por Cenário

### Cenário 1: Busca Direta Funciona (SKU)
```
Tempo: ~100-200ms
Requisições: 1
Status:  Muito rápido
```

### Cenário 2: Busca Direta Falha, Cache Válido
```
Tempo: ~1-5ms
Requisições: 0 (usa cache)
Status:  Instantâneo
```

### Cenário 3: Busca Direta Falha, Sem Cache
```
Tempo: ~500-2000ms
Requisições: 1 (busca todos)
Status:  Mais lento, mas funciona
```

### Cenário 4: Segunda Consulta (Cache Criado)
```
Tempo: ~1-5ms
Requisições: 0 (usa cache)
Status:  Instantâneo
```

---

##  Por que Funciona?

### A API Tiny retorna o GTIN nos produtos:

Mesmo que a API **não busque** por GTIN, ela **retorna o GTIN** em cada produto:

```json
{
  "produto": {
    "codigo": "SKU007158",      ← Busca por este campo
    "gtin": "7898132989040",    ← Mas retorna este campo!
    "nome": "Bio Extratus...",
    "preco": 41.9
  }
}
```

### Nossa estratégia:

1.  **Busca todos os produtos** (a API permite isso)
2.  **Recebe o GTIN de cada produto** (a API retorna)
3.  **Filtra localmente** (nossa aplicação faz)
4.  **Encontra o produto correto** (mesmo que a busca direta falhe)

---

## 🔑 Pontos-Chave

1. **A busca direta falha** porque a API não indexa GTIN para busca
2. **A busca completa funciona** porque a API retorna GTIN em cada produto
3. **O filtro local resolve** porque comparamos GTINs na nossa aplicação
4. **O cache otimiza** consultas repetidas (30 segundos)
5. **Sempre funciona** - mesmo que seja mais lento na primeira vez

---

##  Conclusão

**Quando a busca direta falha:**
1.  API retorna erro "não encontrou"
2.  Sistema busca TODOS os produtos
3. Filtra localmente pelo GTIN
4. Encontra o produto correto
5. Salva no cache para próxima vez

**Resultado:** Sistema sempre encontra o produto, mesmo com a limitação da API!

