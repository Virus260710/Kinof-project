import React from "react";
import TopBar from "../../components/TopBar";
import Card from "../../components/Card";
import Pill from "../../components/Pill";
import { NAVY, NAVY2 } from "../../theme";

// TODO(backend): myBookings should come from GET /api/bookings?user=me
export default function UserHome({ setPage, myBookings }) {
  const latest = myBookings[myBookings.length - 1];
  return (
    <div>
      <TopBar name="สมหญิง ส." />
      <h1 className="text-lg font-medium text-gray-900 mb-4">หน้าหลัก</h1>
      <div className="rounded-xl text-white p-6 mb-6" style={{ background: `linear-gradient(120deg, ${NAVY}, ${NAVY2})` }}>
        <div className="font-medium mb-1">สวัสดี สมหญิง!</div>
        <div className="text-sm text-gray-300">ยินดีต้อนรับเข้าสู่ระบบจองห้องแล็บ KINOF</div>
      </div>
      <div className="grid grid-cols-2 gap-4 mb-6">
        <Card className="p-5">
          <div className="text-xs text-gray-400 mb-1">การจองปัจจุบัน</div>
          <div className="font-medium text-gray-900">{latest ? latest.room : "ยังไม่มีการจอง"}</div>
          {latest && <div className="text-xs text-gray-400 mt-1">{latest.date} / {latest.slot}</div>}
        </Card>
        <Card className="p-5 flex flex-col justify-between">
          <div>
            <div className="text-xs text-gray-400 mb-1">จองห้องแล็บใหม่</div>
            <div className="text-xs text-gray-500">มีการจอง {myBookings.length} ครั้งในเดือนนี้</div>
          </div>
          <button onClick={() => setPage("book")} className="mt-3 self-start text-white text-xs font-medium rounded-lg px-3 py-2" style={{ background: NAVY }}>
            เริ่มต้นการจองใหม่
          </button>
        </Card>
      </div>
      <Card className="p-5">
        <div className="text-sm font-medium text-gray-900 mb-3">ประวัติการจอง</div>
        {myBookings.length === 0 ? (
          <div className="text-xs text-gray-400 text-center py-10">คุณยังไม่มีประวัติการจองห้องแล็บ</div>
        ) : (
          <table className="w-full text-xs">
            <thead>
              <tr className="text-gray-400 text-left border-b border-gray-100">
                <th className="pb-2 font-normal">วันที่</th>
                <th className="pb-2 font-normal">เวลา</th>
                <th className="pb-2 font-normal">ห้อง</th>
                <th className="pb-2 font-normal">สถานะ</th>
              </tr>
            </thead>
            <tbody>
              {myBookings.map((b, i) => (
                <tr key={i} className="border-b border-gray-50">
                  <td className="py-2 text-gray-700">{b.date}</td>
                  <td className="py-2 text-gray-700">{b.slot}</td>
                  <td className="py-2 text-gray-700">{b.room}</td>
                  <td className="py-2"><Pill tone="amber">ยืนยันแล้ว</Pill></td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </Card>
    </div>
  );
}
