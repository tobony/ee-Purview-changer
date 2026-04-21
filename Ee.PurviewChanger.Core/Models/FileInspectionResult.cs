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
    FileInspectionStatus Status,
    string ExecutionMode,
    string ProviderName,
    string CurrentStateSummary,
    string CapabilitySummary,
    string? TechnicalDetails,
    IReadOnlyList<string> Messages);
