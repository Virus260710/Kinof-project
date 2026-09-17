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

รายการ agent + สถานะ online (online = heartbeat ภายใน 60 วินาที)

**Response:**
```json
[
  {
    "id": "uuid",
    "seatId": "uuid",
    "roomId": "uuid",
    "hostname": "PC-LAB-A-01",
    "online": true,
    "lastHeartbeat": "2026-09-17T09:00:00Z"
  }
]
```

---

## Kiosk Endpoints

เครื่องประตูใช้ header `X-Kiosk-Key` ผูก **ห้อง** (แนวเดียวกับ `X-Agent-Key` ที่ผูกที่นั่ง) ไม่ใช้ JWT

คีย์ห้องอื่น / ถูกเพิกถอน / ไม่ส่ง header → **401** `{ "message": "คีย์เครื่อง Kiosk ไม่ถูกต้อง" }`

Kiosk ตรวจสิทธิ์เข้าห้องเท่านั้น **ไม่จ่ายที่นั่ง**

### GET /api/kiosk/rooms/{roomId}

**Response 200:** `{ "id", "name", "building", "status" }`  
**404** ถ้าไม่มีห้องนั้น

### POST /api/kiosk/entry/verify-otp

```json
{ "roomId": "uuid", "code": "123456" }
```

**Response 200:** `{ "granted": true|false, "message", "user"?, "room"? }`  
**429** เมื่อกรอกผิดครบ 5 ครั้งใน 15 นาทีต่อห้อง

### POST /api/kiosk/entry/verify-face

```json
{ "roomId": "uuid", "imageBase64": "data:image/jpeg;base64,..." }
```

**Response 200:** เหมือน verify-otp บวก `suggestOtp` / `identified` / `serviceError`

### GET /api/admin/kiosk-devices

แอดมิน (JWT) — รายการอุปกรณ์ ไม่คืน `apiKey`

Query: `roomId` (optional)

**Response:** `[{ "id", "roomId", "roomName", "label", "revoked", "revokedAt", "lastSeenAt", "createdAt" }]`

### POST /api/admin/kiosk-devices

```json
{ "roomId": "uuid", "label": "เครื่องประตู" }
```

**Response 200:** `{ "id", "roomId", "roomName", "label", "apiKey", "createdAt" }`  
`apiKey` แสดงครั้งเดียวตอนสร้าง

### POST /api/admin/kiosk-devices/{id}/revoke

เพิกถอนคีย์ — เครื่องที่ใช้อยู่จะได้ 401

โหมด dev: seeder ใส่ `dev-kiosk-key-1` ตามลำดับชื่อห้อง (`ห้องแล็บ 1` = key-1)

หน้าเครื่องประตู `/kiosk/:roomId` ส่งคีย์จาก `localStorage` หรือรับครั้งแรกจาก `?key=` แล้วตัดออกจาก URL — ไม่ให้ผู้ใช้ทั่วไปกรอกบนจอสแกน เปิดตั้งค่าใหม่ด้วย `?setup=1`

---

### POST /api/admin/exports

แอดมินเท่านั้น (JWT + role admin/superadmin) ส่งออกรายงานเป็นไฟล์ Excel หรือ CSV ตามช่วงวันที่ **Asia/Bangkok** (รวมทั้งวันเริ่มต้นและวันสิ้นสุด)

**Request:**
```json
{
  "report": "log",
  "format": "Excel",
  "roomId": "all",
  "startDate": "2026-09-01",
  "endDate": "2026-09-17"
}
```

| field | ค่า |
|-------|-----|
| `report` | `log` ประวัติเข้า-ออกระบบ · `prog` โปรแกรม · `web` เว็บไซต์ · `flag` กิจกรรมน่าสงสัย |
| `format` | `Excel` / `xlsx` หรือ `CSV` / `csv` |
| `roomId` | `"all"` หรือ UUID ห้อง |
| `startDate` / `endDate` | `YYYY-MM-DD` ตามเวลากรุงเทพ รวมทั้งสองวัน สูงสุด 366 วัน |

**Response 200:** ไฟล์ (`application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` หรือ `text/csv; charset=utf-8`) ชื่อเช่น `kinof-login-logout-20260901-20260917.xlsx`

ถ้าไม่มีข้อมูลในช่วงที่เลือก ยังได้ไฟล์ที่มีแค่หัวคอลัมน์ (ไม่ error)

ข้อมูลมาจาก `agent_logs` ประเภท `login` / `logout` / `program` / `website` / `suspicious` — คอลัมน์เวลาเป็นกรุงเทพ `yyyy-MM-dd HH:mm:ss`

---

### GET /api/nav-badges

JWT — จำนวนค้างบน Sidebar รีเฟรชเองทุก 30 วินาที

- ผู้ใช้: `invite` = คำเชิญที่รอตอบ
- แอดมิน: `monitor` = คิว `behavior_reviews` ที่ pending · `helpcenter` = รายงานปัญหาที่ยังไม่เสร็จสิ้น

**Response 200:** `{ "invite": 2, "monitor": 0, "helpcenter": 0 }`

---

### GET /api/behavior

JWT ของผู้ใช้ — คะแนนเริ่ม 100

- จองแล้วไม่เข้าห้องในช่วงเวลาที่จอง (ไม่มี `access_logs` granted และไม่มี login บน Agent ในห้องนั้น) → **หักอัตโนมัติ 5**
- Agent จับกิจกรรมน่าสงสัย / เว็บ / โปรแกรม → **ยังไม่หัก** ขึ้นคิวรอแอดมินที่แท็บ Monitor **น่าสงสัย**
  - กด **ผ่าน** → ไม่หัก ไม่บล็อก
  - กด **บล็อก** → บล็อกเว็บหรือโปรแกรมนั้น (ขึ้นแท็บบล็อกเว็บ/บล็อกโปรแกรม) แล้วหัก 5 จากผู้ใช้
- **แผน (ยังไม่ทำ):** เว็บห้ามตามหมวด UT1 (ค่าเริ่มต้นเปิดได้) · โปรแกรมมีรายการอนุญาตของแล็บ · ของไม่รู้จักสรุปรวม ไม่ขึ้นคิวทีละรายการ · ไม่หักคะแนนแค่เพราะไม่รู้จัก

**Response 200:**
```json
{
  "score": 95,
  "maxScore": 100,
  "penalties": [
    {
      "id": "uuid",
      "at": "2026-09-17T03:00:00+07:00",
      "points": 5,
      "reason": "ไม่มาใช้ห้องแล็บตามวัน-เวลาที่จองไว้",
      "source": "no_show"
    }
  ]
}
```

---

### GET /api/admin/behavior/reviews

แอดมิน — คิวรายการต้องสงสัยที่รอตรวจ (`status = pending`) ไม่กรองตามวัน

แท็บ Monitor **น่าสงสัย** รวมคิว pending กับ log เว็บ/โปรแกรมที่ยังน่าสงสัยและยังไม่ได้อยู่ใน blacklist — ไม่ซ่อนแค่เพราะเคยกดบล็อกแล้วถอดออกจากรายการบล็อก

**Query:** `roomId` (optional)

**Response 200:** `{ "items": [...], "clearedKeys": ["..."], "handledKeys": ["..."] }`

- `clearedKeys` — กด **ผ่าน** แล้ว ซ่อนจากแท็บน่าสงสัย
- `handledKeys` — รวมผ่านแล้วและบล็อกแล้ว (compat)

### POST /api/admin/behavior/reviews/{reviewId}/clear

แอดมินกด **ดี** — ผ่านรายการ ไม่หักคะแนน ไม่บล็อก

### POST /api/admin/behavior/reviews/{reviewId}/penalize

แอดมินกด **แย่** — เพิ่มเว็บ/โปรแกรมเข้า blacklist แล้วหัก 5 จากผู้ใช้ถ้ามี `userId`

**Response 200:** `{ "id", "status": "penalized", "blocked", "deducted", "points" }`

---

### GET /api/admin/tracking/activity

### GET /api/admin/tracking/website-blacklist

### POST /api/admin/tracking/website-blacklist

```json
{ "domain": "example.com", "category": "custom" }
```

### DELETE /api/admin/tracking/website-blacklist/{id}

### GET /api/admin/tracking/website-blacklist/categories

หมวดจาก [Blacklists UT1](https://dsi.ut-capitole.fr/blacklists/index_en.php) ที่ระบบรองรับ พร้อมจำนวนโดเมนที่นำเข้าแล้ว

**Response 200:** `{ "source", "defaultLimit": 250, "maxLimit": 400, "items": [{ "id": "social_networks", "label", "description", "importedCount", "lastImportedAt" }] }`

### POST /api/admin/tracking/website-blacklist/import

แอดมินเลือกหมวดแล้วระบบดึงไฟล์โดเมนของหมวดนั้นจาก UT1 มาใส่ `website_blacklist` — **ไม่เททั้งไฟล์** จำกัด 250 ต่อหมวด (ส่ง `limit` ได้ สูงสุด 400) และตั้ง `category` ตามรหัสหมวด UT1

```json
{ "category": "social_networks", "limit": 250 }
```

**Response 200:** `{ "category", "importedCount", "added", "removed", "skippedExisting", "sourceCount", "truncated", "fallback", "limit" }`  
ถ้าดาวน์โหลด UT1 ไม่ได้และหมวดมีรายการสำรอง → `fallback: true`

### DELETE /api/admin/tracking/website-blacklist/categories/{category}

ลบเฉพาะโดเมนที่นำเข้าจาก UT1 ของหมวดนั้น (`source = ut1`) ไม่ลบโดเมนที่แอดมินใส่เอง

### GET /api/admin/tracking/program-blacklist

รายการโปรแกรมที่ห้ามใช้ (`processName` เช่น `discord.exe`)

### POST /api/admin/tracking/program-blacklist

```json
{ "processName": "discord.exe", "category": "chat" }
```

### DELETE /api/admin/tracking/program-blacklist/{id}

Windows Agent ดึง `GET /api/agent/program-blacklist` ตามรอบ heartbeat แล้วปิด process ที่ตรง `processName`

### GET /api/agent/program-blacklist

Header: `X-Agent-Key`

**Response 200:** `{ "processNames": ["discord.exe", "steam.exe"] }`

### GET /api/admin/tracking/program-allowlist

รายการโปรแกรมที่อนุญาตในห้องแล็บ (`processName` เช่น `chrome.exe`, `code.exe`)

### POST /api/admin/tracking/program-allowlist

```json
{ "processName": "chrome.exe", "displayName": "Google Chrome", "category": "browser" }
```

### DELETE /api/admin/tracking/program-allowlist/{id}

### GET /api/admin/tracking/unknown-programs

สรุปโปรแกรมที่ Agent เจอแล้วไม่ใช่ระบบ Windows ไม่ใช่รายการอนุญาต และไม่ใช่รายการห้าม — **ไม่ใช่คิวทีละแถว** และไม่หักคะแนน

Query: `roomId`, `date` (`yyyy-MM-dd` หรือ `all`)

**Response 200:** `[{ "processName", "occurrenceCount", "machineCount", "firstSeenAt", "lastSeenAt" }]`

### GET /api/agent/program-allowlist

Header: `X-Agent-Key`

**Response 200:** `{ "processNames": ["chrome.exe", "code.exe"] }`

Heartbeat คืน `programBlacklist` และ `programAllowlist` ในชุดเดียวกัน

---

## Event Types

บันทึกใน `agent_logs.event_type` จาก Windows Agent / แอดมินสั่งออกจากระบบ

| eventType | คำอธิบาย |
|-----------|----------|
| `login` | เข้าสู่ระบบเครื่องแล็บ (บัญชี KINOF ที่ตรวจแล้ว) |
| `logout` | ออกจากระบบเครื่องแล็บ |
| `program` | เปิดหรือบล็อกโปรแกรม |
| `website` | เข้าเว็บไซต์ |
| `suspicious` | กิจกรรมน่าสงสัย |
| `unknown_program` | โปรแกรมที่ไม่ใช่ระบบ / ไม่อยู่ในรายการอนุญาต / ไม่อยู่ในรายการห้าม — เก็บเพื่อสรุป ไม่ขึ้นคิวน่าสงสัย |
