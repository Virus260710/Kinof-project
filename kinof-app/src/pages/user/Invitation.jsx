import React, { useState } from "react";
import TopBar from "../../components/TopBar";
import Card from "../../components/Card";
import { MailOpen } from "lucide-react";

// Mock Data รายการคำเชิญเข้าร่วมกลุ่ม
const initialInvitations = [
  {
    id: 1,
    inviter: "somy@gmail.com",
    date: "11 เมษายน 2569",
    time: "14.00 น. - 16.30 น.",
    room: "ห้องแล็บ 4",
  },
];

// TODO(backend):
// - get invitations -> GET /api/invitations/me
// - accept -> POST /api/invitations/:id/accept
// - reject -> POST /api/invitations/:id/reject
export default function Invitation({ notify, addBooking }) {
  const [invitations, setInvitations] = useState(initialInvitations);

  // ฟังก์ชันตอบรับคำเชิญ
  const handleAccept = (item) => {
    setInvitations((prev) => prev.filter((inv) => inv.id !== item.id));

    // เพิ่มรายการเข้าสู่รายการจองหลัก
    if (addBooking) {
      addBooking({
        date: item.date,
        slot: item.time,
        room: item.room || "ห้องแล็บ 4",
      });
    }

    if (notify) {
      notify("ยอมรับคำเชิญเข้าร่วมกลุ่มเรียบร้อยแล้ว");
    }
  };

  // ฟังก์ชันปฏิเสธคำเชิญ
  const handleReject = (id) => {
    setInvitations((prev) => prev.filter((inv) => inv.id !== id));
    if (notify) {
      notify("ปฏิเสธคำเชิญเรียบร้อยแล้ว");
    }
  };

  return (
    <div className="w-full">
      <TopBar name="สมหญิง ส." />

      <h1 className="text-base md:text-lg font-bold text-gray-900 mb-4">
        รายการคำเชิญเข้าร่วมกลุ่ม
      </h1>

      <Card className="p-4 md:p-6">
        {/* กล่องตารางรายการคำเชิญ */}
        <div className="border border-gray-100 rounded-2xl overflow-hidden shadow-sm bg-white">
          <div className="overflow-x-auto">
            <div className="min-w-[600px]">
              {/* ส่วนหัวตาราง (Table Header) */}
              <div className="bg-gray-50/80 border-b border-gray-100 px-6 py-3.5">
                <div className="grid grid-cols-12 text-xs font-semibold text-gray-800">
                  <div className="col-span-1">ลำดับ</div>
                  <div className="col-span-4">ผู้เชิญ</div>
                  <div className="col-span-3">วันที่</div>
                  <div className="col-span-2">เวลา</div>
                  <div className="col-span-2 text-center">จัดการ</div>
                </div>
              </div>

              {/* รายการคำเชิญ (Table Body) */}
              <div className="divide-y divide-gray-100">
                {invitations.length > 0 ? (
                  invitations.map((item, index) => (
                    <div
                      key={item.id}
                      className="grid grid-cols-12 px-6 py-4 items-center text-xs text-gray-700 hover:bg-gray-50/40 transition-colors"
                    >
                      <div className="col-span-1 text-gray-600 font-medium">
                        {index + 1}
                      </div>
                      <div className="col-span-4 font-normal text-gray-800 truncate pr-2">
                        {item.inviter}
                      </div>
                      <div className="col-span-3 text-gray-700">
                        {item.date}
                      </div>
                      <div className="col-span-2 text-gray-700">
                        {item.time}
                      </div>
                      <div className="col-span-2 flex items-center justify-center gap-2">
                        {/* ปุ่มปฏิเสธ (สีแดง) */}
                        <button
                          type="button"
                          onClick={() => handleReject(item.id)}
                          className="bg-[#DC2626] hover:bg-[#B91C1C] text-white text-xs font-medium px-3.5 py-1.5 rounded-full transition-colors shadow-sm shrink-0"
                        >
                          ปฏิเสธ
                        </button>

                        {/* ปุ่มยอมรับ (สีเขียว) */}
                        <button
                          type="button"
                          onClick={() => handleAccept(item)}
                          className="bg-[#16A34A] hover:bg-[#15803D] text-white text-xs font-medium px-3.5 py-1.5 rounded-full transition-colors shadow-sm shrink-0"
                        >
                          ยอมรับ
                        </button>
                      </div>
                    </div>
                  ))
                ) : (
                  /* กรณีไม่มีคำเชิญ */
                  <div className="py-16 flex flex-col items-center justify-center text-gray-400">
                    <MailOpen size={36} strokeWidth={1.5} className="mb-2 text-gray-300" />
                    <span className="text-xs">ไม่มีรายการคำเชิญในขณะนี้</span>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      </Card>
    </div>
  );
}