using System.Net.Sockets;
using System.Text;
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

    public async Task<bool> ConnectAsync()
    {
        await _connectionLock.WaitAsync();
        try
        {
            if (IsConnected)
            {
                return true;
            }

            _logger.LogInformation($"Conectando ao Gertec em {_config.IpAddress}:{_config.Port}");

            _tcpClient = new TcpClient();
            _tcpClient.ReceiveTimeout = _config.ResponseTimeoutMilliseconds;
            _tcpClient.SendTimeout = _config.ResponseTimeoutMilliseconds;
            
            await _tcpClient.ConnectAsync(_config.IpAddress, _config.Port);
            _stream = _tcpClient.GetStream();
            _isConnected = true;

            _logger.LogInformation($"Conectado ao Gertec com sucesso - IP: {_config.IpAddress}:{_config.Port}");

            // Inicia thread para escutar mensagens
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
                if (_stream == null || !_stream.DataAvailable)
                {
                    await Task.Delay(100);
                    continue;
                }

                int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    
                    // Log detalhado apenas em desenvolvimento
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug($"Mensagem recebida do Gertec: {message}");
                    }

                    // Processa código de barras (#codigo)
                    if (message.StartsWith("#") && message.Length > 1 && !message.StartsWith("#macaddr") && 
                        !message.StartsWith("#gif") && !message.StartsWith("#mesg") && 
                        !message.StartsWith("#rupdconfig") && !message.StartsWith("#playaudio"))
                    {
                        string barcode = message.Substring(1).TrimEnd('\0');
                        if (!string.IsNullOrWhiteSpace(barcode))
                        {
                            _logger.LogInformation($"Código de barras recebido: {barcode}");
                            BarcodeReceived?.Invoke(this, barcode);
                        }
                    }
                }
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
            // Formato: # + nome (80 bytes) + | + preço (20 bytes)
            var nomeFormatado = nome.PadRight(80).Substring(0, Math.Min(80, nome.Length));
            var precoFormatado = preco.PadRight(20).Substring(0, Math.Min(20, preco.Length));
            
            // Remove o caractere # do preço se existir (não é permitido)
            precoFormatado = precoFormatado.Replace("#", "");

            string response = $"#{nomeFormatado}|{precoFormatado}";
            byte[] data = Encoding.ASCII.GetBytes(response);

            await _stream.WriteAsync(data, 0, data.Length);
            await _stream.FlushAsync();

            _logger.LogInformation($"Produto enviado ao Gertec: {nome} - {preco}");
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

    public async Task<bool> SendImageGifAsync(byte[] imageData, int index = 0, int loops = 1, int durationSeconds = 5)
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
            // Valida tamanho máximo (124KB para G2 S com áudio, 192KB sem áudio)
            // Usando limite conservador de 124KB
            const int maxSize = 124 * 1024; // 124KB
            if (imageData.Length > maxSize)
            {
                _logger.LogWarning($"Imagem muito grande ({imageData.Length} bytes). Tamanho máximo: {maxSize} bytes");
                return false;
            }

            // Comando #gif conforme documentação do Gertec
            // Estrutura: #gif + índice(2) + loops(2) + tempo(2) + tamanho(6) + checksum(4) + ETB(1) + dados
            
            // Índice da imagem (hexadecimal em ASCII)
            // 00: exibição imediata, 01-FE: loop, FF: reset
            byte[] indexBytes = Encoding.ASCII.GetBytes(index.ToString("X2"));
            
            // Número de loops (hexadecimal em ASCII)
            byte[] loopsBytes = Encoding.ASCII.GetBytes(Math.Min(loops, 255).ToString("X2"));
            
            // Tempo de exibição em segundos (hexadecimal em ASCII)
            byte[] durationBytes = Encoding.ASCII.GetBytes(Math.Min(durationSeconds, 255).ToString("X2"));
            
            // Tamanho da imagem (6 bytes em hexadecimal)
            string sizeHex = imageData.Length.ToString("X6");
            byte[] sizeBytes = Encoding.ASCII.GetBytes(sizeHex);
            
            // Checksum (4 bytes) - XOR de todos os bytes da imagem
            // Simplificado: usar 0000 como recomendado na documentação
            byte[] checksumBytes = Encoding.ASCII.GetBytes("0000");
            
            // Separador ETB (0x17)
            byte etb = 0x17;
            
            // Monta o comando
            var command = new List<byte>();
            command.AddRange(Encoding.ASCII.GetBytes("#gif"));
            command.AddRange(indexBytes);
            command.AddRange(loopsBytes);
            command.AddRange(durationBytes);
            command.AddRange(sizeBytes);
            command.AddRange(checksumBytes);
            command.Add(etb);
            command.AddRange(imageData);
            
            await _stream.WriteAsync(command.ToArray(), 0, command.Count);
            await _stream.FlushAsync();
            
            _logger.LogInformation($"Imagem GIF enviada ao Gertec: {imageData.Length} bytes, índice {index}, {loops} loops, {durationSeconds}s");
            
            // Aguarda resposta do Gertec
            await Task.Delay(_config.ResponseTimeoutMilliseconds);
            
            // Verifica resposta (opcional)
            if (_stream.DataAvailable)
            {
                byte[] responseBuffer = new byte[255];
                int bytesRead = await _stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);
                string response = Encoding.ASCII.GetString(responseBuffer, 0, bytesRead);
                
                if (response.StartsWith("#gif_ok"))
                {
                    _logger.LogInformation("Imagem enviada com sucesso ao Gertec");
                    return true;
                }
                else if (response.StartsWith("#img_error"))
                {
                    _logger.LogWarning("Erro ao enviar imagem ao Gertec");
                    return false;
                }
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar imagem ao Gertec");
            return false;
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

