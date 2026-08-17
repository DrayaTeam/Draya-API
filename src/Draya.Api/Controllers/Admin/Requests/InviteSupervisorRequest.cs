namespace Draya.Api.Controllers.Admin.Requests;

public class InviteSupervisorRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Admin";
}
