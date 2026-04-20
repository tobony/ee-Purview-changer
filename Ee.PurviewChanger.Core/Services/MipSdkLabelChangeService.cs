using Ee.PurviewChanger.Core.Models;

namespace Ee.PurviewChanger.Core.Services;

public sealed class MipSdkLabelChangeService(
    IMipSdkFileLabelClient client,
    AuditLogService auditLogService)
    : ILabelChangeService
{
    public async Task<LabelChangeResult> ApplyAsync(
        LabelChangePreview preview,
        PurviewAppOptions options,
        string actor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preview);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(actor);

        if (!preview.CanApply)
        {
            return new LabelChangeResult(false, preview.BlockReason ?? "적용 가능한 상태가 아닙니다.", null, false, null, null);
        }

        var outcome = await client.ApplyAsync(
            preview.FilePath,
            preview.CurrentLabel,
            preview.TargetLabel,
            options,
            actor,
            cancellationToken);

        var entry = new AuditLogEntry(
            DateTimeOffset.UtcNow,
            preview.FilePath,
            preview.CurrentLabel,
            preview.TargetLabel,
            preview.ExecutionMode,
            outcome.Success ? "Applied" : "Failed",
            actor,
            outcome.AppliedToSourceFile,
            outcome.RecheckedLabel,
            outcome.TechnicalDetails);

        var auditLogPath = await auditLogService.WriteAsync(entry, options.AuditLogDirectory, cancellationToken);

        return new LabelChangeResult(
            outcome.Success,
            outcome.Success
                ? $"{outcome.Message} 로그: {auditLogPath}"
                : $"{outcome.Message} 실패 로그: {auditLogPath}",
            auditLogPath,
            outcome.AppliedToSourceFile,
            outcome.RecheckedLabel,
            outcome.TechnicalDetails);
    }
}
