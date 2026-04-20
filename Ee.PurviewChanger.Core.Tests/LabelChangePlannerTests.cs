using Ee.PurviewChanger.Core.Models;
using Ee.PurviewChanger.Core.Services;

namespace Ee.PurviewChanger.Core.Tests;

[TestClass]
public sealed class LabelChangePlannerTests
{
    private readonly LabelChangePlanner _planner = new();

    [TestMethod]
    public void CreatePreview_blocks_when_target_label_matches_current_label()
    {
        var inspection = new FileInspectionResult(
            "C:/temp/file.docx",
            true,
            ".docx",
            true,
            true,
            "Confidential",
            true,
            false,
            "ok",
            "ok",
            Array.Empty<string>());
        var label = new LabelDefinition { Id = "confidential", Name = "Confidential" };

        var preview = _planner.CreatePreview(inspection, label, true);

        Assert.IsFalse(preview.CanApply);
        StringAssert.Contains(preview.Summary, "같습니다");
    }

    [TestMethod]
    public void CreatePreview_allows_change_for_different_label()
    {
        var inspection = new FileInspectionResult(
            "C:/temp/file.docx",
            true,
            ".docx",
            true,
            true,
            "General",
            true,
            false,
            "ok",
            "ok",
            Array.Empty<string>());
        var label = new LabelDefinition { Id = "confidential", Name = "Confidential" };

        var preview = _planner.CreatePreview(inspection, label, true);

        Assert.IsTrue(preview.CanApply);
        StringAssert.Contains(preview.Summary, "Confidential");
    }
}
