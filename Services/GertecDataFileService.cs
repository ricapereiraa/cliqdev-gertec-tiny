using System.Text;
using OlistGertecIntegration.Models;

namespace OlistGertecIntegration.Services;

/// <summary>
/// Serviço para gerar e atualizar arquivo TXT com dados dos produtos para o servidor TCP oficial do Gertec
/// </summary>
public class GertecDataFileService
{
    private readonly ILogger<GertecDataFileService> _logger;
    private readonly IConfiguration _configuration;
    private readonly OlistApiService _olistService;
    private string _dataFilePath;

    public GertecDataFileService(
        ILogger<GertecDataFileService> logger,
        IConfiguration configuration,
        OlistApiService olistService)
    {
        _logger = logger;
        _configuration = configuration;
        _olistService = olistService;

        // Obtém o caminho do arquivo de configuração ou usa padrão
        var customPath = Environment.GetEnvironmentVariable("GERTEC__DATA_FILE_PATH");
        if (string.IsNullOrEmpty(customPath))
        {
            // Caminho padrão: na raiz do projeto ou diretório atual
            _dataFilePath = Path.Combine(Directory.GetCurrentDirectory(), "gertec_produtos.txt");
        }
        else
        {
            _dataFilePath = customPath;
        }

        _logger.LogInformation($"Arquivo de dados Gertec será salvo em: {_dataFilePath}");
        Console.WriteLine($"Arquivo de dados Gertec será salvo em: {_dataFilePath}");

        // Cria o arquivo vazio por padrão na inicialização (sincronamente)
        try
        {
            if (!File.Exists(_dataFilePath))
            {
                var directory = Path.GetDirectoryName(_dataFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllText(_dataFilePath, "", Encoding.UTF8);
                _logger.LogInformation($"Arquivo vazio criado por padrao: {_dataFilePath}");
                Console.WriteLine($"Arquivo vazio criado por padrao: {_dataFilePath}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao criar arquivo por padrao: {ex.Message}");
            Console.WriteLine($"ERRO ao criar arquivo por padrao: {ex.Message}");
        }
    }

    /// <summary>
    /// Gera ou atualiza o arquivo TXT com alguns produtos do Tiny ERP para teste
    /// </summary>
    public async Task<bool> GenerateTestDataFileAsync(int limit = 10)
    {
        try
        {
            _logger.LogInformation($"Gerando arquivo de dados para TESTE (limitando a {limit} produtos)...");
            Console.WriteLine($"Gerando arquivo de dados para TESTE (limitando a {limit} produtos)...");
            Console.WriteLine($"Caminho do arquivo: {_dataFilePath}");

            // PASSO 1: Garante que arquivo existe ANTES de buscar produtos
            if (!File.Exists(_dataFilePath))
            {
                _logger.LogWarning("Arquivo nao existe. Criando agora ANTES de buscar produtos...");
                Console.WriteLine("Arquivo nao existe. Criando agora ANTES de buscar produtos...");
                var directory = Path.GetDirectoryName(_dataFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                await File.WriteAllTextAsync(_dataFilePath, "", Encoding.UTF8);
                _logger.LogInformation("Arquivo vazio criado ANTES de buscar produtos");
                Console.WriteLine("Arquivo vazio criado ANTES de buscar produtos");
            }

            // PASSO 2: Carrega produtos existentes do arquivo (para evitar duplicações)
            var produtosExistentes = new Dictionary<string, string>(); // GTIN -> Linha completa
            if (File.Exists(_dataFilePath))
            {
                try
                {
                    var linhasExistentes = await File.ReadAllLinesAsync(_dataFilePath, Encoding.UTF8);
                    foreach (var linha in linhasExistentes)
                    {
                        if (string.IsNullOrWhiteSpace(linha))
                            continue;
                        
                        var partes = linha.Split('|');
                        if (partes.Length >= 1)
                        {
                            var gtin = partes[0].Trim();
                            if (!string.IsNullOrEmpty(gtin))
                            {
                                produtosExistentes[gtin] = linha;
                            }
                        }
                    }
                    _logger.LogInformation($"Carregados {produtosExistentes.Count} produtos existentes do arquivo");
                    Console.WriteLine($"Carregados {produtosExistentes.Count} produtos existentes do arquivo");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erro ao carregar produtos existentes, continuando sem eles");
                }
            }

            // PASSO 3: Busca alguns produtos do Tiny ERP para teste
            _logger.LogInformation($"Buscando {limit} produtos do Tiny ERP para teste...");
            Console.WriteLine($"Buscando {limit} produtos do Tiny ERP para teste...");
            var produtos = await _olistService.GetTestProductsAsync(limit);

            if (produtos == null || produtos.Count == 0)
            {
                _logger.LogError("Erro: nenhum produto retornado da API para teste");
                Console.WriteLine("ERRO: nenhum produto retornado da API para teste");
                return false;
            }

            _logger.LogInformation($"Total de produtos retornados da API para teste: {produtos.Count}");
            Console.WriteLine($"Total de produtos retornados da API para teste: {produtos.Count}");

            // PASSO 4: Processa produtos e atualiza/insere no dicionário (evita duplicações)
            int produtosProcessados = 0;
            int produtosAtualizados = 0;
            int produtosNovos = 0;
            int produtosSemGtin = 0;
            int produtosComErro = 0;

            _logger.LogInformation($"Processando {produtos.Count} produtos para teste...");
            Console.WriteLine($"Processando {produtos.Count} produtos para teste...");

            foreach (var produto in produtos)
            {
                try
                {
                    // USA APENAS GTIN (nunca código/SKU)
                    var gtin = produto.Gtin;

                    // Log detalhado para debug
                    _logger.LogInformation($"Processando produto: Nome='{produto.Nome}', GTIN='{gtin ?? "NULL"}', Codigo='{produto.Codigo ?? "NULL"}'");
                    Console.WriteLine($"  - {produto.Nome} (GTIN: {gtin ?? "NULL"}, Codigo: {produto.Codigo ?? "NULL"})");

                    // Se não tem GTIN, tenta usar código como fallback (com aviso)
                    if (string.IsNullOrEmpty(gtin))
                    {
                        if (!string.IsNullOrEmpty(produto.Codigo))
                        {
                            // Usa código como fallback (com aviso)
                            gtin = produto.Codigo;
                            _logger.LogWarning($"Produto '{produto.Nome}' (ID: {produto.Id}) sem GTIN, usando código '{produto.Codigo}' como fallback");
                            Console.WriteLine($"    AVISO: Usando código '{produto.Codigo}' como fallback");
                        }
                        else
                        {
                            // Não tem nem GTIN nem código - ignora
                            produtosSemGtin++;
                            _logger.LogWarning($"Produto '{produto.Nome}' (ID: {produto.Id}) ignorado: sem GTIN e sem código");
                            Console.WriteLine($"    IGNORADO: sem GTIN e sem código");
                            continue;
                        }
                    }

                    // Formata nome (remove quebras de linha e caracteres especiais)
                    var nome = produto.Nome
                        .Replace("\n", " ")
                        .Replace("\r", " ")
                        .Replace("|", " ")
                        .Trim();

                    if (string.IsNullOrEmpty(nome))
                    {
                        nome = "Produto sem nome";
                    }

                    // Usa preço promocional se disponível e maior que zero, senão usa preço normal
                    var preco = !string.IsNullOrEmpty(produto.PrecoPromocional) && 
                               decimal.TryParse(produto.PrecoPromocional, out var precoPromo) && 
                               precoPromo > 0
                        ? produto.PrecoPromocional 
                        : produto.Preco;

                    if (string.IsNullOrEmpty(preco))
                    {
                        preco = "0.00";
                    }

                    // Formata preço (remove caracteres especiais, mantém apenas números e ponto/vírgula)
                    var precoFormatado = preco
                        .Replace("R$", "")
                        .Replace("$", "")
                        .Replace(" ", "")
                        .Trim();

                    // Caminho da imagem (URL ou caminho local)
                    var imagem = produto.ImagemPrincipal ?? produto.Imagem ?? "";

                    // Formato: GTIN|NOME|PRECO|IMAGEM (GTIN = código de barras, nunca SKU)
                    var novaLinha = $"{gtin}|{nome}|{precoFormatado}|{imagem}";

                    // Verifica se produto já existe
                    if (produtosExistentes.ContainsKey(gtin))
                    {
                        // Produto existe - verifica se preço mudou
                        var linhaExistente = produtosExistentes[gtin];
                        var partesExistente = linhaExistente.Split('|');
                        var precoExistente = partesExistente.Length >= 3 ? partesExistente[2].Trim() : "";
                        
                        if (precoExistente != precoFormatado)
                        {
                            // Preço mudou - atualiza
                            produtosExistentes[gtin] = novaLinha;
                            produtosAtualizados++;
                            _logger.LogInformation($"Preço atualizado para produto {nome} (GTIN: {gtin}) - Preço anterior: {precoExistente}, Novo: {precoFormatado}");
                            Console.WriteLine($"    ATUALIZADO: Preço mudou de {precoExistente} para {precoFormatado}");
                        }
                        else
                        {
                            // Preço não mudou, mas produto existe - mantém
                            _logger.LogDebug($"Produto {nome} (GTIN: {gtin}) já existe com mesmo preço, mantendo");
                            Console.WriteLine($"    MANTIDO: Já existe com mesmo preço");
                        }
                    }
                    else
                    {
                        // Novo produto - adiciona
                        produtosExistentes[gtin] = novaLinha;
                        produtosNovos++;
                        _logger.LogInformation($"Novo produto adicionado: {nome} (GTIN: {gtin}, Preço: {precoFormatado})");
                        Console.WriteLine($"    NOVO: Adicionado com preço {precoFormatado}");
                    }
                    
                    produtosProcessados++;
                }
                catch (Exception ex)
                {
                    produtosComErro++;
                    _logger.LogWarning(ex, $"Erro ao processar produto {produto.Nome} para arquivo de dados");
                    Console.WriteLine($"    ERRO: {ex.Message}");
                }
            }

            _logger.LogInformation($"Produtos processados: {produtosProcessados} com GTIN, {produtosNovos} novos, {produtosAtualizados} atualizados, {produtosSemGtin} sem GTIN, {produtosComErro} com erro");
            Console.WriteLine($"Produtos processados: {produtosProcessados} com GTIN, {produtosNovos} novos, {produtosAtualizados} atualizados, {produtosSemGtin} sem GTIN, {produtosComErro} com erro");

            // PASSO 5: Escreve todos os produtos no arquivo (sem duplicações)
            var linhasFinais = produtosExistentes.Values.ToList();
            
            _logger.LogInformation($"Total de produtos no dicionário antes de escrever: {produtosExistentes.Count}");
            Console.WriteLine($"Total de produtos no dicionário antes de escrever: {produtosExistentes.Count}");
            
            if (linhasFinais.Count > 0)
            {
                _logger.LogInformation($"Atualizando arquivo com {linhasFinais.Count} produtos (sem duplicações)...");
                Console.WriteLine($"Atualizando arquivo com {linhasFinais.Count} produtos (sem duplicações)...");
                
                try
                {
                    // Escreve todos os produtos no arquivo
                    await File.WriteAllLinesAsync(_dataFilePath, linhasFinais, Encoding.UTF8);
                    
                    // Verifica se arquivo foi escrito corretamente
                    if (File.Exists(_dataFilePath))
                    {
                        var linhasEscritas = await File.ReadAllLinesAsync(_dataFilePath, Encoding.UTF8);
                        _logger.LogInformation($"Arquivo escrito com sucesso! {linhasEscritas.Length} linhas no arquivo");
                        Console.WriteLine($"✓ Arquivo escrito com sucesso! {linhasEscritas.Length} linhas no arquivo");
                        
                        // Log das primeiras 3 linhas para debug
                        if (linhasEscritas.Length > 0)
                        {
                            var primeirasLinhas = linhasEscritas.Take(3).ToList();
                            _logger.LogInformation($"Primeiras linhas do arquivo: {string.Join(" | ", primeirasLinhas)}");
                            Console.WriteLine($"Primeiras linhas:");
                            foreach (var linha in primeirasLinhas)
                            {
                                Console.WriteLine($"  {linha}");
                            }
                        }
                    }
                    else
                    {
                        _logger.LogError("ERRO CRÍTICO: Arquivo não existe após escrita!");
                        Console.WriteLine("✗ ERRO CRÍTICO: Arquivo não existe após escrita!");
                    }
                }
                catch (Exception writeEx)
                {
                    _logger.LogError(writeEx, $"ERRO ao escrever arquivo: {writeEx.Message}");
                    Console.WriteLine($"✗ ERRO ao escrever arquivo: {writeEx.Message}");
                    throw;
                }
            }
            else
            {
                _logger.LogWarning($"Nenhum produto com GTIN encontrado para escrever. Total de produtos da API: {produtos.Count}, Produtos sem GTIN: {produtosSemGtin}");
                Console.WriteLine($"⚠ ATENÇÃO: Nenhum produto com GTIN encontrado para escrever!");
                Console.WriteLine($"Total de produtos da API: {produtos.Count}");
                Console.WriteLine($"Produtos sem GTIN: {produtosSemGtin}");
            }

            // Verifica se arquivo foi atualizado
            if (File.Exists(_dataFilePath))
            {
                var fileInfo = new FileInfo(_dataFilePath);
                _logger.LogInformation($"Arquivo de dados Gertec atualizado com sucesso: {linhasFinais.Count} produtos totais ({produtosNovos} novos, {produtosAtualizados} atualizados), {produtosSemGtin} sem GTIN, {produtosComErro} erros. Caminho: {_dataFilePath}, Tamanho: {fileInfo.Length} bytes");
                Console.WriteLine($"✓ Arquivo de dados Gertec atualizado com sucesso: {linhasFinais.Count} produtos totais ({produtosNovos} novos, {produtosAtualizados} atualizados)");
                Console.WriteLine($"Arquivo salvo em: {_dataFilePath}");
                Console.WriteLine($"Tamanho do arquivo: {fileInfo.Length} bytes");
                return true;
            }
            else
            {
                _logger.LogError($"ERRO: Arquivo nao existe apos atualizacao: {_dataFilePath}");
                Console.WriteLine($"✗ ERRO: Arquivo nao existe apos atualizacao: {_dataFilePath}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao gerar arquivo de dados Gertec para teste: {ex.Message}");
            Console.WriteLine($"✗ ERRO ao gerar arquivo de dados Gertec para teste: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// Gera ou atualiza o arquivo TXT com todos os produtos do Tiny ERP
    /// Formato: GTIN|NOME|PRECO|IMAGEM
    /// IMPORTANTE: Usa apenas GTIN (código de barras), nunca SKU/código interno
    /// Carrega produtos existentes primeiro para evitar duplicações e atualizar preços
    /// </summary>
    public async Task<bool> GenerateDataFileAsync()
    {
        try
        {
            _logger.LogInformation("Gerando arquivo de dados para servidor TCP Gertec...");
            Console.WriteLine("Gerando arquivo de dados para servidor TCP Gertec...");
            Console.WriteLine($"Caminho do arquivo: {_dataFilePath}");

            // PASSO 1: Garante que arquivo existe ANTES de buscar produtos
            if (!File.Exists(_dataFilePath))
            {
                _logger.LogWarning("Arquivo nao existe. Criando agora ANTES de buscar produtos...");
                Console.WriteLine("Arquivo nao existe. Criando agora ANTES de buscar produtos...");
                var directory = Path.GetDirectoryName(_dataFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                await File.WriteAllTextAsync(_dataFilePath, "", Encoding.UTF8);
                _logger.LogInformation("Arquivo vazio criado ANTES de buscar produtos");
                Console.WriteLine("Arquivo vazio criado ANTES de buscar produtos");
            }

            // PASSO 2: Carrega produtos existentes do arquivo (para evitar duplicações)
            var produtosExistentes = new Dictionary<string, string>(); // GTIN -> Linha completa
            if (File.Exists(_dataFilePath))
            {
                try
                {
                    var linhasExistentes = await File.ReadAllLinesAsync(_dataFilePath, Encoding.UTF8);
                    foreach (var linha in linhasExistentes)
                    {
                        if (string.IsNullOrWhiteSpace(linha))
                            continue;
                        
                        var partes = linha.Split('|');
                        if (partes.Length >= 1)
                        {
                            var gtin = partes[0].Trim();
                            if (!string.IsNullOrEmpty(gtin))
                            {
                                produtosExistentes[gtin] = linha;
                            }
                        }
                    }
                    _logger.LogInformation($"Carregados {produtosExistentes.Count} produtos existentes do arquivo");
                    Console.WriteLine($"Carregados {produtosExistentes.Count} produtos existentes do arquivo");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erro ao carregar produtos existentes, continuando sem eles");
                }
            }

            // PASSO 3: Busca todos os produtos do Tiny ERP
            _logger.LogInformation("Buscando produtos do Tiny ERP para registrar no arquivo...");
            Console.WriteLine("Buscando produtos do Tiny ERP para registrar no arquivo...");
            var produtos = await _olistService.GetAllProductsAsync(null);

            if (produtos == null)
            {
                _logger.LogError("Erro: produtos retornou null da API");
                Console.WriteLine("ERRO: produtos retornou null da API");
                Console.WriteLine("Arquivo criado mas vazio (sem produtos)");
                return false;
            }

            _logger.LogInformation($"Total de produtos retornados da API: {produtos.Count}");
            Console.WriteLine($"Total de produtos retornados da API: {produtos.Count}");

            // PASSO 4: Processa produtos e atualiza/insere no dicionário (evita duplicações)
            int produtosProcessados = 0;
            int produtosAtualizados = 0;
            int produtosNovos = 0;
            int produtosSemGtin = 0;
            int produtosComErro = 0;

            _logger.LogInformation($"Processando {produtos.Count} produtos...");
            Console.WriteLine($"Processando {produtos.Count} produtos...");
            
            // Log de progresso a cada 100 produtos
            int contadorProgresso = 0;

            foreach (var produto in produtos)
            {
                try
                {
                    contadorProgresso++;
                    if (contadorProgresso % 100 == 0)
                    {
                        _logger.LogInformation($"Processados {contadorProgresso}/{produtos.Count} produtos...");
                        Console.WriteLine($"Processados {contadorProgresso}/{produtos.Count} produtos...");
                    }

                    // USA APENAS GTIN (nunca código/SKU)
                    var gtin = produto.Gtin;

                    // Log detalhado para debug (apenas para primeiros produtos ou quando há problema)
                    if (contadorProgresso <= 10 || string.IsNullOrEmpty(gtin))
                    {
                        _logger.LogDebug($"Processando produto: Nome='{produto.Nome}', GTIN='{gtin ?? "NULL"}', Codigo='{produto.Codigo ?? "NULL"}'");
                    }

                    // Se não tem GTIN, tenta usar código como fallback (com aviso)
                    if (string.IsNullOrEmpty(gtin))
                    {
                        if (!string.IsNullOrEmpty(produto.Codigo))
                        {
                            // Usa código como fallback (com aviso)
                            gtin = produto.Codigo;
                            _logger.LogWarning($"Produto '{produto.Nome}' (ID: {produto.Id}) sem GTIN, usando código '{produto.Codigo}' como fallback");
                            Console.WriteLine($"AVISO: Produto '{produto.Nome}' sem GTIN, usando código '{produto.Codigo}' como fallback");
                        }
                        else
                        {
                            // Não tem nem GTIN nem código - ignora
                            produtosSemGtin++;
                            _logger.LogWarning($"Produto '{produto.Nome}' (ID: {produto.Id}) ignorado: sem GTIN e sem código");
                            Console.WriteLine($"AVISO: Produto '{produto.Nome}' ignorado - sem GTIN e sem código");
                            continue;
                        }
                    }

                    // Formata nome (remove quebras de linha e caracteres especiais)
                    var nome = produto.Nome
                        .Replace("\n", " ")
                        .Replace("\r", " ")
                        .Replace("|", " ")
                        .Trim();

                    if (string.IsNullOrEmpty(nome))
                    {
                        nome = "Produto sem nome";
                    }

                    // Usa preço promocional se disponível e maior que zero, senão usa preço normal
                    var preco = !string.IsNullOrEmpty(produto.PrecoPromocional) && 
                               decimal.TryParse(produto.PrecoPromocional, out var precoPromo) && 
                               precoPromo > 0
                        ? produto.PrecoPromocional 
                        : produto.Preco;

                    if (string.IsNullOrEmpty(preco))
                    {
                        preco = "0.00";
                    }

                    // Formata preço (remove caracteres especiais, mantém apenas números e ponto/vírgula)
                    var precoFormatado = preco
                        .Replace("R$", "")
                        .Replace("$", "")
                        .Replace(" ", "")
                        .Trim();

                    // Caminho da imagem (URL ou caminho local)
                    var imagem = produto.ImagemPrincipal ?? produto.Imagem ?? "";

                    // Formato: GTIN|NOME|PRECO|IMAGEM (GTIN = código de barras, nunca SKU)
                    var novaLinha = $"{gtin}|{nome}|{precoFormatado}|{imagem}";

                    // Verifica se produto já existe
                    if (produtosExistentes.ContainsKey(gtin))
                    {
                        // Produto existe - verifica se preço mudou
                        var linhaExistente = produtosExistentes[gtin];
                        var partesExistente = linhaExistente.Split('|');
                        var precoExistente = partesExistente.Length >= 3 ? partesExistente[2].Trim() : "";
                        
                        if (precoExistente != precoFormatado)
                        {
                            // Preço mudou - atualiza
                            produtosExistentes[gtin] = novaLinha;
                            produtosAtualizados++;
                            if (contadorProgresso <= 10 || produtosAtualizados <= 20)
                            {
                                _logger.LogInformation($"Preço atualizado para produto {nome} (GTIN: {gtin}) - Preço anterior: {precoExistente}, Novo: {precoFormatado}");
                                Console.WriteLine($"Preço atualizado: {nome} (GTIN: {gtin})");
                            }
                        }
                        else
                        {
                            // Preço não mudou, mas produto existe - mantém
                            _logger.LogDebug($"Produto {nome} (GTIN: {gtin}) já existe com mesmo preço, mantendo");
                        }
                    }
                    else
                    {
                        // Novo produto - adiciona
                        produtosExistentes[gtin] = novaLinha;
                        produtosNovos++;
                        if (contadorProgresso <= 10 || produtosNovos <= 20)
                        {
                            _logger.LogInformation($"Novo produto adicionado: {nome} (GTIN: {gtin}, Preço: {precoFormatado})");
                            Console.WriteLine($"Novo produto: {nome} (GTIN: {gtin})");
                        }
                    }
                    
                    produtosProcessados++;
                }
                catch (Exception ex)
                {
                    produtosComErro++;
                    _logger.LogWarning(ex, $"Erro ao processar produto {produto.Nome} para arquivo de dados");
                }
            }

            _logger.LogInformation($"Produtos processados: {produtosProcessados} com GTIN, {produtosNovos} novos, {produtosAtualizados} atualizados, {produtosSemGtin} sem GTIN, {produtosComErro} com erro");
            Console.WriteLine($"Produtos processados: {produtosProcessados} com GTIN, {produtosNovos} novos, {produtosAtualizados} atualizados, {produtosSemGtin} sem GTIN, {produtosComErro} com erro");

            // PASSO 5: Escreve todos os produtos no arquivo (sem duplicações)
            var linhasFinais = produtosExistentes.Values.ToList();
            
            _logger.LogInformation($"Total de produtos no dicionário antes de escrever: {produtosExistentes.Count}");
            Console.WriteLine($"Total de produtos no dicionário antes de escrever: {produtosExistentes.Count}");
            
            if (linhasFinais.Count > 0)
            {
                _logger.LogInformation($"Atualizando arquivo com {linhasFinais.Count} produtos (sem duplicações)...");
                Console.WriteLine($"Atualizando arquivo com {linhasFinais.Count} produtos (sem duplicações)...");
                
                try
                {
                    // Escreve todos os produtos no arquivo
                    await File.WriteAllLinesAsync(_dataFilePath, linhasFinais, Encoding.UTF8);
                    
                    // Verifica se arquivo foi escrito corretamente
                    if (File.Exists(_dataFilePath))
                    {
                        var linhasEscritas = await File.ReadAllLinesAsync(_dataFilePath, Encoding.UTF8);
                        _logger.LogInformation($"✓ Arquivo escrito com sucesso! {linhasEscritas.Length} linhas no arquivo");
                        Console.WriteLine($"✓ Arquivo escrito com sucesso! {linhasEscritas.Length} linhas no arquivo");
                        
                        // Log das primeiras 3 linhas para debug
                        if (linhasEscritas.Length > 0)
                        {
                            var primeirasLinhas = linhasEscritas.Take(3).ToList();
                            _logger.LogInformation($"Primeiras linhas do arquivo: {string.Join(" | ", primeirasLinhas)}");
                            Console.WriteLine($"Primeiras linhas:");
                            foreach (var linha in primeirasLinhas)
                            {
                                Console.WriteLine($"  {linha}");
                            }
                        }
                    }
                    else
                    {
                        _logger.LogError("✗ ERRO CRÍTICO: Arquivo não existe após escrita!");
                        Console.WriteLine("✗ ERRO CRÍTICO: Arquivo não existe após escrita!");
                    }
                }
                catch (Exception writeEx)
                {
                    _logger.LogError(writeEx, $"ERRO ao escrever arquivo: {writeEx.Message}");
                    Console.WriteLine($"ERRO ao escrever arquivo: {writeEx.Message}");
                    Console.WriteLine($"Stack trace: {writeEx.StackTrace}");
                    throw; // Re-lança para ser capturado no catch externo
                }
            }
            else
            {
                _logger.LogWarning($"Nenhum produto com GTIN encontrado para escrever. Total de produtos da API: {produtos.Count}, Produtos sem GTIN: {produtosSemGtin}");
                Console.WriteLine($"ATENÇÃO: Nenhum produto com GTIN encontrado para escrever!");
                Console.WriteLine($"Total de produtos da API: {produtos.Count}");
                Console.WriteLine($"Produtos sem GTIN: {produtosSemGtin}");
                Console.WriteLine($"Produtos processados: {produtosProcessados}");
            }

            // Verifica se arquivo foi atualizado
            if (File.Exists(_dataFilePath))
            {
                var fileInfo = new FileInfo(_dataFilePath);
                _logger.LogInformation($"✓ Arquivo de dados Gertec atualizado com sucesso: {linhasFinais.Count} produtos totais ({produtosNovos} novos, {produtosAtualizados} atualizados), {produtosSemGtin} sem GTIN, {produtosComErro} erros. Caminho: {_dataFilePath}, Tamanho: {fileInfo.Length} bytes");
                Console.WriteLine($"✓ Arquivo de dados Gertec atualizado com sucesso: {linhasFinais.Count} produtos totais ({produtosNovos} novos, {produtosAtualizados} atualizados)");
                Console.WriteLine($"Arquivo salvo em: {_dataFilePath}");
                Console.WriteLine($"Tamanho do arquivo: {fileInfo.Length} bytes");
                return true;
            }
            else
            {
                _logger.LogError($"✗ ERRO: Arquivo nao existe apos atualizacao: {_dataFilePath}");
                Console.WriteLine($"✗ ERRO: Arquivo nao existe apos atualizacao: {_dataFilePath}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao gerar arquivo de dados Gertec: {ex.Message}");
            Console.WriteLine($"ERRO ao gerar arquivo de dados Gertec: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// Cria apenas o arquivo vazio (sem produtos)
    /// </summary>
    public async Task<bool> CreateEmptyFileAsync()
    {
        try
        {
            // Cria diretório se não existir
            var directory = Path.GetDirectoryName(_dataFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Cria arquivo vazio
            await File.WriteAllTextAsync(_dataFilePath, "", Encoding.UTF8);
            
            if (File.Exists(_dataFilePath))
            {
                _logger.LogInformation($"Arquivo vazio criado com sucesso: {_dataFilePath}");
                Console.WriteLine($"Arquivo vazio criado: {_dataFilePath}");
                return true;
            }
            else
            {
                _logger.LogError($"ERRO: Arquivo nao foi criado: {_dataFilePath}");
                Console.WriteLine($"ERRO: Arquivo nao foi criado: {_dataFilePath}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao criar arquivo vazio: {ex.Message}");
            Console.WriteLine($"ERRO ao criar arquivo vazio: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Atualiza apenas um produto específico no arquivo
    /// </summary>
    public async Task<bool> UpdateProductInFileAsync(Produto produto)
    {
        try
        {
            if (!File.Exists(_dataFilePath))
            {
                // Se arquivo não existe, gera completo
                return await GenerateDataFileAsync();
            }

            var linhas = (await File.ReadAllLinesAsync(_dataFilePath)).ToList();
            
            // USA APENAS GTIN (nunca código/SKU)
            var gtin = produto.Gtin;

            if (string.IsNullOrEmpty(gtin))
            {
                _logger.LogDebug($"Produto {produto.Nome} ignorado: sem GTIN (código de barras)");
                return false;
            }

            // Formata dados do produto
            var nome = produto.Nome
                .Replace("\n", " ")
                .Replace("\r", " ")
                .Replace("|", " ")
                .Trim();

            var preco = !string.IsNullOrEmpty(produto.PrecoPromocional) && 
                       decimal.TryParse(produto.PrecoPromocional, out var precoPromo) && 
                       precoPromo > 0
                ? produto.PrecoPromocional 
                : produto.Preco;

            var precoFormatado = preco
                .Replace("R$", "")
                .Replace("$", "")
                .Replace(" ", "")
                .Trim();

            var imagem = produto.ImagemPrincipal ?? produto.Imagem ?? "";
            var novaLinha = $"{gtin}|{nome}|{precoFormatado}|{imagem}";

            // Procura linha existente e atualiza, ou adiciona nova
            bool encontrado = false;
            for (int i = 0; i < linhas.Count; i++)
            {
                if (linhas[i].StartsWith($"{gtin}|"))
                {
                    linhas[i] = novaLinha;
                    encontrado = true;
                    break;
                }
            }

            if (!encontrado)
            {
                linhas.Add(novaLinha);
            }

            await File.WriteAllLinesAsync(_dataFilePath, linhas, Encoding.UTF8);

            _logger.LogInformation($"Produto atualizado no arquivo de dados: {produto.Nome}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao atualizar produto no arquivo de dados: {produto.Nome}");
            return false;
        }
    }

    /// <summary>
    /// Retorna o caminho do arquivo de dados
    /// </summary>
    public string GetDataFilePath()
    {
        return _dataFilePath;
    }

    /// <summary>
    /// Verifica se o arquivo existe
    /// </summary>
    public bool FileExists()
    {
        return File.Exists(_dataFilePath);
    }
}

