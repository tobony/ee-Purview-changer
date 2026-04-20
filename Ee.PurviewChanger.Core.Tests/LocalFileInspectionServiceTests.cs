using Ee.PurviewChanger.Core.Models;
using Ee.PurviewChanger.Core.Services;

namespace Ee.PurviewChanger.Core.Tests;

[TestClass]
public sealed class LocalFileInspectionServiceTests
{
    private readonly LocalFileInspectionService _service = new();

    [TestMethod]
    public void Inspect_returns_missing_file_result_when_file_does_not_exist()
    {
        var options = new PurviewAppOptions();
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.docx");

        var result = _service.Inspect(filePath, options, "ValidationModeUser");

        Assert.IsFalse(result.FileExists);
        Assert.IsFalse(result.CanPreviewChange);
        Assert.AreEqual(FileInspectionStatus.FileNotFound, result.Status);
        Assert.AreEqual("파일을 찾을 수 없습니다.", result.CurrentStateSummary);
    }

    [TestMethod]
    public void Inspect_returns_validation_mode_label_for_supported_file()
    {
        var options = new PurviewAppOptions();
        var filePath = Path.GetTempFileName() + ".docx";
        File.WriteAllText(filePath, "content");

        try
        {
            var result = _service.Inspect(filePath, options, "ValidationModeUser");

            Assert.IsTrue(result.FileExists);
            Assert.IsTrue(result.IsSupportedFileType);
            Assert.IsTrue(result.CurrentLabelKnown);
            Assert.AreEqual(options.ValidationMode.SimulatedCurrentLabel, result.CurrentLabel);
            Assert.AreEqual("Validation mode", result.ExecutionMode);
            Assert.AreEqual(FileInspectionStatus.ValidationModeSimulated, result.Status);
        }
        finally
        {
            File.Delete(filePath);
        }
    }
}
