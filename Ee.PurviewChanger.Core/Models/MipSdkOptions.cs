namespace Ee.PurviewChanger.Core.Models;

public sealed class MipSdkOptions
{
    public bool Enabled { get; set; }

    public string ApplicationId { get; set; } = string.Empty;

    public string NativeLibraryPath { get; set; } = string.Empty;

    public bool DevelopmentFallbackEnabled { get; set; } = true;

    public string DevelopmentMetadataDirectory { get; set; } = "App_Data/MipSdkMetadata";

    public string DevelopmentDefaultLabel { get; set; } = "General";
}
