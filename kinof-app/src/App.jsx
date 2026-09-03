import React, { useEffect, useState } from "react";
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
import { Navigate, Route, Routes, useNavigate } from "react-router-dom";

import Sidebar from "./components/Sidebar";
import TopBar from "./components/TopBar";
import Toast from "./components/Toast";
import Login from "./pages/Login";
import OtpVerify from "./pages/OtpVerify";
import Register from "./pages/Register";
import ForgotPassword from "./pages/ForgotPassword";
import ResetPassword from "./pages/ResetPassword";
import FaceEnrollIntro from "./pages/face/FaceEnrollIntro";
import FaceEnrollScan from "./pages/face/FaceEnrollScan";
import FaceEnrollSuccess from "./pages/face/FaceEnrollSuccess";

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
import { getMyBookings, formatThaiDate, formatSlotLabel } from "./api/bookings";
import { getMe, readStoredAuth, storeAuth } from "./api/auth";
import { BG_APP } from "./theme";
import { getDisplayName } from "./utils/displayName";

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

function readStoredJson(storage, key) {
  try {
    return JSON.parse(storage.getItem(key));
  } catch {
    return null;
  }
}

function mapBookingRow(booking) {
  return {
    id: booking.id,
    date: formatThaiDate(new Date(booking.startTime)),
    slot: formatSlotLabel(booking.startTime, booking.endTime),
    room: booking.room,
    startTime: booking.startTime,
    endTime: booking.endTime,
    status: booking.status,
  };
}

export default function App() {
  const navigate = useNavigate();
  const [auth, setAuth] = useState(() => readStoredJson(localStorage, "kinofAuth"));
  const [bootstrapping, setBootstrapping] = useState(() => Boolean(readStoredAuth()));
  const [pendingLogin, setPendingLogin] = useState(() => readStoredJson(sessionStorage, "kinofPendingLogin"));
  const role = auth?.user?.userType === "admin" ? "admin" : "user";
  const [page, setPage] = useState(() => (role === "admin" ? "dashboard" : "home"));
  const [toast, setToast] = useState("");
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const notify = (t) => setToast(t);

  const [myBookings, setMyBookings] = useState([]);
  const [tickets, setTickets] = useState(initialTickets);
  const [blocked, setBlocked] = useState(initialBlocked);

  useEffect(() => {
    let active = true;
    const storedAuth = readStoredAuth();
    if (!storedAuth?.accessToken && !storedAuth?.refreshToken) {
      localStorage.removeItem("kinofAuth");
      setAuth(null);
      setBootstrapping(false);
      return () => {
        active = false;
      };
    }

    getMe()
      .then((user) => {
        if (!active) return;
        const currentAuth = readStoredAuth();
        if (!currentAuth?.accessToken) {
          throw new Error("เซสชันหมดอายุ");
        }
        const nextAuth = { ...currentAuth, user };
        storeAuth(nextAuth);
        setAuth(nextAuth);
        setPage(user.userType === "admin" ? "dashboard" : "home");
      })
      .catch(() => {
        if (!active) return;
        localStorage.removeItem("kinofAuth");
        setAuth(null);
        navigate("/login", { replace: true });
      })
      .finally(() => {
        if (active) setBootstrapping(false);
      });

    return () => {
      active = false;
    };
  }, [navigate]);

  useEffect(() => {
    if (bootstrapping || !auth?.accessToken || role !== "user") return;
    getMyBookings()
      .then((rows) => setMyBookings(rows.map(mapBookingRow)))
      .catch(() => {});
  }, [auth?.accessToken, bootstrapping, role]);

  const handleOtpRequired = (loginResult) => {
    setPendingLogin(loginResult);
    sessionStorage.setItem("kinofPendingLogin", JSON.stringify(loginResult));
    navigate("/login/otp");
  };

  const handleVerified = (result) => {
    const nextAuth = {
      accessToken: result.accessToken,
      refreshToken: result.refreshToken,
      user: result.user,
    };
    storeAuth(nextAuth);
    sessionStorage.removeItem("kinofPendingLogin");
    setPendingLogin(null);
    setAuth(nextAuth);
    setPage(result.user.userType === "admin" ? "dashboard" : "home");
    if (!result.user.faceEnrolled && result.user.userType !== "admin") {
      navigate("/register/face");
      return;
    }
    navigate("/");
  };

  const handleFaceEnrolled = (user) => {
    const nextAuth = { ...auth, user };
    storeAuth(nextAuth);
    setAuth(nextAuth);
  };

  const handleLogout = () => {
    localStorage.removeItem("kinofAuth");
    sessionStorage.removeItem("kinofPendingLogin");
    setAuth(null);
    setPendingLogin(null);
    setSidebarOpen(false);
    navigate("/login");
  };

  const appShell = (
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
        <TopBar name={getDisplayName(auth?.user)} onMenuClick={() => setSidebarOpen(true)} />

        {role === "user" && page === "home" && (
          <UserHome setPage={setPage} myBookings={myBookings} auth={auth} />
        )}
        {role === "user" && page === "book" && (
          <BookRoom
            existingBookings={myBookings}
            onBookingCreated={(booking) => (
              setMyBookings((current) => [mapBookingRow(booking), ...current])
            )}
            auth={auth}
            notify={notify}
            setPage={setPage}
          />
        )}
        {role === "user" && page === "invite" && (
          <Invitation
            notify={notify}
            onInvitationAccepted={(booking) => (
              setMyBookings((current) => [booking, ...current])
            )}
          />
        )}
        {role === "user" && page === "profile" && <UserProfile auth={auth} />}
        {role === "user" && page === "help" && (
          <UserHelp
            tickets={tickets}
            addTicket={(t) => setTickets([...tickets, t])}
            notify={notify}
          />
        )}

        {role === "admin" && page === "dashboard" && <AdminDashboard tickets={tickets} />}
        {role === "admin" && page === "monitor" && (
          <AdminMonitor blocked={blocked} setBlocked={setBlocked} notify={notify} />
        )}
        {role === "admin" && page === "export" && <AdminExport notify={notify} />}
        {role === "admin" && page === "helpcenter" && (
          <AdminHelpCenter tickets={tickets} setTickets={setTickets} notify={notify} />
        )}
      </div>

      <Toast text={toast} onDone={() => setToast("")} />
    </div>
  );

  const needsFaceEnroll = auth && !auth.user?.faceEnrolled && auth.user?.userType !== "admin";

  if (bootstrapping) {
    return (
      <div className="min-h-screen flex items-center justify-center text-sm text-gray-500" style={{ background: BG_APP }}>
        กำลังตรวจสอบเซสชัน...
      </div>
    );
  }

  return (
    <Routes>
      <Route
        path="/login"
        element={auth ? <Navigate to="/" replace /> : <Login onOtpRequired={handleOtpRequired} />}
      />
      <Route
        path="/login/otp"
        element={
          auth ? (
            <Navigate to="/" replace />
          ) : pendingLogin ? (
            <OtpVerify pendingLogin={pendingLogin} onVerified={handleVerified} />
          ) : (
            <Navigate to="/login" replace />
          )
        }
      />
      <Route
        path="/register"
        element={auth ? <Navigate to="/" replace /> : <Register onOtpRequired={handleOtpRequired} />}
      />
      <Route
        path="/forgot-password"
        element={auth ? <Navigate to="/" replace /> : <ForgotPassword />}
      />
      <Route
        path="/reset-password"
        element={auth ? <Navigate to="/" replace /> : <ResetPassword />}
      />
      <Route
        path="/register/face"
        element={auth ? <FaceEnrollIntro onLogout={handleLogout} /> : <Navigate to="/login" replace />}
      />
      <Route
        path="/register/face/scan"
        element={
          auth ? (
            <FaceEnrollScan onFaceEnrolled={handleFaceEnrolled} />
          ) : (
            <Navigate to="/login" replace />
          )
        }
      />
      <Route
        path="/register/face/success"
        element={auth ? <FaceEnrollSuccess /> : <Navigate to="/login" replace />}
      />
      <Route
        path="*"
        element={
          auth ? (
            needsFaceEnroll ? (
              <Navigate to="/register/face" replace />
            ) : (
              appShell
            )
          ) : (
            <Navigate to="/login" replace />
          )
        }
      />
    </Routes>
  );
}
