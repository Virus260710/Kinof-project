import React, { useState } from "react";
import { Home, Calendar, User, HelpCircle, LayoutDashboard, ClipboardList, Upload, LifeBuoy } from "lucide-react";

import Sidebar from "./components/Sidebar";
import Toast from "./components/Toast";
import Login from "./pages/Login";

import UserHome from "./pages/user/UserHome";
import BookRoom from "./pages/user/BookRoom";
import UserProfile from "./pages/user/UserProfile";
import UserHelp from "./pages/user/UserHelp";

import AdminDashboard from "./pages/admin/AdminDashboard";
import AdminMonitor from "./pages/admin/AdminMonitor";
import AdminExport from "./pages/admin/AdminExport";
import AdminHelpCenter from "./pages/admin/AdminHelpCenter";

import { initialTickets, initialBlocked } from "./data/mockData";

const USER_NAV = [
  { key: "home", label: "หน้าหลัก", icon: Home },
  { key: "book", label: "จองห้องแล็บ", icon: Calendar },
  { key: "profile", label: "โปรไฟล์", icon: User },
  { key: "help", label: "ช่วยเหลือ", icon: HelpCircle },
];

const ADMIN_NAV = [
  { key: "dashboard", label: "แดชบอร์ด", icon: LayoutDashboard },
  { key: "monitor", label: "ตรวจสอบการใช้งาน", icon: ClipboardList },
  { key: "export", label: "ส่งออกข้อมูล", icon: Upload },
  { key: "helpcenter", label: "ศูนย์แก้ไขปัญหา", icon: LifeBuoy },
];

export default function App() {
  const [stage, setStage] = useState("login"); // login | app
  const [role, setRole] = useState(null); // "user" | "admin"
  const [page, setPage] = useState("home");
  const [toast, setToast] = useState("");
  const notify = (t) => setToast(t);

  // App-wide mock state. Once the backend exists, lift these into data-fetching
  // hooks (e.g. React Query) instead of useState + local mutation.
  const [myBookings, setMyBookings] = useState([
    { date: "20 เม.ย. 2569", slot: "รอบที่ 3  14.00 น. - 16.30 น.", room: "ห้องแล็บ 4" },
  ]);
  const [tickets, setTickets] = useState(initialTickets);
  const [blocked, setBlocked] = useState(initialBlocked);

  const handleLogin = (r) => {
    setRole(r);
    setPage(r === "admin" ? "dashboard" : "home");
    setStage("app");
  };
  const handleLogout = () => {
    setStage("login");
    setRole(null);
  };

  if (stage === "login") return <Login onLogin={handleLogin} />;

  return (
    <div className="flex min-h-screen" style={{ background: "#F4F5F8" }}>
      <Sidebar
        items={role === "admin" ? ADMIN_NAV : USER_NAV}
        page={page}
        setPage={setPage}
        roleLabel={role === "admin" ? "ระบบดูแลและจองห้องแล็บ" : "ระบบจองห้องแล็บ"}
        onLogout={handleLogout}
      />
      <div className="flex-1 p-8 max-w-5xl">
        {role === "user" && page === "home" && <UserHome setPage={setPage} myBookings={myBookings} />}
        {role === "user" && page === "book" && <BookRoom addBooking={(b) => setMyBookings([...myBookings, b])} notify={notify} />}
        {role === "user" && page === "profile" && <UserProfile />}
        {role === "user" && page === "help" && <UserHelp tickets={tickets} addTicket={(t) => setTickets([...tickets, t])} notify={notify} />}

        {role === "admin" && page === "dashboard" && <AdminDashboard tickets={tickets} />}
        {role === "admin" && page === "monitor" && <AdminMonitor blocked={blocked} setBlocked={setBlocked} notify={notify} />}
        {role === "admin" && page === "export" && <AdminExport notify={notify} />}
        {role === "admin" && page === "helpcenter" && <AdminHelpCenter tickets={tickets} setTickets={setTickets} notify={notify} />}
      </div>
      <Toast text={toast} onDone={() => setToast("")} />
    </div>
  );
}
