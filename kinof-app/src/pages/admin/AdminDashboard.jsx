import React, { useState } from "react";
import { ArrowRight, LifeBuoy, Monitor, Radar, Users } from "lucide-react";
import Card from "../../components/Card";
import Button from "../../components/Button";
import Pill from "../../components/Pill";
import { trackingRooms, trackingSummary } from "../../data/trackingMock";

const roomTone = { open: "green", closed: "red", maintenance: "amber" };
const roomLabel = { open: "เปิดใช้งาน", closed: "ปิด", maintenance: "ปรับปรุง" };

export default function AdminDashboard({
  problemReports = [],
  setPage,
  onOpenTrackingRoom,
}) {
  const [roomFilter, setRoomFilter] = useState("all");
  const pending = problemReports.filter((report) => report.status !== "เสร็จสิ้น").length;
  const shownRooms = roomFilter === "all"
    ? trackingRooms
    : trackingRooms.filter((room) => room.id === roomFilter);

  return (
    <div className="w-full max-w-7xl mx-auto">
      <div className="mb-5">
        <h1 className="text-xl md:text-2xl font-bold text-ink tracking-tight">แดชบอร์ด</h1>
        <p className="text-xs text-muted mt-1">ภาพรวมสถานะห้องแล็บและรายการที่ต้องดำเนินการ</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-7">
        <SummaryCard
          icon={Users}
          iconClass="bg-navy-50 text-navy-800"
          value={trackingSummary.activeUsers}
          label="ผู้ใช้งานปัจจุบัน"
          linkLabel="ดูภาพรวมการใช้งาน"
          onClick={() => setPage?.("monitor")}
        />
        <SummaryCard
          icon={Monitor}
          iconClass="bg-teal-50 text-teal-500"
          value={<>{trackingSummary.machinesReady}<span className="text-base text-slate-400 font-medium"> / {trackingSummary.machinesTotal}</span></>}
          label="เครื่องพร้อมใช้งาน"
          linkLabel="จัดการเครื่องทั้งหมด"
          onClick={() => setPage?.("tracking")}
        />
        <SummaryCard
          icon={LifeBuoy}
          iconClass="bg-amber-50 text-amber-600"
          value={pending}
          label="คำขอช่วยเหลือค้าง"
          linkLabel="ไปที่ศูนย์แก้ไขปัญหา"
          onClick={() => setPage?.("helpcenter")}
        />
      </div>

      <Card className="p-5 md:p-6">
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 mb-5">
          <div>
            <div className="flex items-center gap-2">
              <Radar size={18} className="text-navy-800" />
              <h2 className="text-base font-bold text-ink">ประวัติการเข้าใช้งานห้องแล็บ</h2>
            </div>
            <p className="text-xs text-muted mt-1">จำนวนผู้ใช้ที่กำลังใช้งาน แยกตามห้อง</p>
          </div>
          <select
            value={roomFilter}
            onChange={(event) => setRoomFilter(event.target.value)}
            className="text-xs bg-white border border-slate-200 rounded-xl px-3 py-2.5 text-slate-700 focus:outline-none focus:ring-2 focus:ring-navy-100"
            aria-label="กรองห้องแล็บ"
          >
            <option value="all">ทุกห้อง</option>
            {trackingRooms.map((room) => <option key={room.id} value={room.id}>{room.name}</option>)}
          </select>
        </div>

        <div className={`grid gap-4 ${shownRooms.length === 1 ? "grid-cols-1" : "grid-cols-1 sm:grid-cols-2 xl:grid-cols-4"}`}>
          {shownRooms.map((room) => (
            <button
              key={room.id}
              onClick={() => onOpenTrackingRoom?.(room.id)}
              className="text-left rounded-2xl border border-slate-200 bg-slate-50/60 p-4 hover:bg-white hover:border-navy-100 hover:shadow-soft transition-all"
            >
              <div className="flex items-center justify-between gap-2">
                <span className="text-sm font-semibold text-ink">{room.name}</span>
                <Pill tone={roomTone[room.status]} withDot>{roomLabel[room.status]}</Pill>
              </div>
              <div className="mt-5 text-2xl font-bold text-ink">{room.activeUserCount} คน</div>
              <div className="mt-1 text-xs text-muted">กำลังเข้าใช้งาน</div>
              <div className="mt-4 pt-3 border-t border-slate-200 text-xs font-medium text-navy-800 flex items-center gap-1">
                ดูเครื่องในห้อง <ArrowRight size={13} />
              </div>
            </button>
          ))}
        </div>

        <div className="mt-5 pt-5 border-t border-slate-100 flex justify-end">
          <Button variant="secondary" size="sm" icon={ArrowRight} onClick={() => setPage?.("monitor")}>
            ตรวจสอบประวัติทั้งหมด
          </Button>
        </div>
      </Card>
    </div>
  );
}

function SummaryCard({ icon: Icon, iconClass, value, label, linkLabel, onClick }) {
  return (
    <Card className="p-5 md:p-6">
      <div className="flex items-start justify-between">
        <div>
          <div className="text-3xl font-bold text-ink">{value}</div>
          <div className="text-xs text-muted mt-1">{label}</div>
        </div>
        <div className={`w-10 h-10 rounded-xl flex items-center justify-center ${iconClass}`}>
          <Icon size={19} />
        </div>
      </div>
      <button onClick={onClick} className="mt-4 text-xs font-medium text-navy-800 flex items-center gap-1">
        {linkLabel} <ArrowRight size={13} />
      </button>
    </Card>
  );
}
