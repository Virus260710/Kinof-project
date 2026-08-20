import React, { useState } from "react";
import TopBar from "../../components/TopBar";
import Card from "../../components/Card";
import Pill from "../../components/Pill";
import { NAVY, NAVY2 } from "../../theme";

// TODO(backend):
// - ticket history -> GET /api/tickets?user=me
// - submit -> POST /api/tickets { topic, detail }
export default function UserHelp({ tickets, addTicket, notify }) {
  const [topic, setTopic] = useState("");
  const [detail, setDetail] = useState("");

  return (
    <div>
      <TopBar name="สมหญิง ส." />
      <h1 className="text-lg font-medium text-gray-900 mb-4">ช่วยเหลือ</h1>
      <div className="grid grid-cols-2 gap-5">
        <Card className="p-5">
          <div className="rounded-xl text-white p-4 mb-4 text-sm" style={{ background: `linear-gradient(120deg, ${NAVY}, ${NAVY2})` }}>
            ต้องการความช่วยเหลือ? คุณสามารถแจ้งปัญหาเครื่องคอมและเจ้าหน้าที่จะดำเนินการให้
          </div>
          <div className="text-xs text-gray-500 mb-1">หัวข้อปัญหา</div>
          <select value={topic} onChange={(e) => setTopic(e.target.value)} className="w-full text-xs border border-gray-200 rounded-lg px-3 py-2.5 mb-3">
            <option value="">-- เลือกหัวข้อปัญหา --</option>
            <option>ปัญหาการจองห้องแล็บ</option>
            <option>ปัญหาโปรไฟล์/บัญชี</option>
            <option>ปัญหาเครื่องคอมพิวเตอร์</option>
            <option>อื่น ๆ</option>
          </select>
          <div className="text-xs text-gray-500 mb-1">รายละเอียด</div>
          <textarea
            value={detail}
            onChange={(e) => setDetail(e.target.value)}
            rows={4}
            placeholder="อธิบายปัญหาโดยละเอียด"
            className="w-full text-xs border border-gray-200 rounded-lg px-3 py-2.5 mb-4"
          />
          <button
            onClick={() => {
              if (!topic || !detail) return;
              addTicket({ id: Date.now(), user: "som_ying", title: topic, room: "-", machine: "-", detail, status: "รอดำเนินการ" });
              notify("ส่งคำร้องขอความช่วยเหลือเรียบร้อยแล้ว");
              setTopic("");
              setDetail("");
            }}
            className="w-full text-white text-sm font-medium rounded-lg py-2.5"
            style={{ background: NAVY }}
          >
            ส่งคำร้องขอความช่วยเหลือ
          </button>
        </Card>
        <Card className="p-5">
          <div className="text-sm font-medium text-gray-900 mb-3">ประวัติการส่งคำร้อง</div>
          <div className="flex flex-col gap-2">
            {tickets.filter((t) => t.user === "som_ying").map((t) => (
              <div key={t.id} className="flex items-center justify-between border border-gray-100 rounded-lg px-3 py-2.5 text-xs">
                <div>
                  <div className="text-gray-800">{t.title}</div>
                  <div className="text-gray-400">{t.detail.slice(0, 30)}...</div>
                </div>
                <Pill tone={t.status === "เสร็จสิ้น" ? "green" : t.status === "กำลังดำเนินการ" ? "blue" : "amber"}>{t.status}</Pill>
              </div>
            ))}
            {tickets.filter((t) => t.user === "som_ying").length === 0 && (
              <div className="text-xs text-gray-400 text-center py-10">ยังไม่มีประวัติการส่งคำร้อง</div>
            )}
          </div>
        </Card>
      </div>
    </div>
  );
}
