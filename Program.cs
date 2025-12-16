using DotNetEnv;
using OlistGertecIntegration.Models;
using OlistGertecIntegration.Services;

// Carrega variáveis de ambiente do arquivo .env (se existir)
// IMPORTANTE: Deve ser carregado ANTES de criar o builder
// Prioridade: .env > appsettings.json
var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (!File.Exists(envPath))
{
    envPath = Path.Combine(AppContext.BaseDirectory, ".env");
}

if (File.Exists(envPath))
{
    Env.Load(envPath);
    Console.WriteLine($"Arquivo .env carregado: {envPath}");
}
else
{
    Console.WriteLine("AVISO: Arquivo .env não encontrado. Usando appsettings.json");
}

var builder = WebApplication.CreateBuilder(args);

// Adiciona variáveis de ambiente à configuração COM PRIORIDADE
// No .NET, variáveis de ambiente sobrescrevem automaticamente appsettings.json
// quando usamos a nomenclatura com dois underscores (__) para representar dois pontos (:)
// Exemplo: OlistApi__Token no .env = OlistApi:Token no appsettings.json
// IMPORTANTE: AddEnvironmentVariables() deve ser chamado DEPOIS de AddJsonFile() para ter prioridade
builder.Configuration.AddEnvironmentVariables();

// Log das configurações (apenas em desenvolvimento)
if (builder.Environment.IsDevelopment())
{
    var gertecIp = builder.Configuration["Gertec:IpAddress"];
    var token = builder.Configuration["OlistApi:Token"];
    
    Console.WriteLine("=== Configuracoes Carregadas ===");
    Console.WriteLine($"Gertec IP: {gertecIp ?? "NAO CONFIGURADO"}");
    Console.WriteLine($"Olist Token: {(string.IsNullOrEmpty(token) ? "NAO CONFIGURADO" : "CONFIGURADO")}");
    Console.WriteLine("================================");
}

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configuração do Gertec
builder.Services.Configure<GertecConfig>(
    builder.Configuration.GetSection("Gertec"));

// Serviços
builder.Services.AddHttpClient<OlistApiService>();
builder.Services.AddSingleton<GertecProtocolService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<GertecProtocolService>>();
    
    // Prioridade: Variáveis de ambiente > Configuration
    // Formato no .env: GERTEC__IP_ADDRESS, GERTEC__PORT, etc.
    var config = new GertecConfig
    {
        IpAddress = Environment.GetEnvironmentVariable("GERTEC__IP_ADDRESS") 
            ?? builder.Configuration["Gertec:IpAddress"] 
            ?? "192.168.1.3", // IP do servidor Gertec (onde o terminal está rodando)
        Port = int.TryParse(Environment.GetEnvironmentVariable("GERTEC__PORT"), out var port) 
            ? port 
            : builder.Configuration.GetValue<int>("Gertec:Port", 6500),
        ReconnectIntervalSeconds = int.TryParse(Environment.GetEnvironmentVariable("GERTEC__RECONNECT_INTERVAL_SECONDS"), out var reconnect) 
            ? reconnect 
            : builder.Configuration.GetValue<int>("Gertec:ReconnectIntervalSeconds", 5),
        ResponseTimeoutMilliseconds = int.TryParse(Environment.GetEnvironmentVariable("GERTEC__RESPONSE_TIMEOUT_MILLISECONDS"), out var responseTimeout) 
            ? responseTimeout 
            : builder.Configuration.GetValue<int>("Gertec:ResponseTimeoutMilliseconds", 500),
        ConnectionTimeoutMilliseconds = int.TryParse(Environment.GetEnvironmentVariable("GERTEC__CONNECTION_TIMEOUT_MILLISECONDS"), out var connectionTimeout) 
            ? connectionTimeout 
            : builder.Configuration.GetValue<int>("Gertec:ConnectionTimeoutMilliseconds", 15000)
    };
    
    Console.WriteLine($"GertecConfig carregado - IP: {config.IpAddress}, Port: {config.Port}");
    return new GertecProtocolService(logger, config);
});

builder.Services.AddHostedService<IntegrationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Em produção, HTTPS pode ser gerenciado por proxy reverso
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Habilita arquivos estáticos (para o painel web)
app.UseStaticFiles();

// Rota padrão para o painel
app.MapGet("/", () => Results.Redirect("/painel.html"));

app.UseAuthorization();
app.MapControllers();

app.Run();
