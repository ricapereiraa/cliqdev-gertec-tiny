using Npgsql;
using OlistGertecIntegration.Models;

namespace OlistGertecIntegration.Services;

/// <summary>
/// Serviço para gerenciar conexão e operações no banco de dados PostgreSQL
/// </summary>
public class DatabaseService : IDisposable
{
    private readonly ILogger<DatabaseService> _logger;
    private readonly string _connectionString;
    private readonly string _tableName;

    public DatabaseService(ILogger<DatabaseService> logger, IConfiguration configuration)
    {
        _logger = logger;
        
        // Obtém configurações de conexão (prioridade: variáveis de ambiente > appsettings.json)
        var host = Environment.GetEnvironmentVariable("DB__HOST") ?? configuration["Database:Host"] ?? "localhost";
        var database = Environment.GetEnvironmentVariable("DB__DATABASE") ?? configuration["Database:Database"] ?? "tinytog2";
        var username = Environment.GetEnvironmentVariable("DB__USERNAME") ?? configuration["Database:Username"] ?? "tinytog2";
        var password = Environment.GetEnvironmentVariable("DB__PASSWORD") ?? configuration["Database:Password"] ?? "tinytog2";
        var port = Environment.GetEnvironmentVariable("DB__PORT") ?? configuration["Database:Port"] ?? "5432";
        
        _tableName = Environment.GetEnvironmentVariable("DB__TABLE_NAME") ?? configuration["Database:TableName"] ?? "PRODUCT";
        
        _connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};";
        
        _logger.LogInformation($"DatabaseService inicializado - Host: {host}, Database: {database}, Table: {_tableName}");
        Console.WriteLine($"DatabaseService inicializado - Host: {host}, Database: {database}, Table: {_tableName}");
    }

    /// <summary>
    /// Verifica se um produto existe no banco pelo código de barras
    /// </summary>
    public async Task<bool> ProductExistsAsync(string barCode)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            var query = $"SELECT COUNT(*) FROM {_tableName} WHERE BAR_CODE = @barCode";
            await using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("barCode", barCode);
            
            var count = await cmd.ExecuteScalarAsync() as long? ?? 0;
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao verificar se produto existe: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Insere um novo produto no banco de dados (usa UPSERT para garantir que atualiza se já existe)
    /// </summary>
    public async Task<bool> InsertProductAsync(Produto produto)
    {
        // Usa UpsertProductAsync para garantir que não cria duplicado se já existe
        return await UpsertProductAsync(produto);
    }

    /// <summary>
    /// Atualiza um produto existente no banco de dados (usa UPSERT para garantir atualização)
    /// </summary>
    public async Task<bool> UpdateProductAsync(Produto produto)
    {
        // Usa UpsertProductAsync para garantir que atualiza se existe ou insere se não existe
        return await UpsertProductAsync(produto);
    }

    /// <summary>
    /// Insere ou atualiza um produto (UPSERT) baseado no código de barras (GTIN)
    /// Mapeamento conforme padrão: BAR_CODE=GTIN, DESCRIPTION=Nome, PRICE_1=Preço, PRICE_2=PreçoPromocional
    /// Se o BAR_CODE já existe, ATUALIZA os dados. Se não existe, INSERE novo.
    /// </summary>
    public async Task<bool> UpsertProductAsync(Produto produto)
    {
        try
        {
            // BAR_CODE = GTIN (prioridade) ou Código (fallback) - conforme mapeamento da imagem
            var barCode = produto.Gtin ?? produto.Codigo ?? "";
            if (string.IsNullOrEmpty(barCode))
            {
                _logger.LogWarning($"Produto {produto.Nome} ignorado: sem código de barras (GTIN ou código)");
                return false;
            }

            // DESCRIPTION = Nome do produto - conforme mapeamento da imagem
            var description = produto.Nome?.Replace("'", "''") ?? "";
            if (string.IsNullOrEmpty(description))
            {
                description = "Produto sem nome";
            }
            
            // PRICE_1 = Preço normal - conforme mapeamento da imagem
            var price1 = ParsePrice(produto.Preco);
            
            // PRICE_2 = Preço promocional - conforme mapeamento da imagem
            var price2 = ParsePrice(produto.PrecoPromocional);

            // Verifica se produto já existe antes do UPSERT
            var jaExiste = await ProductExistsAsync(barCode);

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            // UPSERT: Se BAR_CODE (GTIN) já existe, ATUALIZA. Se não existe, INSERE novo.
            // Conforme padrão: BAR_CODE é chave primária, então mesmo GTIN = atualiza, não cria duplicado
            var query = $@"
                INSERT INTO {_tableName} (BAR_CODE, DESCRIPTION, PRICE_1, PRICE_2, UPDATED_AT)
                VALUES (@barCode, @description, @price1, @price2, CURRENT_TIMESTAMP)
                ON CONFLICT (BAR_CODE) 
                DO UPDATE SET
                    DESCRIPTION = EXCLUDED.DESCRIPTION,
                    PRICE_1 = EXCLUDED.PRICE_1,
                    PRICE_2 = EXCLUDED.PRICE_2,
                    UPDATED_AT = CURRENT_TIMESTAMP;";
            
            await using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("barCode", barCode);
            cmd.Parameters.AddWithValue("description", description);
            cmd.Parameters.AddWithValue("price1", price1);
            cmd.Parameters.AddWithValue("price2", price2);
            
            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            
            if (rowsAffected > 0)
            {
                if (jaExiste)
                {
                    _logger.LogInformation($"Produto ATUALIZADO no banco: BAR_CODE={barCode}, DESCRIPTION={description}, PRICE_1={price1}, PRICE_2={price2}");
                    Console.WriteLine($"Produto ATUALIZADO: {description} (BAR_CODE: {barCode})");
                }
                else
                {
                    _logger.LogInformation($"NOVO produto INSERIDO no banco: BAR_CODE={barCode}, DESCRIPTION={description}, PRICE_1={price1}, PRICE_2={price2}");
                    Console.WriteLine($"NOVO produto INSERIDO: {description} (BAR_CODE: {barCode})");
                }
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao fazer upsert do produto: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Processa múltiplos produtos (insere novos ou atualiza existentes)
    /// </summary>
    public async Task<(int novos, int atualizados, int erros)> ProcessProductsAsync(List<Produto> produtos)
    {
        int novos = 0;
        int atualizados = 0;
        int erros = 0;

        foreach (var produto in produtos)
        {
            try
            {
                var barCode = produto.Gtin ?? produto.Codigo ?? "";
                if (string.IsNullOrEmpty(barCode))
                {
                    continue;
                }

                var exists = await ProductExistsAsync(barCode);
                
                if (exists)
                {
                    // Verifica se precisa atualizar (preço mudou)
                    var updated = await UpdateProductAsync(produto);
                    if (updated)
                    {
                        atualizados++;
                    }
                }
                else
                {
                    // Novo produto
                    var inserted = await InsertProductAsync(produto);
                    if (inserted)
                    {
                        novos++;
                    }
                }
            }
            catch (Exception ex)
            {
                erros++;
                _logger.LogWarning(ex, $"Erro ao processar produto {produto.Nome}");
            }
        }

        return (novos, atualizados, erros);
    }

    /// <summary>
    /// Processa múltiplos produtos usando UPSERT (mais eficiente)
    /// Retorna: (inseridos, atualizados, erros)
    /// </summary>
    public async Task<(int inseridos, int atualizados, int erros)> UpsertProductsAsync(List<Produto> produtos)
    {
        int inseridos = 0;
        int atualizados = 0;
        int erros = 0;

        foreach (var produto in produtos)
        {
            try
            {
                var barCode = produto.Gtin ?? produto.Codigo ?? "";
                if (string.IsNullOrEmpty(barCode))
                {
                    continue;
                }

                // Verifica se produto já existe antes do UPSERT
                var jaExiste = await ProductExistsAsync(barCode);
                
                var success = await UpsertProductAsync(produto);
                if (success)
                {
                    if (jaExiste)
                    {
                        atualizados++;
                    }
                    else
                    {
                        inseridos++;
                    }
                }
            }
            catch (Exception ex)
            {
                erros++;
                _logger.LogWarning(ex, $"Erro ao processar produto {produto.Nome}: {ex.Message}");
            }
        }

        return (inseridos, atualizados, erros);
    }

    /// <summary>
    /// Testa a conexão com o banco de dados
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            
            await using var cmd = new NpgsqlCommand("SELECT 1", connection);
            await cmd.ExecuteScalarAsync();
            
            _logger.LogInformation("Conexão com banco de dados testada com sucesso!");
            Console.WriteLine("Conexao com banco de dados testada com sucesso!");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao testar conexao com banco de dados: {ex.Message}");
            Console.WriteLine($"ERRO ao testar conexao: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Converte string de preço para decimal
    /// </summary>
    private decimal ParsePrice(string? preco)
    {
        if (string.IsNullOrEmpty(preco))
            return 0;

        // Remove caracteres não numéricos exceto ponto e vírgula
        var precoLimpo = preco
            .Replace("R$", "")
            .Replace("$", "")
            .Replace(" ", "")
            .Trim();

        // Substitui vírgula por ponto
        precoLimpo = precoLimpo.Replace(",", ".");

        if (decimal.TryParse(precoLimpo, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var resultado))
        {
            return resultado;
        }

        return 0;
    }

    public void Dispose()
    {
        // Npgsql gerencia conexões automaticamente, não precisa fechar aqui
    }
}

