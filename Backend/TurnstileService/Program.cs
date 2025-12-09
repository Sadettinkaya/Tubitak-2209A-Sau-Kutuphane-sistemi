using Microsoft.Extensions.Options;
using TurnstileService.Models;
using TurnstileService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<TurnstileOptions>(builder.Configuration.GetSection("Turnstile"));
builder.Services.AddSingleton<ITurnstileEntryLog, InMemoryTurnstileEntryLog>();
builder.Services.AddSingleton<TurnstileAuthProvider>();

builder.Services.AddHttpClient<ReservationAccessClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<TurnstileOptions>>();
    client.BaseAddress = new Uri(options.Value.ReservationServiceBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient("IdentityAuth", (serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<TurnstileOptions>>();
    var baseUrl = options.Value.IdentityBaseUrl?.TrimEnd('/') ?? string.Empty;

    if (string.IsNullOrWhiteSpace(baseUrl))
    {
        throw new InvalidOperationException("Turnstile IdentityBaseUrl must be configured.");
    }

    client.BaseAddress = new Uri(baseUrl + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.MapGet("/", () => "Turnstile Service is running...");

app.MapGet("/health", () => Results.Ok(new
{
    service = "Turnstile",
    status = "Healthy",
    timestamp = DateTime.UtcNow
}));

app.Run();
