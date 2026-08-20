# Smart Lab API — Phase 1 Endpoints

Base URL: `http://localhost:5000`

## Agent Endpoints (ต้องมี Header `X-Api-Key`)

### POST /api/agent/register

ลงทะเบียน agent ครั้งแรก

**Request:**
```json
{
  "seatId": "seat-01",
  "roomId": "lab-a",
  "hostname": "PC-LAB-A-01",
  "osVersion": "Windows 11"
}
```

**Response 200:**
```json
{
  "agentId": "uuid",
  "apiKey": "generated-key-store-in-appsettings"
}
```

---

### POST /api/agent/heartbeat

**Request:**
```json
{
  "agentId": "uuid",
  "timestamp": "2026-07-29T09:00:00Z",
  "uptimeSeconds": 3600
}
```

**Response 200:** `{ "ok": true }`

---

### POST /api/agent/logs

ส่ง log batch (max 100 ต่อ request)

**Request:**
```json
{
  "agentId": "uuid",
  "logs": [
    {
      "eventType": "process_start",
      "data": {
        "processName": "chrome.exe",
        "exePath": "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe",
        "pid": 1234
      },
      "timestamp": "2026-07-29T09:01:00Z"
    },
    {
      "eventType": "process_stop",
      "data": { "processName": "notepad.exe", "pid": 5678 },
      "timestamp": "2026-07-29T09:05:00Z"
    },
    {
      "eventType": "power_boot",
      "data": { "bootTime": "2026-07-29T08:00:00Z" },
      "timestamp": "2026-07-29T08:00:01Z"
    },
    {
      "eventType": "web_block",
      "data": { "url": "blocked-site.com", "reason": "blacklist" },
      "timestamp": "2026-07-29T09:10:00Z"
    }
  ]
}
```

**Response 200:** `{ "received": 4 }`

---

### GET /api/agent/config

Agent ดึง config (blacklist + policy)

**Response 200:**
```json
{
  "blacklist": [
    { "urlPattern": "facebook.com", "category": "social" },
    { "urlPattern": "gambling-site.com", "category": "gambling" }
  ],
  "blockedProcessNames": ["tor.exe", "bittorrent.exe"],
  "configVersion": 3
}
```

---

## Admin Endpoints

### GET /api/admin/agents

รายการ agent + สถานะ online (online = heartbeat ภายใน 90 วินาที)

**Response:**
```json
[
  {
    "agentId": "uuid",
    "seatId": "seat-01",
    "roomId": "lab-a",
    "hostname": "PC-LAB-A-01",
    "isOnline": true,
    "lastHeartbeat": "2026-07-29T09:00:00Z"
  }
]
```

---

### GET /api/admin/logs?seatId=&roomId=&eventType=&from=&to=&page=1&limit=50

---

### GET /api/admin/blacklist

### POST /api/admin/blacklist

```json
{ "urlPattern": "example.com", "category": "custom" }
```

### DELETE /api/admin/blacklist/{id}

---

## Event Types

| eventType | คำอธิบาย |
|-----------|----------|
| `process_start` | เปิดโปรแกรม |
| `process_stop` | ปิดโปรแกรม |
| `power_boot` | เปิดเครื่อง |
| `power_shutdown` | ปิดเครื่อง |
| `web_block` | พยายามเข้าเว็บที่ block |
| `agent_start` | Agent service เริ่มทำงาน |
| `agent_stop` | Agent service หยุด |
