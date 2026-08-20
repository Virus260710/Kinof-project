# Admin UI — Static HTML

> วางใน `backend/SmartLab.Api/wwwroot/admin/`

## index.html

```html
<!DOCTYPE html>
<html lang="th">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Smart Lab — Admin</title>
  <style>
    * { box-sizing: border-box; margin: 0; padding: 0; }
    body { font-family: system-ui, sans-serif; background: #0f172a; color: #e2e8f0; padding: 2rem; }
    h1 { margin-bottom: 1.5rem; }
    .grid { display: grid; grid-template-columns: 1fr 1fr; gap: 1.5rem; }
    .card { background: #1e293b; border-radius: 8px; padding: 1.25rem; }
    .card h2 { font-size: 1rem; color: #94a3b8; margin-bottom: 1rem; }
    table { width: 100%; border-collapse: collapse; font-size: 0.875rem; }
    th, td { text-align: left; padding: 0.5rem; border-bottom: 1px solid #334155; }
    .online { color: #4ade80; }
    .offline { color: #f87171; }
    input, button { padding: 0.5rem; border-radius: 4px; border: 1px solid #475569; background: #0f172a; color: #e2e8f0; }
    button { cursor: pointer; background: #3b82f6; border: none; }
    .add-form { display: flex; gap: 0.5rem; margin-top: 0.75rem; }
  </style>
</head>
<body>
  <h1>Smart Lab Admin</h1>
  <div class="grid">
    <div class="card">
      <h2>Agents</h2>
      <table id="agents-table">
        <thead><tr><th>Seat</th><th>Room</th><th>Hostname</th><th>Status</th></tr></thead>
        <tbody></tbody>
      </table>
    </div>
    <div class="card">
      <h2>Website Blacklist</h2>
      <table id="blacklist-table">
        <thead><tr><th>Domain</th><th>Category</th><th></th></tr></thead>
        <tbody></tbody>
      </table>
      <div class="add-form">
        <input id="new-url" placeholder="domain.com">
        <input id="new-cat" placeholder="category" value="custom">
        <button onclick="addBlacklist()">Add</button>
      </div>
    </div>
  </div>
  <div class="card" style="margin-top:1.5rem">
    <h2>Agent Logs</h2>
    <table id="logs-table">
      <thead><tr><th>Time</th><th>Seat</th><th>Event</th><th>Data</th></tr></thead>
      <tbody></tbody>
    </table>
  </div>
  <script src="app.js"></script>
</body>
</html>
```

## app.js

```javascript
const API = '';

async function loadAgents() {
  const res = await fetch(`${API}/api/admin/agents`);
  const agents = await res.json();
  const tbody = document.querySelector('#agents-table tbody');
  tbody.innerHTML = agents.map(a => `
    <tr>
      <td>${a.seatId}</td>
      <td>${a.roomId}</td>
      <td>${a.hostname}</td>
      <td class="${a.isOnline ? 'online' : 'offline'}">${a.isOnline ? 'Online' : 'Offline'}</td>
    </tr>
  `).join('');
}

async function loadLogs() {
  const res = await fetch(`${API}/api/admin/logs?limit=50`);
  const { logs } = await res.json();
  const tbody = document.querySelector('#logs-table tbody');
  tbody.innerHTML = logs.map(l => `
    <tr>
      <td>${new Date(l.timestamp).toLocaleString('th-TH')}</td>
      <td>${l.seatId}</td>
      <td>${l.eventType}</td>
      <td>${l.dataJson}</td>
    </tr>
  `).join('');
}

async function loadBlacklist() {
  const res = await fetch(`${API}/api/admin/blacklist`);
  const items = await res.json();
  const tbody = document.querySelector('#blacklist-table tbody');
  tbody.innerHTML = items.map(b => `
    <tr>
      <td>${b.urlPattern}</td>
      <td>${b.category}</td>
      <td><button onclick="deleteBlacklist(${b.id})">Delete</button></td>
    </tr>
  `).join('');
}

async function addBlacklist() {
  const urlPattern = document.getElementById('new-url').value;
  const category = document.getElementById('new-cat').value;
  await fetch(`${API}/api/admin/blacklist`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ urlPattern, category })
  });
  document.getElementById('new-url').value = '';
  loadBlacklist();
}

async function deleteBlacklist(id) {
  await fetch(`${API}/api/admin/blacklist/${id}`, { method: 'DELETE' });
  loadBlacklist();
}

loadAgents();
loadLogs();
loadBlacklist();
setInterval(() => { loadAgents(); loadLogs(); }, 10000);
```

เปิดใช้: `http://localhost:5000/admin/`
