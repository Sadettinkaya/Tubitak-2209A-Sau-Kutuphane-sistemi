using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using TurnstileService.Models;

namespace TurnstileService.Services
{
    public class TurnstileAuthProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TurnstileOptions _options;
        private readonly ILogger<TurnstileAuthProvider> _logger;
        private readonly SemaphoreSlim _tokenLock = new(1, 1);

        private string? _accessToken;
        private DateTime _expiresAtUtc;

        public TurnstileAuthProvider(IHttpClientFactory httpClientFactory, IOptions<TurnstileOptions> options, ILogger<TurnstileAuthProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
        {
            await _tokenLock.WaitAsync(cancellationToken);
            try
            {
                if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _expiresAtUtc.AddSeconds(-30))
                {
                    return _accessToken;
                }

                if (string.IsNullOrWhiteSpace(_options.ServiceAccount.StudentNumber) || string.IsNullOrWhiteSpace(_options.ServiceAccount.Password))
                {
                    _logger.LogError("Turnstile service account credentials are not configured.");
                    return null;
                }

                var client = _httpClientFactory.CreateClient("IdentityAuth");
                var response = await client.PostAsJsonAsync("login", new
                {
                    studentNumber = _options.ServiceAccount.StudentNumber,
                    password = _options.ServiceAccount.Password
                }, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Service account login failed with status {StatusCode}", response.StatusCode);
                    _accessToken = null;
                    _expiresAtUtc = DateTime.MinValue;
                    return null;
                }

                var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken);
                if (payload == null || string.IsNullOrWhiteSpace(payload.Token))
                {
                    _logger.LogError("Service account login returned an invalid payload.");
                    _accessToken = null;
                    _expiresAtUtc = DateTime.MinValue;
                    return null;
                }

                _accessToken = payload.Token;
                _expiresAtUtc = payload.ExpiresAt?.ToUniversalTime() ?? DateTime.UtcNow.AddMinutes(5);
                return _accessToken;
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        public async Task InvalidateTokenAsync()
        {
            await _tokenLock.WaitAsync();
            try
            {
                _accessToken = null;
                _expiresAtUtc = DateTime.MinValue;
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        private sealed class TokenResponse
        {
            [JsonPropertyName("token")]
            public string? Token { get; set; }

            [JsonPropertyName("expiresAt")]
            public DateTime? ExpiresAt { get; set; }
        }
    }
}
