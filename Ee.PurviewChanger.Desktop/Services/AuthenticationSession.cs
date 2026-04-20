namespace Ee.PurviewChanger.Desktop.Services;

public sealed record AuthenticationSession(
    bool IsConfigured,
    bool IsSignedIn,
    string StatusMessage,
    string Hint,
    string Actor)
{
    public static AuthenticationSession NotConfigured(Ee.PurviewChanger.Core.Models.AuthenticationOptions options) =>
        new(
            false,
            false,
            "Microsoft 365 로그인이 아직 구성되지 않았습니다.",
            $"appsettings.json에 ClientId를 입력하면 Windows 계정 기반 SSO/WAM 흐름을 시도할 수 있습니다. 현재 스코프: {string.Join(", ", options.Scopes)}",
            "ValidationModeUser");
}
