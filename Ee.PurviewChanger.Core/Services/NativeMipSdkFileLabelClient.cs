using Ee.PurviewChanger.Core.Models;

namespace Ee.PurviewChanger.Core.Services;

public sealed class NativeMipSdkFileLabelClient : IMipSdkFileLabelClient
{
    private const string PlaceholderValuePrefix = "YOUR-";
    private readonly IMipSdkNativeBridge _nativeBridge;
    private readonly Func<bool> _isWindowsChecker;

    public NativeMipSdkFileLabelClient(IMipSdkNativeBridge nativeBridge)
        : this(nativeBridge, OperatingSystem.IsWindows)
    {
    }

    public NativeMipSdkFileLabelClient(IMipSdkNativeBridge nativeBridge, Func<bool> isWindowsChecker)
    {
        _nativeBridge = nativeBridge;
        _isWindowsChecker = isWindowsChecker;
    }

    public MipSdkFileLabelState Inspect(string filePath, PurviewAppOptions options, string actor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(actor);

        var guard = ValidateLiveMode(options, _isWindowsChecker);

        if (guard is not null)
        {
            return guard;
        }

        try
        {
            var response = _nativeBridge.Inspect(new NativeMipSdkInspectRequest(
                Path.GetFullPath(filePath),
                options.MipSdk.ApplicationId,
                actor));

            return new MipSdkFileLabelState(
                response.CanInspect ? FileInspectionStatus.Ready : FileInspectionStatus.InspectionFailed,
                response.CanInspect,
                response.CanApply,
                response.CurrentLabelKnown,
                response.CurrentLabel,
                response.ProviderName,
                response.Summary,
                response.CapabilitySummary,
                response.TechnicalDetails,
                response.Messages);
        }
        catch (Exception exception)
        {
            var status = IsRuntimeBindingException(exception)
                ? FileInspectionStatus.MipSdkUnavailable
                : FileInspectionStatus.InspectionFailed;

            return new MipSdkFileLabelState(
                status,
                false,
                false,
                false,
                "Unknown",
                "Microsoft Information Protection SDK",
                "현재 라벨 조회에 실패했습니다.",
                "MIP SDK 네이티브 바인딩 응답을 확인하세요.",
                exception.Message,
                ["MIP SDK 네이티브 바인딩 호출 중 오류가 발생했습니다."]);
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
        ArgumentException.ThrowIfNullOrWhiteSpace(actor);

        var inspectState = Inspect(filePath, options, actor);

        if (!inspectState.CanApply)
        {
            return new MipSdkLabelChangeOutcome(
                false,
                MapUnavailableStatus(inspectState.Status),
                false,
                inspectState.ProviderName,
                inspectState.Summary,
                null,
                inspectState.TechnicalDetails);
        }

        if (string.Equals(currentLabel, targetLabel, StringComparison.OrdinalIgnoreCase))
        {
            return new MipSdkLabelChangeOutcome(
                false,
                LabelChangeStatus.SameLabel,
                false,
                inspectState.ProviderName,
                "현재 라벨과 대상 라벨이 같아서 재적용하지 않았습니다.",
                currentLabel,
                "다른 라벨을 선택한 뒤 다시 시도하세요.");
        }

        try
        {
            var response = await _nativeBridge.ApplyAsync(
                new NativeMipSdkApplyRequest(
                    Path.GetFullPath(filePath),
                    currentLabel,
                    targetLabel,
                    options.MipSdk.ApplicationId,
                    actor),
                cancellationToken);

            return new MipSdkLabelChangeOutcome(
                response.Success,
                response.Status,
                response.AppliedToSourceFile,
                response.ProviderName,
                response.Message,
                response.RecheckedLabel,
                response.TechnicalDetails);
        }
        catch (Exception exception)
        {
            return new MipSdkLabelChangeOutcome(
                false,
                IsRuntimeBindingException(exception)
                    ? LabelChangeStatus.MipSdkUnavailable
                    : LabelChangeStatus.Blocked,
                false,
                inspectState.ProviderName,
                "라벨 변경 요청을 적용하지 못했습니다.",
                null,
                exception.Message);
        }
    }

    private static MipSdkFileLabelState? ValidateLiveMode(PurviewAppOptions options, Func<bool> isWindowsChecker)
    {
        if (!options.MipSdk.Enabled)
        {
            return CreateUnavailableState(
                FileInspectionStatus.MipSdkDisabled,
                "appsettings.json에서 mipSdk.enabled=true 로 설정해야 Live mode를 사용할 수 있습니다.");
        }

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

        if (!isWindowsChecker())
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

        return null;
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
            "실제 SDK 연결 구성을 확인하세요.",
            message,
            [message]);

    private static LabelChangeStatus MapUnavailableStatus(FileInspectionStatus status) =>
        status switch
        {
            FileInspectionStatus.MipSdkDisabled => LabelChangeStatus.MipSdkUnavailable,
            FileInspectionStatus.MipSdkConfigurationIncomplete => LabelChangeStatus.MipSdkUnavailable,
            FileInspectionStatus.MipSdkUnavailable => LabelChangeStatus.MipSdkUnavailable,
            _ => LabelChangeStatus.Blocked
        };

    private static bool IsRuntimeBindingException(Exception exception) =>
        exception is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException;
}
