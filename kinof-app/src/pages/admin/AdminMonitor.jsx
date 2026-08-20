import React, { useState } from "react";
import { Ban, AlertTriangle, X } from "lucide-react";
import TopBar from "../../components/TopBar";
import Card from "../../components/Card";
import Pill from "../../components/Pill";
import { NAVY } from "../../theme";
import { initialLog, programs } from "../../data/mockData";

// TODO(backend):
// - login/logout log -> GET /api/sessions?date=
// - programs used -> GET /api/usage/programs?date=
// - visited websites / block list -> GET/POST/DELETE /api/websites/blocked
// - flagged activity -> GET /api/usage/flagged, action -> POST /api/users/:id/penalize
export default function AdminMonitor({ blocked, setBlocked, notify }) {
  const [tab, setTab] = useState("log");
  const [newSite, setNewSite] = useState("");
  const tabs = [
    { key: "log", label: "ประวัติเข้า-ออกระบบ" },
    { key: "prog", label: "โปรแกรมที่ถูกใช้งาน" },
    { key: "web", label: "เว็บไซต์ที่เข้าชม" },
    { key: "flag", label: "กิจกรรมน่าสงสัย" },
  ];
  const flagged = initialLog.filter((l) => l.flagged);

  return (
    <div>
      <TopBar name="แอดมิน สมชาย" />
      <h1 className="text-lg font-medium text-gray-900 mb-4">ตรวจสอบการใช้งาน</h1>
      <div className="flex gap-2 mb-4">
        {tabs.map((t) => (
          <button
            key={t.key}
            onClick={() => setTab(t.key)}
            className="text-xs px-3.5 py-2 rounded-lg border"
            style={tab === t.key ? { background: NAVY, color: "white", borderColor: NAVY } : { borderColor: "#e5e5e5", color: "#374151" }}
          >
            {t.label}
          </button>
        ))}
      </div>

      {tab === "log" && (
        <Card className="p-5">
          <div className="grid grid-cols-2 gap-4 mb-5">
            <div className="border border-gray-100 rounded-lg p-4">
              <div className="text-xs text-gray-400">การเข้าใช้เว็บไซต์ทั้งหมดวันนี้</div>
              <div className="text-xl font-medium text-gray-900">126 ครั้ง</div>
            </div>
            <div className="border border-gray-100 rounded-lg p-4">
              <div className="text-xs text-gray-400">ผู้ใช้งานล็อคอินอยู่ขณะนี้</div>
              <div className="text-xl font-medium text-gray-900">{initialLog.filter((l) => l.status === "ล็อคอินอยู่").length} คน</div>
            </div>
          </div>
          <table className="w-full text-xs">
            <thead>
              <tr className="text-gray-400 text-left border-b border-gray-100">
                <th className="pb-2 font-normal">ผู้ใช้งาน</th>
                <th className="pb-2 font-normal">เวลา</th>
                <th className="pb-2 font-normal">ห้อง</th>
                <th className="pb-2 font-normal">เครื่อง</th>
                <th className="pb-2 font-normal">ระยะเวลา</th>
                <th className="pb-2 font-normal">สถานะ</th>
              </tr>
            </thead>
            <tbody>
              {initialLog.map((l, i) => (
                <tr key={i} className="border-b border-gray-50">
                  <td className="py-2.5 text-gray-700">{l.user}</td>
                  <td className="py-2.5 text-gray-700">{l.time}</td>
                  <td className="py-2.5 text-gray-700">{l.room}</td>
                  <td className="py-2.5 text-gray-700">{l.machine}</td>
                  <td className="py-2.5 text-gray-700">{l.duration}</td>
                  <td className="py-2.5"><Pill tone={l.status === "ล็อคอินอยู่" ? "green" : "gray"}>{l.status}</Pill></td>
                </tr>
              ))}
            </tbody>
          </table>
        </Card>
      )}

      {tab === "prog" && (
        <Card className="p-5">
          <table className="w-full text-xs">
            <thead>
              <tr className="text-gray-400 text-left border-b border-gray-100">
                <th className="pb-2 font-normal">โปรแกรม</th>
                <th className="pb-2 font-normal">เวลาใช้เฉลี่ย</th>
              </tr>
            </thead>
            <tbody>
              {programs.map((p, i) => (
                <tr key={i} className="border-b border-gray-50">
                  <td className="py-2.5 text-gray-700">{p.name}</td>
                  <td className="py-2.5 text-gray-700">{p.time}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </Card>
      )}

      {tab === "web" && (
        <Card className="p-5">
          <div className="text-sm font-medium text-gray-900 mb-3">บล็อคเว็บไซต์</div>
          <div className="flex flex-col gap-2 mb-4">
            {blocked.map((b) => (
              <div key={b} className="flex items-center justify-between border border-red-100 bg-red-50 rounded-lg px-3 py-2 text-xs">
                <span className="text-red-700">{b}</span>
                <button onClick={() => setBlocked(blocked.filter((x) => x !== b))} className="text-red-500 flex items-center gap-1">
                  <X size={12} /> เลิกบล็อค
                </button>
              </div>
            ))}
          </div>
          <div className="flex gap-2">
            <input value={newSite} onChange={(e) => setNewSite(e.target.value)} placeholder="เช่น example.com" className="flex-1 text-xs border border-gray-200 rounded-lg px-3 py-2" />
            <button
              onClick={() => {
                if (newSite) {
                  setBlocked([...blocked, newSite]);
                  setNewSite("");
                  notify(`บล็อคเว็บไซต์ ${newSite} แล้ว`);
                }
              }}
              className="flex items-center gap-1 text-xs text-white rounded-lg px-3 py-2"
              style={{ background: "#993C1D" }}
            >
              <Ban size={13} /> บล็อคเว็บไซต์
            </button>
          </div>
        </Card>
      )}

      {tab === "flag" && (
        <Card className="p-5">
          <div className="flex items-center gap-2 mb-4 text-amber-700 text-xs bg-amber-50 border border-amber-100 rounded-lg px-3 py-2">
            <AlertTriangle size={14} /> พบกิจกรรมน่าสงสัย {flagged.length} รายการ
          </div>
          <div className="flex flex-col gap-2">
            {flagged.map((l, i) => (
              <div key={i} className="flex items-center justify-between border border-gray-100 rounded-lg px-3 py-2.5 text-xs">
                <span className="text-gray-700">{l.user} เข้า {l.domain}</span>
                <button
                  onClick={() => {
                    setBlocked([...new Set([...blocked, l.domain])]);
                    notify(`บล็อคและหักคะแนนผู้ใช้งาน ${l.user}`);
                  }}
                  className="text-xs text-white rounded-lg px-3 py-1.5"
                  style={{ background: "#993C1D" }}
                >
                  บล็อคและหักคะแนน
                </button>
              </div>
            ))}
          </div>
        </Card>
      )}
    </div>
  );
}
