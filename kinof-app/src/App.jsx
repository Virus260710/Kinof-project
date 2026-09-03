import React, { useState } from "react";
import {
  Home,
  Calendar,
  Mail,
  User,
  HelpCircle,
  LayoutDashboard,
  ClipboardList,
  Upload,
  LifeBuoy,
} from "lucide-react";

import Sidebar from "./components/Sidebar";
import TopBar from "./components/TopBar";
import Toast from "./components/Toast";
import Login from "./pages/Login";
import { BG_APP } from "./theme";

import UserHome from "./pages/user/UserHome";
import BookRoom from "./pages/user/BookRoom";
import Invitation from "./pages/user/Invitation";
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
  { key: "invite", label: "คำเชิญ", icon: Mail },
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
  const [stage, setStage] = useState("login");
  const [role, setRole] = useState(null);
  const [page, setPage] = useState("home");
  const [toast, setToast] = useState("");
  const [sidebarOpen, setSidebarOpen] = useState(false); // mobile drawer state
  const notify = (t) => setToast(t);

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
    setSidebarOpen(false);
  };

  if (stage === "login") return <Login onLogin={handleLogin} />;

  // Centralized here instead of re-declared in every page — also the single
  // place TopBar's mobile menu button and display name need to be wired.
  const userDisplayName = role === "admin" ? "ผู้ดูแลระบบ" : "สมหญิง ส.";

  return (
    <div className="flex min-h-screen w-full" style={{ background: BG_APP }}>
      <Sidebar
        items={role === "admin" ? ADMIN_NAV : USER_NAV}
        page={page}
        setPage={setPage}
        roleLabel={role === "admin" ? "ระบบดูแลและจองห้องแล็บ" : "ระบบจองห้องแล็บ"}
        onLogout={handleLogout}
        isOpen={sidebarOpen}
        onClose={() => setSidebarOpen(false)}
      />

      <div className="flex-1 p-4 md:p-8 w-full min-w-0">
        <TopBar name={userDisplayName} onMenuClick={() => setSidebarOpen(true)} />

        {/* User Pages */}
        {role === "user" && page === "home" && (
          <UserHome setPage={setPage} myBookings={myBookings} />
        )}
        {role === "user" && page === "book" && (
          <BookRoom
            existingBookings={myBookings}
            addBooking={(b) => setMyBookings([...myBookings, b])}
            notify={notify}
            setPage={setPage}
          />
        )}
        {role === "user" && page === "invite" && (
          <Invitation
            notify={notify}
            addBooking={(b) => setMyBookings([...myBookings, b])}
          />
        )}
        {role === "user" && page === "profile" && <UserProfile />}
        {role === "user" && page === "help" && (
          <UserHelp
            tickets={tickets}
            addTicket={(t) => setTickets([...tickets, t])}
            notify={notify}
          />
        )}

        {/* Admin Pages */}
        {role === "admin" && page === "dashboard" && (
          <AdminDashboard tickets={tickets} />
        )}
        {role === "admin" && page === "monitor" && (
          <AdminMonitor blocked={blocked} setBlocked={setBlocked} notify={notify} />
        )}
        {role === "admin" && page === "export" && (
          <AdminExport notify={notify} />
        )}
        {role === "admin" && page === "helpcenter" && (
          <AdminHelpCenter tickets={tickets} setTickets={setTickets} notify={notify} />
        )}
      </div>

      <Toast text={toast} onDone={() => setToast("")} />
    </div>
  );
}