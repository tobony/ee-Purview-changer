namespace Ee.PurviewChanger.Core.Models;

public sealed class PurviewAppOptions
{
    public AuthenticationOptions Authentication { get; set; } = new();

    public ValidationModeOptions ValidationMode { get; set; } = new();

    public List<string> SupportedFileExtensions { get; set; } = [".docx", ".xlsx", ".pptx", ".pdf", ".txt"];

    public List<LabelDefinition> CandidateLabels { get; set; } =
    [
        new() { Id = "general", Name = "General", Description = "기본 업무 문서." },
        new() { Id = "confidential", Name = "Confidential", Description = "사내 제한 문서." },
        new() { Id = "highly-confidential", Name = "Highly Confidential", Description = "고위험 보호가 필요한 문서." }
    ];

    public string AuditLogDirectory { get; set; } = "App_Data/AuditLogs";
}
