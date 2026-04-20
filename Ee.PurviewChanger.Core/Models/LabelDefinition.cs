namespace Ee.PurviewChanger.Core.Models;

public sealed class LabelDefinition
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public override string ToString() => Name;
}
