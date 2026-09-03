import React, { useState } from "react";
import Card from "../../components/Card";
import Button from "../../components/Button";
import { MailOpen, Check, X, Users, Calendar, Clock } from "lucide-react";

const initialInvitations = [
  {
    id: 1,
    inviter: "somy@gmail.com",
    date: "11 เมษายน 2569",
    time: "14.00 น. - 16.30 น.",
    room: "ห้องแล็บ 4",
  },
];

export default function Invitation({ notify, onInvitationAccepted }) {
  const [invitations, setInvitations] = useState(initialInvitations);

  const handleAccept = (item) => {
    setInvitations((prev) => prev.filter((inv) => inv.id !== item.id));
    onInvitationAccepted?.({
      id: `invitation-${item.id}`,
      date: item.date,
      slot: item.time,
      room: item.room || "ห้องแล็บ 4",
      status: "confirmed",
      source: "mock-invitation",
    });
    if (notify) notify("ยอมรับคำเชิญเข้าร่วมกลุ่มเรียบร้อยแล้ว");
  };

  const handleReject = (id) => {
    setInvitations((prev) => prev.filter((inv) => inv.id !== id));
    if (notify) notify("ปฏิเสธคำเชิญเรียบร้อยแล้ว");
  };

  const EmptyState = () => (
    <div className="py-16 flex flex-col items-center justify-center text-slate-400">
      <div className="w-12 h-12 rounded-2xl bg-slate-50 flex items-center justify-center mb-3 border border-slate-100">
        <MailOpen size={24} strokeWidth={1.5} className="text-slate-300" />
      </div>
      <span className="text-xs font-medium text-slate-500">ไม่มีรายการคำเชิญในขณะนี้</span>
      <span className="text-caption mt-0.5">เมื่อเพื่อนส่งคำเชิญเข้ากลุ่ม รายการจะปรากฏที่นี่</span>
    </div>
  );

  return (
    <div className="w-full max-w-6xl mx-auto">
      <div className="mb-5">
        <h1 className="text-xl md:text-2xl font-bold text-ink tracking-tight">รายการคำเชิญเข้าร่วมกลุ่ม</h1>
        <p className="text-caption mt-0.5">ตอบรับหรือปฏิเสธคำชวนใช้งานห้องแล็บร่วมกับเพื่อน</p>
      </div>

      {/* Mobile: การ์ดแบบ stack */}
      <div className="md:hidden flex flex-col gap-3">
        {invitations.length > 0 ? (
          invitations.map((item) => (
            <Card key={item.id} className="p-4">
              <div className="flex items-start gap-3 mb-3">
                <div className="w-9 h-9 rounded-full bg-navy-50 flex items-center justify-center text-navy-800 shrink-0">
                  <Users size={15} />
                </div>
                <div className="min-w-0">
                  <div className="font-semibold text-ink text-sm truncate">{item.inviter}</div>
                  <div className="flex items-center gap-1.5 text-caption mt-0.5">
                    <Calendar size={11} /> {item.date}
                  </div>
                  <div className="flex items-center gap-1.5 text-caption mt-0.5">
                    <Clock size={11} /> {item.time} · {item.room}
                  </div>
                </div>
              </div>
              <div className="flex items-center gap-2">
                <Button variant="danger" icon={X} iconPosition="left" fullWidth onClick={() => handleReject(item.id)}>
                  ปฏิเสธ
                </Button>
                <Button variant="success" icon={Check} iconPosition="left" fullWidth onClick={() => handleAccept(item)}>
                  ยอมรับ
                </Button>
              </div>
            </Card>
          ))
        ) : (
          <Card className="p-0">
            <EmptyState />
          </Card>
        )}
      </div>

      {/* Desktop / tablet: ตาราง */}
      <Card className="p-5 md:p-6 hidden md:block">
        <div className="border border-slate-200/80 rounded-2xl overflow-hidden bg-white shadow-soft">
          <div className="overflow-x-auto">
            <div className="min-w-[640px]">
              <div className="bg-slate-50 border-b border-slate-200/80 px-6 py-4">
                <div className="grid grid-cols-12 text-xs font-semibold text-slate-600">
                  <div className="col-span-1">ลำดับ</div>
                  <div className="col-span-4">ผู้เชิญ</div>
                  <div className="col-span-3">วันที่</div>
                  <div className="col-span-2">เวลา / ห้อง</div>
                  <div className="col-span-2 text-center">จัดการคำเชิญ</div>
                </div>
              </div>

              <div className="divide-y divide-slate-100">
                {invitations.length > 0 ? (
                  invitations.map((item, index) => (
                    <div
                      key={item.id}
                      className="grid grid-cols-12 px-6 py-4 items-center text-xs text-slate-700 hover:bg-slate-50/50 transition-colors"
                    >
                      <div className="col-span-1 text-slate-400 font-medium">
                        {String(index + 1).padStart(2, "0")}
                      </div>
                      <div className="col-span-4 font-semibold text-ink truncate pr-3 flex items-center gap-2">
                        <div className="w-7 h-7 rounded-full bg-navy-50 flex items-center justify-center text-navy-800 shrink-0">
                          <Users size={13} />
                        </div>
                        <span className="truncate">{item.inviter}</span>
                      </div>
                      <div className="col-span-3 text-slate-600 font-medium">
                        {item.date}
                      </div>
                      <div className="col-span-2 text-slate-600 flex flex-col">
                        <span className="font-medium">{item.time}</span>
                        <span className="text-[11px] text-muted">{item.room}</span>
                      </div>
                      <div className="col-span-2 flex items-center justify-center gap-2">
                        <Button variant="danger" size="sm" icon={X} onClick={() => handleReject(item.id)}>
                          ปฏิเสธ
                        </Button>
                        <Button variant="success" size="sm" icon={Check} onClick={() => handleAccept(item)}>
                          ยอมรับ
                        </Button>
                      </div>
                    </div>
                  ))
                ) : (
                  <EmptyState />
                )}
              </div>
            </div>
          </div>
        </div>
      </Card>
    </div>
  );
}