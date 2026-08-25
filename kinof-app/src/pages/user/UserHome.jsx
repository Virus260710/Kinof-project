import React from "react";
import { Calendar, Clock, Plus, ArrowRight, CheckCircle2 } from "lucide-react";
import TopBar from "../../components/TopBar";
import Card from "../../components/Card";
import Pill from "../../components/Pill";
import { NAVY, NAVY2 } from "../../theme";

// TODO(backend): myBookings should come from GET /api/bookings?user=me
export default function UserHome({ setPage, myBookings = [], auth }) {
  const latest = myBookings.length > 0 ? myBookings[0] : null;
  const displayName = auth?.user?.firstName ?? "ผู้ใช้งาน";
  const topBarName = auth?.user ? `${auth.user.firstName} ${auth.user.lastName?.[0] ?? ""}.` : "ผู้ใช้งาน";

  return (
    <div className="w-full">
      <TopBar name={topBarName} />
      <h1 className="text-base md:text-lg font-medium text-gray-900 mb-4">หน้าหลัก</h1>

      {/* แบนเนอร์ต้อนรับ */}
      <div
        className="rounded-2xl text-white p-5 md:p-6 mb-6 shadow-sm"
        style={{ background: `linear-gradient(120deg, ${NAVY}, ${NAVY2})` }}
      >
        <div className="font-semibold text-sm md:text-base mb-1">สวัสดี {displayName}!</div>
        <div className="text-xs text-blue-100 font-light">
          ยินดีต้อนรับเข้าสู่ระบบจองห้องแล็บ KINOF
        </div>
      </div>

      {/* Grid การ์ดสรุปข้อมูล 2 กล่อง */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-6">
        {/* การ์ด 1: การจองปัจจุบัน */}
        <Card className="p-4 md:p-5 flex flex-col justify-between">
          <div>
            <div className="text-xs text-gray-400 font-medium mb-1.5 flex items-center gap-1.5">
              <Calendar size={14} className="text-gray-400" />
              <span>การจองปัจจุบัน</span>
            </div>
            <div className="font-bold text-gray-800 text-sm">
              {latest ? latest.room : "ยังไม่มีการจองในขณะนี้"}
            </div>
            {latest && (
              <div className="text-xs text-gray-500 mt-2 flex flex-wrap items-center gap-2">
                <span className="bg-gray-100 px-2 py-0.5 rounded-md font-medium text-gray-600">
                  {latest.date}
                </span>
                <span>•</span>
                <span className="flex items-center gap-1 text-gray-500">
                  <Clock size={12} /> {latest.slot}
                </span>
              </div>
            )}
          </div>
          {latest && (
            <div className="mt-3 pt-2 border-t border-gray-100 flex items-center gap-1 text-[11px] text-emerald-600 font-medium">
              <CheckCircle2 size={13} /> สถานะพร้อมเข้าใช้งาน
            </div>
          )}
        </Card>

        {/* การ์ด 2: จองห้องแล็บใหม่ */}
        <Card className="p-4 md:p-5 flex flex-col justify-between">
          <div>
            <div className="text-xs text-gray-400 font-medium mb-1 flex items-center gap-1.5">
              <Plus size={14} className="text-gray-400" />
              <span>จองห้องแล็บใหม่</span>
            </div>
            <div className="text-xs text-gray-500 mt-1">
              มีการจองรวมทั้งหมด <span className="font-bold text-gray-800">{myBookings.length}</span> ครั้ง
            </div>
          </div>
          <button
            type="button"
            onClick={() => setPage("book")}
            className="mt-4 self-start text-white text-xs font-medium rounded-xl px-4 py-2.5 flex items-center gap-2 shadow-sm hover:opacity-90 transition-all"
            style={{ background: NAVY }}
          >
            <span>เริ่มต้นการจองใหม่</span>
            <ArrowRight size={14} />
          </button>
        </Card>
      </div>

      {/* ตารางประวัติการจอง */}
      <Card className="p-4 md:p-5">
        <div className="text-sm font-semibold text-gray-800 mb-4">ประวัติการจอง</div>

        {myBookings.length === 0 ? (
          <div className="text-xs text-gray-400 text-center py-12 border border-dashed border-gray-200 rounded-xl">
            คุณยังไม่มีประวัติการจองห้องแล็บ
          </div>
        ) : (
          <div className="border border-gray-100 rounded-xl overflow-x-auto">
            <table className="w-full min-w-[480px] text-xs">
              <thead>
                <tr className="bg-gray-50/80 text-gray-500 text-left border-b border-gray-100">
                  <th className="py-3 px-4 font-semibold">วันที่</th>
                  <th className="py-3 px-4 font-semibold">เวลา</th>
                  <th className="py-3 px-4 font-semibold">ห้อง</th>
                  <th className="py-3 px-4 font-semibold text-center">สถานะ</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {myBookings.map((b, i) => (
                  <tr key={i} className="hover:bg-gray-50/50 transition-colors">
                    <td className="py-3 px-4 text-gray-800 font-medium">{b.date}</td>
                    <td className="py-3 px-4 text-gray-600">{b.slot}</td>
                    <td className="py-3 px-4 text-gray-800">{b.room}</td>
                    <td className="py-3 px-4 text-center">
                      <Pill tone="green">ยืนยันแล้ว</Pill>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
    </div>
  );
}