using Microsoft.AspNetCore.Identity;

namespace IdentityService.Entities
{
    public class AppUser : IdentityUser
    {
        public string FullName { get; set; }
        public string StudentNumber { get; set; }
        public string AcademicLevel { get; set; } // Lisans, Yüksek Lisans, Doktora
    }
}
