import React from "react";
import Card from "../../components/Card";
import Pill from "../../components/Pill";
import { User, Mail, Hash, BookOpen, AlertCircle, Award } from "lucide-react";

const myScheduleRows = [
  { day: "วันจันทร์", course: "IT319 / Low-Code and No-Code Development Platform", time: "09.00 น. - 11.30 น.", room: "ห้องแล็บ 1" },
  { day: "วันจันทร์", course: "IT320 / Web Application Development", time: "13.30 น. - 16.00 น.", room: "ห้องแล็บ 3" },
  { day: "วันอังคาร", course: "— ไม่มีคาบเรียน —", time: "", room: "" },
  { day: "วันพุธ", course: "IT453 / Robotic Process Automation Development", time: "14.00 น. - 16.30 น.", room: "ห้องแล็บ 3" },
  { day: "วันพฤหัสบดี", course: "IT464 / Web Administration", time: "16.30 น. - 19.00 น.", room: "ห้องแล็บ 2" },
  { day: "วันศุกร์", course: "— ไม่มีคาบเรียน —", time: "", room: "" },
  { day: "วันเสาร์", course: "— ไม่มีคาบเรียน —", time: "", room: "" }
];

const initialPenaltyHistory = [
  { id: 1, date: "2 เมษายน 2569", points: 5, reason: "ไม่มาใช้ห้องแล็บตามวัน-เวลาที่จองไว้" },
];

export default function UserProfile({ scheduleRows = myScheduleRows, userScore = 95 }) {
  const penaltyHistory = initialPenaltyHistory;
  const scoreBarClass = userScore >= 80 ? "bg-teal-500" : userScore >= 50 ? "bg-gold-gradient" : "bg-rose-500";

  return (
    <div className="w-full max-w-6xl mx-auto">
      <div className="mb-5">
        <h1 className="text-xl md:text-2xl font-bold text-ink tracking-tight">โปรไฟล์ผู้ใช้งาน</h1>
        <p className="text-caption mt-0.5">ข้อมูลส่วนตัว คะแนนพฤติกรรม และตารางเรียน</p>
      </div>

      {/* Grid Profile & Stats */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-5 mb-7">
        {/* การ์ดข้อมูลส่วนตัว */}
        <Card className="p-5 md:p-6 lg:col-span-2 flex flex-col justify-between">
          <div>
            <div className="flex items-center gap-4 mb-6">
              <div className="w-14 h-14 rounded-2xl flex items-center justify-center text-navy-900 font-bold text-xl shadow-md shrink-0 bg-gold-gradient">
                ส
              </div>
              <div>
                <div className="flex items-center gap-2 mb-1">
                  <h2 className="font-bold text-ink text-base md:text-lg">สมหญิง สวยงาม</h2>
                  <Pill tone="blue" withDot>นักศึกษา</Pill>
                </div>
                <p className="text-caption">คณะเทคโนโลยีสารสนเทศและนวัตกรรม</p>
              </div>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3.5 text-xs border-t border-slate-100 pt-5">
              <div className="flex items-center gap-2.5 p-3 rounded-xl bg-slate-50/70 border border-slate-100">
                <Mail size={15} className="text-slate-400 shrink-0" />
                <div>
                  <span className="text-muted block text-[10px]">อีเมลมหาวิทยาลัย</span>
                  <span className="text-slate-800 font-medium">somy@gmail.com</span>
                </div>
              </div>

              <div className="flex items-center gap-2.5 p-3 rounded-xl bg-slate-50/70 border border-slate-100">
                <User size={15} className="text-slate-400 shrink-0" />
                <div>
                  <span className="text-muted block text-[10px]">ชื่อบัญชีผู้ใช้</span>
                  <span className="text-slate-800 font-medium">som_ying</span>
                </div>
              </div>

              <div className="flex items-center gap-2.5 p-3 rounded-xl bg-slate-50/70 border border-slate-100">
                <Hash size={15} className="text-slate-400 shrink-0" />
                <div>
                  <span className="text-muted block text-[10px]">รหัสประจำตัว</span>
                  <span className="text-slate-800 font-medium">0005</span>
                </div>
              </div>

              <div className="flex items-center gap-2.5 p-3 rounded-xl bg-slate-50/70 border border-slate-100">
                <Award size={15} className="text-slate-400 shrink-0" />
                <div>
                  <span className="text-muted block text-[10px]">สถานะสิทธิ์การจอง</span>
                  <span className="text-emerald-600 font-medium">ปกติ (ใช้งานได้สมบูรณ์)</span>
                </div>
              </div>
            </div>
          </div>
        </Card>

        {/* การ์ดสถิติ, คะแนน และประวัติการหักคะแนน */}
        <Card className="p-5 md:p-6 flex flex-col gap-4 justify-between">
          <div>
            <div className="text-xs font-semibold text-muted uppercase tracking-wider mb-3">
              สถิติการใช้งานห้องแล็บ
            </div>
            <div className="grid grid-cols-2 gap-3 mb-4">
              <div className="text-center border border-slate-100 bg-slate-50/60 rounded-2xl py-3">
                <div className="font-bold text-ink text-lg">3</div>
                <div className="text-[10px] text-muted mt-0.5">ครั้งที่จองสำเร็จ</div>
              </div>
              <div className="text-center border border-slate-100 bg-slate-50/60 rounded-2xl py-3">
                <div className="font-bold text-ink text-lg">5</div>
                <div className="text-[10px] text-muted mt-0.5">ชั่วโมงที่เข้าใช้งาน</div>
              </div>
            </div>

            <div className="rounded-2xl border border-slate-100 bg-slate-50/60 p-4">
              <div className="flex items-center justify-between mb-2">
                <span className="text-xs font-semibold text-slate-700">คะแนนพฤติกรรม</span>
                <span className="text-sm font-bold text-navy-800">{userScore}<span className="text-muted font-medium text-xs"> / 100</span></span>
              </div>
              <div className="h-2 w-full rounded-full bg-slate-200/70 overflow-hidden">
                <div
                  className={`h-full rounded-full transition-all duration-500 ${scoreBarClass}`}
                  style={{ width: `${userScore}%` }}
                />
              </div>
            </div>
          </div>

          <div>
            <div className="text-xs font-semibold text-slate-700 mb-2 flex items-center gap-1.5">
              <AlertCircle size={14} className="text-slate-400" />
              <span>ประวัติการหักคะแนน</span>
            </div>

            <div className="flex flex-col gap-2">
              {penaltyHistory.length > 0 ? (
                penaltyHistory.map((item) => (
                  <div
                    key={item.id}
                    className="border border-rose-100 rounded-xl p-3 bg-rose-50/30 text-xs"
                  >
                    <div className="font-semibold text-rose-800 mb-0.5">
                      {item.date} • หัก {item.points} คะแนน
                    </div>
                    <div className="text-slate-600 font-light leading-relaxed text-[11px]">
                      สาเหตุ: {item.reason}
                    </div>
                  </div>
                ))
              ) : (
                <div className="text-xs text-muted text-center py-4 border border-dashed border-slate-200 rounded-xl">
                  ไม่มีประวัติการหักคะแนน
                </div>
              )}
            </div>
          </div>
        </Card>
      </div>

      {/* ตารางเรียน */}
      <Card className="p-5 md:p-6">
        <div className="flex items-center gap-2 mb-4">
          <BookOpen size={17} className="text-slate-500" />
          <h3 className="text-sm md:text-base font-bold text-ink">ตารางเรียนประจำภาคการศึกษา</h3>
        </div>

        <div className="border border-slate-200/80 rounded-2xl overflow-hidden">
          <div className="overflow-x-auto">
            <div className="flex flex-col divide-y divide-slate-100 min-w-[500px]">
              {scheduleRows && scheduleRows.length > 0 ? (
                scheduleRows.map((r, i) => (
                  <div
                    key={`${r.day}-${r.course}-${i}`}
                    className="flex items-center text-xs hover:bg-slate-50/50 transition-colors"
                  >
                    <div className="bg-slate-50 text-slate-700 font-semibold px-5 py-3.5 w-32 shrink-0 border-r border-slate-100">
                      {r.day}
                    </div>
                    <div className="px-5 py-3.5 flex-1 font-medium text-slate-800">
                      {r.course || "— ไม่มีคาบเรียน —"}
                    </div>
                    <div className="px-5 py-3.5 text-slate-500 w-60 shrink-0 text-right font-light">
                      {r.time} {r.room && <span className="font-medium text-slate-700">· {r.room}</span>}
                    </div>
                  </div>
                ))
              ) : (
                <div className="text-xs text-muted text-center py-8">ไม่พบข้อมูลตารางเรียน</div>
              )}
            </div>
          </div>
        </div>
      </Card>
    </div>
  );
}