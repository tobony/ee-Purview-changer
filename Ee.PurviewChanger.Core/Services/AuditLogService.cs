using System.Text.Json;
using Ee.PurviewChanger.Core.Models;

namespace Ee.PurviewChanger.Core.Services;

public sealed class AuditLogService
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<string> WriteAsync(AuditLogEntry entry, string auditLogDirectory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentException.ThrowIfNullOrWhiteSpace(auditLogDirectory);

        var targetDirectory = Path.GetFullPath(auditLogDirectory);
        Directory.CreateDirectory(targetDirectory);

        var safeTimestamp = entry.Timestamp.ToString("yyyyMMdd-HHmmss");
        var targetPath = Path.Combine(targetDirectory, $"label-change-{safeTimestamp}.json");

        await using var stream = File.Create(targetPath);
        await JsonSerializer.SerializeAsync(stream, entry, JsonSerializerOptions, cancellationToken);

        return targetPath;
    }
}
