using System.Text.Json;
using Ee.PurviewChanger.Core.Models;

namespace Ee.PurviewChanger.Desktop.Services;

public static class AppOptionsLoader
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static PurviewAppOptions Load()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var filePath = Path.Combine(baseDirectory, "appsettings.json");

        if (!File.Exists(filePath))
        {
            return new PurviewAppOptions();
        }

        var json = File.ReadAllText(filePath);
        var options = JsonSerializer.Deserialize<PurviewAppOptions>(json, JsonSerializerOptions);

        return options ?? new PurviewAppOptions();
    }
}
