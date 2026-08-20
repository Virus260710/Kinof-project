import React from "react";
import { LogOut } from "lucide-react";
import { NAVY, NAVY2, GOLD } from "../theme";

export default function Sidebar({ items, page, setPage, roleLabel, onLogout }) {
  return (
    <div className="w-56 shrink-0 text-white flex flex-col justify-between py-6 px-4 min-h-full" style={{ background: NAVY }}>
      <div>
        <div className="flex items-center gap-2 mb-8 px-2">
          <div
            className="w-9 h-9 rounded-lg flex items-center justify-center font-semibold text-sm"
            style={{ background: GOLD, color: NAVY }}
          >
            KN
          </div>
          <div>
            <div className="font-medium leading-tight">KINOF</div>
            <div className="text-[11px] text-gray-400 leading-tight">{roleLabel}</div>
          </div>
        </div>
        <nav className="flex flex-col gap-1">
          {items.map((it) => (
            <button
              key={it.key}
              onClick={() => setPage(it.key)}
              className={`flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm text-left transition ${
                page === it.key ? "text-white" : "text-gray-400 hover:text-gray-200"
              }`}
              style={page === it.key ? { background: NAVY2 } : {}}
            >
              <it.icon size={17} />
              {it.label}
            </button>
          ))}
        </nav>
      </div>
      <button onClick={onLogout} className="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm text-gray-400 hover:text-gray-200">
        <LogOut size={17} />
        ออกจากระบบ
      </button>
    </div>
  );
}
