using API.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Helpers
{
    /// <summary>Maps user ids (stored as text on reservations and invoices) to usernames for admin views.</summary>
    public static class UsernameLookup
    {
        public static async Task<Dictionary<string, string>> LoadAsync(DataContext context)
        {
            var users = await context.Users.AsNoTracking().Select(u => new { u.Id, u.UserName }).ToListAsync();
            return users.ToDictionary(u => u.Id.ToString(), u => u.UserName);
        }
    }
}
