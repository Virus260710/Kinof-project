// Mock data standing in for backend responses during frontend development.
// See README.md "Backend integration points" for the endpoint each of these should come from.

export const students = [
  { id: "som_ying", name: "สมหญิง สวยงาม", type: "นักศึกษา", score: 95, room: 4 },
  { id: "iceeyy", name: "ไอซี่ ใจดี", type: "นักศึกษา", score: 78, room: 2 },
  { id: "keetauy", name: "คีตะ อยู่ดี", type: "นักศึกษา", score: 88, room: 3 },
  { id: "fighteiei", name: "ไฟท์ สู้ไป", type: "บุคคลภายนอก", score: null, room: 3 },
  { id: "loan_za5187", name: "โลน ซาบซึ้ง", type: "บุคคลภายนอก", score: null, room: 1 },
];

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

export const initialTickets = [
  { id: 1, user: "som_ying", title: "คีย์บอร์ดพัง", room: "ห้องแล็บ 1", machine: 4, detail: "คีย์บอร์ดที่กดแล้ว 3 เครื่องที่ 04 พิมพ์ผิดตำแหน่งคำ", status: "รอดำเนินการ" },
  { id: 2, user: "iceeyy", title: "จอไม่ติด", room: "ห้องแล็บ 2", machine: 9, detail: "เปิดเครื่องแล้วจอไม่ขึ้นภาพเลย", status: "รอดำเนินการ" },
  { id: 3, user: "keetauy", title: "จองห้องเล่นไม่ได้", room: "ห้องแล็บ 3", machine: 23, detail: "กดยืนยันการจองแล้วระบบค้าง", status: "กำลังดำเนินการ" },
];

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

// TODO(backend): invitation/member lookup API
export const bookingUsers = [
  { name: "กิตติ ศักดิ์", email: "kitti_sak5187@gmail.com", status: "joined" },
  { name: "ไฟท์", email: "fighteiei@gmail.com", status: "pending" },
  { name: "วรากร ใจดี", email: "warakorn@gmail.com", status: "joined" },
  { name: "ปิยะนันท์ บุญมี", email: "piyanan@gmail.com", status: "pending" },
  { name: "ธนภัทร วงศ์ษา", email: "thanaphat@gmail.com", status: "joined" },
];

// TODO(backend): profile score and penalty-history endpoints
export const penaltyHistory = [
  { id: 1, date: "2 เมษายน 2569", points: 5, reason: "ไม่มาใช้ห้องแล็บตามวัน-เวลาที่จองไว้" },
];
