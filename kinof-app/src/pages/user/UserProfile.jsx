import React from "react";
import TopBar from "../../components/TopBar";
import Card from "../../components/Card";
import Pill from "../../components/Pill";
import { NAVY, NAVY2, GOLD } from "../../theme";
import { scheduleRows } from "../../data/mockData";

// TODO(backend): profile + score -> GET /api/users/me, schedule -> GET /api/schedule/me
export default function UserProfile() {
  return (
    <div>
      <TopBar name="สมหญิง ส." />
      <h1 className="text-lg font-medium text-gray-900 mb-4">โปรไฟล์</h1>
      <div className="rounded-xl text-white p-6 mb-6" style={{ background: `linear-gradient(120deg, ${NAVY}, ${NAVY2})` }}>
        <div className="font-medium mb-1">สวัสดี สมหญิง!</div>
        <div className="text-sm text-gray-300">ข้อมูลและรายละเอียดโปรไฟล์ของคุณ</div>
      </div>
      <div className="grid grid-cols-3 gap-4 mb-6">
        <Card className="p-5 col-span-2">
          <div className="flex items-center gap-3 mb-4">
            <div className="w-11 h-11 rounded-full flex items-center justify-center text-white font-medium" style={{ background: GOLD }}>
              ส
            </div>
            <div>
              <div className="font-medium text-gray-900">สมหญิง สวยงาม</div>
              <Pill tone="blue">นักศึกษา</Pill>
            </div>
          </div>
          <div className="grid grid-cols-2 gap-y-2 text-xs text-gray-500">
            <div>อีเมล</div><div className="text-gray-800">somy@gmail.com</div>
            <div>ชื่อบัญชี</div><div className="text-gray-800">som_ying</div>
            <div>รหัสผู้ใช้</div><div className="text-gray-800">0005</div>
          </div>
        </Card>
        <Card className="p-5 flex flex-col gap-3">
          <div>
            <div className="text-xs text-gray-400">สถิติการใช้งาน</div>
            <div className="flex gap-3 mt-2">
              <div className="flex-1 text-center border border-gray-100 rounded-lg py-2">
                <div className="font-medium text-gray-900">3</div>
                <div className="text-[10px] text-gray-400">ครั้งที่จอง</div>
              </div>
              <div className="flex-1 text-center border border-gray-100 rounded-lg py-2">
                <div className="font-medium text-gray-900">5</div>
                <div className="text-[10px] text-gray-400">ชั่วโมงที่ใช้</div>
              </div>
            </div>
          </div>
          <div className="rounded-lg py-2 text-center text-sm font-medium text-white" style={{ background: NAVY }}>
            95 / 100 คะแนนการใช้งาน
          </div>
        </Card>
      </div>
      <Card className="p-5">
        <div className="text-sm font-medium text-gray-900 mb-3">ตารางเรียน</div>
        <div className="flex flex-col gap-2">
          {scheduleRows.map((r) => (
            <div key={r.day} className="flex items-center border border-gray-100 rounded-lg overflow-hidden text-xs">
              <div className="bg-gray-50 text-gray-500 px-4 py-3 w-28 shrink-0">{r.day}</div>
              <div className="px-4 py-3 flex-1 text-gray-700">{r.course || "— ไม่มีคาบเรียน —"}</div>
              <div className="px-4 py-3 text-gray-500 w-52 shrink-0 text-right">
                {r.time} {r.room && `· ${r.room}`}
              </div>
            </div>
          ))}
        </div>
      </Card>
    </div>
  );
}
