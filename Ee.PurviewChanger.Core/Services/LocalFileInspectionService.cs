using Ee.PurviewChanger.Core.Models;

namespace Ee.PurviewChanger.Core.Services;

public sealed class LocalFileInspectionService
    : IFileInspectionService
{
    public FileInspectionResult Inspect(string filePath, PurviewAppOptions options, string actor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(options);

        var normalizedPath = Path.GetFullPath(filePath.Trim());
        var fileExists = File.Exists(normalizedPath);
        var extension = Path.GetExtension(normalizedPath);
        var isSupportedType = options.SupportedFileExtensions.Any(
            supported => string.Equals(supported, extension, StringComparison.OrdinalIgnoreCase));
        var messages = new List<string>();

        if (!fileExists)
        {
            messages.Add("선택한 파일이 존재하지 않습니다.");

            return new FileInspectionResult(
                normalizedPath,
                false,
                extension,
                false,
                false,
                "Unknown",
                false,
                false,
                FileInspectionStatus.FileNotFound,
                "Validation mode",
                "Validation mode",
                "파일을 찾을 수 없습니다.",
                "경로를 다시 확인하세요.",
                null,
                messages);
        }

        if (!isSupportedType)
        {
            messages.Add("현재 MVP에서 허용한 파일 형식이 아닙니다.");

            return new FileInspectionResult(
                normalizedPath,
                true,
                extension,
                false,
                false,
                "Unknown",
                false,
                false,
                FileInspectionStatus.UnsupportedFileType,
                "Validation mode",
                "Validation mode",
                "지원되지 않는 파일 형식입니다.",
                $"지원 형식: {string.Join(", ", options.SupportedFileExtensions)}",
                null,
                messages);
        }

        if (options.ValidationMode.Enabled)
        {
            messages.Add("검증 모드에서 시뮬레이션된 현재 라벨을 사용합니다.");

            return new FileInspectionResult(
                normalizedPath,
                true,
                extension,
                true,
                true,
                options.ValidationMode.SimulatedCurrentLabel,
                true,
                false,
                FileInspectionStatus.ValidationModeSimulated,
                "Validation mode",
                "Validation mode",
                $"현재 라벨(검증 모드): {options.ValidationMode.SimulatedCurrentLabel}",
                "실서비스 연결 전 UI/흐름 검증을 위한 모드입니다.",
                "실제 Purview 라벨 변경은 수행하지 않습니다.",
                messages);
        }

        messages.Add("실서비스 모드에서는 Microsoft Information Protection SDK가 필요합니다.");

        return new FileInspectionResult(
            normalizedPath,
            true,
            extension,
            true,
            false,
            "Unknown",
            false,
            true,
            FileInspectionStatus.MipSdkUnavailable,
            "Live mode",
            "Microsoft Information Protection SDK",
            "현재 라벨을 아직 조회할 수 없습니다.",
            "실제 라벨 조회/변경은 Windows에서 MIP SDK 연동 후 활성화됩니다.",
            "현재 구현에서는 Validation mode가 기본입니다.",
            messages);
    }
}
