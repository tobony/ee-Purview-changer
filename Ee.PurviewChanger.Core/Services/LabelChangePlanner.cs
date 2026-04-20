using Ee.PurviewChanger.Core.Models;

namespace Ee.PurviewChanger.Core.Services;

public sealed class LabelChangePlanner
{
    public LabelChangePreview CreatePreview(FileInspectionResult inspection, LabelDefinition? targetLabel, bool validationModeEnabled)
    {
        ArgumentNullException.ThrowIfNull(inspection);

        if (targetLabel is null)
        {
            return new LabelChangePreview(
                inspection.FilePath,
                inspection.CurrentLabel,
                string.Empty,
                false,
                ResolveExecutionMode(validationModeEnabled),
                "대상 라벨을 선택해야 합니다.",
                "라벨을 선택하세요.");
        }

        if (!inspection.CanPreviewChange)
        {
            return new LabelChangePreview(
                inspection.FilePath,
                inspection.CurrentLabel,
                targetLabel.Name,
                false,
                ResolveExecutionMode(validationModeEnabled),
                inspection.CapabilitySummary,
                inspection.RequiresMipSdk
                    ? "실서비스 모드에서는 MIP SDK 연동 전까지 적용할 수 없습니다."
                    : "현재 파일 상태를 먼저 확인해야 합니다.");
        }

        if (string.Equals(inspection.CurrentLabel, targetLabel.Name, StringComparison.OrdinalIgnoreCase))
        {
            return new LabelChangePreview(
                inspection.FilePath,
                inspection.CurrentLabel,
                targetLabel.Name,
                false,
                ResolveExecutionMode(validationModeEnabled),
                "현재 라벨과 대상 라벨이 같습니다.",
                "다른 라벨을 선택하세요.");
        }

        return new LabelChangePreview(
            inspection.FilePath,
            inspection.CurrentLabel,
            targetLabel.Name,
            true,
            ResolveExecutionMode(validationModeEnabled),
            $"'{inspection.CurrentLabel}'에서 '{targetLabel.Name}'로 변경할 준비가 되었습니다.",
            null);
    }

    private static string ResolveExecutionMode(bool validationModeEnabled) =>
        validationModeEnabled ? "Validation mode" : "Live mode";
}
