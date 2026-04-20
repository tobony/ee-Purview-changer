namespace Ee.PurviewChanger.Core.Models;

public sealed record LabelChangeResult(
    bool Success,
    string Message,
    string? AuditLogPath,
    bool AppliedToSourceFile,
    string? RecheckedLabel,
    string? TechnicalDetails);
