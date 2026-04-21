using Ee.PurviewChanger.Core.Models;

namespace Ee.PurviewChanger.Core.Services;

public sealed class ValidationModeChangeService
    : ILabelChangeService
{
    private readonly AuditLogService _auditLogService;

    public ValidationModeChangeService(AuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    public async Task<LabelChangeResult> ApplyAsync(
        LabelChangePreview preview,
        PurviewAppOptions options,
        string actor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preview);
        ArgumentNullException.ThrowIfNull(options);

        if (!preview.CanApply)
        {
            return new LabelChangeResult(false, LabelChangeStatus.Blocked, preview.BlockReason ?? "적용 가능한 상태가 아닙니다.", null, false, null, null);
        }

        var entry = new AuditLogEntry(
            DateTimeOffset.UtcNow,
            preview.FilePath,
            preview.CurrentLabel,
            preview.TargetLabel,
            preview.ExecutionMode,
            "SimulatedSuccess",
            actor,
            false,
            preview.TargetLabel,
            "Validation mode simulated request.");

        var auditLogPath = await _auditLogService.WriteAsync(entry, options.AuditLogDirectory, cancellationToken);

        return new LabelChangeResult(
            true,
            LabelChangeStatus.Simulated,
            $"검증 모드에서 변경 요청을 기록했습니다. 로그: {auditLogPath}",
            auditLogPath,
            false,
            preview.TargetLabel,
            "실제 파일 라벨은 변경되지 않았습니다.");
    }
}
