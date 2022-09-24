using WebApi.Enums;

namespace WebApi.Models;

public class Rule
{
    public int? Age { get; set; }
    public Gender Gender { get; set; }
    public bool? FlexibleWorking { get; set; }
    public bool? SupportMinority { get; set; }
    public List<Language> Languages { get; set; } = new List<Language>();
}