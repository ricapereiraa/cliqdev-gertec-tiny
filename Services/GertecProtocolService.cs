using System.Net.Sockets;
using System.Text;
using System.IO;
using OlistGertecIntegration.Models;

namespace OlistGertecIntegration.Services;

public class GertecProtocolService : IDisposable
{
    private readonly ILogger<GertecProtocolService> _logger;
    private readonly GertecConfig _config;
    private readonly GertecProductCacheService _productCache;
    private TcpListener? _tcpListener;
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private bool _isConnected = false;
    private bool _isListening = false;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private CancellationTokenSource? _listenerCancellationToken;

    public event EventHandler<string>? BarcodeReceived;
    public bool IsConnected => _isConnected && _tcpClient?.Connected == true;

    public GertecProtocolService(
        ILogger<GertecProtocolService> logger, 
        GertecConfig config,
        GertecProductCacheService productCache)
    {
        _logger = logger;
        _config = config;
        _productCache = productCache;
    }

    /// <summary>
    /// Inicia o servidor TCP para escutar conexões do Gertec
    /// </summary>
    public async Task<bool> ConnectAsync()
    {
        return await StartListeningAsync();
    }

    /// <summary>
    /// Inicia o servidor TCP na porta configurada
    /// </summary>
    public async Task<bool> StartListeningAsync()
    {
        await _connectionLock.WaitAsync();
        try
        {
            if (_isListening)
            {
                _logger.LogInformation("Servidor já está escutando");
                return true;
            }

            int portToUse = _config.Port;
            
            // Escuta em todas as interfaces (0.0.0.0)
            System.Net.IPAddress listenAddress = System.Net.IPAddress.Any;

            _tcpListener = new TcpListener(listenAddress, portToUse);
            _tcpListener.Start();

            _isListening = true;
            _listenerCancellationToken = new CancellationTokenSource();

            _logger.LogInformation($"Servidor TCP iniciado - Escutando em {listenAddress}:{portToUse}");
            Console.WriteLine($"Servidor TCP iniciado - Escutando em {listenAddress}:{portToUse}");
            _logger.LogInformation($"Aguardando conexão do Gertec na porta {portToUse}...");
            Console.WriteLine($"Aguardando conexão do Gertec na porta {portToUse}...");

            // Inicia thread para aceitar conexões
            _ = Task.Run(() => AcceptConnectionsAsync(_listenerCancellationToken.Token));

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao iniciar servidor TCP na porta {_config.Port}");
            _isListening = false;
            return false;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// Aceita conexões do Gertec
    /// </summary>
    private async Task AcceptConnectionsAsync(CancellationToken cancellationToken)
    {
        while (_isListening && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (_tcpListener == null)
                {
                    await Task.Delay(1000, cancellationToken);
                    continue;
                }

                _logger.LogInformation("Servidor TCP aguardando conexão do Gertec...");
                
                // Aceita conexão do Gertec
                var client = await _tcpListener.AcceptTcpClientAsync();
                
                var clientEndPoint = client.Client.RemoteEndPoint;
                _logger.LogInformation($"CONEXAO ESTABELECIDA! Gertec se conectou ao servidor!");
                Console.WriteLine($"CONEXAO ESTABELECIDA! Gertec se conectou ao servidor!");
                _logger.LogInformation($"   IP do Gertec: {clientEndPoint}");
                Console.WriteLine($"   IP do Gertec: {clientEndPoint}");
                _logger.LogInformation($"   Servidor escutando na porta: {_config.Port}");
                Console.WriteLine($"   Servidor escutando na porta: {_config.Port}");
                
                // Se já há uma conexão ativa, fecha a anterior
                if (_tcpClient != null && _tcpClient.Connected)
                {
                    var oldEndPoint = _tcpClient.Client.RemoteEndPoint;
                    _logger.LogWarning($"Nova conexao recebida de {clientEndPoint}, fechando conexao anterior {oldEndPoint}...");
                    await Task.Delay(100);
                    _tcpClient?.Close();
                    _tcpClient?.Dispose();
                }

                _tcpClient = client;
                _tcpClient.ReceiveTimeout = System.Threading.Timeout.Infinite;
                _tcpClient.SendTimeout = 30000;
                _tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                
                _stream = _tcpClient.GetStream();
                _stream.ReadTimeout = System.Threading.Timeout.Infinite;
                _stream.WriteTimeout = 30000;
                
                _isConnected = true;
                
                _logger.LogInformation($"Conexao TCP estabelecida e ativa!");
                Console.WriteLine($"Conexao TCP estabelecida e ativa!");
                _logger.LogInformation($"   Aguardando codigos de barras do Gertec...");
                Console.WriteLine($"   Aguardando codigos de barras do Gertec...");
                
                // Inicia thread de leitura de mensagens
                _ = Task.Run(() => ListenForMessagesAsync());
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Erro ao aceitar conexão do Gertec");
                    await Task.Delay(1000, cancellationToken);
                }
            }
        }
    }

    /// <summary>
    /// Para o servidor TCP
    /// </summary>
    public async Task StopListeningAsync()
    {
        await _connectionLock.WaitAsync();
        try
        {
            _isListening = false;
            _listenerCancellationToken?.Cancel();
            
            _tcpListener?.Stop();
            _tcpListener = null;
            
            await DisconnectAsync();
            
            _logger.LogInformation("Servidor TCP parado");
            Console.WriteLine("Servidor TCP parado");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao parar servidor TCP");
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
            _stream?.Dispose();
            _tcpClient?.Close();
            _tcpClient?.Dispose();
            _tcpClient = null;
            _stream = null;
            _logger.LogInformation("Conexão com Gertec fechada");
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
        
        _logger.LogInformation("Thread de leitura de mensagens iniciada. Mantendo conexão aberta...");
        
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
                    await Task.Delay(100);
                    continue;
                }

                // Lê dados disponíveis
                int bytesRead = 0;
                try
                {
                    bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                }
                catch (IOException ioEx) when (ioEx.InnerException is SocketException socketEx)
                {
                    if (socketEx.SocketErrorCode == SocketError.ConnectionReset || 
                        socketEx.SocketErrorCode == SocketError.ConnectionAborted)
                    {
                        _logger.LogWarning($"Conexão com Gertec foi fechada pelo cliente. Código: {socketEx.SocketErrorCode}");
                        _isConnected = false;
                        break;
                    }
                    else
                    {
                        _logger.LogWarning($"Erro temporário ao ler do Gertec: {socketEx.SocketErrorCode}. Continuando...");
                        await Task.Delay(1000);
                        continue;
                    }
                }
                catch (ObjectDisposedException)
                {
                    _logger.LogInformation("Stream foi fechado. Encerrando leitura de mensagens.");
                    _isConnected = false;
                    break;
                }

                if (bytesRead == 0)
                {
                    _logger.LogInformation("Conexão fechada pelo Gertec (bytesRead == 0).");
                    _isConnected = false;
                    break;
                }
                
                if (bytesRead > 0)
                {
                    string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    
                    _logger.LogInformation($"Mensagem recebida do Gertec ({bytesRead} bytes): [{message.TrimEnd('\0', '\r', '\n')}]");
                    Console.WriteLine($"Mensagem recebida do Gertec: [{message.TrimEnd('\0', '\r', '\n')}]");

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
                            Console.WriteLine($"Código de barras recebido: {barcode}");
                            
                            // Processa código de barras de forma assíncrona
                            _ = Task.Run(async () => await ProcessBarcodeAsync(barcode));
                        }
                    }
                    else
                    {
                        _logger.LogDebug($"Mensagem recebida mas não processada: {message.TrimEnd('\0', '\r', '\n')}");
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                _logger.LogInformation("Stream foi fechado. Encerrando leitura de mensagens.");
                _isConnected = false;
                break;
            }
            catch (Exception ex)
            {
                if (_isConnected && _tcpClient?.Connected == true)
                {
                    _logger.LogError(ex, "Erro ao ler mensagem do Gertec. Tentando continuar...");
                    await Task.Delay(1000);
                }
                else
                {
                    _logger.LogInformation("Conexão perdida. Encerrando leitura de mensagens.");
                    _isConnected = false;
                    break;
                }
            }
        }
        
        _logger.LogInformation("Thread de leitura de mensagens encerrada. Aguardando nova conexão...");
    }

    /// <summary>
    /// Processa código de barras recebido: busca no arquivo e envia resposta
    /// </summary>
    private async Task ProcessBarcodeAsync(string barcode)
    {
        try
        {
            // Recarrega produtos se arquivo foi modificado
            await _productCache.RefreshIfNeededAsync();

            // Busca produto no cache (arquivo TXT)
            var produto = _productCache.GetProductByGtin(barcode);

            if (produto != null)
            {
                _logger.LogInformation($"Produto encontrado: {produto.Nome} - Preço: {produto.Preco}");
                Console.WriteLine($"Produto encontrado: {produto.Nome} - Preço: {produto.Preco}");

                // Envia imagem se disponível (antes de enviar nome e preço)
                if (!string.IsNullOrEmpty(produto.Imagem))
                {
                    try
                    {
                        _logger.LogInformation($"Enviando imagem do produto: {produto.Imagem}");
                        Console.WriteLine($"Enviando imagem do produto: {produto.Nome}");
                        var imagemEnviada = await SendImageFromFileAsync(produto.Imagem, indice: 0, numeroLoops: 1, tempoExibicao: 5);
                        if (imagemEnviada)
                        {
                            Console.WriteLine($"Imagem enviada com sucesso: {produto.Nome}");
                        }
                    }
                    catch (Exception imgEx)
                    {
                        _logger.LogWarning(imgEx, $"Erro ao enviar imagem do produto {produto.Nome}. Continuando com nome e preço...");
                    }
                }

                // Formata nome para 4 linhas x 20 colunas (80 bytes)
                var nomeFormatado = FormatProductName(produto.Nome);
                
                // Envia nome e preço para o Gertec
                await SendProductInfoAsync(nomeFormatado, produto.Preco);
                
                _logger.LogInformation($"ENVIADO para Gertec: {produto.Nome} - {produto.Preco}");
                Console.WriteLine($"ENVIADO para Gertec: {produto.Nome} - {produto.Preco}");
            }
            else
            {
                _logger.LogWarning($"Produto não encontrado para código: {barcode}");
                Console.WriteLine($"Produto não encontrado: {barcode}");
                await SendProductNotFoundAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao processar código de barras: {barcode}");
            await SendProductNotFoundAsync();
        }
    }

    private string FormatProductName(string nome)
    {
        // Formata para 4 linhas x 20 colunas (80 bytes total)
        var linhas = new List<string>();
        var nomeLimpo = nome.Replace("\n", " ").Replace("\r", "");

        for (int i = 0; i < 4 && i * 20 < nomeLimpo.Length; i++)
        {
            var linha = nomeLimpo.Substring(i * 20, Math.Min(20, nomeLimpo.Length - i * 20));
            linhas.Add(linha);
        }

        // Preenche até 4 linhas
        while (linhas.Count < 4)
        {
            linhas.Add(new string(' ', 20));
        }

        return string.Join("", linhas);
    }

    public async Task<bool> SendProductInfoAsync(string nome, string preco)
    {
        if (!IsConnected || _stream == null)
        {
            _logger.LogWarning("Não conectado ao Gertec. Não é possível enviar produto.");
            return false;
        }

        try
        {
            // Formato conforme manual: # + nome (exatamente 80 bytes = 4 linhas x 20 colunas) + | + preço (exatamente 20 bytes)
            string nomeFormatado = nome.Length > 80 
                ? nome.Substring(0, 80) 
                : nome.PadRight(80, ' ');
            
            string precoLimpo = preco.Replace("#", "");
            string precoFormatado = precoLimpo.Length > 20 
                ? precoLimpo.Substring(0, 20) 
                : precoLimpo.PadRight(20, ' ');

            string response = $"#{nomeFormatado}|{precoFormatado}";
            byte[] data = Encoding.ASCII.GetBytes(response);

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
            Console.WriteLine("Produto não encontrado enviado ao Gertec");
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
            _logger.LogWarning("Não conectado ao Gertec. Não é possível enviar mensagem.");
            return false;
        }

        try
        {
            // Comando #mesg conforme manual Gertec G2 S
            string linha1Formatada = linha1.Length > 20 ? linha1.Substring(0, 20) : linha1;
            string linha2Formatada = linha2.Length > 20 ? linha2.Substring(0, 20) : linha2;
            
            char tamLinha1 = (char)(linha1Formatada.Length + 48);
            char tamLinha2 = (char)(linha2Formatada.Length + 48);
            
            int tempoValor = Math.Max(0, Math.Min(tempoSegundos, 99));
            char tempo = (char)(tempoValor + 48);
            char reservado = (char)48;

            string command = "#mesg" + tamLinha1 + linha1Formatada + tamLinha2 + linha2Formatada + tempo + reservado;
            byte[] data = Encoding.ASCII.GetBytes(command);

            await _stream.WriteAsync(data, 0, data.Length);
            await _stream.FlushAsync();

            _logger.LogInformation($"Mensagem enviada ao Gertec: {linha1} / {linha2} (tempo: {tempoSegundos}s)");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar mensagem ao Gertec");
            _isConnected = false;
            return false;
        }
    }

    /// <summary>
    /// Envia imagem/GIF para o terminal Gertec usando o comando #gif
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
            const int maxSize = 124 * 1024; // 124KB
            if (imageData.Length > maxSize)
            {
                _logger.LogError($"Imagem muito grande: {imageData.Length} bytes. Máximo permitido: {maxSize} bytes");
                return false;
            }

            if (indice < 0 || indice > 0xFE) indice = 0;
            if (numeroLoops < 0 || numeroLoops > 0xFF) numeroLoops = 1;
            if (tempoExibicao < 0 || tempoExibicao > 0xFF) tempoExibicao = 5;

            string indiceHex = indice.ToString("X2");
            string loopsHex = numeroLoops.ToString("X2");
            string tempoHex = tempoExibicao.ToString("X2");
            string tamanhoHex = imageData.Length.ToString("X6");
            string checksumHex = "0000";

            string commandHeader = $"#gif{indiceHex}{loopsHex}{tempoHex}{tamanhoHex}{checksumHex}";
            byte[] headerBytes = Encoding.ASCII.GetBytes(commandHeader);
            byte etb = 0x17;

            byte[] fullCommand = new byte[headerBytes.Length + 1 + imageData.Length];
            Array.Copy(headerBytes, 0, fullCommand, 0, headerBytes.Length);
            fullCommand[headerBytes.Length] = etb;
            Array.Copy(imageData, 0, fullCommand, headerBytes.Length + 1, imageData.Length);

            await _stream.WriteAsync(fullCommand, 0, fullCommand.Length);
            await _stream.FlushAsync();

            await Task.Delay(500);

            if (_stream.DataAvailable)
            {
                byte[] responseBuffer = new byte[255];
                int bytesRead = await _stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);
                string response = Encoding.ASCII.GetString(responseBuffer, 0, bytesRead);

                if (response.StartsWith("#gif_ok"))
                {
                    _logger.LogInformation($"Imagem enviada com sucesso ao Gertec (índice: {indice})");
                    Console.WriteLine($"Imagem enviada com sucesso ao Gertec (índice: {indice})");
                    return true;
                }
                else if (response.StartsWith("#img_error"))
                {
                    _logger.LogWarning("Erro ao enviar imagem: terminal retornou #img_error");
                    return false;
                }
            }

            _logger.LogInformation($"Imagem enviada ao Gertec (índice: {indice}, tamanho: {imageData.Length} bytes)");
            Console.WriteLine($"Imagem enviada ao Gertec (índice: {indice}, tamanho: {imageData.Length} bytes)");
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
            
            if (imagePath.StartsWith("http://") || imagePath.StartsWith("https://"))
            {
                using var httpClient = new HttpClient();
                imageData = await httpClient.GetByteArrayAsync(imagePath);
            }
            else
            {
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

    public void Dispose()
    {
        _isConnected = false;
        _isListening = false;
        _stream?.Dispose();
        _tcpClient?.Dispose();
        _tcpListener?.Stop();
        _connectionLock.Dispose();
    }
}
