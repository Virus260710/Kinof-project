import React from "react";

export default function Card({ children, className = "", style = {} }) {
  return (
    <div className={`bg-white rounded-xl border border-gray-100 shadow-sm ${className}`} style={style}>
      {children}
    </div>
  );
}
