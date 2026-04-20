using Ee.PurviewChanger.Core.Models;

namespace Ee.PurviewChanger.Core.Services;

public sealed class MipSdkFileInspectionService(IMipSdkFileLabelClient client)
    : IFileInspectionService
{
    public FileInspectionResult Inspect(string filePath, PurviewAppOptions options, string actor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(actor);

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
                "Live mode",
                "Microsoft Information Protection SDK",
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
                "Live mode",
                "Microsoft Information Protection SDK",
                "지원되지 않는 파일 형식입니다.",
                $"지원 형식: {string.Join(", ", options.SupportedFileExtensions)}",
                null,
                messages);
        }

        var state = client.Inspect(normalizedPath, options, actor);
        messages.AddRange(state.Messages);

        return new FileInspectionResult(
            normalizedPath,
            true,
            extension,
            true,
            state.CurrentLabelKnown,
            state.CurrentLabel,
            state.CanInspect && state.CanApply && state.CurrentLabelKnown,
            !state.CanApply,
            state.Status,
            "Live mode",
            state.ProviderName,
            state.Summary,
            state.CapabilitySummary,
            state.TechnicalDetails,
            messages);
    }
}
