using WebApi.Enums;

namespace WebApi.Models;

public class Rule
{
    public int? Age { get; set; }
    public Gender Gender { get; set; }
    public bool? CaringSomeone { get; set; }
    public bool? Disabled { get; set; }
    public bool? Parenting { get; set; }
}