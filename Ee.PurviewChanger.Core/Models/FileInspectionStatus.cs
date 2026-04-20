namespace Ee.PurviewChanger.Core.Models;

public enum FileInspectionStatus
{
    Ready,
    ValidationModeSimulated,
    FileNotFound,
    UnsupportedFileType,
    MipSdkDisabled,
    MipSdkConfigurationIncomplete,
    MipSdkUnavailable,
    InspectionFailed
}
