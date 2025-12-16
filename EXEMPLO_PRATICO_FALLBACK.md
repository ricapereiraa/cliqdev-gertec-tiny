# Exemplo Prático: Como o Fallback Funciona

## 🎬 Cenário Real

**Código de barras escaneado no Gertec:** `7898132989040`

---

##  Código Executado Passo a Passo

### **Linha 34-41:** Validação Inicial
```csharp
public async Task<Produto?> GetProductByBarcodeAsync(string barcode)
{
    // barcode = "7898132989040"
    
    if (string.IsNullOrWhiteSpace(barcode))
    {
        return null;  //  Não entra aqui (barcode tem valor)
    }
    //  Continua...
}
```

---

### **Linha 43-49:** Primeira Tentativa - Busca Direta
```csharp
// Monta requisição HTTP
var content = new FormUrlEncodedContent(new[]
{
    new KeyValuePair<string, string>("token", _token),
    new KeyValuePair<string, string>("formato", "json"),
    new KeyValuePair<string, string>("pesquisa", "7898132989040")  // ← GTIN
});

// Envia requisição
var response = await _httpClient.PostAsync(
    "https://api.tiny.com.br/api2/produtos.pesquisa.php", 
    content
);
```

**O que acontece:**
- 📤 Envia: `pesquisa=7898132989040` para a API
- 📥 Recebe: `{"retorno": {"status": "Erro", "erro": "A consulta não retornou registros"}}`

---

### **Linha 63-86:** Processa Resposta da Busca Direta
```csharp
var apiResponse = JsonConvert.DeserializeObject<TinyApiResponse>(responseContent);
// apiResponse.Retorno.Status = "Erro" 

if (apiResponse?.Retorno?.Status == "OK" &&  //  FALSE (status = "Erro")
    apiResponse.Retorno.Produtos != null && 
    apiResponse.Retorno.Produtos.Count > 0)
{
    //  NÃO ENTRA AQUI
    // ...
}
//  Pula este bloco e continua...
```

**Resultado:** Busca direta falhou, continua para o fallback.

---

### **Linha 92-111:** Verifica Cache
```csharp
Produto? produtoCache = null;
lock (_cacheLock)
{
    // Tenta buscar do cache
    if (_gtinCache.TryGetValue("7898132989040", out var cacheEntry))
    {
        // cacheEntry = (produto, timestamp)
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
//  Cache não existe ou expirado, continua...
```

**Neste exemplo:** Assumindo cache vazio → `produtoCache = null`

---

### **Linha 115-118:** Retorna do Cache (se encontrou)
```csharp
if (produtoCache != null)
{
    return produtoCache;  //  Não entra (produtoCache = null)
}
//  Continua para busca completa...
```

---

### **Linha 120:** Log de Fallback
```csharp
_logger.LogInformation(
    $"Busca direta não retornou resultado para 7898132989040. " +
    "Buscando em todos os produtos..."
);
```

**Log gerado:**
```
info: Busca direta não retornou resultado para 7898132989040. 
      Buscando em todos os produtos...
```

---

### **Linha 123:** Chama GetAllProductsAsync()
```csharp
// Busca TODOS os produtos (sem filtro)
var todosProdutos = await GetAllProductsAsync();
```

**O que `GetAllProductsAsync()` faz (linha 157-186):**

```csharp
public async Task<List<Produto>> GetAllProductsAsync(DateTime? sinceDate = null)
{
    // Monta requisição SEM parâmetro "pesquisa"
    var content = new FormUrlEncodedContent(new[]
    {
        new KeyValuePair<string, string>("token", _token),
        new KeyValuePair<string, string>("formato", "json")
        //  SEM "pesquisa" = retorna TODOS
    });

    var response = await _httpClient.PostAsync(
        "https://api.tiny.com.br/api2/produtos.pesquisa.php", 
        content
    );
    
    // Recebe TODOS os produtos
    var apiResponse = JsonConvert.DeserializeObject<TinyApiResponse>(responseContent);
    
    if (apiResponse?.Retorno?.Status == "OK" && 
        apiResponse.Retorno.Produtos != null)
    {
        // Extrai lista de produtos
        return apiResponse.Retorno.Produtos
            .Where(p => p.Produto != null)
            .Select(p => p.Produto!)
            .ToList();
    }
    
    return new List<Produto>();
}
```

**Resposta da API:**
```json
{
  "retorno": {
    "status": "OK",
    "produtos": [
      {
        "produto": {
          "id": "878745884",
          "nome": " Bio Extratus Condicionador Cachos 250ml",
          "codigo": "SKU007158",
          "gtin": "7898132989040",  ← AQUI ESTÁ!
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
      }
      // ... centenas/milhares de outros produtos
    ]
  }
}
```

**Resultado:**
```csharp
todosProdutos = [
    Produto { Gtin = "7898132989040", Nome = "Bio Extratus...", Preco = 41.9 },
    Produto { Gtin = "7898489512687", Nome = "Davene...", Preco = 24.9 },
    // ... muitos outros
]
```

---

### **Linha 124-125:** Filtra Localmente pelo GTIN
```csharp
var produtoPorGtin = todosProdutos
    .FirstOrDefault(p => 
        !string.IsNullOrEmpty(p.Gtin) && 
        p.Gtin == "7898132989040"  // ← Procura este GTIN
    );
```

**O que `FirstOrDefault` faz:**
```csharp
// Equivalente a:
foreach (var produto in todosProdutos)
{
    if (!string.IsNullOrEmpty(produto.Gtin) && 
        produto.Gtin == "7898132989040")
    {
        return produto;  //  ENCONTROU!
    }
}
return null;  // Se não encontrou
```

**Resultado:**
```csharp
produtoPorGtin = Produto {
    Id = 878745884,
    Nome = " Bio Extratus Condicionador Cachos 250ml",
    Codigo = "SKU007158",
    Gtin = "7898132989040",  ←  CORRESPONDE!
    Preco = 41.9
}
```

---

### **Linha 127-137:** Atualiza Cache e Retorna
```csharp
if (produtoPorGtin != null)  //  TRUE
{
    // Salva no cache para próxima vez
    lock (_cacheLock)
    {
        _gtinCache["7898132989040"] = (
            produtoPorGtin,           // O produto encontrado
            DateTime.Now              // Timestamp atual
        );
        _lastFullSync = DateTime.Now;
    }
    
    _logger.LogInformation(
        $"Produto encontrado por GTIN em busca completa: {produtoPorGtin.Nome}"
    );
    
    return produtoPorGtin;  //  RETORNA O PRODUTO!
}
```

**Log gerado:**
```
info: Produto encontrado por GTIN em busca completa: 
      Bio Extratus Condicionador Cachos 250ml
```

**Cache atualizado:**
```csharp
_gtinCache["7898132989040"] = (
    produto: Produto { Nome = "Bio Extratus...", Preco = 41.9 },
    timestamp: 2024-01-15 10:30:45
)
```

---

##  Próxima Consulta (Cache Funciona!)

Se o mesmo código for consultado novamente dentro de 30 segundos:

```csharp
// Linha 96-103
if (_gtinCache.TryGetValue("7898132989040", out var cacheEntry))
{
    var idadeCache = DateTime.Now - cacheEntry.timestamp;
    // idadeCache = 5 segundos (< 30 segundos) 
    
    if (idadeCache < CacheValidity)  //  TRUE
    {
        produtoCache = cacheEntry.produto;
        _logger.LogInformation(
            $"Produto encontrado no cache por GTIN (idade: 5.0s): Bio Extratus..."
        );
        return produtoCache;  //  RETORNA IMEDIATAMENTE!
    }
}
```

**Resultado:**  **Instantâneo** (~1-5ms) - não precisa buscar na API!

---

##  Resumo do Fluxo

```
Consulta GTIN: 7898132989040
    │
    ├─► Busca Direta (API)
    │   └─►  Falha (API não busca por GTIN)
    │
    ├─► Verifica Cache
    │   └─►  Não existe ou expirado
    │
    ├─► Busca Completa (API)
    │   └─►  Retorna TODOS os produtos
    │
    ├─► Filtra Localmente
    │   └─►  Encontra produto com GTIN = 7898132989040
    │
    ├─► Atualiza Cache
    │   └─► 💾 Salva para próxima vez
    │
    └─►  Retorna Produto Encontrado!
```

---

##  Por que Funciona?

### A chave está aqui:

1. **API não busca por GTIN** → Busca direta falha
2. **API retorna GTIN nos produtos** → Busca completa funciona
3. **Filtramos localmente** → Encontramos o produto correto
4. **Cache otimiza** → Próximas consultas são instantâneas

### Exemplo Visual:

```
API Tiny ERP:
┌─────────────────────────────────────┐
│ Busca por "7898132989040"          │
│  Não encontra (GTIN não indexado)│
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│ Busca TODOS os produtos             │
│  Retorna lista com GTINs          │
│    [Produto1, Produto2, ...]        │
└─────────────────────────────────────┘
         │
         ▼
Nossa Aplicação:
┌─────────────────────────────────────┐
│ Filtra: p.Gtin == "7898132989040"  │
│  Encontra Produto1!               │
└─────────────────────────────────────┘
```

---

##  Conclusão

**Quando a busca direta falha:**
1. Sistema busca **TODOS** os produtos da API
2. Recebe uma **lista completa** com GTINs
3. **Filtra localmente** pelo GTIN desejado
4. **Encontra o produto** correto
5. **Salva no cache** para otimizar próximas consultas

**Resultado:** Sempre funciona, mesmo com a limitação da API! 🎉

