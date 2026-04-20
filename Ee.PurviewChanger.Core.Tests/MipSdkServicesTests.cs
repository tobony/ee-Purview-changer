using Ee.PurviewChanger.Core.Models;
using Ee.PurviewChanger.Core.Services;

namespace Ee.PurviewChanger.Core.Tests;

[TestClass]
public sealed class MipSdkServicesTests
{
    [TestMethod]
    public void Inspect_returns_known_label_when_development_fallback_is_enabled()
    {
        var metadataDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var options = new PurviewAppOptions
        {
            ValidationMode = new ValidationModeOptions { Enabled = false },
            MipSdk = new MipSdkOptions
            {
                Enabled = true,
                DevelopmentFallbackEnabled = true,
                DevelopmentMetadataDirectory = metadataDirectory,
                DevelopmentDefaultLabel = "General"
            }
        };
        var filePath = Path.Combine(metadataDirectory, "sample.docx");
        Directory.CreateDirectory(metadataDirectory);
        File.WriteAllText(filePath, "content");
        var service = new MipSdkFileInspectionService(new DevelopmentMipSdkFileLabelClient());

        try
        {
            var result = service.Inspect(filePath, options, "tester@example.com");

            Assert.IsTrue(result.CurrentLabelKnown);
            Assert.AreEqual("General", result.CurrentLabel);
            Assert.AreEqual("Live mode", result.ExecutionMode);
            Assert.AreEqual("MIP SDK development fallback", result.ProviderName);
        }
        finally
        {
            Directory.Delete(metadataDirectory, true);
        }
    }

    [TestMethod]
    public async Task ApplyAsync_records_live_mode_result_and_rechecks_label()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var filePath = Path.Combine(rootDirectory, "sample.docx");
        var options = new PurviewAppOptions
        {
            ValidationMode = new ValidationModeOptions { Enabled = false },
            AuditLogDirectory = Path.Combine(rootDirectory, "audit"),
            MipSdk = new MipSdkOptions
            {
                Enabled = true,
                DevelopmentFallbackEnabled = true,
                DevelopmentMetadataDirectory = Path.Combine(rootDirectory, "metadata"),
                DevelopmentDefaultLabel = "General"
            }
        };

        Directory.CreateDirectory(rootDirectory);
        File.WriteAllText(filePath, "content");

        var preview = new LabelChangePreview(
            filePath,
            "General",
            "Confidential",
            true,
            "Live mode",
            "ready",
            null);
        var service = new MipSdkLabelChangeService(new DevelopmentMipSdkFileLabelClient(), new AuditLogService());

        try
        {
            var result = await service.ApplyAsync(preview, options, "tester@example.com");

            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.AppliedToSourceFile);
            Assert.AreEqual("Confidential", result.RecheckedLabel);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.AuditLogPath));
            Assert.IsTrue(File.Exists(result.AuditLogPath));
        }
        finally
        {
            Directory.Delete(rootDirectory, true);
        }
    }
}
