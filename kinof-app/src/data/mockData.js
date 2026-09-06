// Mock data standing in for backend responses during frontend development.
// See README.md "Backend integration points" for the endpoint each of these should come from.

export const initialLog = [
  { user: "som_ying", time: "14:03 น.", room: "ห้องแล็บ 4", machine: 17, domain: "google.com", duration: "12 นาที", status: "ออกไล้ว" },
  { user: "iceeyy", time: "14:02 น.", room: "ห้องแล็บ 2", machine: 2, domain: "bet365.net", duration: "20 นาที", status: "ล็อคอินอยู่", flagged: true },
  { user: "keetauy", time: "14:02 น.", room: "ห้องแล็บ 3", machine: 23, domain: "youtube.com", duration: "25 นาที", status: "ล็อคอินอยู่" },
  { user: "fighteiei", time: "14:01 น.", room: "ห้องแล็บ 1", machine: 11, domain: "github.com", duration: "38 นาที", status: "ออกไล้ว" },
];

export const programs = [
  { name: "Google Chrome", time: "54 นาที" },
  { name: "Visual Studio Code", time: "42 นาที" },
  { name: "Adobe Photoshop", time: "33 นาที" },
  { name: "Microsoft Word", time: "29 นาที" },
  { name: "Microsoft Team", time: "26 นาที" },
];

export const initialBlocked = ["uea8sabai.com"];

export const scheduleRows = [
  { day: "วันจันทร์", course: "IT319 / Low-Code and No-Code Development Platform", time: "09.00 น. - 11.30 น.", room: "ห้องแล็บ 1" },
  { day: "วันอังคาร", course: "", time: "", room: "" },
  { day: "วันพุธ", course: "IT453 / Robotic Process Automation Development", time: "14.00 น. - 16.30 น.", room: "ห้องแล็บ 3" },
  { day: "วันพฤหัส", course: "IT464 / Web Administration", time: "16.30 น. - 19.00 น.", room: "ห้องแล็บ 2" },
  { day: "วันศุกร์", course: "", time: "", room: "" },
  { day: "วันเสาร์", course: "", time: "", room: "" },
];

// TODO(backend): GET /api/schedule/me
export const bookingScheduleConflicts = [
  { day: "วันจันทร์", course: "IT319 / Low-Code and No-Code Development", slotId: 1 },
  { day: "วันจันทร์", course: "IT320 / Web Application Development", slotId: 3 },
  { day: "วันพุธ", course: "IT453 / RPA Development", slotId: 3 },
  { day: "วันพฤหัสบดี", course: "IT464 / Web Administration", slotId: 4 },
];

// TODO(backend): profile score and penalty-history endpoints
export const penaltyHistory = [
  { id: 1, date: "2 เมษายน 2569", points: 5, reason: "ไม่มาใช้ห้องแล็บตามวัน-เวลาที่จองไว้" },
];
