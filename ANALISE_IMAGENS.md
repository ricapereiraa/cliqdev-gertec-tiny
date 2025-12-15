# Análise: Envio de Imagens Tiny → Gertec G2

## Documentação Analisada

### 1. Protocolo Gertec G2 - Comando #gif

Baseado no manual do desenvolvedor fornecido:

**Comando:** `#gif`

**Formato:**
```
#gif + dados
```

**Estrutura dos dados:**
1. **2 bytes:** Índice da imagem (hexadecimal em ASCII)
   - `00`: Imagem exibida imediatamente
   - `01` a `FE`: Imagem do loop de imagens
   - `FF`: Reset do loop de imagens

2. **2 bytes:** Número de loops (hexadecimal em ASCII)
   - `00` a `FF`: Número de vezes que o GIF animado será repetido

3. **2 bytes:** Tempo de espera (hexadecimal em ASCII)
   - `00` a `FF`: Tempo em segundos em que a imagem será exibida

4. **6 bytes:** Tamanho da imagem (hexadecimal)
   - Tamanho de cada quadro da imagem que será enviada

5. **4 bytes:** Checksum (hexadecimal)
   - Operação XOR entre todos os bytes da imagem
   - Recomendado: `0000`

6. **1 byte:** Separador ETB (`0x17`)
   - Separador entre cabeçalho e dados das imagens

7. **Dados da imagem:** Bytes da imagem/gif

**Limitações:**
- **Busca Preço G2 sem áudio:** Máximo 192KB
- **Busca Preço G2 S (com áudio):** Máximo 124KB
- **Formato:** GIF animado
- **Dimensão:** 320x240 pixels

**Resposta do equipamento:**
- `#gif_ok00` - Sucesso
- `#img_error` - Erro

### 2. API Tiny - Campos de Produto

**Endpoint:** `produtos.pesquisa.php`

**Campos retornados (baseado na documentação oficial):**
- `id` - ID do produto
- `codigo` - Código do produto
- `nome` - Nome do produto
- `preco` - Preço de venda
- `preco_promocional` - Preço promocional
- `gtin` - GTIN/EAN
- `unidade` - Unidade
- `situacao` - Situação
- `data_criacao` - Data de criação

**⚠️ IMPORTANTE:** A documentação oficial da API Tiny **NÃO menciona** campos de imagem/URL de imagem no retorno de `produtos.pesquisa.php`.

**Possíveis campos de imagem (a verificar):**
- `imagem` ou `imagem_url` - Pode existir mas não está documentado
- `foto` ou `foto_url` - Pode existir mas não está documentado
- `imagens[]` - Array de imagens (não confirmado)

## Conclusão da Análise

### ✅ Gertec G2 - Suporta Imagens

**SIM, o Gertec G2 suporta imagens:**
- Comando `#gif` implementado no protocolo
- Suporta GIF animado
- Dimensão: 320x240 pixels
- Tamanho máximo: 192KB (sem áudio) ou 124KB (com áudio)

### ❓ API Tiny - Campos de Imagem

**NÃO CONFIRMADO:**
- A documentação oficial não menciona campos de imagem
- Pode existir mas não está documentado
- Necessário testar com API real para confirmar

## Recomendações

### Opção 1: Verificar API Real

Testar a API do Tiny com um produto real para verificar se retorna campos de imagem:

```bash
curl -X POST https://api.tiny.com.br/api2/produtos.pesquisa.php \
  -d "token=SEU_TOKEN" \
  -d "formato=json" \
  -d "pesquisa=CODIGO_PRODUTO"
```

**Verificar se a resposta contém:**
- `imagem` ou `imagem_url`
- `foto` ou `foto_url`
- `imagens[]` ou similar

### Opção 2: Implementar com Fallback

Implementar a funcionalidade assumindo que:
1. A API pode retornar URL de imagem (se disponível)
2. Se não houver imagem, enviar apenas texto (comportamento atual)
3. Se houver imagem, baixar, converter e enviar ao Gertec

### Opção 3: Usar Endpoint Alternativo

Verificar se existe endpoint específico para obter detalhes completos do produto:
- `produto.obter.php` - Pode retornar mais campos incluindo imagens
- `produto.consultar.php` - Pode retornar mais informações

## Próximos Passos

1. **Testar API Tiny** com produto real para verificar campos de imagem
2. **Implementar suporte a imagens** no modelo `Produto`
3. **Implementar método `SendImageGifAsync`** no `GertecProtocolService`
4. **Modificar `IntegrationService`** para enviar imagem quando disponível
5. **Adicionar conversão de imagem** (se necessário) para formato GIF 320x240

## Implementação Sugerida

### 1. Adicionar campo de imagem no modelo

```csharp
public class Produto
{
    // ... campos existentes ...
    
    [JsonProperty("imagem")]
    public string? Imagem { get; set; }
    
    [JsonProperty("imagem_url")]
    public string? ImagemUrl { get; set; }
}
```

### 2. Implementar método de envio de imagem

```csharp
public async Task<bool> SendImageGifAsync(byte[] imageData, int index = 0, int loops = 1, int duration = 5)
{
    // Implementar protocolo #gif conforme documentação
}
```

### 3. Modificar fluxo de consulta

```csharp
if (produto != null)
{
    // Se tiver imagem, enviar imagem
    if (!string.IsNullOrEmpty(produto.ImagemUrl))
    {
        await SendImageWithProductAsync(produto);
    }
    else
    {
        // Fallback: enviar apenas texto
        await SendProductInfoAsync(nomeFormatado, precoFormatado);
    }
}
```

## Status

- ✅ **Gertec G2:** Suporta imagens via comando `#gif`
- ❓ **API Tiny:** Campos de imagem não confirmados na documentação
- ⏳ **Implementação:** Aguardando confirmação de campos da API

