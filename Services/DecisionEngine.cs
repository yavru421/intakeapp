using System.Collections.Generic;
using IntakeApp.Models;
using System.Linq;

namespace IntakeApp.Services
{
    public static class DecisionEngine
    {
        private static readonly Dictionary<string, QuestionNode> _graph = new();

        static DecisionEngine()
        {
            // Build the expert system logic graph
            
            // Phase 1: Core Identity
            AddNode("start", "What is the primary domain of this application?", new[]
            {
                new AnswerOption { Text = "Field Service / Dispatch", NextNodeId = "q_field_actor" },
                new AnswerOption { Text = "Internal Operations / Management", NextNodeId = "q_office_actor" },
                new AnswerOption { Text = "Customer-Facing Portal / E-commerce", NextNodeId = "q_customer_actor" }
            });

            // Branch: Field Service
            AddNode("q_field_actor", "Who is the primary actor using the app day-to-day?", new[]
            {
                new AnswerOption { Text = "Our technicians out in the field", NextNodeId = "q_offline" },
                new AnswerOption { Text = "Dispatchers in the office", NextNodeId = "q_volume" }
            });

            // Branch: Office
            AddNode("q_office_actor", "Who is the primary actor using the app day-to-day?", new[]
            {
                new AnswerOption { Text = "Managers / Executives", NextNodeId = "q_volume" },
                new AnswerOption { Text = "Data Entry / Operations staff", NextNodeId = "q_volume" }
            });

            // Branch: Customer
            AddNode("q_customer_actor", "Who is the primary actor using the app day-to-day?", new[]
            {
                new AnswerOption { Text = "Public Consumers (B2C)", NextNodeId = "q_volume" },
                new AnswerOption { Text = "Corporate Clients (B2B)", NextNodeId = "q_volume" }
            });

            AddNode("q_volume", "Roughly how many people will use this app every day?", new[]
            {
                new AnswerOption { Text = "Under 10", NextNodeId = "q_input" },
                new AnswerOption { Text = "10 to 50", NextNodeId = "q_input" },
                new AnswerOption { Text = "50 to 500", NextNodeId = "q_input" },
                new AnswerOption { Text = "500+ (Mass consumer)", NextNodeId = "q_input" }
            });

            // Phase 2: Data & Hardware
            AddNode("q_input", "How will users primarily enter data?", new[]
            {
                new AnswerOption { Text = "Lots of manual typing (forms, notes)", NextNodeId = "q_location" },
                new AnswerOption { Text = "Taking photos / uploading files", NextNodeId = "q_media" },
                new AnswerOption { Text = "Scanning Barcodes or QR codes", NextNodeId = "q_location" },
                new AnswerOption { Text = "Automated data feeds from other systems", NextNodeId = "q_integrations" }
            });

            AddNode("q_media", "Will users be uploading heavy media?", new[]
            {
                new AnswerOption { Text = "Yes, high-res photos or videos from job sites", NextNodeId = "q_location" },
                new AnswerOption { Text = "Yes, but mostly small PDFs or documents", NextNodeId = "q_location" },
                new AnswerOption { Text = "No, strictly text and numbers", NextNodeId = "q_location" }
            });

            AddNode("q_offline", "What happens if the user's internet connection drops?", new[]
            {
                new AnswerOption { Text = "The app can just show an error (always online)", NextNodeId = "q_location" },
                new AnswerOption { Text = "They need to view previously loaded data (read-only offline)", NextNodeId = "q_location" },
                new AnswerOption { Text = "They MUST be able to continue working and save new data that syncs later", NextNodeId = "q_location" }
            });

            AddNode("q_location", "Does the app need to track physical locations?", new[]
            {
                new AnswerOption { Text = "Yes, live GPS tracking / Geofencing", NextNodeId = "q_auth" },
                new AnswerOption { Text = "Yes, just capturing a static address or pin drop", NextNodeId = "q_auth" },
                new AnswerOption { Text = "No location features needed", NextNodeId = "q_auth" }
            });

            // Phase 3: Auth & Logic
            AddNode("q_auth", "How does someone get an account?", new[]
            {
                new AnswerOption { Text = "Anyone can sign up freely", NextNodeId = "q_output" },
                new AnswerOption { Text = "Invite-only (Administrators create accounts)", NextNodeId = "q_output" },
                new AnswerOption { Text = "Single-Sign-On (Microsoft/Google workplace)", NextNodeId = "q_output" },
                new AnswerOption { Text = "No accounts needed (Public app)", NextNodeId = "q_output" }
            });

            AddNode("q_output", "What is the primary output or deliverable of the app?", new[]
            {
                new AnswerOption { Text = "A generated PDF report (e.g., invoices, inspections)", NextNodeId = "q_money" },
                new AnswerOption { Text = "A live analytics dashboard", NextNodeId = "q_money" },
                new AnswerOption { Text = "Pushing data silently to another database", NextNodeId = "q_integrations" }
            });

            AddNode("q_money", "Are there any financial transactions happening inside the app?", new[]
            {
                new AnswerOption { Text = "Yes, recurring subscriptions (Stripe)", NextNodeId = "q_notifications" },
                new AnswerOption { Text = "Yes, one-off invoice payments", NextNodeId = "q_notifications" },
                new AnswerOption { Text = "No money is handled directly in the app", NextNodeId = "q_notifications" }
            });

            AddNode("q_notifications", "Do users need to be actively notified of events?", new[]
            {
                new AnswerOption { Text = "Yes, via SMS text messages", NextNodeId = "q_integrations" },
                new AnswerOption { Text = "Yes, via App Push Notifications", NextNodeId = "q_integrations" },
                new AnswerOption { Text = "Yes, but standard Emails are fine", NextNodeId = "q_integrations" },
                new AnswerOption { Text = "No active alerts needed", NextNodeId = "q_integrations" }
            });

            AddNode("q_integrations", "Does this app need to integrate with your existing legacy software?", new[]
            {
                new AnswerOption { Text = "Yes, accounting software (QuickBooks, Sage)", NextNodeId = "q_process" },
                new AnswerOption { Text = "Yes, a CRM (Salesforce, HubSpot)", NextNodeId = "q_process" },
                new AnswerOption { Text = "Yes, a custom internal database (SQL)", NextNodeId = "q_process" },
                new AnswerOption { Text = "No, it will operate completely standalone", NextNodeId = "q_process" }
            });

            // Phase 4: Project Realities
            AddNode("q_process", "Is there an existing manual process (paper/Excel) this is replacing?", new[]
            {
                new AnswerOption { Text = "Yes, and it's a complete mess (Hair on fire)", NextNodeId = "q_timeline" },
                new AnswerOption { Text = "Yes, but it somewhat works right now", NextNodeId = "q_timeline" },
                new AnswerOption { Text = "No, this is a brand new initiative", NextNodeId = "q_timeline" }
            });

            AddNode("q_timeline", "What is the driving timeline for this project?", new[]
            {
                new AnswerOption { Text = "ASAP - We are losing money/time right now", NextNodeId = "end" },
                new AnswerOption { Text = "1 to 3 months", NextNodeId = "end" },
                new AnswerOption { Text = "3 to 6 months", NextNodeId = "end" },
                new AnswerOption { Text = "Just exploring for next year", NextNodeId = "end" }
            });

            // Terminal Node
            _graph["end"] = new QuestionNode 
            { 
                Id = "end", 
                Text = "Assessment Complete", 
                Options = new List<AnswerOption>(),
                IsTerminal = true
            };
        }

        private static void AddNode(string id, string text, AnswerOption[] options)
        {
            _graph[id] = new QuestionNode
            {
                Id = id,
                Text = text,
                Options = options.ToList(),
                IsTerminal = false
            };
        }

        public static QuestionNode GetNode(string id)
        {
            if (_graph.TryGetValue(id, out var node))
                return node;
            return _graph["end"];
        }
    }
}
