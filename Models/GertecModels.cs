namespace OlistGertecIntegration.Models;

public class GertecConfig
{
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; } = 6500;
    public int ReconnectIntervalSeconds { get; set; } = 5;
    public int ResponseTimeoutMilliseconds { get; set; } = 500;
    public int ConnectionTimeoutMilliseconds { get; set; } = 10000; // 10 segundos padrão
}

public class GertecMessage
{
    public string Command { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public class ProductDisplayInfo
{
    public string Nome { get; set; } = string.Empty;
    public string Preco { get; set; } = string.Empty;
    
    public string FormatForDisplay()
    {
        // Formato: 4 linhas x 20 colunas para nome (80 bytes) + | + 1 linha x 20 colunas para preço (20 bytes)
        var nomeFormatado = Nome.PadRight(80).Substring(0, Math.Min(80, Nome.Length));
        var precoFormatado = Preco.PadRight(20).Substring(0, Math.Min(20, Preco.Length));
        
        return $"#{nomeFormatado}|{precoFormatado}";
    }
}

