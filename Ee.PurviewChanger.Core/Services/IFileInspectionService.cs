using Ee.PurviewChanger.Core.Models;

namespace Ee.PurviewChanger.Core.Services;

public interface IFileInspectionService
{
    FileInspectionResult Inspect(string filePath, PurviewAppOptions options, string actor);
}
