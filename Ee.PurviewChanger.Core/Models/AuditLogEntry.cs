namespace Ee.PurviewChanger.Core.Models;

public sealed record AuditLogEntry(
    DateTimeOffset Timestamp,
    string FilePath,
    string CurrentLabel,
    string TargetLabel,
    string ExecutionMode,
    string Result,
    string Actor,
    bool AppliedToSourceFile,
    string? RecheckedLabel,
    string? Details);
