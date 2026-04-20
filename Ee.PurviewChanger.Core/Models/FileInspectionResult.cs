namespace Ee.PurviewChanger.Core.Models;

public sealed record FileInspectionResult(
    string FilePath,
    bool FileExists,
    string FileExtension,
    bool IsSupportedFileType,
    bool CurrentLabelKnown,
    string CurrentLabel,
    bool CanPreviewChange,
    bool RequiresMipSdk,
    string CurrentStateSummary,
    string CapabilitySummary,
    IReadOnlyList<string> Messages);
