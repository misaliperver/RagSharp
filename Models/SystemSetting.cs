using System.ComponentModel.DataAnnotations;

namespace Decisionman.Models;

public class SystemSetting
{
    [Key]
    public string Key { get; set; } = null!;
    public string? Value { get; set; }
}
