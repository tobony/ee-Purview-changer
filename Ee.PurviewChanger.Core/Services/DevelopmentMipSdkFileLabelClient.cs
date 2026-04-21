using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ee.PurviewChanger.Core.Models;

namespace Ee.PurviewChanger.Core.Services;

public sealed class DevelopmentMipSdkFileLabelClient : IMipSdkFileLabelClient
{
    private const string PlaceholderValuePrefix = "YOUR-";

    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public MipSdkFileLabelState Inspect(string filePath, PurviewAppOptions options, string actor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(options);

        if (!options.MipSdk.Enabled)
        {
            return CreateUnavailableState(
                FileInspectionStatus.MipSdkDisabled,
                "appsettings.json에서 mipSdk.enabled=true 로 설정해야 Live mode를 사용할 수 있습니다.");
        }

        if (!options.MipSdk.DevelopmentFallbackEnabled)
        {
            if (string.IsNullOrWhiteSpace(options.MipSdk.ApplicationId) ||
                options.MipSdk.ApplicationId.Contains(PlaceholderValuePrefix, StringComparison.OrdinalIgnoreCase))
            {
                return CreateUnavailableState(
                    FileInspectionStatus.MipSdkConfigurationIncomplete,
                    "mipSdk.applicationId 값을 먼저 설정해야 실제 MIP SDK 연결을 점검할 수 있습니다.");
            }

            if (string.IsNullOrWhiteSpace(options.MipSdk.NativeLibraryPath))
            {
                return CreateUnavailableState(
                    FileInspectionStatus.MipSdkConfigurationIncomplete,
                    "mipSdk.nativeLibraryPath 값을 설정해야 실제 MIP SDK 런타임을 확인할 수 있습니다.");
            }

            if (!OperatingSystem.IsWindows())
            {
                return CreateUnavailableState(
                    FileInspectionStatus.MipSdkUnavailable,
                    "실제 MIP SDK 연결은 현재 Windows 환경에서만 지원됩니다.");
            }

            if (!File.Exists(Path.GetFullPath(options.MipSdk.NativeLibraryPath)))
            {
                return CreateUnavailableState(
                    FileInspectionStatus.MipSdkUnavailable,
                    $"MIP SDK 네이티브 라이브러리를 찾을 수 없습니다: {Path.GetFullPath(options.MipSdk.NativeLibraryPath)}");
            }

            return CreateUnavailableState(
                FileInspectionStatus.MipSdkUnavailable,
                "현재 저장소에는 실제 Microsoft Information Protection SDK 연결 코드가 아직 포함되지 않았습니다.");
        }

        try
        {
            var metadata = LoadMetadata(filePath, options);
            var currentLabel = metadata?.CurrentLabel ?? options.MipSdk.DevelopmentDefaultLabel;

            return new MipSdkFileLabelState(
                FileInspectionStatus.Ready,
                true,
                true,
                true,
                currentLabel,
                "MIP SDK development fallback",
                $"현재 라벨(Live mode): {currentLabel}",
                "개발 중에는 MIP SDK 대체 메타데이터 저장소를 사용해 조회/변경 흐름을 검증합니다.",
                string.IsNullOrWhiteSpace(options.MipSdk.ApplicationId)
                    ? "실제 SDK 전환 전에는 ApplicationId와 NativeLibraryPath를 채워 두는 것을 권장합니다."
                    : null,
                [
                    "실제 Microsoft Purview 라벨 대신 개발용 메타데이터 저장소를 사용했습니다.",
                    $"메타데이터 저장소: {Path.GetFullPath(options.MipSdk.DevelopmentMetadataDirectory)}"
                ]);
        }
        catch (Exception exception)
        {
            return new MipSdkFileLabelState(
                FileInspectionStatus.InspectionFailed,
                false,
                false,
                false,
                "Unknown",
                "MIP SDK development fallback",
                "현재 라벨 조회에 실패했습니다.",
                "메타데이터 저장소 접근 권한 또는 경로 구성을 확인하세요.",
                exception.Message,
                ["Live mode용 현재 라벨 조회에 실패했습니다."]);
        }
    }

    public async Task<MipSdkLabelChangeOutcome> ApplyAsync(
        string filePath,
        string currentLabel,
        string targetLabel,
        PurviewAppOptions options,
        string actor,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetLabel);
        ArgumentNullException.ThrowIfNull(options);

        var state = Inspect(filePath, options, actor);

        if (!state.CanApply)
        {
            return new MipSdkLabelChangeOutcome(
                false,
                state.Status switch
                {
                    FileInspectionStatus.MipSdkDisabled => LabelChangeStatus.MipSdkUnavailable,
                    FileInspectionStatus.MipSdkConfigurationIncomplete => LabelChangeStatus.MipSdkUnavailable,
                    FileInspectionStatus.MipSdkUnavailable => LabelChangeStatus.MipSdkUnavailable,
                    _ => LabelChangeStatus.Blocked
                },
                false,
                state.ProviderName,
                state.Summary,
                null,
                state.TechnicalDetails);
        }

        if (string.Equals(currentLabel, targetLabel, StringComparison.OrdinalIgnoreCase))
        {
            return new MipSdkLabelChangeOutcome(
                false,
                LabelChangeStatus.SameLabel,
                false,
                state.ProviderName,
                "현재 라벨과 대상 라벨이 같아서 재적용하지 않았습니다.",
                currentLabel,
                "다른 라벨을 선택한 뒤 다시 시도하세요.");
        }

        try
        {
            Directory.CreateDirectory(Path.GetFullPath(options.MipSdk.DevelopmentMetadataDirectory));
            var metadata = new DevelopmentMipSdkLabelMetadata(
                filePath,
                targetLabel,
                actor,
                DateTimeOffset.UtcNow);

            await using (var stream = new FileStream(
                             GetMetadataPath(filePath, options),
                             FileMode.Create,
                             FileAccess.Write,
                             FileShare.None,
                             4096,
                             useAsync: true))
            {
                await JsonSerializer.SerializeAsync(stream, metadata, JsonSerializerOptions, cancellationToken);
            }
        }
        catch (Exception exception)
        {
            return new MipSdkLabelChangeOutcome(
                false,
                LabelChangeStatus.WriteFailed,
                false,
                state.ProviderName,
                "라벨 변경 내용을 저장하지 못했습니다.",
                null,
                exception.Message);
        }

        try
        {
            var rechecked = LoadMetadata(filePath, options)?.CurrentLabel;

            if (!string.Equals(rechecked, targetLabel, StringComparison.OrdinalIgnoreCase))
            {
                return new MipSdkLabelChangeOutcome(
                    false,
                    LabelChangeStatus.RecheckFailed,
                    true,
                    state.ProviderName,
                    "라벨 변경 후 재조회 결과가 예상과 다릅니다.",
                    rechecked,
                    "변경은 기록되었지만 재조회 검증에 실패했습니다.");
            }

            return new MipSdkLabelChangeOutcome(
                true,
                LabelChangeStatus.Applied,
                true,
                state.ProviderName,
                $"Live mode에서 '{targetLabel}' 라벨을 기록했습니다.",
                rechecked,
                "개발용 메타데이터 저장소 기준으로 재조회했습니다.");
        }
        catch (Exception exception)
        {
            return new MipSdkLabelChangeOutcome(
                false,
                LabelChangeStatus.RecheckFailed,
                true,
                state.ProviderName,
                "라벨 변경 후 재조회에 실패했습니다.",
                null,
                exception.Message);
        }
    }

    private static MipSdkFileLabelState CreateUnavailableState(FileInspectionStatus status, string message) =>
        new(
            status,
            false,
            false,
            false,
            "Unknown",
            "Microsoft Information Protection SDK",
            "현재 라벨을 조회할 수 없습니다.",
            "실제 SDK 연결 또는 개발용 폴백 설정이 필요합니다.",
            message,
            [message]);

    private static DevelopmentMipSdkLabelMetadata? LoadMetadata(string filePath, PurviewAppOptions options)
    {
        var metadataPath = GetMetadataPath(filePath, options);

        if (!File.Exists(metadataPath))
        {
            return null;
        }

        var json = File.ReadAllText(metadataPath);
        return JsonSerializer.Deserialize<DevelopmentMipSdkLabelMetadata>(json, JsonSerializerOptions);
    }

    private static string GetMetadataPath(string filePath, PurviewAppOptions options)
    {
        var targetDirectory = Path.GetFullPath(options.MipSdk.DevelopmentMetadataDirectory);
        var fileName = CreateMetadataFileNameFromPath(filePath);
        return Path.Combine(targetDirectory, $"{fileName}.json");
    }

    private static string CreateMetadataFileNameFromPath(string filePath) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(filePath))));

    private sealed record DevelopmentMipSdkLabelMetadata(
        string FilePath,
        string CurrentLabel,
        string UpdatedBy,
        DateTimeOffset UpdatedAt);
}
