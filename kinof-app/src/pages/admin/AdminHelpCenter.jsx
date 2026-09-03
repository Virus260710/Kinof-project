import React, { useState } from "react";
import Card from "../../components/Card";
import Pill from "../../components/Pill";
import { NAVY } from "../../theme";

// TODO(backend):
// - tickets -> GET /api/tickets
// - advance status -> PATCH /api/tickets/:id { status }
const TABS = [
  { key: "all", label: "คำขอความช่วยเหลือทั้งหมด" },
  { key: "รอดำเนินการ", label: "รอดำเนินการ" },
  { key: "กำลังดำเนินการ", label: "กำลังดำเนินการ" },
  { key: "เสร็จสิ้น", label: "เสร็จสิ้น" },
];

export default function AdminHelpCenter({ tickets, setTickets, notify }) {
  const [tab, setTab] = useState("all");
  const shown = tab === "all" ? tickets : tickets.filter((t) => t.status === tab);

  const advance = (id) => {
    setTickets(
      tickets.map((t) => {
        if (t.id !== id) return t;
        const next = t.status === "รอดำเนินการ" ? "กำลังดำเนินการ" : "เสร็จสิ้น";
        notify(next === "เสร็จสิ้น" ? "อัปเดตสถานะเป็นเสร็จสิ้นแล้ว" : "รับเรื่องเรียบร้อย กำลังดำเนินการ");
        return { ...t, status: next };
      })
    );
  };

  return (
    <div>
      <h1 className="text-lg font-medium text-gray-900 mb-4">ศูนย์แก้ไขปัญหา</h1>
      <div className="flex gap-2 mb-4">
        {TABS.map((t) => (
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
      <Card className="p-5">
        <table className="w-full text-xs">
          <thead>
            <tr className="text-gray-400 text-left border-b border-gray-100">
              <th className="pb-2 font-normal">ผู้ใช้งาน</th>
              <th className="pb-2 font-normal">หัวข้อ</th>
              <th className="pb-2 font-normal">รายละเอียด</th>
              <th className="pb-2 font-normal">สถานะ</th>
              <th className="pb-2 font-normal">การดำเนินการ</th>
            </tr>
          </thead>
          <tbody>
            {shown.map((t) => (
              <tr key={t.id} className="border-b border-gray-50 align-top">
                <td className="py-3 text-gray-700">{t.user}</td>
                <td className="py-3 text-gray-700">{t.title}</td>
                <td className="py-3 text-gray-500 max-w-xs">{t.detail}</td>
                <td className="py-3"><Pill tone={t.status === "เสร็จสิ้น" ? "green" : t.status === "กำลังดำเนินการ" ? "blue" : "amber"}>{t.status}</Pill></td>
                <td className="py-3">
                  {t.status !== "เสร็จสิ้น" ? (
                    <button onClick={() => advance(t.id)} className="text-xs text-white rounded-lg px-3 py-1.5" style={{ background: NAVY }}>
                      {t.status === "รอดำเนินการ" ? "รับเรื่อง" : "ทำเครื่องหมายเสร็จสิ้น"}
                    </button>
                  ) : (
                    <span className="text-gray-300 text-xs">—</span>
                  )}
                </td>
              </tr>
            ))}
            {shown.length === 0 && (
              <tr>
                <td colSpan={5} className="py-10 text-center text-gray-400">ไม่มีรายการในหมวดนี้</td>
              </tr>
            )}
          </tbody>
        </table>
      </Card>
    </div>
  );
}
