namespace Draya.Api.Controllers.Admin.Requests;

public class UpdateAdminProfileRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}
