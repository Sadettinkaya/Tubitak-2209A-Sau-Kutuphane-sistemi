using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot(builder.Configuration);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy =>
        {
            policy.SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrWhiteSpace(origin))
                {
                    return false;
                }
                try
                {
                    var uri = new Uri(origin);
                    return string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    return false;
                }
            })
            .AllowAnyHeader()
            .AllowAnyMethod();
        });
});


var app = builder.Build();

app.UseCors("AllowAngular");

app.MapGet("/", () => "API Gateway is running...").WithMetadata(new AllowAnonymousAttribute());

app.MapGet("/health", () => Results.Ok(new
{
    service = "ApiGateway",
    status = "Healthy",
    timestamp = DateTime.UtcNow
})).WithMetadata(new AllowAnonymousAttribute());

app.MapGet("/routes", (IConfiguration configuration) =>
{
    var routes = configuration.GetSection("Routes")
        .GetChildren()
        .Select(route => new
        {
            Upstream = route["UpstreamPathTemplate"],
            Downstream = route["DownstreamPathTemplate"],
            Methods = route.GetSection("UpstreamHttpMethod")
                .GetChildren()
                .Select(method => method.Value)
                .ToArray(),
            DownstreamPorts = route.GetSection("DownstreamHostAndPorts")
                .GetChildren()
                .Select(port => port["Port"])
                .ToArray()
        });

    return Results.Ok(routes);
}).WithMetadata(new AllowAnonymousAttribute());

await app.UseOcelot();

app.Run();
