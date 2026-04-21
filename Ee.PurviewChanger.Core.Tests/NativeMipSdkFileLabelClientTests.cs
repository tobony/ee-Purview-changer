using Ee.PurviewChanger.Core.Models;
using Ee.PurviewChanger.Core.Services;

namespace Ee.PurviewChanger.Core.Tests;

[TestClass]
public sealed class NativeMipSdkFileLabelClientTests
{
    [TestMethod]
    public void Inspect_returns_configuration_incomplete_when_application_id_is_placeholder()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.docx");
        File.WriteAllText(filePath, "content");
        var options = new PurviewAppOptions
        {
            MipSdk = new MipSdkOptions
            {
                Enabled = true,
                DevelopmentFallbackEnabled = false,
                ApplicationId = "YOUR-MIP-SDK-APP-ID",
                NativeLibraryPath = filePath
            }
        };
        var client = new NativeMipSdkFileLabelClient(new StubNativeBridge());

        try
        {
            var state = client.Inspect(filePath, options, "tester@example.com");

            Assert.AreEqual(FileInspectionStatus.MipSdkConfigurationIncomplete, state.Status);
            Assert.IsFalse(state.CanInspect);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [TestMethod]
    public async Task ApplyAsync_returns_bridge_result_when_native_bridge_succeeds()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        var filePath = Path.Combine(tempDirectory, "mip-native.dll");
        File.WriteAllText(filePath, "placeholder");
        var options = new PurviewAppOptions
        {
            MipSdk = new MipSdkOptions
            {
                Enabled = true,
                DevelopmentFallbackEnabled = false,
                ApplicationId = "app-id",
                NativeLibraryPath = filePath
            }
        };
        var client = new NativeMipSdkFileLabelClient(
            new StubNativeBridge(
                inspectResponse: new NativeMipSdkInspectResponse(
                    true,
                    true,
                    true,
                    "General",
                    "stub native",
                    "ready",
                    "ready",
                    null,
                    Array.Empty<string>()),
                applyResponse: new NativeMipSdkApplyResponse(
                    true,
                    LabelChangeStatus.Applied,
                    true,
                    "stub native",
                    "applied",
                    "Confidential",
                    null)),
            () => true);

        try
        {
            var outcome = await client.ApplyAsync(
                filePath,
                "General",
                "Confidential",
                options,
                "tester@example.com");

            Assert.IsTrue(outcome.Success);
            Assert.AreEqual(LabelChangeStatus.Applied, outcome.Status);
            Assert.AreEqual("Confidential", outcome.RecheckedLabel);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }

    private sealed class StubNativeBridge(
        NativeMipSdkInspectResponse? inspectResponse = null,
        NativeMipSdkApplyResponse? applyResponse = null)
        : IMipSdkNativeBridge
    {
        public NativeMipSdkInspectResponse Inspect(NativeMipSdkInspectRequest request) =>
            inspectResponse ?? new NativeMipSdkInspectResponse(
                true,
                true,
                true,
                "General",
                "stub native",
                "ready",
                "ready",
                null,
                Array.Empty<string>());

        public Task<NativeMipSdkApplyResponse> ApplyAsync(
            NativeMipSdkApplyRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(applyResponse ?? new NativeMipSdkApplyResponse(
                true,
                LabelChangeStatus.Applied,
                true,
                "stub native",
                "applied",
                request.TargetLabel,
                null));
    }
}
