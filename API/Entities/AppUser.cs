using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace API.Entities;

public class AppUser : IdentityUser<int>
{

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public DateOnly DateofBirth { get; set; }

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public ICollection<Photo> Photos { get; set; }

    public ICollection<AppUserRole> UserRoles { get; set; }
}
