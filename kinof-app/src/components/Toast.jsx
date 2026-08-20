import React, { useEffect } from "react";
import { Check } from "lucide-react";

export default function Toast({ text, onDone }) {
  useEffect(() => {
    if (!text) return;
    const t = setTimeout(onDone, 2600);
    return () => clearTimeout(t);
  }, [text, onDone]);

  if (!text) return null;

  return (
    <div className="fixed bottom-6 right-6 z-50 flex items-center gap-2 rounded-lg bg-white px-4 py-3 shadow-lg border border-gray-200">
      <Check size={16} className="text-[#0F6E56]" />
      <span className="text-sm text-gray-800">{text}</span>
    </div>
  );
}
