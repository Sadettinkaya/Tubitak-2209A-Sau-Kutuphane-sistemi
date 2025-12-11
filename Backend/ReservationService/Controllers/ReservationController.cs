using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReservationService.Data;

namespace ReservationService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationController : ControllerBase
    {
        private readonly ReservationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ReservationController> _logger;

        private const int EarlyToleranceMinutes = 5;  // Başlangıçtan 5dk önce giriş yapılabilir
        private const int EntryGracePeriodMinutes = 15; // Başlangıçtan 15dk sonrasına kadar giriş yapılabilir, sonra ceza
        private const int PenaltyThreshold = 3;
        private const int BanDurationDays = 7;
        private static readonly TimeSpan MinReservationDuration = TimeSpan.FromHours(1);
        private static readonly TimeSpan MaxReservationDuration = TimeSpan.FromHours(4);

        private static readonly StringComparer StudentTypeComparer = StringComparer.OrdinalIgnoreCase;

        private static readonly Dictionary<string, StudentTypeRule> StudentTypeRules = new(StudentTypeComparer)
        {
            ["lisans"] = new StudentTypeRule(Priority: 1, MaxAdvanceDays: 3, MaxActiveReservations: 1),
            ["yukseklisans"] = new StudentTypeRule(Priority: 2, MaxAdvanceDays: 5, MaxActiveReservations: 2),
            ["yükseklisans"] = new StudentTypeRule(Priority: 2, MaxAdvanceDays: 5, MaxActiveReservations: 2),
            ["doktora"] = new StudentTypeRule(Priority: 3, MaxAdvanceDays: 7, MaxActiveReservations: 3),
            // Admin için kısıtları esnetiyoruz
            ["admin"] = new StudentTypeRule(Priority: 99, MaxAdvanceDays: 30, MaxActiveReservations: 10)
        };

        private static readonly Dictionary<string, string> StudentTypeCanonicalNames = new(StudentTypeComparer)
        {
            ["lisans"] = "Lisans",
            ["yukseklisans"] = "YüksekLisans",
            ["yükseklisans"] = "YüksekLisans",
            ["doktora"] = "Doktora",
            ["admin"] = "Admin"
        };

        public ReservationController(ReservationDbContext context, IHttpClientFactory httpClientFactory, ILogger<ReservationController> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        private string? GetCurrentStudentNumber()
        {
            // JWT devre dışıyken claim olmayabilir; eski davranışa yakın kalmak için
            // varsa claim'i kullan, yoksa null döndür.
            return User.FindFirst("studentNumber")?.Value;
        }

        // Geçici olarak tüm istekleri admin / service gibi kabul ediyoruz
        // ki JWT zorunluluğu kalktığında eski UI akışı çalışsın.
        private bool IsAdmin => true;

        private bool IsService => true;

        private record StudentTypeRule(int Priority, int MaxAdvanceDays, int MaxActiveReservations);

        private sealed class IdentityProfileDto
        {
            public string StudentNumber { get; set; } = string.Empty;
            public string? AcademicLevel { get; set; }
            public string? Role { get; set; }
            public string? FullName { get; set; }
            public string? Email { get; set; }
        }

        [HttpGet("Tables")]
        public async Task<IActionResult> GetTables(string date, string start, string end, int floorId)
        {
            if (!DateOnly.TryParse(date, out var rDate) || 
                !TimeOnly.TryParse(start, out var rStart) || 
                !TimeOnly.TryParse(end, out var rEnd))
            {
                return BadRequest("Geçersiz tarih/saat formatı.");
            }

            var tables = await _context.Tables
                .Where(t => t.FloorId == floorId)
                .ToListAsync();

            var result = new List<object>();

            foreach (var table in tables)
            {
                // Check if table is occupied in the requested time slot
                var isOccupied = await _context.Reservations.AnyAsync(r => 
                    r.TableId == table.Id && 
                    r.ReservationDate == rDate &&
                    ((r.StartTime < rEnd && r.EndTime > rStart))); // Overlap logic

                result.Add(new { 
                    table.Id, 
                    table.TableNumber, 
                    table.FloorId,
                    IsAvailable = !isOccupied
                });
            }

            return Ok(result);
        }

        [HttpPost("Create")]
        public async Task<IActionResult> CreateReservation([FromBody] ReservationRequest request)
        {
            var currentStudentNumber = GetCurrentStudentNumber();

            if (IsAdmin)
            {
                if (string.IsNullOrWhiteSpace(request.StudentNumber))
                {
                    return BadRequest("Öğrenci numarası zorunludur.");
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(currentStudentNumber))
                {
                    return Forbid();
                }

                if (!string.IsNullOrWhiteSpace(request.StudentNumber) &&
                    !string.Equals(request.StudentNumber.Trim(), currentStudentNumber, StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }

                request.StudentNumber = currentStudentNumber;
            }

            if (request.StudentNumber.Trim().Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Yönetici rezervasyon alamaz.");
            }

            if (!DateOnly.TryParse(request.ReservationDate, out var rDate) ||
                !TimeOnly.TryParse(request.StartTime, out var rStart) ||
                !TimeOnly.TryParse(request.EndTime, out var rEnd))
            {
                return BadRequest("Geçersiz tarih/saat formatı.");
            }

            var profile = await GetOrCreateStudentProfileAsync(request.StudentNumber, request.StudentType);
            await ApplyNoShowPenaltiesAsync(profile);

            var today = DateOnly.FromDateTime(DateTime.Now);

            if (rEnd <= rStart)
            {
                return BadRequest(new
                {
                    message = "Bitiş saati başlangıç saatinden sonra olmalıdır."
                });
            }

            var reservationDuration = rEnd.ToTimeSpan() - rStart.ToTimeSpan();

            if (reservationDuration < MinReservationDuration)
            {
                return BadRequest(new
                {
                    message = $"Rezervasyon süresi en az {MinReservationDuration.TotalHours:F0} saat olmalıdır."
                });
            }

            if (reservationDuration > MaxReservationDuration)
            {
                return BadRequest(new
                {
                    message = $"Rezervasyon süresi en fazla {MaxReservationDuration.TotalHours:F0} saat olabilir."
                });
            }

            if (profile.BanUntil.HasValue && rDate <= profile.BanUntil.Value)
            {
                return BadRequest(new
                {
                    message = $"Ceza süreniz {profile.BanUntil:yyyy-MM-dd} tarihine kadar devam ediyor. Bu tarihten önceki bir gün için rezervasyon oluşturamazsınız.",
                    reason = profile.BanReason
                });
            }

            var rule = ResolveRule(profile.StudentType);

            if (rDate > today.AddDays(rule.MaxAdvanceDays))
            {
                return BadRequest(new
                {
                    message = $"{profile.StudentType} öğrencileri en fazla {rule.MaxAdvanceDays} gün öncesine kadar rezervasyon oluşturabilir."
                });
            }

            var activeReservationCount = await _context.Reservations
                .CountAsync(r => r.StudentNumber == request.StudentNumber && r.ReservationDate >= today);

            if (activeReservationCount >= rule.MaxActiveReservations)
            {
                return BadRequest(new
                {
                    message = $"{profile.StudentType} öğrencileri aynı anda en fazla {rule.MaxActiveReservations} aktif rezervasyona sahip olabilir."
                });
            }

            // Aynı öğrencinin diğer masalarda çakışan bir rezervasyonu olup olmadığını kontrol et
            var hasConflictForStudent = await _context.Reservations.AnyAsync(r =>
                r.StudentNumber == request.StudentNumber &&
                r.ReservationDate == rDate &&
                ((r.StartTime <= rStart && r.EndTime > rStart) ||
                 (r.StartTime < rEnd && r.EndTime >= rEnd)));

            if (hasConflictForStudent)
            {
                return BadRequest(new
                {
                    message = "Bu saat aralığında başka bir rezervasyonunuz bulunuyor."
                });
            }

            var isOccupied = await _context.Reservations.AnyAsync(r =>
                r.TableId == request.TableId &&
                r.ReservationDate == rDate &&
                ((r.StartTime <= rStart && r.EndTime > rStart) ||
                 (r.StartTime < rEnd && r.EndTime >= rEnd)));

            if (isOccupied)
            {
                return BadRequest(new
                {
                    message = "Bu saat aralığında masa dolu."
                });
            }

            var reservation = new Reservation
            {
                TableId = request.TableId,
                StudentNumber = request.StudentNumber.Trim(),
                ReservationDate = rDate,
                StartTime = rStart,
                EndTime = rEnd,
                IsAttended = false,
                PenaltyProcessed = false,
                StudentType = profile.StudentType
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Rezervasyon oluşturuldu.", reservationId = reservation.Id });
        }

        [HttpGet("MyReservations")]
        public async Task<IActionResult> GetMyReservations(string studentNumber)
        {
            Console.WriteLine($"GetMyReservations called for: {studentNumber}");

            var currentStudentNumber = GetCurrentStudentNumber();
            if (!IsAdmin)
            {
                if (string.IsNullOrWhiteSpace(currentStudentNumber) ||
                    !string.Equals(studentNumber, currentStudentNumber, StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }

                studentNumber = currentStudentNumber;
            }

            var reservations = await _context.Reservations
                .Where(r => r.StudentNumber == studentNumber)
                .OrderByDescending(r => r.ReservationDate)
                .ThenByDescending(r => r.StartTime)
                .ToListAsync();

            Console.WriteLine($"Found {reservations.Count} reservations.");

            var result = new List<object>();
            foreach(var res in reservations)
            {
                var table = await _context.Tables.FindAsync(res.TableId);
                result.Add(new {
                    res.Id,
                    ReservationDate = res.ReservationDate.ToString("yyyy-MM-dd"),
                    StartTime = res.StartTime.ToString("HH:mm"),
                    EndTime = res.EndTime.ToString("HH:mm"),
                    res.IsAttended,
                    res.StudentType,
                    TableNumber = table?.TableNumber ?? "Bilinmiyor",
                    FloorId = table?.FloorId
                });
            }

            return Ok(result);
        }

        [HttpGet("All")]
        public async Task<IActionResult> GetAllReservations()
        {
            var reservations = await _context.Reservations
                .OrderByDescending(r => r.ReservationDate)
                .ThenByDescending(r => r.StartTime)
                .ToListAsync();

            var result = new List<object>();
            foreach(var res in reservations)
            {
                var table = await _context.Tables.FindAsync(res.TableId);
                result.Add(new {
                    res.Id,
                    res.StudentNumber,
                    ReservationDate = res.ReservationDate.ToString("yyyy-MM-dd"),
                    StartTime = res.StartTime.ToString("HH:mm"),
                    EndTime = res.EndTime.ToString("HH:mm"),
                    res.IsAttended,
                    res.StudentType,
                    TableNumber = table?.TableNumber ?? "Bilinmiyor",
                    FloorId = table?.FloorId
                });
            }

            return Ok(result);
        }

        [HttpDelete("Cancel/{id}")]
        public async Task<IActionResult> CancelReservation(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                return NotFound("Rezervasyon bulunamadı.");
            }

            var currentStudentNumber = GetCurrentStudentNumber();
            if (!IsAdmin && (string.IsNullOrWhiteSpace(currentStudentNumber) ||
                !string.Equals(reservation.StudentNumber, currentStudentNumber, StringComparison.OrdinalIgnoreCase)))
            {
                return Forbid();
            }

            // Geçmiş rezervasyonlar iptal edilemez kuralı eklenebilir
            // if (reservation.ReservationDate < DateOnly.FromDateTime(DateTime.Now)) ...

            _context.Reservations.Remove(reservation);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Rezervasyon iptal edildi." });
        }

        private static string BuildRuleKey(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "lisans";
            }

            var cleaned = value.Trim().Replace(" ", string.Empty);
            cleaned = cleaned
                .Replace("İ", "i")
                .Replace("I", "i")
                .Replace("ı", "i")
                .Replace("Ü", "u")
                .Replace("ü", "u")
                .Replace("Ö", "o")
                .Replace("ö", "o")
                .Replace("Ğ", "g")
                .Replace("ğ", "g")
                .Replace("Ş", "s")
                .Replace("ş", "s")
                .Replace("Ç", "c")
                .Replace("ç", "c");

            return cleaned.ToLowerInvariant();
        }

        private static string NormalizeStudentType(string? rawType)
        {
            var ruleKey = BuildRuleKey(rawType);
            if (StudentTypeCanonicalNames.TryGetValue(ruleKey, out var canonical))
            {
                return canonical;
            }

            return StudentTypeCanonicalNames["lisans"];
        }

        private static StudentTypeRule ResolveRule(string studentType)
        {
            var key = BuildRuleKey(studentType);
            if (StudentTypeRules.TryGetValue(key, out var rule))
            {
                return rule;
            }

            return StudentTypeRules["lisans"];
        }

        private async Task<StudentProfile> GetOrCreateStudentProfileAsync(string studentNumber, string? studentTypeHint)
        {
            var normalizedNumber = studentNumber.Trim();
            var profile = await _context.StudentProfiles
                .SingleOrDefaultAsync(p => p.StudentNumber == normalizedNumber);

            var canonicalHint = string.IsNullOrWhiteSpace(studentTypeHint)
                ? null
                : NormalizeStudentType(studentTypeHint);

            if (profile == null)
            {
                profile = new StudentProfile
                {
                    StudentNumber = normalizedNumber,
                    StudentType = canonicalHint ?? "Lisans"
                };

                _context.StudentProfiles.Add(profile);
            }

            var identityApplied = await TryEnrichStudentTypeFromIdentityAsync(profile, normalizedNumber);

            if (!identityApplied && canonicalHint != null && !StudentTypeComparer.Equals(profile.StudentType, canonicalHint))
            {
                if (StudentTypeComparer.Equals(profile.StudentType, "Lisans"))
                {
                    profile.StudentType = canonicalHint;
                }
            }

            if (string.IsNullOrWhiteSpace(profile.StudentType))
            {
                profile.StudentType = "Lisans";
            }

            return profile;
        }

        private async Task<bool> TryEnrichStudentTypeFromIdentityAsync(StudentProfile profile, string normalizedStudentNumber)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("IdentityService");
                using var response = await client.GetAsync($"api/Auth/profile/{normalizedStudentNumber}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("IdentityService returned {StatusCode} for student {StudentNumber}", response.StatusCode, normalizedStudentNumber);
                    return false;
                }

                var identityProfile = await response.Content.ReadFromJsonAsync<IdentityProfileDto>();
                if (identityProfile == null || string.IsNullOrWhiteSpace(identityProfile.AcademicLevel))
                {
                    return false;
                }

                var canonical = NormalizeStudentType(identityProfile.AcademicLevel);
                if (!StudentTypeComparer.Equals(profile.StudentType, canonical))
                {
                    profile.StudentType = canonical;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "IdentityService lookup failed for student {StudentNumber}", normalizedStudentNumber);
                return false;
            }
        }

        private async Task ApplyNoShowPenaltiesAsync(StudentProfile profile)
        {
            var nowLocal = DateTime.Now;
            var nowUtc = DateTime.UtcNow;
            var today = DateOnly.FromDateTime(nowLocal);
            var stateChanged = false;
            var penaltyAppliedThisCycle = false;

            if (profile.BanUntil.HasValue && profile.BanUntil.Value < today)
            {
                profile.BanUntil = null;
                profile.BanReason = null;
                stateChanged = true;
            }

            var overdueReservations = await _context.Reservations
                .Where(r => r.StudentNumber == profile.StudentNumber && !r.IsAttended && !r.PenaltyProcessed)
                .ToListAsync();

            foreach (var reservation in overdueReservations)
            {
                // Başlangıç saatinden 15dk sonrasına kadar giriş yapılmalı, aksi halde ceza
                var reservationStart = reservation.ReservationDate.ToDateTime(reservation.StartTime);
                if (reservationStart.AddMinutes(EntryGracePeriodMinutes) < nowLocal)
                {
                    profile.PenaltyPoints++;
                    reservation.PenaltyProcessed = true;
                    stateChanged = true;
                    penaltyAppliedThisCycle = true;
                }
            }

            if (profile.PenaltyPoints >= PenaltyThreshold)
            {
                profile.PenaltyPoints = 0;
                profile.BanUntil = DateOnly.FromDateTime(nowLocal.AddDays(BanDurationDays));
                profile.BanReason = "Rezervasyonunuza katılmadığınız için sistem 7 gün ceza uyguladı.";
                stateChanged = true;
            }
            else if (penaltyAppliedThisCycle && string.IsNullOrEmpty(profile.BanReason))
            {
                profile.BanReason = "Rezervasyonunuza katılmadığınız için ceza puanı aldınız.";
                stateChanged = true;
            }

            if (stateChanged)
            {
                profile.LastNoShowProcessedAt = nowUtc;
                await _context.SaveChangesAsync();
            }
        }

        [HttpGet("Profile/{studentNumber}")]
        public async Task<IActionResult> GetProfile(string studentNumber)
        {
            if (string.IsNullOrWhiteSpace(studentNumber))
            {
                return BadRequest(new { message = "Öğrenci numarası zorunludur." });
            }

            if (!IsAdmin)
            {
                var currentStudentNumber = GetCurrentStudentNumber();
                if (string.IsNullOrWhiteSpace(currentStudentNumber) ||
                    !string.Equals(studentNumber, currentStudentNumber, StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }
            }

            var profile = await GetOrCreateStudentProfileAsync(studentNumber, null);
            await ApplyNoShowPenaltiesAsync(profile);

            await _context.SaveChangesAsync();

            var response = new StudentProfileDto
            {
                StudentNumber = profile.StudentNumber,
                StudentType = profile.StudentType,
                PenaltyPoints = profile.PenaltyPoints,
                BanUntil = profile.BanUntil?.ToString("yyyy-MM-dd"),
                BanReason = profile.BanReason,
                LastNoShowProcessedAt = profile.LastNoShowProcessedAt
            };

            return Ok(response);
        }

        [HttpGet("Penalties")]
        public async Task<IActionResult> GetPenalties()
        {
            var profiles = await _context.StudentProfiles
                .Where(p => p.BanUntil != null || p.PenaltyPoints > 0)
                .OrderByDescending(p => p.BanUntil)
                .ThenByDescending(p => p.PenaltyPoints)
                .ToListAsync();

            var result = profiles.Select(p => new StudentProfileDto
            {
                StudentNumber = p.StudentNumber,
                StudentType = p.StudentType,
                PenaltyPoints = p.PenaltyPoints,
                BanUntil = p.BanUntil?.ToString("yyyy-MM-dd"),
                BanReason = p.BanReason,
                LastNoShowProcessedAt = p.LastNoShowProcessedAt
            });

            return Ok(result);
        }

        [HttpGet("CheckAccess")]
        public async Task<IActionResult> CheckAccess(string studentNumber)
        {
            if (string.IsNullOrWhiteSpace(studentNumber))
            {
                return Ok(new { allowed = false, message = "Geçerli bir öğrenci numarası gönderilmedi." });
            }

            var normalizedStudentNumber = studentNumber.Trim();
            var nowLocal = DateTime.Now;
            var today = DateOnly.FromDateTime(nowLocal);
            var nowTime = TimeOnly.FromDateTime(nowLocal);

            var profile = await GetOrCreateStudentProfileAsync(normalizedStudentNumber, null);
            await ApplyNoShowPenaltiesAsync(profile);

            if (profile.BanUntil.HasValue && profile.BanUntil.Value >= today)
            {
                return Ok(new
                {
                    allowed = false,
                    message = $"Ceza sisteminden dolayı {profile.BanUntil:yyyy-MM-dd} tarihine kadar giriş hakkınız bulunmuyor.",
                    reason = profile.BanReason
                });
            }

            var todaysReservations = await _context.Reservations
                .Where(r => r.ReservationDate == today && EF.Functions.ILike(r.StudentNumber, normalizedStudentNumber))
                .OrderBy(r => r.StartTime)
                .ToListAsync();

            // Giriş yapılabilir aralık: Başlangıç - 5dk ile Başlangıç + 15dk (veya EndTime) arasında
            var activeReservation = todaysReservations
                .FirstOrDefault(r =>
                    r.StartTime.AddMinutes(-EarlyToleranceMinutes) <= nowTime &&
                    r.EndTime >= nowTime);

            if (activeReservation != null)
            {
                // Başlangıç + 15dk geçtiyse artık giriş yapılamaz (ceza alır)
                var entryDeadline = activeReservation.StartTime.AddMinutes(EntryGracePeriodMinutes);
                if (nowTime > entryDeadline && !activeReservation.IsAttended)
                {
                    return Ok(new
                    {
                        allowed = false,
                        message = $"Giriş süresi doldu! Rezervasyon başlangıcından itibaren {EntryGracePeriodMinutes} dakika içinde giriş yapılmalıydı. Ceza puanı uygulanacak."
                    });
                }

                if (!activeReservation.IsAttended)
                {
                    activeReservation.IsAttended = true;
                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    allowed = true,
                    message = $"Giriş onaylandı. Masa: {activeReservation.TableId}",
                    reservationId = activeReservation.Id
                });
            }

            var upcomingToday = todaysReservations
                .FirstOrDefault(r => nowTime < r.StartTime.AddMinutes(-EarlyToleranceMinutes));

            if (upcomingToday != null)
            {
                var waitMinutes = Math.Ceiling((upcomingToday.StartTime.AddMinutes(-EarlyToleranceMinutes) - nowTime).TotalMinutes);
                return Ok(new
                {
                    allowed = false,
                    message = $"Rezervasyon saati henüz başlamadı. Tahmini bekleme: {waitMinutes} dakika. ({upcomingToday.StartTime} - {upcomingToday.EndTime})"
                });
            }

            if (todaysReservations.Count > 0)
            {
                var lastReservation = todaysReservations.Last();
                return Ok(new
                {
                    allowed = false,
                    message = $"Rezervasyon saati sona erdi. ({lastReservation.StartTime} - {lastReservation.EndTime})"
                });
            }

            var nextReservation = await _context.Reservations
                .Where(r => r.ReservationDate > today && EF.Functions.ILike(r.StudentNumber, normalizedStudentNumber))
                .OrderBy(r => r.ReservationDate)
                .ThenBy(r => r.StartTime)
                .FirstOrDefaultAsync();

            if (nextReservation != null)
            {
                return Ok(new
                {
                    allowed = false,
                    message = $"Sonraki rezervasyonunuz {nextReservation.ReservationDate:yyyy-MM-dd} tarihinde. ({nextReservation.StartTime} - {nextReservation.EndTime})"
                });
            }

            return Ok(new { allowed = false, message = "Şu an için aktif bir rezervasyonunuz bulunmamaktadır." });
        }
    }

    public class ReservationRequest
    {
        public int TableId { get; set; }
        public string StudentNumber { get; set; } = string.Empty;
        public string ReservationDate { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public string? StudentType { get; set; }
    }

    public class StudentProfileDto
    {
        public string StudentNumber { get; set; } = string.Empty;
        public string StudentType { get; set; } = string.Empty;
        public int PenaltyPoints { get; set; }
        public string? BanUntil { get; set; }
        public string? BanReason { get; set; }
        public DateTime? LastNoShowProcessedAt { get; set; }
    }
}