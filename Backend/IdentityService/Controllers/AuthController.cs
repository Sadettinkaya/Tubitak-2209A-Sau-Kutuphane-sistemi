using IdentityService.Entities;
using IdentityService.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace IdentityService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
            private readonly JwtOptions _jwtOptions;
            private readonly ILogger<AuthController> _logger;

            public AuthController(
                UserManager<AppUser> userManager,
                SignInManager<AppUser> signInManager,
                IOptions<JwtOptions> jwtOptions,
                ILogger<AuthController> logger)
            {
                _userManager = userManager;
                _signInManager = signInManager;
                _jwtOptions = jwtOptions.Value;
                _logger = logger;
            }

            [HttpPost("login")]
            [AllowAnonymous]
            public async Task<IActionResult> Login([FromBody] LoginModel model)
            {
                if (model == null || string.IsNullOrWhiteSpace(model.StudentNumber) || string.IsNullOrWhiteSpace(model.Password))
                {
                    return BadRequest("Öğrenci numarası ve şifre zorunludur");
                }

                var normalizedStudentNumber = model.StudentNumber.Trim();
                var user = await _userManager.FindByNameAsync(normalizedStudentNumber);
                if (user == null)
                {
                    return Unauthorized("Öğrenci numarası veya şifre hatalı");
                }

                var signInResult = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
                if (!signInResult.Succeeded)
                {
                    return Unauthorized("Öğrenci numarası veya şifre hatalı");
                }

                var roles = await _userManager.GetRolesAsync(user);
                var tokenResult = GenerateToken(user, roles);

                return Ok(new
                {
                    message = "Giriş başarılı",
                    token = tokenResult.Token,
                    expiresAt = tokenResult.ExpiresAt,
                    studentNumber = user.StudentNumber,
                    role = roles.FirstOrDefault() ?? "student",
                    academicLevel = user.AcademicLevel,
                    fullName = user.FullName
                });
            }

            [HttpPost("register")]
            [AllowAnonymous]
            public async Task<IActionResult> Register([FromBody] RegisterModel model)
            {
                if (model == null)
                {
                    return BadRequest("Geçersiz kayıt isteği");
                }

                var user = new AppUser
                {
                    UserName = model.StudentNumber?.Trim(),
                    Email = model.Email,
                    StudentNumber = model.StudentNumber?.Trim(),
                    FullName = model.FullName,
                    AcademicLevel = model.AcademicLevel,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (!result.Succeeded)
                {
                    return BadRequest(result.Errors);
                }

                var roleResult = await _userManager.AddToRoleAsync(user, "student");
                if (!roleResult.Succeeded)
                {
                    _logger.LogWarning("Yeni kullanıcı {StudentNumber} role eklenemedi: {Errors}", user.StudentNumber, string.Join(",", roleResult.Errors.Select(e => e.Description)));
                }

                return Ok(new { message = "Kayıt başarılı" });
            }

            [HttpGet("profile/{studentNumber}")]
            [Authorize]
            public async Task<IActionResult> GetProfile(string studentNumber)
            {
                if (string.IsNullOrWhiteSpace(studentNumber))
                {
                    return BadRequest("Öğrenci numarası zorunludur");
                }

                var user = await _userManager.FindByNameAsync(studentNumber.Trim());
                if (user == null)
                {
                    return NotFound("Kullanıcı bulunamadı");
                }

                var roles = await _userManager.GetRolesAsync(user);

                return Ok(new
                {
                    studentNumber = user.StudentNumber,
                    fullName = user.FullName,
                    academicLevel = user.AcademicLevel,
                    role = roles.FirstOrDefault() ?? "student",
                    email = user.Email
                });
            }

            private (string Token, DateTime ExpiresAt) GenerateToken(AppUser user, IList<string> roles)
            {
                var claims = new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim("studentNumber", user.StudentNumber ?? string.Empty),
                    new Claim("academicLevel", user.AcademicLevel ?? string.Empty),
                    new Claim("fullName", user.FullName ?? string.Empty)
                };

                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
                var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
                var expiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenMinutes);

                var token = new JwtSecurityToken(
                    issuer: _jwtOptions.Issuer,
                    audience: _jwtOptions.Audience,
                    claims: claims,
                    expires: expiresAt,
                    signingCredentials: creds);

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
                return (tokenString, expiresAt);
            }
    }

    public class LoginModel
    {
        public string StudentNumber { get; set; }
        public string Password { get; set; }
    }

    public class RegisterModel
    {
        public string StudentNumber { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string AcademicLevel { get; set; }
    }
}