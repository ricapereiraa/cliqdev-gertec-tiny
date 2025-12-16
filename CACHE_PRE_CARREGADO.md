# Cache Pré-Carregado - Implementação Completa

## Objetivo

Implementar cache pré-carregado de todos os produtos na inicialização da API, permitindo buscas instantâneas por GTIN sem necessidade de chamadas à API durante a operação normal.

## Como Funciona

### 1. **Pré-Carregamento na Inicialização**

Quando a API inicia, o `IntegrationService` chama automaticamente `PreloadCacheAsync()`:

```csharp
// No IntegrationService.ExecuteAsync()
await _olistService.PreloadCacheAsync();
```

Este método:
- Busca **todos os produtos** do Tiny ERP
- Indexa por **GTIN** (código de barras EAN/UPC) e por **Código** (SKU interno do sistema)
- Armazena em memória para acesso instantâneo

### 2. **Busca Instantânea por Código de Barras**

Quando um código de barras é escaneado (GTIN):

1. **Primeira tentativa**: Busca direto no cache pré-carregado por GTIN
   - **Resposta instantânea** (sem chamada à API!)
   - Se não encontrar, tenta buscar por código interno (SKU) como fallback
2. **Segunda tentativa**: Se não encontrou no cache, busca na API (produto novo)

### 3. **Atualização Automática do Cache**

O cache é atualizado automaticamente quando:

- **Mudança de preço detectada**: `RefreshCacheAsync()` é chamado
- **Novo produto detectado**: `RefreshCacheAsync()` é chamado
- **Primeira execução do monitoramento**: Cache é atualizado

## Estrutura do Cache

```csharp
// Cache indexado por GTIN (código de barras EAN/UPC)
private static readonly Dictionary<string, Produto> _gtinCache = new();

// Cache indexado por Código (SKU interno do sistema)
private static readonly Dictionary<string, Produto> _codigoCache = new();
```

## Vantagens

1. **Performance**: Busca instantânea (milissegundos vs segundos)
2. **Menos chamadas à API**: Reduz carga no servidor Tiny ERP
3. **Funciona mesmo quando API está lenta**: Cache local sempre disponível
4. **Atualização automática**: Cache sempre sincronizado com mudanças

## Fluxo Completo

```
┌─────────────────────────────────────────────────────────┐
│ 1. API Inicia                                            │
│    └─> PreloadCacheAsync()                               │
│        └─> Busca todos os produtos                       │
│            └─> Indexa por GTIN (código de barras)        │
│            └─> Indexa por Código (SKU interno)          │
└─────────────────────────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────┐
│ 2. Código de Barras Escaneado (GTIN)                    │
│    └─> GetProductByBarcodeAsync(barcode)                 │
│        ├─> Busca no cache por GTIN (instantâneo!)       │
│        ├─> Se não encontrar, busca por Código (SKU)     │
│        └─> Se ainda não encontrar, busca na API        │
│            └─> Retorna INSTANTANEAMENTE do cache!        │
└─────────────────────────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────┐
│ 3. Monitoramento de Preços                              │
│    └─> Detecta mudança de preço ou produto novo         │
│        └─> RefreshCacheAsync()                          │
│            └─> Atualiza cache completo                  │
│                └─> Próxima busca já usa dados atualizados│
└─────────────────────────────────────────────────────────┘
```

## Métodos Principais

### `PreloadCacheAsync()`
Pré-carrega todos os produtos no cache na inicialização.

### `RefreshCacheAsync()`
Atualiza o cache completo quando há mudanças detectadas.

### `GetProductByBarcodeAsync(string barcode)`
Busca produto por código de barras (GTIN):
1. Busca primeiro no cache pré-carregado por GTIN (instantâneo!)
2. Se não encontrar, tenta buscar por código interno (SKU) no cache
3. Se ainda não encontrar, busca na API (produto novo) e adiciona ao cache

## Resultado

- **Busca por código de barras (GTIN)**: Instantânea (milissegundos) - busca direta no cache
- **Menos chamadas à API**: Apenas quando necessário (produtos novos)
- **Cache sempre atualizado**: Atualização automática quando há mudanças
- **Performance máxima**: Sistema muito mais rápido!

## Configuração

Não requer configuração adicional. O cache é gerenciado automaticamente pelo sistema.

## Logs

O sistema registra:
- `"Pré-carregando cache de produtos..."`
- `"Cache pré-carregado com sucesso: X produtos por GTIN, Y produtos por código"`
- `"Produto encontrado no cache por GTIN (instantâneo): {nome}"`
- `"Cache atualizado: X produtos por GTIN, Y produtos por código"`

