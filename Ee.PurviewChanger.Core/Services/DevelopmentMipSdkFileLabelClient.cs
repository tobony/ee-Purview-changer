using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ee.PurviewChanger.Core.Models;

namespace Ee.PurviewChanger.Core.Services;

public sealed class DevelopmentMipSdkFileLabelClient : IMipSdkFileLabelClient
{
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
            return CreateUnavailableState("appsettings.json에서 mipSdk.enabled=true 로 설정해야 Live mode를 사용할 수 있습니다.");
        }

        if (!options.MipSdk.DevelopmentFallbackEnabled)
        {
            return CreateUnavailableState("현재 저장소에는 실제 Microsoft Information Protection SDK 연결 코드가 아직 포함되지 않았습니다.");
        }

        var metadata = LoadMetadata(filePath, options);
        var currentLabel = metadata?.CurrentLabel ?? options.MipSdk.DevelopmentDefaultLabel;

        return new MipSdkFileLabelState(
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
                false,
                state.ProviderName,
                state.Summary,
                null,
                state.TechnicalDetails);
        }

        Directory.CreateDirectory(Path.GetFullPath(options.MipSdk.DevelopmentMetadataDirectory));
        var metadata = new DevelopmentMipSdkLabelMetadata(
            filePath,
            targetLabel,
            actor,
            DateTimeOffset.UtcNow);

        await using (var stream = File.Create(GetMetadataPath(filePath, options)))
        {
            await JsonSerializer.SerializeAsync(stream, metadata, JsonSerializerOptions, cancellationToken);
        }

        var rechecked = LoadMetadata(filePath, options)?.CurrentLabel;

        return new MipSdkLabelChangeOutcome(
            true,
            true,
            state.ProviderName,
            $"Live mode에서 '{targetLabel}' 라벨을 기록했습니다.",
            rechecked,
            "개발용 메타데이터 저장소 기준으로 재조회했습니다.");
    }

    private static MipSdkFileLabelState CreateUnavailableState(string message) =>
        new(
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
        var fileName = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(filePath))));
        return Path.Combine(targetDirectory, $"{fileName}.json");
    }

    private sealed record DevelopmentMipSdkLabelMetadata(
        string FilePath,
        string CurrentLabel,
        string UpdatedBy,
        DateTimeOffset UpdatedAt);
}
