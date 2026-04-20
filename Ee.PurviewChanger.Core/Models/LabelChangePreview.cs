namespace Ee.PurviewChanger.Core.Models;

public sealed record LabelChangePreview(
    string FilePath,
    string CurrentLabel,
    string TargetLabel,
    bool CanApply,
    string ExecutionMode,
    string Summary,
    string? BlockReason);
