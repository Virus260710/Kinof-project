# KINOF - ระบบดูแลและจองห้องคอมพิวเตอร์

Frontend prototype แยกไฟล์ตามหน้า/component เพื่อให้ต่อ backend ทีหลังได้ง่าย ไม่มีระบบสแกนใบหน้าในเวอร์ชันนี้
(login ใช้ email/password + ปุ่ม Google/Facebook แบบ mock แทนไปก่อน จนกว่าจะเลือกอุปกรณ์กล้อง/AI ที่จะใช้จริง)

## โครงสร้างโปรเจกต์

```
src/
  theme.js                 สี/ธีมกลาง (navy, gold)
  App.jsx                  shell หลัก - state, routing ระหว่างหน้า, ส่ง props ลงแต่ละหน้า
  main.jsx                 entry point ของ React

  data/
    mockData.js             ข้อมูลจำลองทั้งหมด (ดูหัวข้อ "จุดต่อ backend" ด้านล่าง)

  components/               ชิ้นส่วน UI ที่ใช้ซ้ำได้ทุกหน้า
    Card.jsx
    Pill.jsx                 badge สถานะ
    Toast.jsx                แจ้งเตือนมุมขวาล่าง
    TopBar.jsx                แถบบนแสดงชื่อผู้ใช้ที่ล็อกอิน
    Sidebar.jsx                เมนูซ้าย (เปลี่ยนตาม role)

  pages/
    Login.jsx                 เลือก role -> ฟอร์ม login / OAuth mock

    user/
      UserHome.jsx             หน้าหลักผู้ใช้งาน
      BookRoom.jsx              จองห้องแล็บ (wizard 3 ขั้นตอน)
      UserProfile.jsx            โปรไฟล์ + คะแนน + ตารางเรียน
      UserHelp.jsx                 ส่งคำร้องขอความช่วยเหลือ

    admin/
      AdminDashboard.jsx         แดชบอร์ดสรุป
      AdminMonitor.jsx            ตรวจสอบการใช้งาน (log / โปรแกรม / เว็บไซต์ / กิจกรรมน่าสงสัย)
      AdminExport.jsx              ส่งออกข้อมูล
      AdminHelpCenter.jsx           จัดการคำร้องขอความช่วยเหลือ
```

## รันดูตัวอย่าง

```
npm install
npm run dev
```

เปิด http://localhost:5173

## จุดต่อ backend (TODO markers ในโค้ด)

ทุกจุดที่ต้องเปลี่ยนจาก mock เป็น API จริงมีคอมเมนต์ `// TODO(backend): ...` กำกับไว้ในไฟล์ที่เกี่ยวข้องแล้ว สรุปchangepoint หลัก:

| ข้อมูล/การกระทำ | ไฟล์ | endpoint แนะนำ |
|---|---|---|
| Login (email/password) | `pages/Login.jsx` | `POST /api/auth/login` |
| Login ผ่าน Google/Facebook | `pages/Login.jsx` | `POST /api/auth/oauth/google`, `/facebook` |
| ประวัติการจองของผู้ใช้ | `pages/user/UserHome.jsx`, `App.jsx` | `GET /api/bookings/me` |
| ตารางเรียนของผู้ใช้ (เพื่อกำหนดห้องอัตโนมัติ) | `pages/user/BookRoom.jsx` | `GET /api/schedule/me?date=` |
| ห้องว่างในแต่ละช่วงเวลา | `pages/user/BookRoom.jsx` | `GET /api/rooms/available?startTime=&endTime=` |
| ค้นหาเพื่อนเพื่อเชิญเข้ากลุ่ม | `pages/user/BookRoom.jsx` | `GET /api/invitations/users?query=` |
| ยืนยันการจอง | `pages/user/BookRoom.jsx` | `POST /api/bookings` |
| โปรไฟล์ + คะแนน | `pages/user/UserProfile.jsx` | auth ใช้ `GET /api/auth/me`; คะแนนยังรอ API |
| คำร้องขอความช่วยเหลือ (ผู้ใช้) | `pages/user/UserHelp.jsx` | `GET /api/problem-reports/me`, `POST /api/problem-reports` |
| สรุปแดชบอร์ด (เครื่อง/ผู้ใช้งาน) | `pages/admin/AdminDashboard.jsx` | `GET /api/machines/summary`, `GET /api/sessions/active` |
| ประวัติเข้า-ออกระบบ (login/logout log) | `pages/admin/AdminMonitor.jsx` | `GET /api/sessions?date=` |
| โปรแกรมที่ถูกใช้งาน | `pages/admin/AdminMonitor.jsx` | `GET /api/usage/programs?date=` |
| บล็อค/เลิกบล็อคเว็บไซต์ | `pages/admin/AdminMonitor.jsx` | `GET/POST/DELETE /api/websites/blocked` |
| กิจกรรมน่าสงสัย + หักคะแนน | `pages/admin/AdminMonitor.jsx` | `GET /api/usage/flagged`, `POST /api/users/:id/penalize` |
| ส่งออกรายงาน | `pages/admin/AdminExport.jsx` | `POST /api/exports` |
| จัดการคำร้อง (รับเรื่อง/เสร็จสิ้น) | `pages/admin/AdminHelpCenter.jsx` | `GET /api/problem-reports`, `PATCH /api/problem-reports/:id/status` |
| ประตูเปิดจากการสแกนใบหน้า (ยังไม่ทำ) | - | รอเลือกอุปกรณ์กล้อง/เอนจิน face recognition ก่อน |

ปัจจุบัน `App.jsx` เป็นเจ้าของ auth/routing และ state หลัก โดย auth เก็บใน
`sessionStorage["kinofAuth"]`; API client ใน `src/api/` แนบ token และ refresh token ให้อัตโนมัติ
