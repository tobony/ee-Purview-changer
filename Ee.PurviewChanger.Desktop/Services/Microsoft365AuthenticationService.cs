using Ee.PurviewChanger.Core.Models;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;

namespace Ee.PurviewChanger.Desktop.Services;

public sealed class Microsoft365AuthenticationService
{
    private readonly AuthenticationOptions _options;
    private IPublicClientApplication? _clientApplication;
    private AuthenticationSession _currentSession;

    public Microsoft365AuthenticationService(AuthenticationOptions options)
    {
        _options = options;
        _currentSession = AuthenticationSession.NotConfigured(options);
    }

    public string CurrentActor => _currentSession.Actor;

    public async Task<AuthenticationSession> SignInAsync(IntPtr parentWindowHandle)
    {
        if (!IsConfigured())
        {
            _currentSession = AuthenticationSession.NotConfigured(_options);
            return _currentSession;
        }

        try
        {
            var application = GetClientApplication();
            var scopes = _options.Scopes.Count > 0 ? _options.Scopes : [AuthenticationOptions.DefaultGraphScope];
            var accounts = await application.GetAccountsAsync();
            AuthenticationResult result;

            try
            {
                result = await application.AcquireTokenSilent(scopes, accounts.FirstOrDefault()).ExecuteAsync();
            }
            catch (MsalUiRequiredException)
            {
                var builder = application.AcquireTokenInteractive(scopes).WithPrompt(Prompt.SelectAccount);

                if (parentWindowHandle != IntPtr.Zero)
                {
                    builder = builder.WithParentActivityOrWindow(parentWindowHandle);
                }

                result = await builder.ExecuteAsync();
            }

            _currentSession = new AuthenticationSession(
                true,
                true,
                $"로그인됨: {result.Account.Username}",
                "Windows 11에서는 WAM/Broker 구성을 통해 기존 Microsoft 365 로그인 상태를 재사용할 수 있습니다.",
                result.Account.Username);
        }
        catch (Exception exception)
        {
            _currentSession = new AuthenticationSession(
                true,
                false,
                "로그인 실패",
                exception.Message,
                "ValidationModeUser");
        }

        return _currentSession;
    }

    public async Task<AuthenticationSession> SignOutAsync()
    {
        if (_clientApplication is null)
        {
            _currentSession = AuthenticationSession.NotConfigured(_options);
            return _currentSession;
        }

        foreach (var account in await _clientApplication.GetAccountsAsync())
        {
            await _clientApplication.RemoveAsync(account);
        }

        _currentSession = new AuthenticationSession(
            true,
            false,
            "로그아웃됨",
            "다시 로그인하면 토큰 캐시 또는 WAM을 통해 계정을 선택할 수 있습니다.",
            "ValidationModeUser");

        return _currentSession;
    }

    private bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(_options.ClientId) &&
        !_options.ClientId.Contains("YOUR-", StringComparison.OrdinalIgnoreCase);

    private IPublicClientApplication GetClientApplication()
    {
        if (_clientApplication is not null)
        {
            return _clientApplication;
        }

        var builder = PublicClientApplicationBuilder
            .Create(_options.ClientId)
            .WithAuthority(AzureCloudInstance.AzurePublic, _options.TenantId)
            .WithDefaultRedirectUri();

        if (_options.UseBroker && OperatingSystem.IsWindows())
        {
            builder = builder.WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows));
        }

        _clientApplication = builder.Build();
        return _clientApplication;
    }
}
