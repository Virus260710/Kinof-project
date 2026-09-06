# KINOF Admin / Schedule Spec — Final v1.1

สถานะ: ตัดสินใจครบแล้ว — ใช้ implement Phase นี้  
อัปเดต: 7 ก.ย. 2569

ยังไม่ทำใน Phase นี้: Kiosk ตรวจตารางเรียน, Partial booking หลังเลิกเรียน, auto-sync จากระบบมหาวิทยาลัย

---

## 1. บทบาทและสิทธิ์

| Role | จำนวน | หน้าที่ |
|------|-------|--------|
| **superadmin** | 2–3 คน (seed 3) | จัดการ admin + ดู admin audit log |
| **admin** | หลายคน | งาน lab ประจำวัน (ตาราง/ห้อง/monitor) |
| student / external | — | ใช้งาน user ตามเดิม |

ไม่มี role `staff`

| งาน | admin | superadmin |
|-----|:-----:|:----------:|
| Dashboard / Monitor / Export / Help | ✅ | ✅ |
| จัดการข้อมูล → ตารางเรียน | ✅ CRUD + import | ✅ |
| จัดการข้อมูล → ห้องแล็บ | ✅ CRUD + capacity | ✅ |
| จัดการข้อมูล → ผู้ดูแลระบบ | ❌ | ✅ |
| ดู Log แอดมิน | ❌ | ✅ |

### Auth Admin

| หัวข้อ | ตัดสิน |
|--------|--------|
| สร้าง admin | Superadmin เท่านั้น → ส่งลิงก์ email ตั้งรหัส |
| Login | password + OTP email ทุกครั้ง |
| Face | ไม่ต้อง |
| ลบ admin | `status = disabled` (soft delete, เก็บประวัติ) |
| ตำแหน่ง | `job_title` optional |

### Seed Superadmin

- 3 บัญชี: `superadmin1` / `superadmin2` / `superadmin3`
- username/email ใน `Seed:SuperAdmins` (appsettings)
- password จาก `Seed:SuperAdminPassword` (user-secrets / env) — **ห้าม commit**
- Development fallback: `SuperAdmin123!` ถ้ายังไม่ตั้ง secrets
- Production: ต้องตั้ง secrets และเปลี่ยนรหัสทันทีหลัง seed

---

## 2. เวลา — Schedule vs Booking

| ระบบ | รูปแบบเวลา | ตัวอย่าง |
|------|-----------|----------|
| Schedule (กลุ่ม 1) | Free time `start_time` + `end_time` | 08:40–11:00 |
| Booking (กลุ่ม 2–3) | Slot 4 รอบ | 09:00–11:30, 11:30–14:00, 14:00–16:30, 16:30–19:00 |

Timezone: **Asia/Bangkok** ทุก datetime / slot / schedule

คาบเรียนเป็นช่วงครึ่งเปิด `[start, end)` — จบ 09:00 = ใช้ได้ตั้งแต่ 09:00

ขัดกันเมื่อ: `schedule.start < booking.end` AND `schedule.end > booking.start`

- Booking ทับ schedule แม้ส่วนเดียว → **block ทั้งรอบ** (MVP)
- Schedule 2 คาบ ห้องเดียวกันทับกัน → **reject** ตอน save/import
- Partial booking หลังเลิกเรียน = Phase ถัดไป

---

## 3. เทอม / ปีการศึกษา

| ฟิลด์ | ตัวอย่าง |
|-------|----------|
| `academic_year` | `2569` |
| `semester` | `1` หรือ `2` |
| แสดงผล | `1/2569` |

Import เทอมใหม่ (ปี+เทอมต่างจากเทอมที่ active อยู่): ปิด `is_active=false` **ทั้งเทอมเก่าทุกห้อง**  
Kiosk / booking / แสดงผล ใช้ `is_active = true` เท่านั้น  
ลบ schedule = soft delete (`is_active=false`)

---

## 4. รหัสนักศึกษา + Enrollment

| หัวข้อ | ตัดสิน |
|--------|--------|
| Format | 10 หลัก ตัวเลข เช่น `1660705443` |
| มี user แล้ว | สร้าง `schedule_enrollments` |
| ยังไม่สมัคร | เก็บ pending (`student_id` + `schedule_id`) |
| สมัครทีหลัง | auto-link pending → enrollment |
| สร้างบัญชีเปล่า auto | ไม่ทำ |
| จำนวนต่อคาบ | enrolled + pending ≤ `room.capacity` |
| แก้/ลบทีละคน | Admin ทำได้ใน UI |

---

## 5. Template Excel — 2 sheet

Header:

| แถว | เนื้อหา |
|-----|---------|
| 1 | ชื่อ template (ไทย) |
| 2 | คำอธิบาย (ไทย) |
| 3 | **key อังกฤษ** (backend อ่านแถวนี้) |
| 4+ | ข้อมูล |

**Sheet `schedules`:**  
`subject_code, subject_name, section, instructor_name, day_of_week, start_time, end_time, academic_year, semester, room_name`

**Sheet `enrollments`:**  
`subject_code, section, day_of_week, start_time, room_name, student_id`

ผูก sheet ด้วย composite key (ไม่ใช้ UUID ใน Excel)

`day_of_week` ใน Excel: `MON/TUE/WED/THU/FRI/SAT/SUN` → DB `0=Sun … 6=Sat`

Flow: อัปโหลด → validate → preview (error/warning/pending) → ยืนยัน → บันทึก

---

## 6. ห้องแล็บ + Seat

| การเปลี่ยน | พฤติกรรม |
|-----------|----------|
| capacity เพิ่ม | สร้าง seat ใหม่ auto |
| capacity ลด | ได้ถ้า seat ที่จะตัดไม่ `occupied` |
| ลบห้อง | ได้ถ้าไม่มี schedule active + booking อนาคต — ไม่งั้น `status=closed` |

---

## 7. Admin Audit Log

Log: `admin.create/update/disable`, `schedule.create/update/delete/import`, `room.create/update/delete/capacity_change`  
ไม่ log login / เปิดดู dashboard  
เก็บ **90 วัน** — Superadmin ลบ/แก้ log ไม่ได้

---

## 8. API Phase นี้

- Superadmin CRUD admin + email invite
- Admin CRUD schedule + enrollment + pending
- Import preview → confirm
- Room CRUD + capacity/seats
- Booking overlap check (block ทั้งรอบ)
- `GET /api/schedule/me`
- Register → auto-link pending
- Admin audit log (90 วัน, GET เท่านั้น)
