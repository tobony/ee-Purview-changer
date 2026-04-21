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
            Assert.AreEqual(FileInspectionStatus.Ready, result.Status);
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
            Assert.AreEqual(LabelChangeStatus.Applied, result.Status);
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

    [TestMethod]
    public void Inspect_returns_mip_disabled_status_when_live_mode_sdk_is_off()
    {
        var options = new PurviewAppOptions
        {
            ValidationMode = new ValidationModeOptions { Enabled = false },
            MipSdk = new MipSdkOptions
            {
                Enabled = false,
                DevelopmentFallbackEnabled = true
            }
        };
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.docx");
        File.WriteAllText(filePath, "content");
        var service = new MipSdkFileInspectionService(new DevelopmentMipSdkFileLabelClient());

        try
        {
            var result = service.Inspect(filePath, options, "tester@example.com");

            Assert.AreEqual(FileInspectionStatus.MipSdkDisabled, result.Status);
            Assert.IsFalse(result.CanPreviewChange);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [TestMethod]
    public async Task ApplyAsync_returns_same_label_status_when_target_matches_current()
    {
        var client = new StubMipSdkFileLabelClient(
            inspectState: new MipSdkFileLabelState(
                FileInspectionStatus.Ready,
                true,
                true,
                true,
                "General",
                "stub",
                "ready",
                "ready",
                null,
                Array.Empty<string>()),
            applyOutcome: null);
        var service = new MipSdkLabelChangeService(client, new AuditLogService());
        var options = new PurviewAppOptions { AuditLogDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")) };
        var preview = new LabelChangePreview("C:/temp/file.docx", "General", "General", true, "Live mode", "ready", null);

        try
        {
            var result = await service.ApplyAsync(preview, options, "tester@example.com");

            Assert.IsFalse(result.Success);
            Assert.AreEqual(LabelChangeStatus.SameLabel, result.Status);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.AuditLogPath));
        }
        finally
        {
            if (Directory.Exists(options.AuditLogDirectory))
            {
                Directory.Delete(options.AuditLogDirectory, true);
            }
        }
    }

    [TestMethod]
    public async Task ApplyAsync_returns_recheck_failed_status_when_client_reports_recheck_failure()
    {
        var client = new StubMipSdkFileLabelClient(
            inspectState: new MipSdkFileLabelState(
                FileInspectionStatus.Ready,
                true,
                true,
                true,
                "General",
                "stub",
                "ready",
                "ready",
                null,
                Array.Empty<string>()),
            applyOutcome: new MipSdkLabelChangeOutcome(
                false,
                LabelChangeStatus.RecheckFailed,
                true,
                "stub",
                "라벨 변경 후 재조회에 실패했습니다.",
                null,
                "simulated failure"));
        var service = new MipSdkLabelChangeService(client, new AuditLogService());
        var options = new PurviewAppOptions { AuditLogDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")) };
        var preview = new LabelChangePreview("C:/temp/file.docx", "General", "Confidential", true, "Live mode", "ready", null);

        try
        {
            var result = await service.ApplyAsync(preview, options, "tester@example.com");

            Assert.IsFalse(result.Success);
            Assert.AreEqual(LabelChangeStatus.RecheckFailed, result.Status);
            Assert.IsTrue(result.AppliedToSourceFile);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.AuditLogPath));
        }
        finally
        {
            if (Directory.Exists(options.AuditLogDirectory))
            {
                Directory.Delete(options.AuditLogDirectory, true);
            }
        }
    }

    private sealed class StubMipSdkFileLabelClient(
        MipSdkFileLabelState inspectState,
        MipSdkLabelChangeOutcome? applyOutcome)
        : IMipSdkFileLabelClient
    {
        public MipSdkFileLabelState Inspect(string filePath, PurviewAppOptions options, string actor) => inspectState;

        public Task<MipSdkLabelChangeOutcome> ApplyAsync(
            string filePath,
            string currentLabel,
            string targetLabel,
            PurviewAppOptions options,
            string actor,
            CancellationToken cancellationToken = default)
        {
            if (applyOutcome is not null)
            {
                return Task.FromResult(applyOutcome);
            }

            return Task.FromResult(new MipSdkLabelChangeOutcome(
                false,
                LabelChangeStatus.SameLabel,
                false,
                "stub",
                "현재 라벨과 대상 라벨이 같아서 재적용하지 않았습니다.",
                currentLabel,
                "stub"));
        }
    }
}
