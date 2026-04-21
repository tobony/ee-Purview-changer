using Ee.PurviewChanger.Core.Models;

namespace Ee.PurviewChanger.Core.Services;

public interface IMipSdkNativeBridge
{
    NativeMipSdkInspectResponse Inspect(NativeMipSdkInspectRequest request);

    Task<NativeMipSdkApplyResponse> ApplyAsync(
        NativeMipSdkApplyRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record NativeMipSdkInspectRequest(
    string FilePath,
    string ApplicationId,
    string Actor);

public sealed record NativeMipSdkInspectResponse(
    bool CanInspect,
    bool CanApply,
    bool CurrentLabelKnown,
    string CurrentLabel,
    string ProviderName,
    string Summary,
    string CapabilitySummary,
    string? TechnicalDetails,
    IReadOnlyList<string> Messages);

public sealed record NativeMipSdkApplyRequest(
    string FilePath,
    string CurrentLabel,
    string TargetLabel,
    string ApplicationId,
    string Actor);

public sealed record NativeMipSdkApplyResponse(
    bool Success,
    LabelChangeStatus Status,
    bool AppliedToSourceFile,
    string ProviderName,
    string Message,
    string? RecheckedLabel,
    string? TechnicalDetails);
