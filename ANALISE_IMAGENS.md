# Análise do Fluxo de Imagens dos Produtos

## Resumo

O sistema **SIM busca e exibe imagens dos produtos** no terminal Gertec. O fluxo está implementado e funcional.

## Fluxo Completo de Imagens

### 1. Busca da Imagem na API

**Localização**: `Services/OlistApiService.cs`

#### Quando a imagem é buscada:

1. **No cache pré-carregado** (linha 154-183):
   - Se o produto está no cache mas não tem imagem
   - Busca imagem via `GetProductByIdAsync()` usando `produto.obter.php`
   - Atualiza o cache com a imagem encontrada

2. **Método `GetProductByIdAsync()`** (linha 360-410):
   - Chama endpoint `produto.obter.php` com o ID do produto
   - Extrai imagens do campo `anexos` da resposta
   - Retorna produto completo com URL da imagem

```csharp
// Se houver anexos (imagens), pega o primeiro
if (apiResponse.Retorno.ProdutoAnexos != null && 
    apiResponse.Retorno.ProdutoAnexos.Count > 0)
{
    produto.Imagem = apiResponse.Retorno.ProdutoAnexos[0].Anexo;
    produto.ImagemPrincipal = apiResponse.Retorno.ProdutoAnexos[0].Anexo;
}
```

### 2. Envio da Imagem para o Gertec

**Localização**: `Services/IntegrationService.cs` (linha 130-146)

#### Quando a imagem é enviada:

- **Antes** de enviar nome e preço
- Verifica se produto tem `Imagem` ou `ImagemPrincipal`
- Chama `SendImageFromFileAsync()` para enviar ao terminal

```csharp
// Envia imagem do produto se disponível (antes de enviar nome e preço)
if (!string.IsNullOrEmpty(produto.Imagem) || !string.IsNullOrEmpty(produto.ImagemPrincipal))
{
    var imagemUrl = produto.ImagemPrincipal ?? produto.Imagem;
    await _gertecService.SendImageFromFileAsync(imagemUrl, indice: 0, numeroLoops: 1, tempoExibicao: 5);
}
```

### 3. Protocolo Gertec para Imagens

**Localização**: `Services/GertecProtocolService.cs`

#### Métodos implementados:

1. **`SendImageFromFileAsync()`** (linha 571-600):
   - Aceita URL HTTP/HTTPS ou caminho de arquivo local
   - Se for URL, baixa a imagem usando `HttpClient`
   - Se for arquivo local, lê do disco
   - Chama `SendImageAsync()` para enviar

2. **`SendImageAsync()`** (linha 491-567):
   - Implementa protocolo `#gif` do Gertec
   - Formato: `#gif + índice (2 bytes hex) + loops (2 bytes hex) + tempo (2 bytes hex) + tamanho (6 bytes hex) + checksum (4 bytes hex) + ETB (0x17) + dados da imagem`
   - Valida tamanho máximo (192KB para G2, 124KB para G2 S)
   - Aguarda confirmação do terminal (`#gif_ok` ou `#img_error`)

## Estrutura de Dados

### Modelo de Produto

```csharp
public class Produto
{
    [JsonProperty("imagem")]
    public string? Imagem { get; set; }

    [JsonProperty("imagem_principal")]
    public string? ImagemPrincipal { get; set; }
}
```

### Resposta da API (produto.obter.php)

```csharp
public class RetornoResponse
{
    [JsonProperty("produto")]
    public Produto? Produto { get; set; }
    
    [JsonProperty("anexos")]
    public List<AnexoResponse>? ProdutoAnexos { get; set; }
}

public class AnexoResponse
{
    [JsonProperty("anexo")]
    public string Anexo { get; set; } = string.Empty;
}
```

## Ordem de Execução

```
1. Código de barras escaneado
   ↓
2. Busca produto no cache (ou API)
   ↓
3. Se produto não tem imagem no cache:
   - Busca via produto.obter.php
   - Atualiza cache com imagem
   ↓
4. Se produto tem imagem:
   - Envia mensagem "Consultando... Aguarde"
   - Envia imagem para Gertec (#gif)
   - Envia nome e preço (#nome|preço)
   ↓
5. Terminal exibe imagem + nome + preço
```

## Configuração Atual

- **Índice da imagem**: 0 (exibição imediata)
- **Número de loops**: 1
- **Tempo de exibição**: 5 segundos
- **Tamanho máximo**: 192KB (G2) ou 124KB (G2 S)

## Logs Importantes

O sistema registra:
- `"Imagem obtida para produto {nome}: {url}"` - Quando busca imagem
- `"Enviando imagem do produto: {url}"` - Quando inicia envio
- `"Imagem enviada com sucesso ao Gertec (índice: {indice})"` - Quando terminal confirma
- `"Erro ao enviar imagem do produto"` - Se houver erro (não bloqueia envio de nome/preço)

## Tratamento de Erros

- Se falhar ao buscar imagem: Continua sem imagem (não bloqueia)
- Se falhar ao enviar imagem: Continua com nome e preço
- Se imagem muito grande: Rejeita e loga erro
- Se terminal não conectado: Não tenta enviar

## Status

 **Implementado e funcional**

O sistema:
- Busca imagens da API quando disponíveis
- Envia imagens para o terminal Gertec
- Exibe imagens antes do nome e preço
- Trata erros graciosamente (não bloqueia operação)

## Melhorias Possíveis

1. **Cache de imagens**: Baixar e armazenar imagens localmente para evitar downloads repetidos
2. **Otimização de tamanho**: Redimensionar imagens antes de enviar
3. **Múltiplas imagens**: Suporte para loop de imagens (índices 01-FE)
4. **Formato de imagem**: Validar se é GIF (requisito do Gertec)

