function checkAuth(request, env) {
  const authHeader = request.headers.get("Authorization");
  if (!authHeader || !authHeader.startsWith("Basic ")) {
    return false;
  }
  try {
    const base64Credentials = authHeader.substring(6);
    const credentials = atob(base64Credentials);
    const [username, password] = credentials.split(":");
    const expectedUsername = env.ADMIN_USERNAME || "admin";
    const expectedPassword = env.ADMIN_PASSWORD;
    if (!expectedPassword) {
      return false;
    }
    return username === expectedUsername && password === expectedPassword;
  } catch (err) {
    return false;
  }
}

export default {
  async fetch(request, env) {
    const url = new URL(request.url);

    // Basic Auth Protection for Admin Routes
    if (url.pathname === "/admin" || (url.pathname === "/api/submissions" && request.method === "GET")) {
      if (!checkAuth(request, env)) {
        return new Response("Unauthorized", {
          status: 401,
          headers: {
            "WWW-Authenticate": 'Basic realm="Admin Dashboard", charset="UTF-8"'
          }
        });
      }
    }

    // API: POST a new submission
    if (url.pathname === "/api/submissions" && request.method === "POST") {
      try {
        const entry = await request.json();
        
        await env.DB.prepare(
          "INSERT INTO submissions (id, timestamp, device_metadata, answers_json) VALUES (?, ?, ?, ?)"
        ).bind(
          entry.Id || entry.id || crypto.randomUUID(),
          entry.Timestamp || entry.timestamp || new Date().toISOString(),
          entry.DeviceMetadata || entry.deviceMetadata || "Unknown",
          JSON.stringify(entry.Answers || entry.answers || [])
        ).run();

        return new Response(JSON.stringify({ success: true }), {
          status: 200,
          headers: { "Content-Type": "application/json" }
        });
      } catch (err) {
        return new Response(JSON.stringify({ error: err.message }), {
          status: 500,
          headers: { "Content-Type": "application/json" }
        });
      }
    }

    // GET /api/submissions: JSON list
    if (url.pathname === "/api/submissions" && request.method === "GET") {
      try {
        const { results } = await env.DB.prepare("SELECT * FROM submissions ORDER BY timestamp DESC").all();
        const submissions = results.map(row => ({
          id: row.id,
          timestamp: row.timestamp,
          device_metadata: row.device_metadata,
          answers: JSON.parse(row.answers_json || "[]")
        }));
        return new Response(JSON.stringify(submissions, null, 2), {
          status: 200,
          headers: { "Content-Type": "application/json" }
        });
      } catch (err) {
        return new Response(JSON.stringify({ error: err.message }), {
          status: 500,
          headers: { "Content-Type": "application/json" }
        });
      }
    }

    // GET /admin: Beautiful admin dashboard with flowchart bootstrapping & details
    if (url.pathname === "/admin") {
      try {
        const { results } = await env.DB.prepare("SELECT * FROM submissions ORDER BY timestamp DESC").all();
        
        const submissions = results.map(row => ({
          id: row.id,
          timestamp: row.timestamp,
          device_metadata: row.device_metadata,
          answers: JSON.parse(row.answers_json || "[]")
        }));

        let rowsHtml = "";
        for (const sub of submissions) {
          const clientName = getAnswerText(sub.answers, "client_name") || "Unknown Client";
          const clientEmail = getAnswerText(sub.answers, "client_email") || "No Email";
          const projectName = getAnswerText(sub.answers, "project_name") || "Untitled Project";
          const timeline = getAnswerText(sub.answers, "q_timeline");

          rowsHtml += `
            <tr onclick="selectSubmission('${sub.id}')" id="row-${sub.id}">
              <td>${escapeHtml(new Date(sub.timestamp).toLocaleDateString())}</td>
              <td><strong>${escapeHtml(clientName)}</strong></td>
              <td>${escapeHtml(clientEmail)}</td>
              <td>${escapeHtml(projectName)}</td>
              <td style="white-space: nowrap;"><span class="badge ${timeline.includes("ASAP") ? "badge-danger" : "badge-info"}">${escapeHtml(timeline)}</span></td>
            </tr>
          `;
        }

        const html = `
          <!DOCTYPE html>
          <html lang="en">
          <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>Dondlinger Digital - Intake Submissions</title>
            <!-- Mermaid.js for flowcharts -->
            <script src="https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.min.js"></script>
            <style>
              :root {
                --bg: #0f172a;
                --pane: #1e293b;
                --text: #cbd5e1;
                --primary: #3b82f6;
                --accent: #10b981;
                --border: #334155;
              }
              body {
                background-color: var(--bg);
                color: var(--text);
                font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
                margin: 0;
                padding: 0;
                height: 100vh;
                display: flex;
                flex-direction: column;
                overflow: hidden;
              }
              header {
                background-color: var(--pane);
                padding: 16px 24px;
                border-bottom: 1px solid var(--border);
                display: flex;
                justify-content: space-between;
                align-items: center;
                flex-shrink: 0;
              }
              h1 {
                margin: 0;
                color: #ffffff;
                font-size: 1.5rem;
              }
              .subtitle {
                color: #64748b;
                font-size: 0.85rem;
                margin-top: 2px;
              }
              .main-layout {
                display: flex;
                flex: 1;
                overflow: hidden;
              }
              .left-column {
                width: 45%;
                border-right: 1px solid var(--border);
                overflow-y: auto;
                padding: 24px;
              }
              .right-column {
                width: 55%;
                overflow-y: auto;
                padding: 24px;
                background-color: #0b0f19;
                display: flex;
                flex-direction: column;
              }
              table {
                width: 100%;
                border-collapse: collapse;
                background-color: var(--pane);
                border-radius: 8px;
                overflow: hidden;
                box-shadow: 0 4px 6px rgba(0,0,0,0.3);
              }
              th, td {
                padding: 12px;
                text-align: left;
                font-size: 0.9rem;
              }
              th {
                background-color: #0f172a;
                color: #ffffff;
                font-weight: 600;
              }
              tr {
                border-bottom: 1px solid var(--border);
                cursor: pointer;
                transition: background-color 0.2s;
              }
              tr:hover {
                background-color: #334155;
              }
              tr.selected {
                background-color: #1e3a8a;
                border-left: 4px solid var(--primary);
              }
              .badge {
                padding: 4px 8px;
                border-radius: 9999px;
                font-size: 0.75rem;
                font-weight: bold;
              }
              .badge-danger { background-color: #ef4444; color: #ffffff; }
              .badge-info { background-color: #0ea5e9; color: #ffffff; }
              
              /* Detail layout */
              .detail-card {
                background-color: var(--pane);
                border-radius: 12px;
                padding: 24px;
                border: 1px solid var(--border);
                display: none;
                flex: 1;
              }
              .placeholder-detail {
                display: flex;
                justify-content: center;
                align-items: center;
                flex: 1;
                color: #64748b;
                font-style: italic;
              }
              
              /* Tabs styling */
              .tabs {
                display: flex;
                gap: 8px;
                margin-top: 20px;
                border-bottom: 1px solid var(--border);
                padding-bottom: 8px;
              }
              .tab-btn {
                background: none;
                border: none;
                color: #64748b;
                padding: 8px 16px;
                cursor: pointer;
                font-weight: 600;
                font-size: 0.9rem;
                border-radius: 6px;
              }
              .tab-btn.active {
                background-color: var(--primary);
                color: #ffffff;
              }
              .tab-content {
                display: none;
                padding-top: 16px;
                flex: 1;
              }
              .tab-content.active {
                display: block;
              }
              
              pre {
                background-color: #0f172a;
                padding: 16px;
                border-radius: 8px;
                color: #38bdf8;
                font-family: 'Courier New', Courier, monospace;
                overflow-x: auto;
                font-size: 0.85rem;
                border: 1px solid var(--border);
                margin: 0;
              }
              
              .flowchart-container {
                background-color: #0f172a;
                padding: 20px;
                border-radius: 8px;
                border: 1px solid var(--border);
                display: flex;
                justify-content: center;
                align-items: center;
                min-height: 250px;
              }
              
              .tech-spec {
                display: grid;
                grid-template-columns: repeat(2, 1fr);
                gap: 16px;
              }
              .spec-item {
                background-color: #0f172a;
                padding: 12px 16px;
                border-radius: 8px;
                border: 1px solid var(--border);
              }
              .spec-label {
                font-size: 0.75rem;
                color: #64748b;
                text-transform: uppercase;
                margin-bottom: 4px;
                font-weight: bold;
              }
              .spec-value {
                font-size: 0.95rem;
                color: #ffffff;
              }
              
              /* Scrollbars */
              ::-webkit-scrollbar {
                width: 8px;
              }
              ::-webkit-scrollbar-track {
                background: var(--bg);
              }
              ::-webkit-scrollbar-thumb {
                background: var(--border);
                border-radius: 4px;
              }
            </style>
          </head>
          <body>
            <header>
              <div>
                <h1>Dondlinger Digital</h1>
                <div class="subtitle">Client Solutions Intake Submissions Dashboard</div>
              </div>
            </header>
            
            <div class="main-layout">
              <div class="left-column">
                <table>
                  <thead>
                    <tr>
                      <th>Date</th>
                      <th>Client Name</th>
                      <th>Email</th>
                      <th>Project Name</th>
                      <th>Timeline</th>
                    </tr>
                  </thead>
                  <tbody>
                    ${rowsHtml || '<tr><td colspan="5" class="no-data">No submissions found yet. Completed client assessments will appear here.</td></tr>'}
                  </tbody>
                </table>
              </div>
              
              <div class="right-column">
                <div id="placeholder" class="placeholder-detail">
                  Select a client submission to view custom flow charts and SQL database bootstrap recommendations.
                </div>
                
                <div id="detail-card" class="detail-card">
                  <h2 id="detail-title" style="margin-top: 0; margin-bottom: 4px; color: #fff;">Client Project Blueprint</h2>
                  <div id="detail-contact" style="font-size: 0.9rem; color: #94a3b8; margin-bottom: 20px;"></div>
                  
                  <div class="tabs">
                    <button class="tab-btn active" onclick="switchTab('tab-flowchart')">Architecture Flow Chart</button>
                    <button class="tab-btn" onclick="switchTab('tab-sql')">SQL Database Schema</button>
                    <button class="tab-btn" onclick="switchTab('tab-wrangler')">Wrangler Config</button>
                    <button class="tab-btn" onclick="switchTab('tab-stack')">Tech Stack</button>
                  </div>
                  
                  <!-- Tab 1: Flowchart -->
                  <div id="tab-flowchart" class="tab-content active">
                    <div class="flowchart-container">
                      <div class="mermaid" id="mermaid-container"></div>
                    </div>
                  </div>
                  
                  <!-- Tab 2: SQL -->
                  <div id="tab-sql" class="tab-content">
                    <pre><code id="sql-code"></code></pre>
                  </div>
                  
                  <!-- Tab 3: Wrangler -->
                  <div id="tab-wrangler" class="tab-content">
                    <pre><code id="wrangler-code"></code></pre>
                  </div>
                  
                  <!-- Tab 4: Tech Stack -->
                  <div id="tab-stack" class="tab-content">
                    <div class="tech-spec">
                      <div class="spec-item"><div class="spec-label">Client Interface</div><div class="spec-value" id="spec-frontend"></div></div>
                      <div class="spec-item"><div class="spec-label">Backend API</div><div class="spec-value" id="spec-backend"></div></div>
                      <div class="spec-item"><div class="spec-label">Database Store</div><div class="spec-value" id="spec-db"></div></div>
                      <div class="spec-item"><div class="spec-label">Asset Hosting</div><div class="spec-value" id="spec-hosting"></div></div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
            
            <script>
              const submissions = ${JSON.stringify(submissions)};
              mermaid.initialize({ startOnLoad: false, theme: 'dark', securityLevel: 'loose' });
              
              function getAnswerText(answers, qId) {
                const a = answers.find(ans => ans.QuestionId === qId);
                return a ? a.AnswerText : "";
              }
              
              function selectSubmission(id) {
                // Highlight row
                document.querySelectorAll('tr').forEach(r => r.classList.remove('selected'));
                document.getElementById('row-' + id).classList.add('selected');
                
                // Show card
                document.getElementById('placeholder').style.display = 'none';
                document.getElementById('detail-card').style.display = 'flex';
                
                const sub = submissions.find(s => s.id === id);
                
                // Generate Details
                const clientName = getAnswerText(sub.answers, "client_name") || "Unknown";
                const clientEmail = getAnswerText(sub.answers, "client_email") || "N/A";
                const clientPhone = getAnswerText(sub.answers, "client_phone") || "N/A";
                const projectName = getAnswerText(sub.answers, "project_name") || "Untitled";

                document.getElementById('detail-title').innerHTML = escapeHtml(projectName) + ' <span style="font-size: 0.95rem; font-weight: normal; color: #94a3b8;">by ' + escapeHtml(clientName) + '</span>';
                document.getElementById('detail-contact').innerHTML = '<strong>Email:</strong> <a href="mailto:' + escapeHtml(clientEmail) + '" style="color: var(--primary); text-decoration: none;">' + escapeHtml(clientEmail) + '</a> | <strong>Phone:</strong> ' + escapeHtml(clientPhone);
                
                // Tech Stack Tab
                const offline = getAnswerText(sub.answers, "q_offline");
                const input = getAnswerText(sub.answers, "q_input");
                
                document.getElementById('spec-frontend').innerText = offline.includes("continue working") ? "PWA Client + Offline IndexedDB" : "React/Vue PWA Application";
                document.getElementById('spec-backend').innerText = "Cloudflare Workers (ES Modules)";
                document.getElementById('spec-db').innerText = "Cloudflare D1 (SQLite compatible)";
                document.getElementById('spec-hosting').innerText = "Cloudflare Pages & Workers Assets";
                
                // SQL DDL Tab
                document.getElementById('sql-code').textContent = generateSql(sub.answers);
                
                // Wrangler Config Tab
                document.getElementById('wrangler-code').textContent = generateWrangler(sub.answers, sub.id);
                
                // Flowchart Tab (Mermaid)
                const flowchartDiv = document.getElementById('mermaid-container');
                flowchartDiv.removeAttribute('data-processed');
                flowchartDiv.innerHTML = generateMermaid(sub.answers);
                mermaid.run({ nodes: [flowchartDiv] });
              }
              
              function generateMermaid(answers) {
                const start = getAnswerText(answers, "start");
                const offline = getAnswerText(answers, "q_offline");
                const input = getAnswerText(answers, "q_input");
                const location = getAnswerText(answers, "q_location");
                const money = getAnswerText(answers, "q_money");
                const notifications = getAnswerText(answers, "q_notifications");
                const integrations = getAnswerText(answers, "q_integrations");
                
                let diagram = "graph TD\\n";
                
                let clientText = "Client Interface";
                let clientSubText = "Web Application";
                if (offline.includes("continue working")) {
                  clientText = "Offline PWA Client";
                  clientSubText = "IndexedDB Store";
                }
                
                diagram += "  Client[\\\"<strong>" + clientText + "</strong><br/>" + clientSubText + "\\\"]\\n";
                diagram += "  API[\\\"<strong>Cloudflare Worker</strong><br/>ES Module Router\\\"]\\n";
                diagram += "  DB[\\\"<strong>D1 SQL Database</strong><br/>Persistent Store\\\"]\\n";
                
                diagram += "  Client -->|HTTPS API Requests| API\\n";
                diagram += "  API -->|SQL Queries| DB\\n";
                
                if (input.includes("photos") || input.includes("uploading")) {
                  diagram += "  R2[\\\"<strong>Cloudflare R2</strong><br/>Object Media Store\\\"]\\n";
                  diagram += "  API -->|Upload Assets| R2\\n";
                }
                
                if (money.includes("Stripe") || money.includes("invoice")) {
                  diagram += "  Stripe[\\\"<strong>Stripe Gateway</strong><br/>Payment Services\\\"]\\n";
                  diagram += "  API -->|Billing| Stripe\\n";
                }
                
                if (notifications.includes("SMS") || notifications.includes("Push")) {
                  const chan = notifications.includes("SMS") ? "Twilio SMS Gateway" : "Firebase Cloud Messaging";
                  diagram += "  Notify[\\\"<strong>" + chan + "</strong><br/>Push Alerts\\\"]\\n";
                  diagram += "  API -->|Send Notifications| Notify\\n";
                }
                
                if (integrations.includes("QuickBooks") || integrations.includes("Sage")) {
                  diagram += "  QB[\\\"<strong>QuickBooks API</strong><br/>Accounting Sync\\\"]\\n";
                  diagram += "  API -->|Post Invoices| QB\\n";
                }
                
                return diagram;
              }
              
              function generateSql(answers) {
                const offline = getAnswerText(answers, "q_offline");
                const location = getAnswerText(answers, "q_location");
                const input = getAnswerText(answers, "q_input");
                const money = getAnswerText(answers, "q_money");
                
                let sql = "-- Recommended Database Schema Setup\\n\\n";
                sql += "CREATE TABLE IF NOT EXISTS users (\\n  id TEXT PRIMARY KEY,\\n  email TEXT UNIQUE NOT NULL,\\n  role TEXT NOT NULL,\\n  created_at TEXT DEFAULT CURRENT_TIMESTAMP\\n);\\n\\n";
                
                if (offline.includes("continue working")) {
                  sql += "CREATE TABLE IF NOT EXISTS sync_queue (\\n  id TEXT PRIMARY KEY,\\n  payload TEXT NOT NULL,\\n  status TEXT DEFAULT 'pending',\\n  created_at TEXT DEFAULT CURRENT_TIMESTAMP\\n);\\n\\n";
                }
                
                if (location.includes("GPS") || location.includes("address")) {
                  sql += "CREATE TABLE IF NOT EXISTS locations (\\n  id TEXT PRIMARY KEY,\\n  latitude REAL NOT NULL,\\n  longitude REAL NOT NULL,\\n  recorded_at TEXT NOT NULL,\\n  user_id TEXT NOT NULL\\n);\\n\\n";
                }
                
                if (input.includes("photos") || input.includes("uploading")) {
                  sql += "CREATE TABLE IF NOT EXISTS media (\\n  id TEXT PRIMARY KEY,\\n  url TEXT NOT NULL,\\n  uploaded_by TEXT NOT NULL,\\n  uploaded_at TEXT DEFAULT CURRENT_TIMESTAMP\\n);\\n\\n";
                }
                
                if (money.includes("Stripe") || money.includes("invoice")) {
                  sql += "CREATE TABLE IF NOT EXISTS transactions (\\n  id TEXT PRIMARY KEY,\\n  amount INTEGER NOT NULL,\\n  status TEXT NOT NULL,\\n  paid_at TEXT\\n);\\n\\n";
                }
                
                sql += "CREATE TABLE IF NOT EXISTS logs (\\n  id TEXT PRIMARY KEY,\\n  level TEXT NOT NULL,\\n  message TEXT NOT NULL,\\n  timestamp TEXT DEFAULT CURRENT_TIMESTAMP\\n);";
                return sql;
              }
              
              function generateWrangler(answers, id) {
                const input = getAnswerText(answers, "q_input");
                let toml = "name = \\\"client-app-" + id.substring(0, 8) + "\\\"\\n";
                toml += "main = \\\"src/index.js\\\"\\n";
                toml += "compatibility_date = \\\"2024-05-26\\\"\\n\\n";
                toml += "[assets]\\n";
                toml += "directory = \\\"public\\\"\\n";
                toml += "not_found_handling = \\\"single-page-application\\\"\\n\\n";
                toml += "[[d1_databases]]\\n";
                toml += "binding = \\\"DB\\\"\\n";
                toml += "database_name = \\\"client_db\\\"\\n";
                toml += "database_id = \\\"your-d1-uuid-here\\\"\\n\\n";
                
                if (input.includes("photos") || input.includes("uploading")) {
                  toml += "[[r2_buckets]]\\n";
                  toml += "binding = \\\"BUCKET\\\"\\n";
                  toml += "bucket_name = \\\"client-media-bucket\\\"\\n";
                }
                
                return toml;
              }
              
              function switchTab(tabId) {
                document.querySelectorAll('.tab-btn').forEach(btn => btn.classList.remove('active'));
                document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));
                
                // Find button mapping
                const clickedBtn = Array.from(document.querySelectorAll('.tab-btn')).find(btn => btn.getAttribute('onclick').includes(tabId));
                if (clickedBtn) clickedBtn.classList.add('active');
                
                document.getElementById(tabId).classList.add('active');
              }
            </script>
          </body>
          </html>
        `;

        return new Response(html, {
          status: 200,
          headers: { "Content-Type": "text/html" }
        });
      } catch (err) {
        return new Response(`<h1>Database Error</h1><p>${err.message}</p>`, {
          status: 500,
          headers: { "Content-Type": "text/html" }
        });
      }
    }

    // Default fallback: Serve static assets
    return env.ASSETS.fetch(request);
  }
};

function getAnswerText(answers, qId) {
  const a = answers.find(ans => ans.QuestionId === qId || ans.questionId === qId);
  return a ? a.AnswerText || a.answerText : "N/A";
}

function escapeHtml(str) {
  if (!str) return "";
  return str
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#039;");
}
