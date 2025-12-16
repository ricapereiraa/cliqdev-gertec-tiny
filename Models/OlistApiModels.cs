using Newtonsoft.Json;

namespace OlistGertecIntegration.Models;

// Estrutura baseada na documentação oficial da API Tiny
// https://tiny.com.br/api-docs/api2-produtos-pesquisar
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
    
    // Para produto.obter.php - retorna produto único
    [JsonProperty("produto")]
    public Produto? Produto { get; set; }
    
    // Anexos/imagens do produto (produto.obter.php)
    [JsonProperty("anexos")]
    public List<AnexoResponse>? ProdutoAnexos { get; set; }
}

public class AnexoResponse
{
    [JsonProperty("anexo")]
    public string Anexo { get; set; } = string.Empty;
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

public class Produto
{
    [JsonProperty("id")]
    public int Id { get; set; }
    
    [JsonProperty("codigo")]
    public string? Codigo { get; set; }
    
    [JsonProperty("nome")]
    public string Nome { get; set; } = string.Empty;
    
    [JsonProperty("preco")]
    public string Preco { get; set; } = string.Empty;
    
    [JsonProperty("preco_promocional")]
    public string PrecoPromocional { get; set; } = string.Empty;
    
    [JsonProperty("preco_custo")]
    public string? PrecoCusto { get; set; }
    
    [JsonProperty("preco_custo_medio")]
    public string? PrecoCustoMedio { get; set; }
    
    [JsonProperty("unidade")]
    public string? Unidade { get; set; }
    
    [JsonProperty("gtin")]
    public string? Gtin { get; set; }
    
    [JsonProperty("tipoVariacao")]
    public string TipoVariacao { get; set; } = string.Empty;
    
    [JsonProperty("localizacao")]
    public string? Localizacao { get; set; }
    
    [JsonProperty("situacao")]
    public string? Situacao { get; set; }
    
    [JsonProperty("data_criacao")]
    public string? DataCriacao { get; set; }
    
    // Campo de imagem (URL ou caminho local) - opcional
    [JsonProperty("imagem")]
    public string? Imagem { get; set; }
    
    [JsonProperty("imagem_principal")]
    public string? ImagemPrincipal { get; set; }
    
    // Campos legados para compatibilidade
    public string Descricao { get; set; } = string.Empty;
    public string Estoque { get; set; } = string.Empty;
}

// Mantido para compatibilidade com código existente
public class OlistApiResponse<T>
{
    public string Status { get; set; } = string.Empty;
    public T? Retorno { get; set; }
}

public class ProdutoResponse
{
    public Produto? Produto { get; set; }
}

public class ListaProdutosResponse
{
    public List<Produto>? Produtos { get; set; }
}

