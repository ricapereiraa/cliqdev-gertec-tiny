# Correções Necessárias - API Tiny Oficial

## Análise da Documentação Oficial

Baseado na documentação oficial: https://tiny.com.br/api-docs/api2-produtos-pesquisar

## AVISO: Problemas Identificados

### 1. Endpoint Incorreto

**Nossa Implementação:**
```csharp
var response = await _httpClient.PostAsync($"{_baseUrl}/produto.pesquisa.php", content);
```

**Documentação Oficial:**
- Endpoint correto: `produtos.pesquisa.php` (PLURAL)
- URL: `https://api.tiny.com.br/api2/produtos.pesquisa.php`

**ERRO: ERRO:** Estamos usando `produto.pesquisa.php` (singular) quando deveria ser `produtos.pesquisa.php` (plural)

### 2. Estrutura de Resposta Incorreta

**Nossa Implementação Esperada:**
```csharp
OlistApiResponse<ProdutoResponse>
// Onde ProdutoResponse tem:
public Produto? Produto { get; set; } // Objeto único
```

**Documentação Oficial:**
```json
{
 "retorno": {
  "status": "OK",
  "produtos": [      // ARRAY de produtos
   {
    "produto": {
     "id": 46829062,
     "codigo": "123",
     "nome": "produto teste",
     "preco": "1.20",
     "preco_promocional": "1.10"
    }
   }
  ]
 }
}
```

**ERRO: ERRO:** A resposta retorna um **ARRAY** de produtos, não um objeto único!

### 3. Estrutura de Status

**Nossa Implementação:**
```csharp
public class OlistApiResponse<T>
{
  public string Status { get; set; } // Esperamos no nível raiz
  public T? Retorno { get; set; }
}
```

**Documentação Oficial:**
```json
{
 "retorno": {       // Tudo está dentro de "retorno"
  "status": "OK",     // Status está dentro de retorno
  "produtos": [...]
 }
}
```

**ERRO: ERRO:** O `status` está dentro de `retorno`, não no nível raiz!

## Correções Necessárias

### Correção 1: Endpoint

```csharp
// ANTES (ERRADO):
var response = await _httpClient.PostAsync($"{_baseUrl}/produto.pesquisa.php", content);

// DEPOIS (CORRETO):
var response = await _httpClient.PostAsync($"{_baseUrl}/produtos.pesquisa.php", content);
```

### Correção 2: Modelos

```csharp
// NOVO modelo baseado na documentação oficial:
public class TinyApiResponse
{
  [JsonProperty("retorno")]
  public RetornoResponse? Retorno { get; set; }
}

public class RetornoResponse
{
  [JsonProperty("status")]
  public string Status { get; set; } = string.Empty;
  
  [JsonProperty("status_processamento")]
  public int StatusProcessamento { get; set; }
  
  [JsonProperty("codigo_erro")]
  public int? CodigoErro { get; set; }
  
  [JsonProperty("erros")]
  public List<ErroResponse>? Erros { get; set; }
  
  [JsonProperty("pagina")]
  public int Pagina { get; set; }
  
  [JsonProperty("numero_paginas")]
  public int NumeroPaginas { get; set; }
  
  [JsonProperty("produtos")]
  public List<ProdutoWrapper>? Produtos { get; set; }
}

public class ProdutoWrapper
{
  [JsonProperty("produto")]
  public Produto? Produto { get; set; }
}

public class ErroResponse
{
  [JsonProperty("erro")]
  public string Erro { get; set; } = string.Empty;
}
```

### Correção 3: Parser de Resposta

```csharp
// ANTES (ERRADO):
var apiResponse = JsonConvert.DeserializeObject<OlistApiResponse<ProdutoResponse>>(responseContent);
if (apiResponse?.Status == "OK" && apiResponse.Retorno?.Produto != null)
{
  return apiResponse.Retorno.Produto;
}

// DEPOIS (CORRETO):
var apiResponse = JsonConvert.DeserializeObject<TinyApiResponse>(responseContent);
if (apiResponse?.Retorno?.Status == "OK" && apiResponse.Retorno.Produtos != null && apiResponse.Retorno.Produtos.Count > 0)
{
  // Retorna o primeiro produto encontrado
  return apiResponse.Retorno.Produtos[0].Produto;
}
```

## Campos do Produto (Confirmados)

Baseado na documentação oficial:

| Campo | Tipo | Obrigatório | Observação |
|-------|------|-------------|------------|
| `id` | int | | ID do produto |
| `codigo` | string | AVISO: | Código do produto (pode ser null) |
| `nome` | string | | Nome do produto |
| `preco` | decimal | | Preço de venda (formato "1.20") |
| `preco_promocional` | decimal | | Preço promocional (formato "1.10") |
| `gtin` | string | AVISO: | GTIN/EAN (pode ser null) |

** Nossos campos estão corretos!** Apenas precisamos ajustar a estrutura de resposta.

## Implementação Corrigida

Veja os arquivos corrigidos:
- `Models/OlistApiModels.cs` - Modelos atualizados
- `Services/OlistApiService.cs` - Parser corrigido

