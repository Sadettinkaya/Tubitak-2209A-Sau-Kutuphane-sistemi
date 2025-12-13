using Microsoft.EntityFrameworkCore;
using ReservationService.Data;
using Shared.Events;

namespace ReservationService.Services;

public class PenaltyCheckService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PenaltyCheckService> _logger;
    private readonly RabbitMQPublisher _publisher;
    private const int CheckIntervalMinutes = 1; // Her 1 dakikada bir kontrol

    private const int EntryGracePeriodMinutes = 15;
    private const int PenaltyThreshold = 3;
    private const int BanDurationDays = 7;

    public PenaltyCheckService(
        IServiceProvider serviceProvider,
        ILogger<PenaltyCheckService> logger,
        RabbitMQPublisher publisher)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _publisher = publisher;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PenaltyCheckService başlatıldı. Her {Minutes} dakikada bir kontrol yapılacak.", CheckIntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndApplyPenaltiesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ceza kontrolü sırasında hata oluştu.");
            }

            await Task.Delay(TimeSpan.FromMinutes(CheckIntervalMinutes), stoppingToken);
        }
    }

    private async Task CheckAndApplyPenaltiesAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReservationDbContext>();

        var nowLocal = DateTime.Now;
        var today = DateOnly.FromDateTime(nowLocal);

        // Geçmiş cezaları temizle
        var expiredBans = await context.StudentProfiles
            .Where(p => p.BanUntil.HasValue && p.BanUntil.Value < today)
            .ToListAsync();

        foreach (var profile in expiredBans)
        {
            _logger.LogInformation("Öğrenci {StudentNumber} için ceza süresi doldu. Ban kaldırılıyor.", profile.StudentNumber);
            profile.BanUntil = null;
            profile.BanReason = null;
        }

        // Ceza almamış ama süresi geçmiş rezervasyonları bul
        var overdueReservations = await context.Reservations
            .Where(r => !r.IsAttended && !r.PenaltyProcessed)
            .ToListAsync();

        var penaltiesApplied = 0;
        var studentsProcessed = new HashSet<string>();

        foreach (var reservation in overdueReservations)
        {
            var reservationStart = reservation.ReservationDate.ToDateTime(reservation.StartTime);
            var entryDeadline = reservationStart.AddMinutes(EntryGracePeriodMinutes);

            // 15 dakika geçmiş mi kontrol et
            if (nowLocal > entryDeadline)
            {
                var profile = await context.StudentProfiles
                    .FirstOrDefaultAsync(p => p.StudentNumber == reservation.StudentNumber);

                if (profile != null)
                {
                    profile.PenaltyPoints++;
                    reservation.PenaltyProcessed = true;
                    penaltiesApplied++;
                    studentsProcessed.Add(profile.StudentNumber);

                    _logger.LogWarning(
                        "Öğrenci {StudentNumber} - Rezervasyon ID {ReservationId} için ceza uygulandı. " +
                        "Toplam ceza puanı: {PenaltyPoints}. Rezervasyon saati: {ReservationTime}",
                        profile.StudentNumber,
                        reservation.Id,
                        profile.PenaltyPoints,
                        reservationStart.ToString("dd.MM.yyyy HH:mm"));

                    // 3 ceza puanına ulaştıysa ban uygula
                    if (profile.PenaltyPoints >= PenaltyThreshold)
                    {
                        profile.PenaltyPoints = 0;
                        profile.BanUntil = DateOnly.FromDateTime(nowLocal.AddDays(BanDurationDays));
                        profile.BanReason = "Rezervasyonunuza katılmadığınız için sistem 7 gün ceza uyguladı.";
                        
                        _logger.LogWarning(
                            "Öğrenci {StudentNumber} için 7 günlük ban uygulandı. Ban bitiş tarihi: {BanUntil}",
                            profile.StudentNumber,
                            profile.BanUntil.Value.ToString("dd.MM.yyyy"));
                    }
                    else
                    {
                        profile.BanReason = $"Rezervasyonunuza katılmadığınız için ceza puanı aldınız. ({profile.PenaltyPoints}/3)";
                    }

                    profile.LastNoShowProcessedAt = DateTime.UtcNow;

                    // RabbitMQ Event: Profil güncellendi (ceza verildi)
                    try
                    {
                        var profileUpdatedEvent = new StudentProfileUpdatedEvent
                        {
                            StudentNumber = profile.StudentNumber,
                            StudentType = profile.StudentType,
                            PenaltyPoints = profile.PenaltyPoints,
                            BanUntil = profile.BanUntil?.ToString("yyyy-MM-dd"),
                            BanReason = profile.BanReason,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _publisher.Publish(profileUpdatedEvent, "student.profile.updated");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to publish StudentProfileUpdated event for {StudentNumber}", profile.StudentNumber);
                    }
                }
            }
        }

        if (expiredBans.Any() || penaltiesApplied > 0)
        {
            await context.SaveChangesAsync();
            
            if (penaltiesApplied > 0)
            {
                _logger.LogInformation(
                    "Ceza kontrolü tamamlandı. {Count} rezervasyon için ceza uygulandı. İşlenen öğrenci sayısı: {StudentCount}",
                    penaltiesApplied,
                    studentsProcessed.Count);
            }
        }
    }
}
