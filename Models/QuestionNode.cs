using System.Collections.Generic;

namespace IntakeApp.Models
{
    public class QuestionNode
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public List<AnswerOption> Options { get; set; } = new();
        public bool IsTerminal { get; set; }
    }

    public class AnswerOption
    {
        public string Text { get; set; } = string.Empty;
        public string NextNodeId { get; set; } = string.Empty;
    }
}
