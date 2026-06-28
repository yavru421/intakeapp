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

    // GET /admin: Beautiful admin dashboard to view submissions and contact clients
    if (url.pathname === "/admin") {
      try {
        const { results } = await env.DB.prepare("SELECT * FROM submissions ORDER BY timestamp DESC").all();
        
        let rowsHtml = "";
        for (const row of results) {
          const answers = JSON.parse(row.answers_json || "[]");
          let answersHtml = "<ul style='margin:0; padding-left:20px;'>";
          for (const ans of answers) {
            answersHtml += `<li><strong>${escapeHtml(ans.QuestionText || ans.questionText || "")}:</strong> ${escapeHtml(ans.AnswerText || ans.answerText || "")}</li>`;
          }
          answersHtml += "</ul>";

          rowsHtml += `
            <tr style="border-bottom: 1px solid #3e3e3e;">
              <td style="padding: 12px; font-family: monospace; font-size: 0.85rem; color: #888;">${escapeHtml(row.id)}</td>
              <td style="padding: 12px; white-space: nowrap;">${escapeHtml(new Date(row.timestamp).toLocaleString())}</td>
              <td style="padding: 12px; font-size: 0.9rem;">${escapeHtml(row.device_metadata || "N/A")}</td>
              <td style="padding: 12px;">${answersHtml}</td>
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
            <style>
              body {
                background-color: #121212;
                color: #e0e0e0;
                font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
                margin: 0;
                padding: 40px 20px;
              }
              .container {
                max-width: 1200px;
                margin: 0 auto;
              }
              header {
                margin-bottom: 30px;
                border-bottom: 1px solid #2e2e2e;
                padding-bottom: 20px;
              }
              h1 {
                margin: 0;
                color: #ffffff;
                font-size: 2rem;
              }
              .subtitle {
                color: #888;
                margin-top: 5px;
              }
              table {
                width: 100%;
                border-collapse: collapse;
                background-color: #1e1e1e;
                border-radius: 8px;
                overflow: hidden;
                box-shadow: 0 4px 6px rgba(0,0,0,0.3);
              }
              th {
                background-color: #2a2a2a;
                color: #ffffff;
                text-align: left;
                padding: 12px;
                font-weight: 600;
              }
              td {
                vertical-align: top;
              }
              tr:hover {
                background-color: #252525;
              }
              .no-data {
                padding: 40px;
                text-align: center;
                color: #888;
                font-style: italic;
              }
            </style>
          </head>
          <body>
            <div class="container">
              <header>
                <h1>Dondlinger Digital</h1>
                <div class="subtitle">Client Solutions Intake Submissions Dashboard</div>
              </header>
              
              <table>
                <thead>
                  <tr>
                    <th style="width: 15%">Submission ID</th>
                    <th style="width: 15%">Timestamp</th>
                    <th style="width: 20%">Device Info</th>
                    <th style="width: 50%">Assessment Details</th>
                  </tr>
                </thead>
                <tbody>
                  ${rowsHtml || '<tr><td colspan="4" class="no-data">No submissions found yet. Completed client assessments will appear here.</td></tr>'}
                </tbody>
              </table>
            </div>
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

function escapeHtml(str) {
  if (!str) return "";
  return str
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#039;");
}
