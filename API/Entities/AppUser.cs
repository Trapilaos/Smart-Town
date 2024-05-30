using System.ComponentModel.DataAnnotations;
using API.Extensions;

namespace API.Entities;

public class AppUser
{
    public int Id { get; set; }
    
    public string UserName { get; set; }
    
    public byte[] PasswordHash { get; set; }

    public byte[] PasswordSalt { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }
    
    [EmailAddress]
    public string Email { get; set; }

    [Phone]
    public string PhoneNumber { get; set; }

    public DateOnly DateofBirth { get; set; }

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public ICollection<Photo> Photos { get; set; } 

}
