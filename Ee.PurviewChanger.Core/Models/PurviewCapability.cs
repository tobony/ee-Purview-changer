namespace Ee.PurviewChanger.Core.Models;

public sealed record PurviewCapability(
    string Feature,
    string Technology,
    SupportLevel SupportLevel,
    string Notes);
