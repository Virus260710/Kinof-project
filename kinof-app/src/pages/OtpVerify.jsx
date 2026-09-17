import React, { useState } from "react";
import { ArrowLeft, KeyRound, Mail } from "lucide-react";
import { useNavigate } from "react-router-dom";
import Card from "../components/Card";
import { resendEmailOtp, verifyEmailOtp } from "../api/auth";
import { isStaffAdmin } from "../utils/roles";
import { GOLD, NAVY } from "../theme";

function visibleDevOtp(deliveryMode, code) {
  if (deliveryMode === "smtp") return "";
  return code || "";
}

export default function OtpVerify({ pendingLogin, onVerified, onBack }) {
  const navigate = useNavigate();
  const [code, setCode] = useState("");
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(false);
  const [resending, setResending] = useState(false);
  const [deliveryMode, setDeliveryMode] = useState(pendingLogin.deliveryMode);
  const [devOtp, setDevOtp] = useState(visibleDevOtp(pendingLogin.deliveryMode, pendingLogin.devOtp));

  const handleVerify = async (event) => {
    event.preventDefault();
    if (code.length !== 6) {
      setError("กรุณากรอก OTP ให้ครบ 6 หลัก");
      return;
    }

    setError("");
    setLoading(true);
    try {
      const result = await verifyEmailOtp(pendingLogin.userId, code);
      const isAdmin = isStaffAdmin(result.user.userType);
      if ((pendingLogin.expectedRole === "admin") !== isAdmin) {
        setError("ประเภทบัญชีไม่ตรงกับหน้าที่เลือก กรุณากลับไปเลือกประเภทผู้ใช้ใหม่");
        return;
      }
      onVerified(result);
    } catch (requestError) {
      setError(requestError.message);
    } finally {
      setLoading(false);
    }
  };

  const handleResend = async () => {
    setError("");
    setMessage("");
    setResending(true);
    try {
      const result = await resendEmailOtp(pendingLogin.userId);
      setDeliveryMode(result.deliveryMode);
      setDevOtp(visibleDevOtp(result.deliveryMode, result.devOtp));
      setMessage(result.deliveryMode === "smtp"
        ? `ส่ง OTP ใหม่ไปยัง ${result.maskedEmail} แล้ว`
        : visibleDevOtp(result.deliveryMode, result.devOtp)
          ? "สร้าง OTP ใหม่แล้ว — ใช้รหัสด้านล่างหรือดูใน console backend"
          : "สร้าง OTP ใหม่แล้ว แต่ยังไม่ได้ส่งเข้าอีเมล");
      setCode("");
    } catch (requestError) {
      setError(requestError.message);
    } finally {
      setResending(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center px-4" style={{ background: "#F4F5F8" }}>
      <Card className="w-[400px] p-7">
        <button
          onClick={() => (onBack ? onBack() : navigate("/login"))}
          className="flex items-center gap-1 text-xs text-gray-400 mb-5 hover:text-gray-600"
        >
          <ArrowLeft size={13} /> กลับไปหน้าเข้าสู่ระบบ
        </button>

        <div className="w-11 h-11 rounded-xl flex items-center justify-center mb-4" style={{ background: GOLD, color: NAVY }}>
          <KeyRound size={20} />
        </div>
        <h1 className="text-lg font-medium text-gray-900">ยืนยันรหัส OTP</h1>
        <p className="text-sm text-gray-500 mt-1 mb-5">
          {deliveryMode === "smtp"
            ? <>รหัส OTP ถูกส่งไปยังอีเมล <strong>{pendingLogin.maskedEmail}</strong> ที่ลงทะเบียนไว้</>
            : <>รหัส OTP สำหรับ <strong>{pendingLogin.maskedEmail}</strong></>}
        </p>
        {deliveryMode === "console" && (
          <p className="text-xs text-amber-700 bg-amber-50 border border-amber-200 rounded-lg p-3 mb-4">
            โหมดพัฒนา: ยังไม่ได้ส่งเข้าอีเมล (ยังไม่มี SMTP password หรือ SMTP ส่งไม่สำเร็จ) — ใช้รหัสด้านล่างหรือดูใน console backend
          </p>
        )}
        {deliveryMode === "failed" && (
          <p className="text-xs text-red-700 bg-red-50 border border-red-200 rounded-lg p-3 mb-4">
            ส่งอีเมลไม่สำเร็จ กรุณาตรวจ SMTP / Resend domain แล้วกดส่ง OTP ใหม่
          </p>
        )}
        {deliveryMode !== "smtp" && devOtp && (
          <p className="text-sm text-navy-900 bg-white border border-navy-100 rounded-lg p-3 mb-4 text-center">
            <span className="block text-xs text-gray-500 mb-1">รหัส OTP (โหมดพัฒนา)</span>
            <span className="font-mono tracking-[0.4em] text-2xl font-semibold">{devOtp}</span>
          </p>
        )}

        <form onSubmit={handleVerify}>
          <label className="block text-xs text-gray-600 mb-2" htmlFor="otp-code">รหัส OTP 6 หลัก</label>
          <div className="relative">
            <Mail size={15} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
            <input
              id="otp-code"
              value={code}
              onChange={(event) => setCode(event.target.value.replace(/\D/g, "").slice(0, 6))}
              inputMode="numeric"
              autoComplete="one-time-code"
              autoFocus
              placeholder="000000"
              className="w-full tracking-[0.45em] text-center border border-gray-200 rounded-lg pl-9 pr-3 py-3 focus:outline-none focus:border-gray-400"
            />
          </div>

          {error && <p className="text-xs text-red-600 mt-3" role="alert">{error}</p>}
          {message && <p className="text-xs text-green-700 mt-3" role="status">{message}</p>}

          <button disabled={loading} className="w-full text-white text-sm font-medium rounded-lg py-2.5 mt-5 disabled:opacity-60" style={{ background: NAVY }}>
            {loading ? "กำลังตรวจสอบ..." : "ยืนยัน OTP"}
          </button>
        </form>

        <button
          onClick={handleResend}
          disabled={resending}
          className="w-full text-xs text-gray-600 mt-4 hover:underline disabled:opacity-50"
        >
          {resending ? "กำลังส่ง..." : "ส่ง OTP ใหม่"}
        </button>
        <p className="text-xs text-gray-400 text-center mt-2">OTP ล็อกอินใช้ได้ 10 นาที ไม่จำกัดจำนวนครั้ง — โควตา 5 ครั้งต่อเดือนใช้กับรหัสฉุกเฉินเข้าห้องเท่านั้น</p>
      </Card>
    </div>
  );
}
