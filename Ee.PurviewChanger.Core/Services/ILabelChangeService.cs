using Ee.PurviewChanger.Core.Models;

namespace Ee.PurviewChanger.Core.Services;

public interface ILabelChangeService
{
    Task<LabelChangeResult> ApplyAsync(
        LabelChangePreview preview,
        PurviewAppOptions options,
        string actor,
        CancellationToken cancellationToken = default);
}
