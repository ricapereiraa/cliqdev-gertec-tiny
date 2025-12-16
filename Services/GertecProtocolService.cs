using System.Net.Sockets;
using System.Text;
using System.Linq;
using System.Threading;
using System.IO;
using OlistGertecIntegration.Models;

namespace OlistGertecIntegration.Services;

public class GertecProtocolService : IDisposable
{
    private readonly ILogger<GertecProtocolService> _logger;
    private readonly GertecConfig _config;
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private bool _isConnected = false;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    public event EventHandler<string>? BarcodeReceived;
    public bool IsConnected => _isConnected && _tcpClient?.Connected == true;

    public GertecProtocolService(ILogger<GertecProtocolService> logger, GertecConfig config)
    {
        _logger = logger;
        _config = config;
    }

    /// <summary>
    /// Atualiza o IP do Gertec e reconecta
    /// </summary>
    public async Task<bool> UpdateIpAddressAsync(string newIpAddress)
    {
        if (string.IsNullOrWhiteSpace(newIpAddress))
        {
            return false;
        }

        // Valida formato de IP
        if (!System.Net.IPAddress.TryParse(newIpAddress, out _))
        {
            _logger.LogWarning($"Formato de IP inválido: {newIpAddress}");
            return false;
        }

        // Desconecta se estiver conectado
        if (IsConnected)
        {
            await DisconnectAsync();
            await Task.Delay(1000);
        }

        // Atualiza o IP na configuração
        _config.IpAddress = newIpAddress;
        _logger.LogInformation($"IP do Gertec atualizado para: {newIpAddress}");

        // Reconecta com novo IP
        return await ConnectAsync();
    }

    private async Task<int?> FindOpenPortAsync(string ipAddress, int startPort = 1, int endPort = 65535, int timeoutMs = 150)
    {
        _logger.LogInformation($"Procurando porta aberta no Gertec {ipAddress} (testando todas as portas 1-65535)...");
        
        var commonPorts = new[] { 6500, 8080, 80, 23, 9100, 9101, 5000, 3000, 8888, 8081, 9090, 9999, 7070, 8099, 7680 };
        var portsToTest = commonPorts.Where(p => p >= startPort && p <= endPort).ToList();
        
        var semaphore = new SemaphoreSlim(20, 20);
        var cancellationTokenSource = new CancellationTokenSource();
        int? foundPort = null;
        object lockObject = new object();
        
        async Task<int?> TestPortAsync(int port, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return null;
                
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                if (cancellationToken.IsCancellationRequested)
                    return null;
                    
                using var testClient = new TcpClient();
                var connectTask = testClient.ConnectAsync(ipAddress, port);
                var timeoutTask = Task.Delay(timeoutMs, cancellationToken);
                
                var completed = await Task.WhenAny(connectTask, timeoutTask);
                if (cancellationToken.IsCancellationRequested)
                    return null;
                    
                if (completed == connectTask && testClient.Connected)
                {
                    lock (lockObject)
                    {
                        if (foundPort == null)
                        {
                            foundPort = port;
                            cancellationTokenSource.Cancel();
                            _logger.LogInformation($"Porta {port} encontrada e acessível!");
                        }
                    }
                    return port;
                }
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch { }
            finally
            {
                semaphore.Release();
            }
            return null;
        }
        
        var allTestTasks = new List<Task<int?>>();
        
        foreach (var port in portsToTest)
        {
            allTestTasks.Add(TestPortAsync(port, cancellationTokenSource.Token));
        }
        
        var commonResults = await Task.WhenAll(allTestTasks);
        var foundCommonPort = commonResults.FirstOrDefault(r => r.HasValue);
        if (foundCommonPort.HasValue)
        {
            return foundCommonPort.Value;
        }
        
        if (foundPort.HasValue)
        {
            return foundPort.Value;
        }
        
        _logger.LogInformation("Portas comuns não responderam. Testando todas as portas restantes...");
        
        var allPorts = Enumerable.Range(startPort, endPort - startPort + 1)
            .Where(p => !portsToTest.Contains(p))
            .ToList();
        
        int batchSize = 100;
        int tested = 0;
        
        for (int i = 0; i < allPorts.Count; i += batchSize)
        {
            if (cancellationTokenSource.Token.IsCancellationRequested)
                break;
                
            var portBatch = allPorts.Skip(i).Take(batchSize);
            var batchTasks = portBatch.Select(port => TestPortAsync(port, cancellationTokenSource.Token)).ToList();
            
            try
            {
                await Task.WhenAny(Task.WhenAll(batchTasks), Task.Delay(Timeout.Infinite, cancellationTokenSource.Token));
            }
            catch (OperationCanceledException) { }
            
            if (foundPort.HasValue)
            {
                return foundPort.Value;
            }
            
            tested += batchTasks.Count;
            if (tested % 1000 == 0)
            {
                _logger.LogInformation($"Testadas {tested} portas...");
            }
        }
        
        return foundPort;
    }

    public async Task<bool> ConnectAsync()
    {
        await _connectionLock.WaitAsync();
        try
        {
            if (IsConnected)
            {
                return true;
            }

            int portToUse = _config.Port;
            
            _logger.LogInformation($"Tentando conectar ao Gertec em {_config.IpAddress}:{portToUse} (timeout: {_config.ConnectionTimeoutMilliseconds}ms)");

            _tcpClient = new TcpClient();
            _tcpClient.ReceiveTimeout = _config.ResponseTimeoutMilliseconds;
            _tcpClient.SendTimeout = _config.ResponseTimeoutMilliseconds;
            
            var connectionTimeout = _config.ConnectionTimeoutMilliseconds > 0 
                ? _config.ConnectionTimeoutMilliseconds 
                : 10000;
            
            var connectTask = _tcpClient.ConnectAsync(_config.IpAddress, portToUse);
            var timeoutTask = Task.Delay(TimeSpan.FromMilliseconds(Math.Min(connectionTimeout, 3000)));
            
            var completedTask = await Task.WhenAny(connectTask, timeoutTask);
            
            if (completedTask == timeoutTask || !_tcpClient.Connected)
            {
                _tcpClient?.Close();
                _tcpClient?.Dispose();
                _tcpClient = null;
                
                _logger.LogWarning($"Porta {portToUse} não respondeu. Procurando porta alternativa...");
                
                var foundPort = await FindOpenPortAsync(_config.IpAddress, 1, 65535, 150);
                
                if (foundPort.HasValue)
                {
                    portToUse = foundPort.Value;
                    _logger.LogInformation($"Porta {portToUse} encontrada! Tentando conectar...");
                    
                    _tcpClient = new TcpClient();
                    _tcpClient.ReceiveTimeout = _config.ResponseTimeoutMilliseconds;
                    _tcpClient.SendTimeout = _config.ResponseTimeoutMilliseconds;
                    
                    connectTask = _tcpClient.ConnectAsync(_config.IpAddress, portToUse);
                    timeoutTask = Task.Delay(TimeSpan.FromMilliseconds(connectionTimeout));
                    completedTask = await Task.WhenAny(connectTask, timeoutTask);
                    
                    if (completedTask == timeoutTask)
                    {
                        _tcpClient?.Close();
                        _tcpClient?.Dispose();
                        _tcpClient = null;
                        throw new TimeoutException($"Timeout ao conectar ao Gertec na porta {portToUse} após {connectionTimeout}ms");
                    }
                }
                else
                {
                    var errorMsg = $"Não foi possível encontrar nenhuma porta aberta no Gertec {_config.IpAddress}. " +
                                  $"Verifique: 1) Se o Gertec está ligado, 2) Se está na mesma rede, 3) Se há firewall bloqueando.";
                    _logger.LogError(errorMsg);
                    throw new TimeoutException(errorMsg);
                }
            }
            
            try
            {
                await connectTask;
            }
            catch (SocketException socketEx)
            {
                _tcpClient?.Close();
                _tcpClient?.Dispose();
                _tcpClient = null;
                var errorMsg = $"Erro de socket ao conectar ao Gertec {_config.IpAddress}:{portToUse}. " +
                              $"Código: {socketEx.SocketErrorCode}, Mensagem: {socketEx.Message}";
                _logger.LogError(socketEx, errorMsg);
                throw new InvalidOperationException(errorMsg, socketEx);
            }
            
            _stream = _tcpClient.GetStream();
            
            // Configura o stream para não bloquear e ter timeouts adequados
            _stream.ReadTimeout = _config.ResponseTimeoutMilliseconds;
            _stream.WriteTimeout = _config.ResponseTimeoutMilliseconds;
            
            _isConnected = true;

            // Atualiza a porta na configuração se foi encontrada uma diferente
            if (portToUse != _config.Port)
            {
                _config.Port = portToUse;
                _logger.LogInformation($"Porta atualizada na configuração: {portToUse}");
            }
            
            // Salva a porta no arquivo .env para próxima inicialização
            await SavePortToEnvAsync(portToUse);

            _logger.LogInformation($"Conectado ao Gertec com sucesso - IP: {_config.IpAddress}:{portToUse}");

            // Inicia thread de leitura de mensagens
            _ = Task.Run(ListenForMessagesAsync);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao conectar ao Gertec");
            _isConnected = false;
            return false;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async Task DisconnectAsync()
    {
        await _connectionLock.WaitAsync();
        try
        {
            _isConnected = false;
            _stream?.Close();
            _tcpClient?.Close();
            _logger.LogInformation("Desconectado do Gertec");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao desconectar do Gertec");
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private async Task ListenForMessagesAsync()
    {
        var buffer = new byte[255];
        
        while (_isConnected && _tcpClient?.Connected == true)
        {
            try
            {
                if (_stream == null)
                {
                    await Task.Delay(100);
                    continue;
                }

                // Verifica se há dados disponíveis antes de ler
                if (!_stream.DataAvailable)
                {
                    await Task.Delay(50); // Reduz delay para resposta mais rápida
                    continue;
                }

                // Lê dados disponíveis sem bloquear
                int bytesRead = 0;
                try
                {
                    bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                }
                catch (IOException ioEx) when (ioEx.InnerException is SocketException)
                {
                    // Timeout ou erro de socket - reconecta
                    _logger.LogWarning("Timeout ou erro ao ler do Gertec. Tentando reconectar...");
                    _isConnected = false;
                    await Task.Delay(1000);
                    continue;
                }

                if (bytesRead > 0)
                {
                    string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    
                    // Log detalhado apenas em desenvolvimento
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug($"Mensagem recebida do Gertec ({bytesRead} bytes): {message}");
                        _logger.LogDebug($"Bytes hex: {BitConverter.ToString(buffer, 0, bytesRead)}");
                    }

                    // Processa código de barras (#codigo)
                    // Conforme manual: terminal envia #123456 quando lê código de barras
                    if (message.StartsWith("#") && message.Length > 1 && !message.StartsWith("#macaddr") && 
                        !message.StartsWith("#gif") && !message.StartsWith("#mesg") && 
                        !message.StartsWith("#rupdconfig") && !message.StartsWith("#playaudio") &&
                        !message.StartsWith("#fullmacaddr") && !message.StartsWith("#audioconfig") &&
                        !message.StartsWith("#raudioconfig") && !message.StartsWith("#nfound") &&
                        !message.StartsWith("#gif_ok") && !message.StartsWith("#rupdconfig_ok") &&
                        !message.StartsWith("#playaudiowithmessage_ok") && !message.StartsWith("#playaudiowithmessage_error"))
                    {
                        // Remove o # inicial e limpa caracteres nulos e espaços
                        string barcode = message.Substring(1).TrimEnd('\0', '\r', '\n', ' ').Trim();
                        
                        if (!string.IsNullOrWhiteSpace(barcode))
                        {
                            _logger.LogInformation($"Código de barras recebido do Gertec: {barcode}");
                            
                            // Dispara evento de forma assíncrona para não bloquear a leitura
                            // Isso garante que o terminal continue recebendo dados enquanto processamos
                            _ = Task.Run(() => BarcodeReceived?.Invoke(this, barcode));
                        }
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // Stream foi fechado - sai do loop
                _isConnected = false;
                break;
            }
            catch (Exception ex)
            {
                if (_isConnected)
                {
                    _logger.LogError(ex, "Erro ao ler mensagem do Gertec");
                    await Task.Delay(1000);
                }
            }
        }
        
        _logger.LogInformation("Thread de leitura do Gertec finalizada");
    }

    public async Task<bool> SendProductInfoAsync(string nome, string preco)
    {
        if (!IsConnected || _stream == null)
        {
            _logger.LogWarning("Não conectado ao Gertec. Tentando reconectar...");
            await ConnectAsync();
            if (!IsConnected)
            {
                return false;
            }
        }

        try
        {
            // Formato conforme manual: # + nome (exatamente 80 bytes = 4 linhas x 20 colunas) + | + preço (exatamente 20 bytes)
            // O nome já vem formatado do IntegrationService com 80 bytes
            
            // Garante que o nome tem exatamente 80 bytes (preenche com espaços se menor, corta se maior)
            string nomeFormatado = nome.Length > 80 
                ? nome.Substring(0, 80) 
                : nome.PadRight(80, ' ');
            
            // Garante que o preço tem exatamente 20 bytes (preenche com espaços se menor, corta se maior)
            // Remove o caractere # do preço se existir (não é permitido conforme manual)
            string precoLimpo = preco.Replace("#", "");
            string precoFormatado = precoLimpo.Length > 20 
                ? precoLimpo.Substring(0, 20) 
                : precoLimpo.PadRight(20, ' ');

            // Monta a resposta conforme protocolo: # + nome (80 bytes) + | + preço (20 bytes)
            string response = $"#{nomeFormatado}|{precoFormatado}";
            byte[] data = Encoding.ASCII.GetBytes(response);

            if (_stream == null)
            {
                _logger.LogError("Stream não está disponível");
                return false;
            }

            // Log detalhado para debug
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"Enviando ao Gertec: {response.Length} bytes - Nome: [{nomeFormatado}] Preço: [{precoFormatado}]");
                _logger.LogDebug($"Bytes enviados: {BitConverter.ToString(data)}");
            }

            await _stream.WriteAsync(data, 0, data.Length);
            await _stream.FlushAsync();

            _logger.LogInformation($"Produto enviado ao Gertec: {nome.Trim()} - {preco.Trim()}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar produto ao Gertec");
            _isConnected = false;
            return false;
        }
    }

    public async Task<bool> SendProductNotFoundAsync()
    {
        if (!IsConnected || _stream == null)
        {
            return false;
        }

        try
        {
            string response = "#nfound";
            byte[] data = Encoding.ASCII.GetBytes(response);

            await _stream.WriteAsync(data, 0, data.Length);
            await _stream.FlushAsync();

            _logger.LogInformation("Produto não encontrado enviado ao Gertec");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar 'produto não encontrado' ao Gertec");
            return false;
        }
    }

    public async Task<bool> SendMessageAsync(string linha1, string linha2, int tempoSegundos)
    {
        if (!IsConnected || _stream == null)
        {
            return false;
        }

        try
        {
            // Comando #mesg
            // 1 byte: tamanho linha1 + 48
            // string: linha1
            // 1 byte: tamanho linha2 + 48
            // string: linha2
            // 1 byte: tempo + 48
            // 1 byte: reservado (48)

            char tamLinha1 = (char)(Math.Min(linha1.Length, 20) + 48);
            char tamLinha2 = (char)(Math.Min(linha2.Length, 20) + 48);
            char tempo = (char)(Math.Min(tempoSegundos, 99) + 48);
            char reservado = (char)48;

            string linha1Formatada = linha1.Substring(0, Math.Min(linha1.Length, 20));
            string linha2Formatada = linha2.Substring(0, Math.Min(linha2.Length, 20));

            string command = "#mesg" + tamLinha1 + linha1Formatada + tamLinha2 + linha2Formatada + tempo + reservado;
            byte[] data = Encoding.ASCII.GetBytes(command);

            await _stream.WriteAsync(data, 0, data.Length);
            await _stream.FlushAsync();

            _logger.LogInformation($"Mensagem enviada ao Gertec: {linha1} / {linha2}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar mensagem ao Gertec");
            return false;
        }
    }

    /// <summary>
    /// Envia imagem/GIF para o terminal Gertec usando o comando #gif
    /// Conforme manual: #gif + índice (2 bytes hex) + loops (2 bytes hex) + tempo (2 bytes hex) + tamanho (6 bytes hex) + checksum (4 bytes hex) + ETB (0x17) + dados da imagem
    /// </summary>
    public async Task<bool> SendImageAsync(byte[] imageData, int indice = 0, int numeroLoops = 1, int tempoExibicao = 5)
    {
        if (!IsConnected || _stream == null)
        {
            _logger.LogWarning("Não conectado ao Gertec. Não é possível enviar imagem.");
            return false;
        }

        try
        {
            // Valida tamanho máximo (192KB sem áudio, 124KB com áudio - usando limite menor para segurança)
            const int maxSize = 124 * 1024; // 124KB
            if (imageData.Length > maxSize)
            {
                _logger.LogError($"Imagem muito grande: {imageData.Length} bytes. Máximo permitido: {maxSize} bytes");
                return false;
            }

            // Valida limites conforme manual
            if (indice < 0 || indice > 0xFE) indice = 0;
            if (numeroLoops < 0 || numeroLoops > 0xFF) numeroLoops = 1;
            if (tempoExibicao < 0 || tempoExibicao > 0xFF) tempoExibicao = 5;

            // Converte valores para hexadecimal em ASCII (2 bytes cada)
            string indiceHex = indice.ToString("X2");
            string loopsHex = numeroLoops.ToString("X2");
            string tempoHex = tempoExibicao.ToString("X2");
            string tamanhoHex = imageData.Length.ToString("X6"); // 6 bytes hex
            string checksumHex = "0000"; // Checksum não validado pelo equipamento, mas deve ser enviado

            // Monta o comando conforme protocolo
            // #gif + índice (2 bytes) + loops (2 bytes) + tempo (2 bytes) + tamanho (6 bytes) + checksum (4 bytes) + ETB (0x17)
            string commandHeader = $"#gif{indiceHex}{loopsHex}{tempoHex}{tamanhoHex}{checksumHex}";
            byte[] headerBytes = Encoding.ASCII.GetBytes(commandHeader);
            byte etb = 0x17; // Separador ETB

            // Monta array completo: header + ETB + dados da imagem
            byte[] fullCommand = new byte[headerBytes.Length + 1 + imageData.Length];
            Array.Copy(headerBytes, 0, fullCommand, 0, headerBytes.Length);
            fullCommand[headerBytes.Length] = etb;
            Array.Copy(imageData, 0, fullCommand, headerBytes.Length + 1, imageData.Length);

            // Envia comando
            await _stream.WriteAsync(fullCommand, 0, fullCommand.Length);
            await _stream.FlushAsync();

            // Aguarda resposta do terminal (#gif_ok + índice ou #img_error)
            await Task.Delay(500); // Tempo para garantir resposta

            if (_stream.DataAvailable)
            {
                byte[] responseBuffer = new byte[255];
                int bytesRead = await _stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);
                string response = Encoding.ASCII.GetString(responseBuffer, 0, bytesRead);

                if (response.StartsWith("#gif_ok"))
                {
                    _logger.LogInformation($"Imagem enviada com sucesso ao Gertec (índice: {indice})");
                    return true;
                }
                else if (response.StartsWith("#img_error"))
                {
                    _logger.LogWarning("Erro ao enviar imagem: terminal retornou #img_error");
                    return false;
                }
            }

            _logger.LogInformation($"Imagem enviada ao Gertec (índice: {indice}, tamanho: {imageData.Length} bytes)");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar imagem ao Gertec");
            return false;
        }
    }

    /// <summary>
    /// Envia imagem a partir de um arquivo local ou URL
    /// </summary>
    public async Task<bool> SendImageFromFileAsync(string imagePath, int indice = 0, int numeroLoops = 1, int tempoExibicao = 5)
    {
        try
        {
            byte[] imageData;
            
            // Se for URL, baixa a imagem
            if (imagePath.StartsWith("http://") || imagePath.StartsWith("https://"))
            {
                using var httpClient = new HttpClient();
                imageData = await httpClient.GetByteArrayAsync(imagePath);
            }
            else
            {
                // Arquivo local
                if (!File.Exists(imagePath))
                {
                    _logger.LogError($"Arquivo de imagem não encontrado: {imagePath}");
                    return false;
                }
                imageData = await File.ReadAllBytesAsync(imagePath);
            }

            return await SendImageAsync(imageData, indice, numeroLoops, tempoExibicao);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao carregar/enviar imagem de {imagePath}");
            return false;
        }
    }

    public async Task<string?> GetMacAddressAsync()
    {
        if (!IsConnected || _stream == null)
        {
            return null;
        }

        try
        {
            byte[] command = Encoding.ASCII.GetBytes("#macaddr?");
            await _stream.WriteAsync(command, 0, command.Length);
            await _stream.FlushAsync();

            await Task.Delay(_config.ResponseTimeoutMilliseconds);

            byte[] buffer = new byte[255];
            if (_stream.DataAvailable)
            {
                int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                
                if (response.StartsWith("#macaddr"))
                {
                    // Parse da resposta: #macaddr + interface + tamanho + MAC
                    // Exemplo: #macaddr0A00:1D:5B:00:65:A8
                    if (response.Length > 9)
                    {
                        int interfaceValue = response[8] - 48;
                        int tamanho = response[9] - 48;
                        string macAddress = response.Substring(10, Math.Min(tamanho, response.Length - 10));
                        return macAddress;
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter MAC Address do Gertec");
            return null;
        }
    }

    /// <summary>
    /// Salva a porta no arquivo .env para usar na próxima inicialização
    /// </summary>
    private async Task SavePortToEnvAsync(int port)
    {
        try
        {
            // Encontra o arquivo .env
            var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
            if (!File.Exists(envPath))
            {
                envPath = Path.Combine(AppContext.BaseDirectory, ".env");
            }

            if (!File.Exists(envPath))
            {
                _logger.LogDebug("Arquivo .env não encontrado, não é possível salvar a porta");
                return;
            }

            var lines = new List<string>();
            
            // Lê as linhas existentes
            if (File.Exists(envPath))
            {
                lines = (await File.ReadAllLinesAsync(envPath)).ToList();
            }

            // Procura se a chave já existe
            bool keyFound = false;
            string key = "GERTEC__PORT";
            
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i].Trim();
                // Ignora comentários e linhas vazias
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                    continue;

                // Verifica se a linha contém a chave
                if (line.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith(key + " =", StringComparison.OrdinalIgnoreCase))
                {
                    lines[i] = $"{key}={port}";
                    keyFound = true;
                    break;
                }
            }

            // Se não encontrou, adiciona no final
            if (!keyFound)
            {
                lines.Add($"{key}={port}");
            }

            // Salva o arquivo
            await File.WriteAllLinesAsync(envPath, lines);

            // Atualiza variável de ambiente em tempo de execução
            Environment.SetEnvironmentVariable(key, port.ToString());

            _logger.LogInformation($"Porta {port} salva no arquivo .env para próxima inicialização");
        }
        catch (Exception ex)
        {
            // Não falha a conexão se não conseguir salvar a porta
            _logger.LogWarning(ex, $"Não foi possível salvar a porta no arquivo .env: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _isConnected = false;
        _stream?.Dispose();
        _tcpClient?.Dispose();
        _connectionLock.Dispose();
    }
}

