using Ee.PurviewChanger.Core.Models;
using Ee.PurviewChanger.Core.Services;

namespace Ee.PurviewChanger.Core.Tests;

[TestClass]
public sealed class MipSdkFileLabelClientFactoryTests
{
    [TestMethod]
    public void Create_returns_development_client_when_fallback_is_enabled()
    {
        var options = new PurviewAppOptions
        {
            MipSdk = new MipSdkOptions
            {
                DevelopmentFallbackEnabled = true
            }
        };

        var client = MipSdkFileLabelClientFactory.Create(options);

        Assert.IsInstanceOfType<DevelopmentMipSdkFileLabelClient>(client);
    }

    [TestMethod]
    public void Create_returns_native_client_when_fallback_is_disabled()
    {
        var options = new PurviewAppOptions
        {
            MipSdk = new MipSdkOptions
            {
                DevelopmentFallbackEnabled = false,
                NativeLibraryPath = "mip-native.dll"
            }
        };

        var client = MipSdkFileLabelClientFactory.Create(options);

        Assert.IsInstanceOfType<NativeMipSdkFileLabelClient>(client);
    }
}
