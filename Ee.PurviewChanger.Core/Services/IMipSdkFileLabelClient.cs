using Ee.PurviewChanger.Core.Models;

namespace Ee.PurviewChanger.Core.Services;

public interface IMipSdkFileLabelClient
{
    MipSdkFileLabelState Inspect(string filePath, PurviewAppOptions options, string actor);

    Task<MipSdkLabelChangeOutcome> ApplyAsync(
        string filePath,
        string currentLabel,
        string targetLabel,
        PurviewAppOptions options,
        string actor,
        CancellationToken cancellationToken = default);
}

public sealed record MipSdkFileLabelState(
    bool CanInspect,
    bool CanApply,
    bool CurrentLabelKnown,
    string CurrentLabel,
    string ProviderName,
    string Summary,
    string CapabilitySummary,
    string? TechnicalDetails,
    IReadOnlyList<string> Messages);

public sealed record MipSdkLabelChangeOutcome(
    bool Success,
    bool AppliedToSourceFile,
    string ProviderName,
    string Message,
    string? RecheckedLabel,
    string? TechnicalDetails);
