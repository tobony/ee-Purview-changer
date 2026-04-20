namespace Ee.PurviewChanger.Core.Models;

public sealed class AuthenticationOptions
{
    public string ClientId { get; set; } = string.Empty;

    public string TenantId { get; set; } = "organizations";

    public bool UseBroker { get; set; } = true;

    public List<string> Scopes { get; set; } = ["User.Read"];
}
