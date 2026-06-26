using System;
using System.Collections.Generic;

namespace IntakeApp.Models;

public class IntakeRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public List<AnswerRecord> Answers { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string DeviceMetadata { get; set; } = string.Empty;
    
    public bool IsSynced { get; set; } = false;
    public bool SyncSkip { get; set; } = false;
    public bool SyncFailed { get; set; } = false;
    public string? ErrorMessage { get; set; } = null;
}

public class AnswerRecord
{
    public string QuestionId { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public string AnswerText { get; set; } = string.Empty;
}
