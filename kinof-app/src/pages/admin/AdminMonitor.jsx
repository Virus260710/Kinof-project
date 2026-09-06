import React, { useState } from "react";
import { AlertTriangle, Ban, Globe2, Monitor, Users, X } from "lucide-react";
import Button from "../../components/Button";
import Card from "../../components/Card";
import Pill from "../../components/Pill";
import { NAVY } from "../../theme";
import {
  trackingActivity,
  trackingFlagged,
  trackingPrograms,
  trackingRooms,
  trackingSummary,
  trackingWebsites,
  websiteBlacklist,
} from "../../data/trackingMock";
import { getDisplayName } from "../../utils/displayName";

const TABS = [
  { key: "session", label: "เข้า-ออก" },
  { key: "program", label: "โปรแกรม" },
  { key: "website", label: "เว็บไซต์" },
  { key: "flagged", label: "น่าสงสัย" },
  { key: "blacklist", label: "Blacklist เว็บ" },
];

const formatDateTime = (value) => new Date(value).toLocaleString("th-TH", {
  dateStyle: "short",
  timeStyle: "short",
});

const displayName = (user) => user?.displayName || getDisplayName(user);

export default function AdminMonitor({
  blocked = [],
  setBlocked,
  notify,
  onOpenTrackingSeat,
}) {
  const [tab, setTab] = useState("session");
  const [roomFilter, setRoomFilter] = useState("all");
  const [dateFilter, setDateFilter] = useState("2026-09-06");
  const [newSite, setNewSite] = useState("");

  const sourceByTab = {
    session: trackingActivity.filter((item) => ["login", "logout"].includes(item.activityType)),
    program: trackingPrograms,
    website: trackingWebsites,
    flagged: trackingFlagged,
  };

  const rows = (sourceByTab[tab] || []).filter((item) => {
    const roomMatches = roomFilter === "all" || item.roomId === roomFilter;
    const dateMatches = dateFilter === "all" || item.at.startsWith(dateFilter);
    return roomMatches && dateMatches;
  });

  const blacklist = [
    ...websiteBlacklist.map((item) => item.domain),
    ...blocked.filter((domain) => !websiteBlacklist.some((item) => item.domain === domain)),
  ];

  const addToBlacklist = (domain, message) => {
    if (!domain || blacklist.includes(domain)) {
      if (domain) notify?.(`${domain} อยู่ใน Blacklist แล้ว`);
      return;
    }
    setBlocked?.([...blocked, domain]);
    notify?.(message || `เพิ่ม ${domain} ใน Blacklist แล้ว`);
  };

  return (
    <div className="w-full max-w-7xl mx-auto">
      <div className="mb-5">
        <h1 className="text-xl md:text-2xl font-bold text-ink tracking-tight">ตรวจสอบการใช้งาน</h1>
        <p className="text-xs text-muted mt-1">Audit กิจกรรมจากทุกห้องและทุกเครื่องในภาพรวม</p>
      </div>

      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-5">
        <Summary icon={Users} label="ผู้ใช้งานปัจจุบัน" value={`${trackingSummary.activeUsers} คน`} tone="bg-navy-50 text-navy-800" />
        <Summary icon={Monitor} label="เครื่องพร้อมใช้งาน" value={`${trackingSummary.machinesReady}/${trackingSummary.machinesTotal}`} tone="bg-teal-50 text-teal-500" />
        <Summary icon={Globe2} label="เว็บไซต์วันนี้" value={`${trackingWebsites.length} รายการ`} tone="bg-blue-50 text-blue-600" />
        <Summary icon={AlertTriangle} label="รายการน่าสงสัย" value={`${trackingFlagged.length} รายการ`} tone="bg-amber-50 text-amber-600" />
      </div>

      <Card variant="flat" className="p-4 mb-5">
        <div className="flex flex-col sm:flex-row gap-3">
          <label className="flex-1 text-xs font-medium text-slate-600">
            ห้อง
            <select
              value={roomFilter}
              onChange={(event) => setRoomFilter(event.target.value)}
              className="block w-full mt-1.5 bg-white border border-slate-200 rounded-xl px-3 py-2.5 text-slate-700"
            >
              <option value="all">ทุกห้อง</option>
              {trackingRooms.map((room) => <option key={room.id} value={room.id}>{room.name}</option>)}
            </select>
          </label>
          <label className="flex-1 text-xs font-medium text-slate-600">
            วันที่
            <select
              value={dateFilter}
              onChange={(event) => setDateFilter(event.target.value)}
              className="block w-full mt-1.5 bg-white border border-slate-200 rounded-xl px-3 py-2.5 text-slate-700"
            >
              <option value="2026-09-06">วันนี้ (6 ก.ย. 2569)</option>
              <option value="2026-09-05">เมื่อวาน (5 ก.ย. 2569)</option>
              <option value="all">ทุกวันที่มีข้อมูล</option>
            </select>
          </label>
        </div>
      </Card>

      <div className="flex gap-2 mb-4 overflow-x-auto pb-1">
        {TABS.map((item) => (
          <button
            key={item.key}
            onClick={() => setTab(item.key)}
            className="shrink-0 text-xs px-3.5 py-2 rounded-xl border transition-colors"
            style={tab === item.key
              ? { background: NAVY, color: "white", borderColor: NAVY }
              : { background: "white", borderColor: "#e2e8f0", color: "#475569" }}
          >
            {item.label}
          </button>
        ))}
      </div>

      {tab === "blacklist" ? (
        <Blacklist
          domains={blacklist}
          blocked={blocked}
          setBlocked={setBlocked}
          newSite={newSite}
          setNewSite={setNewSite}
          addToBlacklist={addToBlacklist}
          notify={notify}
        />
      ) : (
        <ActivityTable
          rows={rows}
          tab={tab}
          onOpenTrackingSeat={onOpenTrackingSeat}
          addToBlacklist={addToBlacklist}
        />
      )}
    </div>
  );
}

function Summary({ icon: Icon, label, value, tone }) {
  return (
    <Card className="p-4 md:p-5">
      <div className={`w-9 h-9 rounded-xl flex items-center justify-center mb-3 ${tone}`}><Icon size={17} /></div>
      <div className="text-xl font-bold text-ink">{value}</div>
      <div className="text-[11px] text-muted mt-1">{label}</div>
    </Card>
  );
}

function ActivityTable({ rows, tab, onOpenTrackingSeat, addToBlacklist }) {
  return (
    <Card className="p-5 md:p-6 overflow-hidden">
      {tab === "flagged" && (
        <div className="flex items-center gap-2 mb-4 text-amber-700 text-xs bg-amber-50 border border-amber-100 rounded-xl px-3 py-2.5">
          <AlertTriangle size={14} /> พบกิจกรรมน่าสงสัย {rows.length} รายการตามตัวกรอง
        </div>
      )}
      <div className="overflow-x-auto">
        <table className="w-full min-w-[780px] text-xs text-left">
          <thead>
            <tr className="text-slate-500 border-b border-slate-200">
              <th className="pb-3 font-semibold">เวลา</th>
              <th className="pb-3 font-semibold">ผู้ใช้</th>
              <th className="pb-3 font-semibold">ห้อง / เครื่อง</th>
              <th className="pb-3 font-semibold">กิจกรรม</th>
              <th className="pb-3 font-semibold">สถานะ</th>
              <th className="pb-3 font-semibold text-right">การดำเนินการ</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {rows.map((row) => (
              <tr key={row.id} className="hover:bg-slate-50/70">
                <td className="py-3.5 text-slate-600">{formatDateTime(row.at)}</td>
                <td className="py-3.5 font-medium text-ink">{displayName(row.user)}</td>
                <td className="py-3.5 text-slate-600">{row.roomName}<br /><span className="text-slate-400">{row.seatLabel}</span></td>
                <td className="py-3.5 text-slate-700 max-w-xs">{row.activity}</td>
                <td className="py-3.5"><Pill tone={row.suspicious ? "red" : "green"}>{row.suspicious ? "น่าสงสัย" : "ปกติ"}</Pill></td>
                <td className="py-3.5">
                  <div className="flex justify-end gap-2">
                    {row.suspicious && (
                      <Button
                        size="sm"
                        variant="danger"
                        onClick={() => addToBlacklist(row.website || "ufaflow2.com")}
                      >
                        เพิ่ม Blacklist
                      </Button>
                    )}
                    <Button
                      size="sm"
                      variant="secondary"
                      onClick={() => onOpenTrackingSeat?.({ roomId: row.roomId, seatId: row.seatId })}
                    >
                      ไปที่เครื่อง
                    </Button>
                  </div>
                </td>
              </tr>
            ))}
            {rows.length === 0 && (
              <tr><td colSpan={6} className="py-12 text-center text-muted">ไม่พบข้อมูลตามตัวกรอง</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </Card>
  );
}

function Blacklist({ domains, blocked, setBlocked, newSite, setNewSite, addToBlacklist, notify }) {
  const remove = (domain) => {
    if (websiteBlacklist.some((item) => item.domain === domain)) {
      notify?.("รายการตั้งต้นเป็นข้อมูลจำลอง จึงยังลบไม่ได้");
      return;
    }
    setBlocked?.(blocked.filter((item) => item !== domain));
    notify?.(`นำ ${domain} ออกจาก Blacklist แล้ว`);
  };

  return (
    <Card className="p-5 md:p-6">
      <div className="flex items-center gap-2 mb-1">
        <Ban size={17} className="text-rose-600" />
        <h2 className="text-sm font-bold text-ink">Blacklist เว็บไซต์</h2>
      </div>
      <p className="text-xs text-muted mb-5">จัดการโดเมนที่ไม่อนุญาตให้เข้าจากเครื่องในห้องแล็บ</p>
      <div className="space-y-2 mb-5">
        {domains.map((domain) => (
          <div key={domain} className="flex items-center justify-between gap-3 border border-rose-100 bg-rose-50 rounded-xl px-3.5 py-3 text-xs">
            <span className="font-medium text-rose-700">{domain}</span>
            <button onClick={() => remove(domain)} className="text-rose-600 flex items-center gap-1 hover:text-rose-800">
              <X size={13} /> นำออก
            </button>
          </div>
        ))}
      </div>
      <div className="flex flex-col sm:flex-row gap-2">
        <input
          value={newSite}
          onChange={(event) => setNewSite(event.target.value)}
          placeholder="เช่น example.com"
          className="flex-1 text-xs border border-slate-200 rounded-xl px-3 py-2.5"
        />
        <Button
          variant="danger"
          icon={Ban}
          onClick={() => {
            const domain = newSite.trim().toLowerCase();
            if (!domain) return;
            addToBlacklist(domain);
            setNewSite("");
          }}
        >
          เพิ่มใน Blacklist
        </Button>
      </div>
    </Card>
  );
}
