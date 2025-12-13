using System.Net.Http;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using TurnstileService.Models;
using TurnstileService.Services;
using Shared.Events;

namespace TurnstileService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TurnstileController : ControllerBase
{
    private readonly ReservationAccessClient _reservationClient;
    private readonly ITurnstileEntryLog _entryLog;
    private readonly ILogger<TurnstileController> _logger;
    private readonly RabbitMQPublisher _publisher;

    public TurnstileController(
        ReservationAccessClient reservationClient,
        ITurnstileEntryLog entryLog,
        ILogger<TurnstileController> logger,
        RabbitMQPublisher publisher)
    {
        _reservationClient = reservationClient;
        _entryLog = entryLog;
        _logger = logger;
        _publisher = publisher;
    }

    [HttpPost("enter")]
    public async Task<IActionResult> Enter([FromBody] TurnstileRequest request, CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.StudentNumber))
        {
            return BadRequest(new { message = "Öğrenci numarası gerekli.", doorOpen = false });
        }

        try
        {
            var reservationResponse = await _reservationClient.CheckAccessAsync(request.StudentNumber, cancellationToken);

            if (reservationResponse == null)
            {
                const string unknownResponse = "Rezervasyon servisi yanıt vermedi.";
                _entryLog.Record(request.StudentNumber, false, unknownResponse);
                return StatusCode(503, new { message = unknownResponse, doorOpen = false });
            }

            _entryLog.Record(request.StudentNumber, reservationResponse.Allowed, reservationResponse.Message);

            if (reservationResponse.Allowed)
            {
                // Başarılı giriş event'i gönder
                var entryEvent = new StudentEnteredEvent
                {
                    StudentNumber = request.StudentNumber,
                    EntryTime = DateTime.UtcNow,
                    TurnstileId = "turnstile-1"
                };
                _publisher.Publish(entryEvent, "student.entered");
                _logger.LogInformation("Student {StudentNumber} entered via turnstile", request.StudentNumber);

                return Ok(new { message = reservationResponse.Message, doorOpen = true });
            }

            return Ok(new { message = reservationResponse.Message, doorOpen = false });
        }
        catch (HttpRequestException ex)
        {
            const string errorMessage = "Rezervasyon servisine ulaşılamadı.";
            _entryLog.Record(request.StudentNumber, false, errorMessage);
            _logger.LogError(ex, "Reservation service unreachable while processing student {StudentNumber}", request.StudentNumber);
            return StatusCode(503, new { message = errorMessage, doorOpen = false });
        }
    }

    [HttpGet("logs")]
    public IActionResult GetLogs([FromQuery] int take = 20)
    {
        var logs = _entryLog.GetLatest(take)
            .Select(entry => new
            {
                entry.StudentNumber,
                entry.Allowed,
                entry.Message,
                timestamp = entry.TimestampUtc,
                localTime = entry.TimestampUtc.ToLocalTime()
            });

        return Ok(logs);
    }
}