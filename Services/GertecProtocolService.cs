using System.Net.Sockets;
using System.Text;
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

    public bool IsConnected => _isConnected && _tcpClient?.Connected == true;

    public GertecProtocolService(ILogger<GertecProtocolService> logger, GertecConfig config)
    {
        _logger = logger;
        _config = config;
    }

    /// <summary>
    /// Conecta ao servidor do Gertec como cliente TCP
    /// </summary>
    public async Task<bool> ConnectAsync()
    {
        await _connectionLock.WaitAsync();
        try
        {
            if (IsConnected)
            {
                _logger.LogInformation("Já conectado ao servidor Gertec");
                return true;
            }

            // Valida IP e porta
            if (string.IsNullOrWhiteSpace(_config.IpAddress))
            {
                _logger.LogError("IP do servidor Gertec não configurado");
                return false;
            }

            if (!System.Net.IPAddress.TryParse(_config.IpAddress, out _))
            {
                _logger.LogError($"IP inválido: {_config.IpAddress}");
                return false;
            }

            // Fecha conexão anterior se existir
            await DisconnectAsync();

            _logger.LogInformation($"Conectando ao servidor Gertec em {_config.IpAddress}:{_config.Port}...");

            try
            {
                _tcpClient = new TcpClient();
                _tcpClient.ReceiveTimeout = 30000; // 30 segundos
                _tcpClient.SendTimeout = 30000; // 30 segundos

                // Habilita keep-alive TCP
                _tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                // Conecta ao servidor com timeout
                var connectTask = _tcpClient.ConnectAsync(_config.IpAddress, _config.Port);
                var timeoutTask = Task.Delay(_config.ConnectionTimeoutMilliseconds);

                var completed = await Task.WhenAny(connectTask, timeoutTask);
                
                if (completed == timeoutTask)
                {
                    _logger.LogError($"Timeout ao conectar ao servidor Gertec {_config.IpAddress}:{_config.Port}");
                    _tcpClient?.Close();
                    _tcpClient?.Dispose();
                    _tcpClient = null;
                    return false;
                }

                if (!_tcpClient.Connected)
                {
                    _logger.LogError($"Falha ao conectar ao servidor Gertec {_config.IpAddress}:{_config.Port}");
                    _tcpClient?.Close();
                    _tcpClient?.Dispose();
                    _tcpClient = null;
                    return false;
                }

                _stream = _tcpClient.GetStream();
                _stream.ReadTimeout = 30000;
                _stream.WriteTimeout = 30000;

                _isConnected = true;

                _logger.LogInformation($"Conectado com sucesso ao servidor Gertec em {_config.IpAddress}:{_config.Port}");
                return true;
            }
            catch (SocketException ex)
            {
                _logger.LogError(ex, $"Erro de socket ao conectar ao servidor Gertec: {ex.Message}");
                _tcpClient?.Close();
                _tcpClient?.Dispose();
                _tcpClient = null;
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao conectar ao servidor Gertec: {ex.Message}");
                _tcpClient?.Close();
                _tcpClient?.Dispose();
                _tcpClient = null;
                return false;
            }
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
            _logger.LogInformation("Desconectado do servidor Gertec");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao desconectar do servidor Gertec");
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// Reconecta ao servidor Gertec
    /// </summary>
    public async Task<bool> ReconnectAsync()
    {
        await DisconnectAsync();
        await Task.Delay(1000);
        return await ConnectAsync();
    }

    public async Task<bool> SendProductInfoAsync(string nome, string preco)
    {
        if (!IsConnected || _stream == null)
        {
            _logger.LogWarning("Não conectado ao servidor Gertec. Tentando reconectar...");
            if (!await ConnectAsync())
            {
                return false;
            }
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

            if (_stream == null)
            {
                _logger.LogError("Stream não está disponível");
                return false;
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
            if (!await ConnectAsync())
            {
                return false;
            }
        }

        try
        {
            string response = "#nfound";
            byte[] data = Encoding.ASCII.GetBytes(response);

            await _stream!.WriteAsync(data, 0, data.Length);
            await _stream.FlushAsync();

            _logger.LogInformation("Produto não encontrado enviado ao Gertec");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar 'produto não encontrado' ao Gertec");
            _isConnected = false;
            return false;
        }
    }

    public async Task<bool> SendMessageAsync(string linha1, string linha2, int tempoSegundos)
    {
        if (!IsConnected || _stream == null)
        {
            _logger.LogWarning("Não conectado ao servidor Gertec. Não é possível enviar mensagem.");
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
            _logger.LogWarning("Não conectado ao servidor Gertec. Não é possível enviar imagem.");
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

    /// <summary>
    /// Configura o terminal Gertec com o IP do servidor usando o comando #rupdconfig
    /// </summary>
    public async Task<bool> UpdateTerminalConfigAsync(string gatewayIp, string nomeTerminal = "Meu BPG2")
    {
        if (!IsConnected || _stream == null)
        {
            _logger.LogWarning("Não conectado ao servidor Gertec. Não é possível atualizar configuração.");
            return false;
        }

        try
        {
            string servidorNomes = "";
            string ns = "Não suportado";
            int extra = 61;

            char tamGateway = (char)(gatewayIp.Length + 48);
            char tamServidor = (char)(servidorNomes.Length + 48);
            char tamNome = (char)(nomeTerminal.Length + 48);

            string command = "#rupdconfig" +
                tamGateway + gatewayIp +
                tamServidor + servidorNomes +
                tamNome + nomeTerminal +
                (char)extra + ns +
                (char)extra + ns +
                (char)extra + ns;

            byte[] commandBytes = Encoding.ASCII.GetBytes(command);

            await _stream.WriteAsync(commandBytes, 0, commandBytes.Length);
            await _stream.FlushAsync();

            await Task.Delay(500);

            if (_stream.DataAvailable)
            {
                byte[] responseBuffer = new byte[255];
                int bytesRead = await _stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);
                string response = Encoding.ASCII.GetString(responseBuffer, 0, bytesRead);

                if (response.StartsWith("#rupdconfig_ok"))
                {
                    _logger.LogInformation($"Configuração do terminal atualizada com sucesso. Gateway: {gatewayIp}, Nome: {nomeTerminal}");
                    return true;
                }
                else
                {
                    _logger.LogWarning($"Resposta inesperada do terminal: {response}");
                    return false;
                }
            }

            _logger.LogInformation($"Comando #rupdconfig enviado ao terminal (Gateway: {gatewayIp})");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar configuração do terminal Gertec");
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
