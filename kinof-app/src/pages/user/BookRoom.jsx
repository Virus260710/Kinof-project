import React, { useState } from "react";
import { ClipboardList, AlertTriangle, Plus, Trash2, Check } from "lucide-react";
import TopBar from "../../components/TopBar";
import Card from "../../components/Card";
import Pill from "../../components/Pill";
import { NAVY } from "../../theme";
import { timeSlots } from "../../data/mockData";

// TODO(backend):
// - hasClassToday / assignedRoom -> GET /api/schedule/me?date=
// - room availability -> GET /api/rooms/available?date=&slot=
// - friend invite lookup -> GET /api/users/lookup?email=
// - submit -> POST /api/bookings { date, slot, room, friends: [] }
export default function BookRoom({ addBooking, notify }) {
  const [step, setStep] = useState(1);
  const [date, setDate] = useState("11 เม.ย. 2569");
  const [slot, setSlot] = useState(null);
  const [friends, setFriends] = useState(["kitti_sak5187@gmail.com"]);
  const [newFriend, setNewFriend] = useState("");
  const [room, setRoom] = useState(null);

  const hasClassToday = true; // mock: student has a scheduled class that day
  const assignedRoom = "ห้องแล็บ 4";
  const steps = ["เลือกวัน-เวลา", "จัดการสมาชิก-ดูห้อง", "ยืนยันการจอง"];

  return (
    <div>
      <TopBar name="สมหญิง ส." />
      <h1 className="text-lg font-medium text-gray-900 mb-4">จองห้องแล็บ</h1>

      <Card className="p-6">
        <div className="flex items-center mb-6">
          {steps.map((s, i) => (
            <React.Fragment key={s}>
              <div className="flex flex-col items-center gap-1.5">
                <div
                  className="w-7 h-7 rounded-full flex items-center justify-center text-xs font-medium"
                  style={step >= i + 1 ? { background: NAVY, color: "white" } : { background: "#EEE", color: "#999" }}
                >
                  {i + 1}
                </div>
                <span className="text-[11px] text-gray-500">{s}</span>
              </div>
              {i < steps.length - 1 && <div className="flex-1 h-px bg-gray-200 mx-3 -mt-4" />}
            </React.Fragment>
          ))}
        </div>

        {step === 1 && (
          <div>
            {hasClassToday ? (
              <div className="flex items-start gap-3 bg-blue-50 border border-blue-100 rounded-lg p-4 mb-5">
                <ClipboardList size={16} className="text-blue-600 mt-0.5" />
                <div className="text-xs text-blue-800">
                  ตารางเรียนของคุณวันนี้ตรงกับวิชาที่ใช้ <b>{assignedRoom}</b> โดยเฉพาะ ระบบจะจองห้องนี้ให้อัตโนมัติ
                  หากต้องการห้องอื่นนอกตารางเรียน สามารถเลือกวัน-เวลาที่ไม่ตรงกับคาบเรียนได้ด้านล่าง
                </div>
              </div>
            ) : (
              <div className="flex items-start gap-3 bg-amber-50 border border-amber-100 rounded-lg p-4 mb-5">
                <AlertTriangle size={16} className="text-amber-600 mt-0.5" />
                <div className="text-xs text-amber-800">คุณไม่มีตารางเรียนในช่วงนี้ สามารถจองห้องที่ว่างจากตารางเรียนได้ตามปกติ</div>
              </div>
            )}
            <div className="grid grid-cols-2 gap-6">
              <div>
                <div className="text-xs text-gray-500 mb-2">เลือกวันที่</div>
                <div className="border border-gray-200 rounded-lg p-4 text-sm text-gray-700">
                  {date} <span className="text-xs text-gray-400 ml-2">(mock date picker)</span>
                </div>
              </div>
              <div>
                <div className="text-xs text-gray-500 mb-2">เลือกเวลา</div>
                <div className="flex flex-col gap-2">
                  {timeSlots.map((s) => (
                    <button
                      key={s}
                      onClick={() => setSlot(s)}
                      className="text-xs text-left rounded-lg px-3 py-2 border"
                      style={slot === s ? { background: NAVY, color: "white", borderColor: NAVY } : { borderColor: "#e5e5e5", color: "#374151" }}
                    >
                      {s}
                    </button>
                  ))}
                </div>
              </div>
            </div>
            <div className="flex justify-end mt-6">
              <button disabled={!slot} onClick={() => setStep(2)} className="text-white text-sm font-medium rounded-lg px-5 py-2.5 disabled:opacity-40" style={{ background: NAVY }}>
                ถัดไป →
              </button>
            </div>
          </div>
        )}

        {step === 2 && (
          <div>
            <div className="text-xs text-gray-500 mb-2">เชิญเพื่อนเข้าร่วมกลุ่ม (สูงสุด 5 คน, ต้องเป็นสมาชิกในเว็บไซต์)</div>
            <div className="flex flex-col gap-2 mb-3">
              {friends.map((f, i) => (
                <div key={i} className="flex items-center justify-between border border-gray-200 rounded-lg px-3 py-2 text-xs">
                  <span className="text-gray-700">{f}</span>
                  <div className="flex items-center gap-2">
                    <Pill tone="green">เข้าร่วมแล้ว</Pill>
                    <button onClick={() => setFriends(friends.filter((x) => x !== f))} className="text-red-500">
                      <Trash2 size={13} />
                    </button>
                  </div>
                </div>
              ))}
            </div>
            <div className="flex gap-2 mb-6">
              <input
                value={newFriend}
                onChange={(e) => setNewFriend(e.target.value)}
                placeholder="กรอกอีเมลเพื่อนเพื่อเชิญเข้ากลุ่ม"
                className="flex-1 text-xs border border-gray-200 rounded-lg px-3 py-2"
              />
              <button
                onClick={() => { if (newFriend) { setFriends([...friends, newFriend]); setNewFriend(""); } }}
                className="flex items-center gap-1 text-xs border border-gray-200 rounded-lg px-3 py-2 text-gray-600 hover:bg-gray-50"
              >
                <Plus size={13} /> เพิ่มเพื่อน
              </button>
            </div>

            <div className="text-xs text-gray-500 mb-2">ห้องที่ว่างในช่วงเวลานี้</div>
            <div className="grid grid-cols-4 gap-3 mb-6">
              {["ห้องแล็บ 1", "ห้องแล็บ 2", "ห้องแล็บ 3", "ห้องแล็บ 4"].map((r) => (
                <button
                  key={r}
                  onClick={() => setRoom(r)}
                  className="border rounded-lg py-3 text-xs"
                  style={room === r ? { borderColor: NAVY, background: "#F4F5F8" } : { borderColor: "#e5e5e5" }}
                >
                  {r}
                </button>
              ))}
            </div>

            <div className="flex justify-between">
              <button onClick={() => setStep(1)} className="text-xs text-gray-500 border border-gray-200 rounded-lg px-4 py-2">
                ย้อนกลับและแก้ไข
              </button>
              <button disabled={!room} onClick={() => setStep(3)} className="text-white text-sm font-medium rounded-lg px-5 py-2.5 disabled:opacity-40" style={{ background: NAVY }}>
                ถัดไป →
              </button>
            </div>
          </div>
        )}

        {step === 3 && (
          <div>
            <div className="text-sm font-medium text-gray-900 mb-3">ยืนยันรายละเอียดการจอง</div>
            <Card className="p-4 mb-6" style={{ background: NAVY, color: "white" }}>
              <div className="grid grid-cols-2 gap-y-2 text-xs">
                <div className="text-gray-300">วันที่</div><div>{date}</div>
                <div className="text-gray-300">เวลา</div><div>{slot}</div>
                <div className="text-gray-300">ห้อง</div><div>{room}</div>
                <div className="text-gray-300">เพื่อนร่วมกลุ่ม</div><div>{friends.length} คน</div>
              </div>
            </Card>
            <div className="flex justify-between">
              <button onClick={() => setStep(2)} className="text-xs text-gray-500 border border-gray-200 rounded-lg px-4 py-2">
                ย้อนกลับและแก้ไข
              </button>
              <button
                onClick={() => {
                  addBooking({ date, slot, room });
                  notify("ยืนยันการจองห้องแล็บเรียบร้อยแล้ว");
                  setStep(1);
                  setSlot(null);
                  setRoom(null);
                }}
                className="text-white text-sm font-medium rounded-lg px-5 py-2.5 flex items-center gap-1.5"
                style={{ background: "#0F6E56" }}
              >
                <Check size={14} /> ยืนยันการจอง
              </button>
            </div>
          </div>
        )}
      </Card>
    </div>
  );
}
