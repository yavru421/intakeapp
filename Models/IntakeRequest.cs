namespace IntakeApp.Models;

public class IntakeRequest
{
    public string ProjectGoal { get; set; } = string.Empty;
    public string BusinessImpact { get; set; } = string.Empty;
    public string ServiceOptions { get; set; } = string.Empty;
    public string BudgetAlignment { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string DeviceMetadata { get; set; } = string.Empty;
}
