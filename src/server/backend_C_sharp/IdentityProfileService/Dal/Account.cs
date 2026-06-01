using Microsoft.AspNetCore.Identity;

namespace IdentityProfileService.Dal;

public class Account : IdentityUser<Guid>
{
    public Role Role  { get; set; }
    
    public Profile Profile { get; set; }
    
    public string? LockoutReason { get; set; }
}