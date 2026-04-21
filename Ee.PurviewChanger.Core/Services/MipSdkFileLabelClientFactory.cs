using Ee.PurviewChanger.Core.Models;

namespace Ee.PurviewChanger.Core.Services;

public static class MipSdkFileLabelClientFactory
{
    public static IMipSdkFileLabelClient Create(PurviewAppOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.MipSdk.DevelopmentFallbackEnabled)
        {
            return new DevelopmentMipSdkFileLabelClient();
        }

        return new NativeMipSdkFileLabelClient(
            new NativeLibraryMipSdkNativeBridge(options.MipSdk.NativeLibraryPath));
    }
}
