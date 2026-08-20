import React from "react";
import { GOLD } from "../theme";

export default function TopBar({ name }) {
  return (
    <div className="flex items-center justify-between mb-6">
      <div />
      <div className="flex items-center gap-2 bg-white border border-gray-200 rounded-full pl-1 pr-3 py-1">
        <div
          className="w-6 h-6 rounded-full flex items-center justify-center text-white text-xs font-medium"
          style={{ background: GOLD }}
        >
          {name[0]}
        </div>
        <span className="text-sm text-gray-700">{name}</span>
      </div>
    </div>
  );
}
