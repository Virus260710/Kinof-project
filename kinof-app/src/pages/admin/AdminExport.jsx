import React, { useState } from "react";
import { FileSpreadsheet } from "lucide-react";
import Card from "../../components/Card";
import { NAVY } from "../../theme";

// TODO(backend): POST /api/exports { report: "log"|"prog"|"web"|"flag", format, room, dateRange }
// should return a file download URL or blob.
const ROWS = [
  { label: "ประวัติเข้า-ออกระบบ", key: "log" },
  { label: "โปรแกรมที่ถูกใช้งาน", key: "prog" },
  { label: "เว็บไซต์ที่เข้าชม", key: "web" },
  { label: "กิจกรรมน่าสงสัย", key: "flag" },
];

export default function AdminExport({ notify }) {
  const [formats, setFormats] = useState({ log: "Excel", prog: "Excel", web: "Excel", flag: "Excel" });

  return (
    <div>
      <h1 className="text-lg font-medium text-gray-900 mb-4">ส่งออกข้อมูล</h1>
      <Card className="p-5">
        <div className="flex gap-3 mb-5">
          <select className="text-xs border border-gray-200 rounded-lg px-3 py-2">
            <option>ทุกห้อง</option>
            <option>ห้องแล็บ 1</option>
            <option>ห้องแล็บ 2</option>
            <option>ห้องแล็บ 3</option>
            <option>ห้องแล็บ 4</option>
          </select>
          <select className="text-xs border border-gray-200 rounded-lg px-3 py-2">
            <option>วันนี้ (20 เม.ย. 2569)</option>
            <option>สัปดาห์นี้</option>
            <option>เดือนนี้</option>
          </select>
        </div>
        <div className="text-sm font-medium text-gray-900 mb-3">รายงานพร้อมส่งออก</div>
        <div className="flex flex-col gap-2">
          {ROWS.map((r) => (
            <div key={r.key} className="flex items-center justify-between border border-gray-100 rounded-lg px-4 py-3">
              <span className="text-xs text-gray-700">{r.label}</span>
              <div className="flex items-center gap-2">
                <select
                  value={formats[r.key]}
                  onChange={(e) => setFormats({ ...formats, [r.key]: e.target.value })}
                  className="text-xs border border-gray-200 rounded-lg px-2 py-1.5"
                >
                  <option>Excel</option>
                  <option>CSV</option>
                  <option>PDF</option>
                </select>
                <button
                  onClick={() => notify(`ส่งออก "${r.label}" เป็นไฟล์ ${formats[r.key]} แล้ว`)}
                  className="flex items-center gap-1 text-xs text-white rounded-lg px-3 py-1.5"
                  style={{ background: NAVY }}
                >
                  <FileSpreadsheet size={13} /> ส่งออก
                </button>
              </div>
            </div>
          ))}
        </div>
      </Card>
    </div>
  );
}
