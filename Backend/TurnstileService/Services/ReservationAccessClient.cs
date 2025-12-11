using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using TurnstileService.Models;

namespace TurnstileService.Services;

public class ReservationAccessClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ReservationAccessClient> _logger;
    private readonly TurnstileOptions _options;
    private readonly TurnstileAuthProvider _authProvider;

    public ReservationAccessClient(HttpClient httpClient,
        ILogger<ReservationAccessClient> logger,
        IOptions<TurnstileOptions> options,
        TurnstileAuthProvider authProvider)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
        _authProvider = authProvider;

        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri(_options.ReservationServiceBaseUrl);
        }
    }

    public async Task<ReservationAccessResponse?> CheckAccessAsync(string studentNumber, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(studentNumber))
        {
            return null;
        }

        var sanitizedNumber = studentNumber.Trim();
        var requestUri = $"api/Reservation/CheckAccess?studentNumber={Uri.EscapeDataString(sanitizedNumber)}";

        try
        {
            for (var attempt = 0; attempt < 2; attempt++)
            {
                var accessToken = await _authProvider.GetAccessTokenAsync(cancellationToken);

                using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

                // JWT'i zorunlu kılmayalım; token alınamazsa headers eklenmeden istek atılsın.
                if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }

                using var response = await _httpClient.SendAsync(request, cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized && attempt == 0)
                {
                    _logger.LogInformation("Turnstile service account token expired. Refreshing token.");
                    await _authProvider.InvalidateTokenAsync();
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Reservation service returned {StatusCode} for student {StudentNumber}",
                        response.StatusCode, sanitizedNumber);
                    return new ReservationAccessResponse
                    {
                        Allowed = false,
                        Message = "Rezervasyon servisi erişilemedi."
                    };
                }

                return await response.Content.ReadFromJsonAsync<ReservationAccessResponse>(cancellationToken: cancellationToken);
            }

            _logger.LogError("Turnstile service account could not refresh token after retry.");
            return null;
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "Reservation service is unreachable for student {StudentNumber}", sanitizedNumber);
            throw;
        }
    }
}
