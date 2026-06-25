using System;
using System.Collections.Generic;

namespace IntakeApp.Models;

public class IntakeRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ProjectGoal { get; set; } = string.Empty;
    public string BusinessImpact { get; set; } = string.Empty;
    public List<string> ServiceOptions { get; set; } = new List<string>();
    public string BudgetAlignment { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string DeviceMetadata { get; set; } = string.Empty;
    
    public bool IsSynced { get; set; } = false;
    public bool SyncSkip { get; set; } = false;
    public bool SyncFailed { get; set; } = false;
    public string? ErrorMessage { get; set; } = null;
}
