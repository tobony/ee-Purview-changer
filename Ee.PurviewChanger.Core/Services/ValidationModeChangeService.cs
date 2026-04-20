using Ee.PurviewChanger.Core.Models;

namespace Ee.PurviewChanger.Core.Services;

public sealed class ValidationModeChangeService
{
    private readonly AuditLogService _auditLogService;

    public ValidationModeChangeService(AuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    public async Task<LabelChangeResult> ApplyAsync(
        LabelChangePreview preview,
        string auditLogDirectory,
        string actor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preview);

        if (!preview.CanApply)
        {
            return new LabelChangeResult(false, preview.BlockReason ?? "적용 가능한 상태가 아닙니다.", null);
        }

        var entry = new AuditLogEntry(
            DateTimeOffset.UtcNow,
            preview.FilePath,
            preview.CurrentLabel,
            preview.TargetLabel,
            preview.ExecutionMode,
            "SimulatedSuccess",
            actor);

        var auditLogPath = await _auditLogService.WriteAsync(entry, auditLogDirectory, cancellationToken);

        return new LabelChangeResult(
            true,
            $"검증 모드에서 변경 요청을 기록했습니다. 로그: {auditLogPath}",
            auditLogPath);
    }
}
