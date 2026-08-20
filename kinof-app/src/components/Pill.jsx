import React from "react";

const TONES = {
  gray: "bg-gray-100 text-gray-600",
  amber: "bg-amber-100 text-amber-700",
  blue: "bg-blue-100 text-blue-700",
  green: "bg-green-100 text-green-700",
  red: "bg-red-100 text-red-700",
};

export default function Pill({ children, tone = "gray" }) {
  return <span className={`px-2.5 py-1 rounded-full text-xs font-medium ${TONES[tone]}`}>{children}</span>;
}
