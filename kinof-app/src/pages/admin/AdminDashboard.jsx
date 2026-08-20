import React from "react";
import { Monitor, Users, LifeBuoy } from "lucide-react";
import TopBar from "../../components/TopBar";
import Card from "../../components/Card";
import { initialLog } from "../../data/mockData";

// TODO(backend):
// - machine availability -> GET /api/machines/summary
// - active users -> GET /api/sessions/active
// - pending tickets count -> derived from tickets prop (GET /api/tickets)
export default function AdminDashboard({ tickets }) {
  const pending = tickets.filter((t) => t.status !== "เสร็จสิ้น").length;
  return (
    <div>
      <TopBar name="แอดมิน สมชาย" />
      <h1 className="text-lg font-medium text-gray-900 mb-4">แดชบอร์ด</h1>
      <div className="grid grid-cols-3 gap-4">
        <Card className="p-5">
          <Monitor size={17} className="text-gray-400 mb-2" />
          <div className="text-2xl font-medium text-gray-900">18 / 24</div>
          <div className="text-xs text-gray-400">เครื่องที่พร้อมใช้งาน</div>
        </Card>
        <Card className="p-5">
          <Users size={17} className="text-gray-400 mb-2" />
          <div className="text-2xl font-medium text-gray-900">{initialLog.filter((l) => l.status === "ล็อคอินอยู่").length}</div>
          <div className="text-xs text-gray-400">ผู้ใช้งานปัจจุบัน</div>
        </Card>
        <Card className="p-5">
          <LifeBuoy size={17} className="text-gray-400 mb-2" />
          <div className="text-2xl font-medium text-gray-900">{pending}</div>
          <div className="text-xs text-gray-400">คำขอความช่วยเหลือที่รอดำเนินการ</div>
        </Card>
      </div>
    </div>
  );
}
