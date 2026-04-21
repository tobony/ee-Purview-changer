using System.Runtime.InteropServices;
using System.Text.Json;
using Ee.PurviewChanger.Core.Models;

namespace Ee.PurviewChanger.Core.Services;

public sealed class NativeLibraryMipSdkNativeBridge(string nativeLibraryPath)
    : IMipSdkNativeBridge
{
    private const string InspectExportName = "EePurviewInspectLabelUtf8";
    private const string ApplyExportName = "EePurviewApplyLabelUtf8";
    private const string FreeBufferExportName = "EePurviewFreeUtf8Buffer";

    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly string _nativeLibraryPath = Path.GetFullPath(nativeLibraryPath);

    public NativeMipSdkInspectResponse Inspect(NativeMipSdkInspectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var nativeLibrary = LoadNativeLibrary();
        var inspect = ResolveUtf8JsonFunction(nativeLibrary.Handle, InspectExportName);
        var freeBuffer = ResolveFreeFunction(nativeLibrary.Handle);
        var payload = JsonSerializer.Serialize(request, JsonSerializerOptions);
        var outputBuffer = IntPtr.Zero;

        try
        {
            outputBuffer = inspect(payload);
            var rawJson = Marshal.PtrToStringUTF8(outputBuffer)
                ?? throw new InvalidOperationException("MIP SDK inspect 응답이 비어 있습니다.");
            var response = JsonSerializer.Deserialize<NativeInspectBridgeResponse>(rawJson, JsonSerializerOptions)
                ?? throw new InvalidOperationException("MIP SDK inspect 응답을 역직렬화하지 못했습니다.");

            return new NativeMipSdkInspectResponse(
                response.CanInspect,
                response.CanApply,
                response.CurrentLabelKnown,
                string.IsNullOrWhiteSpace(response.CurrentLabel) ? "Unknown" : response.CurrentLabel,
                string.IsNullOrWhiteSpace(response.ProviderName) ? "Microsoft Information Protection SDK" : response.ProviderName,
                string.IsNullOrWhiteSpace(response.Summary) ? "현재 라벨을 조회했습니다." : response.Summary,
                string.IsNullOrWhiteSpace(response.CapabilitySummary) ? "MIP SDK 응답을 확인하세요." : response.CapabilitySummary,
                response.TechnicalDetails,
                response.Messages ?? Array.Empty<string>());
        }
        finally
        {
            if (outputBuffer != IntPtr.Zero)
            {
                freeBuffer(outputBuffer);
            }
        }
    }

    public Task<NativeMipSdkApplyResponse> ApplyAsync(
        NativeMipSdkApplyRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        using var nativeLibrary = LoadNativeLibrary();
        var apply = ResolveUtf8JsonFunction(nativeLibrary.Handle, ApplyExportName);
        var freeBuffer = ResolveFreeFunction(nativeLibrary.Handle);
        var payload = JsonSerializer.Serialize(request, JsonSerializerOptions);
        var outputBuffer = IntPtr.Zero;

        try
        {
            outputBuffer = apply(payload);
            var rawJson = Marshal.PtrToStringUTF8(outputBuffer)
                ?? throw new InvalidOperationException("MIP SDK apply 응답이 비어 있습니다.");
            var response = JsonSerializer.Deserialize<NativeApplyBridgeResponse>(rawJson, JsonSerializerOptions)
                ?? throw new InvalidOperationException("MIP SDK apply 응답을 역직렬화하지 못했습니다.");

            var status = Enum.TryParse<LabelChangeStatus>(response.Status, ignoreCase: true, out var parsedStatus)
                ? parsedStatus
                : response.Success
                    ? LabelChangeStatus.Applied
                    : LabelChangeStatus.Blocked;

            return Task.FromResult(new NativeMipSdkApplyResponse(
                response.Success,
                status,
                response.AppliedToSourceFile,
                string.IsNullOrWhiteSpace(response.ProviderName) ? "Microsoft Information Protection SDK" : response.ProviderName,
                string.IsNullOrWhiteSpace(response.Message) ? "MIP SDK 적용 결과를 확인하세요." : response.Message,
                response.RecheckedLabel,
                response.TechnicalDetails));
        }
        finally
        {
            if (outputBuffer != IntPtr.Zero)
            {
                freeBuffer(outputBuffer);
            }
        }
    }

    private LoadedNativeLibrary LoadNativeLibrary()
    {
        if (!NativeLibrary.TryLoad(_nativeLibraryPath, out var handle))
        {
            throw new DllNotFoundException($"MIP SDK 라이브러리를 로드하지 못했습니다: {_nativeLibraryPath}");
        }

        return new LoadedNativeLibrary(handle);
    }

    private static Utf8JsonFunction ResolveUtf8JsonFunction(IntPtr libraryHandle, string exportName)
    {
        if (!NativeLibrary.TryGetExport(libraryHandle, exportName, out var functionPointer))
        {
            throw new EntryPointNotFoundException($"MIP SDK 내보내기 함수를 찾지 못했습니다: {exportName}");
        }

        return Marshal.GetDelegateForFunctionPointer<Utf8JsonFunction>(functionPointer);
    }

    private static FreeUtf8BufferFunction ResolveFreeFunction(IntPtr libraryHandle)
    {
        if (!NativeLibrary.TryGetExport(libraryHandle, FreeBufferExportName, out var functionPointer))
        {
            throw new EntryPointNotFoundException($"MIP SDK 내보내기 함수를 찾지 못했습니다: {FreeBufferExportName}");
        }

        return Marshal.GetDelegateForFunctionPointer<FreeUtf8BufferFunction>(functionPointer);
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr Utf8JsonFunction([MarshalAs(UnmanagedType.LPUTF8Str)] string requestJson);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void FreeUtf8BufferFunction(IntPtr buffer);

    private sealed class LoadedNativeLibrary(IntPtr handle)
        : IDisposable
    {
        public IntPtr Handle { get; } = handle;

        public void Dispose()
        {
            if (Handle != IntPtr.Zero)
            {
                NativeLibrary.Free(Handle);
            }
        }
    }

    private sealed record NativeInspectBridgeResponse(
        bool CanInspect,
        bool CanApply,
        bool CurrentLabelKnown,
        string? CurrentLabel,
        string? ProviderName,
        string? Summary,
        string? CapabilitySummary,
        string? TechnicalDetails,
        IReadOnlyList<string>? Messages);

    private sealed record NativeApplyBridgeResponse(
        bool Success,
        string? Status,
        bool AppliedToSourceFile,
        string? ProviderName,
        string? Message,
        string? RecheckedLabel,
        string? TechnicalDetails);
}
