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
            AddNode("start", "Where will people use this app most?", new[]
            {
                new AnswerOption { Text = "Out on the road (by drivers, technicians, or field workers)", NextNodeId = "q_field_actor" },
                new AnswerOption { Text = "In the office (by employees, managers, or admin staff)", NextNodeId = "q_office_actor" },
                new AnswerOption { Text = "On the web/app store (by the general public or our customers)", NextNodeId = "q_customer_actor" }
            });

            // Branch: Field Service
            AddNode("q_field_actor", "Who will be using this app most often out in the field?", new[]
            {
                new AnswerOption { Text = "Our workers/technicians on the road", NextNodeId = "q_offline" },
                new AnswerOption { Text = "Our office coordinators/dispatchers", NextNodeId = "q_volume" }
            });

            // Branch: Office
            AddNode("q_office_actor", "Who will be using this app most often in the office?", new[]
            {
                new AnswerOption { Text = "Managers and business leaders", NextNodeId = "q_volume" },
                new AnswerOption { Text = "Regular staff and data entry workers", NextNodeId = "q_volume" }
            });

            // Branch: Customer
            AddNode("q_customer_actor", "Who are the customers using this app?", new[]
            {
                new AnswerOption { Text = "Everyday consumers (like people buying online)", NextNodeId = "q_volume" },
                new AnswerOption { Text = "Other businesses or corporate clients", NextNodeId = "q_volume" }
            });

            AddNode("q_volume", "Roughly how many people will use this app every day?", new[]
            {
                new AnswerOption { Text = "Fewer than 10 people", NextNodeId = "q_input" },
                new AnswerOption { Text = "Between 10 and 50 people", NextNodeId = "q_input" },
                new AnswerOption { Text = "Between 50 and 500 people", NextNodeId = "q_input" },
                new AnswerOption { Text = "More than 500 people", NextNodeId = "q_input" }
            });

            // Phase 2: Data & Hardware
            AddNode("q_input", "How will people enter information into the app?", new[]
            {
                new AnswerOption { Text = "Typing in text, notes, or filling out forms", NextNodeId = "q_location" },
                new AnswerOption { Text = "Taking photos or uploading documents/files", NextNodeId = "q_media" },
                new AnswerOption { Text = "Scanning barcodes or QR codes with a camera", NextNodeId = "q_location" },
                new AnswerOption { Text = "Getting it automatically sent from other computer systems", NextNodeId = "q_integrations" }
            });

            AddNode("q_media", "Will people need to upload large files like videos or high-quality photos?", new[]
            {
                new AnswerOption { Text = "Yes, large videos or high-resolution photos", NextNodeId = "q_location" },
                new AnswerOption { Text = "Yes, but only small files like PDFs or documents", NextNodeId = "q_location" },
                new AnswerOption { Text = "No, only text and numbers", NextNodeId = "q_location" }
            });

            AddNode("q_offline", "Should the app work even when there's no internet or cell service (like in remote areas or basements)?", new[]
            {
                new AnswerOption { Text = "No, it is fine if the app requires internet to work", NextNodeId = "q_location" },
                new AnswerOption { Text = "Yes, they should be able to view information offline", NextNodeId = "q_location" },
                new AnswerOption { Text = "Yes, they must be able to continue working and save info offline, then sync later when back online", NextNodeId = "q_location" }
            });

            AddNode("q_location", "Does the app need to track where people are located?", new[]
            {
                new AnswerOption { Text = "Yes, we need live location tracking (GPS)", NextNodeId = "q_auth" },
                new AnswerOption { Text = "Yes, we just need to save an address or a single location pin", NextNodeId = "q_auth" },
                new AnswerOption { Text = "No, location tracking is not needed", NextNodeId = "q_auth" }
            });

            // Phase 3: Auth & Logic
            AddNode("q_auth", "How should users sign in or gain access to the app?", new[]
            {
                new AnswerOption { Text = "Anyone should be able to sign up on their own", NextNodeId = "q_output" },
                new AnswerOption { Text = "Accounts must be created manually by an administrator", NextNodeId = "q_output" },
                new AnswerOption { Text = "Users should log in using their work email (like Google or Microsoft office account)", NextNodeId = "q_output" },
                new AnswerOption { Text = "No log-in or accounts are needed at all", NextNodeId = "q_output" }
            });

            AddNode("q_output", "What is the main thing the app should produce or create?", new[]
            {
                new AnswerOption { Text = "A PDF document (like an invoice, receipt, or inspection report)", NextNodeId = "q_money" },
                new AnswerOption { Text = "Charts, graphs, and stats on a dashboard screen", NextNodeId = "q_money" },
                new AnswerOption { Text = "Just sending data silently to our other database systems", NextNodeId = "q_integrations" }
            });

            AddNode("q_money", "Will you be collecting credit card payments or invoicing clients directly through this app?", new[]
            {
                new AnswerOption { Text = "Yes, recurring subscription payments (like Stripe)", NextNodeId = "q_notifications" },
                new AnswerOption { Text = "Yes, one-time payments or invoices", NextNodeId = "q_notifications" },
                new AnswerOption { Text = "No, we won't be handling payments in the app", NextNodeId = "q_notifications" }
            });

            AddNode("q_notifications", "How should the app send alerts or notifications to users?", new[]
            {
                new AnswerOption { Text = "Via SMS text messages", NextNodeId = "q_integrations" },
                new AnswerOption { Text = "Via Push notifications on their phones", NextNodeId = "q_integrations" },
                new AnswerOption { Text = "Via standard emails", NextNodeId = "q_integrations" },
                new AnswerOption { Text = "No alerts or notifications are needed", NextNodeId = "q_integrations" }
            });

            AddNode("q_integrations", "Do you want this app to automatically sync data with other systems you already use (like QuickBooks, Salesforce, or Sage)?", new[]
            {
                new AnswerOption { Text = "Yes, accounting systems (like QuickBooks or Sage)", NextNodeId = "q_process" },
                new AnswerOption { Text = "Yes, customer systems (like Salesforce or HubSpot)", NextNodeId = "q_process" },
                new AnswerOption { Text = "Yes, our own custom internal database", NextNodeId = "q_process" },
                new AnswerOption { Text = "No, this app can be completely standalone", NextNodeId = "q_process" }
            });

            // Phase 4: Project Realities
            AddNode("q_process", "Are you replacing an existing manual process, like paper forms or Excel sheets?", new[]
            {
                new AnswerOption { Text = "Yes, and the current manual way is a complete mess", NextNodeId = "q_timeline" },
                new AnswerOption { Text = "Yes, and it somewhat works but could be much better", NextNodeId = "q_timeline" },
                new AnswerOption { Text = "No, this is a completely new project", NextNodeId = "q_timeline" }
            });

            AddNode("q_timeline", "When do you need this app to be up and running?", new[]
            {
                new AnswerOption { Text = "As soon as possible (it is urgent!)", NextNodeId = "end" },
                new AnswerOption { Text = "In 1 to 3 months", NextNodeId = "end" },
                new AnswerOption { Text = "In 3 to 6 months", NextNodeId = "end" },
                new AnswerOption { Text = "We are just exploring options for the future", NextNodeId = "end" }
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
