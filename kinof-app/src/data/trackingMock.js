// TODO(backend): Replace with GET /api/tracking/summary.
export const trackingSummary = {
  activeUsers: 68,
  machinesReady: 52,
  machinesTotal: 120,
  pendingHelpRequests: 1,
};

// TODO(backend): Replace with GET /api/tracking/rooms.
export const trackingRooms = [
  { id: "room-01", name: "ห้องแล็บ 01", status: "open", activeUserCount: 18 },
  { id: "room-02", name: "ห้องแล็บ 02", status: "open", activeUserCount: 20 },
  { id: "room-03", name: "ห้องแล็บ 03", status: "closed", activeUserCount: 0 },
  { id: "room-04", name: "ห้องแล็บ 04", status: "maintenance", activeUserCount: 12 },
];

const seatStatuses = {
  "room-01": ["in_use", "in_use", "available", "in_use", "available", "in_use", "available", "offline", "in_use", "available", "in_use", "available", "maintenance", "in_use", "available", "in_use", "available", "offline", "in_use", "available"],
  "room-02": ["in_use", "available", "in_use", "available", "offline", "in_use", "maintenance", "available", "in_use", "in_use", "available", "in_use", "available", "offline", "in_use", "available", "maintenance", "in_use", "available", "in_use"],
  "room-03": Array(20).fill("offline"),
  "room-04": ["maintenance", "maintenance", "available", "offline", "maintenance", "available", "offline", "maintenance", "available", "offline", "maintenance", "available", "offline", "maintenance", "available", "offline", "maintenance", "available", "offline", "maintenance"],
};

// TODO(backend): Replace with GET /api/tracking/seats and live agent heartbeats.
export const trackingSeats = trackingRooms.flatMap((room) =>
  seatStatuses[room.id].map((status, index) => ({
    id: `${room.id}-seat-${String(index + 1).padStart(2, "0")}`,
    roomId: room.id,
    number: index + 1,
    label: `คอม ${String(index + 1).padStart(2, "0")}`,
    status,
    agentOnline: status !== "offline",
  })),
);

const sessionUsers = [
  { displayName: "กิตติ ศ.", username: "65010010", userType: "นักศึกษา" },
  { displayName: "พิมพ์ชนก ว.", username: "65010022", userType: "นักศึกษา" },
  { displayName: "ณัฐดนัย ก.", username: "64010104", userType: "นักศึกษา" },
  { displayName: "อาจารย์ศิริพร", username: "teacher.siriporn", userType: "อาจารย์" },
];

// TODO(backend): Replace with GET /api/tracking/sessions.
export const trackingSessions = trackingSeats
  .filter((seat) => seat.status === "in_use")
  .map((seat, index) => ({
    seatId: seat.id,
    user: seat.id === "room-02-seat-10" ? sessionUsers[0] : sessionUsers[index % sessionUsers.length],
    startedAt: `2026-09-06T${String(8 + (index % 7)).padStart(2, "0")}:${String((index * 7) % 60).padStart(2, "0")}:00+07:00`,
  }));

// TODO(backend): Replace with GET /api/tracking/activity.
export const trackingActivity = [
  {
    id: "activity-001",
    seatId: "room-02-seat-10",
    roomId: "room-02",
    roomName: "ห้องแล็บ 02",
    seatLabel: "คอม 10",
    user: sessionUsers[0],
    at: "2026-09-06T13:42:00+07:00",
    activity: "เข้าเว็บไซต์ ufaflow2.com",
    activityType: "suspicious",
    durationMinutes: 8,
    suspicious: true,
  },
  {
    id: "activity-002",
    seatId: "room-02-seat-10",
    roomId: "room-02",
    roomName: "ห้องแล็บ 02",
    seatLabel: "คอม 10",
    user: sessionUsers[0],
    at: "2026-09-06T13:30:00+07:00",
    activity: "เปิด Google Chrome",
    activityType: "program",
    durationMinutes: 24,
    suspicious: false,
  },
  {
    id: "activity-003",
    seatId: "room-02-seat-10",
    roomId: "room-02",
    roomName: "ห้องแล็บ 02",
    seatLabel: "คอม 10",
    user: sessionUsers[0],
    at: "2026-09-06T13:18:00+07:00",
    activity: "เข้าสู่ระบบ",
    activityType: "login",
    durationMinutes: 0,
    suspicious: false,
  },
  {
    id: "activity-004",
    seatId: "room-01-seat-01",
    roomId: "room-01",
    roomName: "ห้องแล็บ 01",
    seatLabel: "คอม 01",
    user: sessionUsers[1],
    at: "2026-09-06T12:55:00+07:00",
    activity: "เข้าเว็บไซต์ docs.google.com",
    activityType: "web",
    durationMinutes: 36,
    suspicious: false,
  },
  {
    id: "activity-005",
    seatId: "room-04-seat-06",
    roomId: "room-04",
    roomName: "ห้องแล็บ 04",
    seatLabel: "คอม 06",
    user: sessionUsers[2],
    at: "2026-09-06T11:40:00+07:00",
    activity: "เปิด Visual Studio Code",
    activityType: "program",
    durationMinutes: 52,
    suspicious: false,
  },
  {
    id: "activity-006",
    seatId: "room-01-seat-04",
    roomId: "room-01",
    roomName: "ห้องแล็บ 01",
    seatLabel: "คอม 04",
    user: sessionUsers[3],
    at: "2026-09-05T16:20:00+07:00",
    activity: "ออกจากระบบ",
    activityType: "logout",
    durationMinutes: 95,
    suspicious: false,
  },
];

// TODO(backend): Replace with GET /api/tracking/programs.
export const trackingPrograms = [
  { ...trackingActivity[1], id: "program-001", program: "Google Chrome" },
  { ...trackingActivity[4], id: "program-002", program: "Visual Studio Code" },
  { ...trackingActivity[3], id: "program-003", activity: "เปิด Microsoft Word", activityType: "program", program: "Microsoft Word", durationMinutes: 41 },
];

// TODO(backend): Replace with GET /api/tracking/websites.
export const trackingWebsites = [
  { ...trackingActivity[0], id: "website-001", website: "ufaflow2.com" },
  { ...trackingActivity[3], id: "website-002", website: "docs.google.com" },
  { ...trackingActivity[4], id: "website-003", activity: "เข้าเว็บไซต์ github.com", activityType: "web", website: "github.com", durationMinutes: 18 },
];

// TODO(backend): Replace with GET /api/tracking/flagged.
export const trackingFlagged = [
  trackingActivity[0],
  { ...trackingActivity[3], id: "flagged-002", activity: "พยายามเปิดโปรแกรมที่ไม่ได้รับอนุญาต", activityType: "suspicious", suspicious: true },
];

// TODO(backend): Replace with GET/POST/DELETE /api/tracking/website-blacklist.
export const websiteBlacklist = [
  { id: "blacklist-001", domain: "ufaflow2.com", reason: "เว็บไซต์พนัน", addedAt: "2026-09-06T13:45:00+07:00" },
  { id: "blacklist-002", domain: "example-malware.test", reason: "ความปลอดภัย", addedAt: "2026-09-05T10:00:00+07:00" },
];
