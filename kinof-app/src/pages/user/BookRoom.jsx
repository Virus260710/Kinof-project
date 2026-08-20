import React, { useState, useEffect } from "react";
import { ChevronLeft, ChevronRight, Calendar as CalendarIcon, ArrowRight, Plus, Trash2, Check, AlertTriangle, CalendarPlus } from "lucide-react";
import TopBar from "../../components/TopBar";
import Card from "../../components/Card";
import Pill from "../../components/Pill";
import { NAVY } from "../../theme";

const THAI_MONTHS = [
  "มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน",
  "กรกฎาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม"
];

const morningSlots = [
  "รอบที่ 1 09.00 น. - 11.30 น.",
  "รอบที่ 2 11.30 น. - 14.00 น."
];

const afternoonSlots = [
  "รอบที่ 3 14.00 น. - 16.30 น.",
  "รอบที่ 4 16.30 น. - 19.00 น."
];

const initialBookings = [
  {
    date: "10 เมษายน 2569",
    slot: "รอบที่ 1 09.00 น. - 11.30 น.",
    room: "ห้องแล็บ 1"
  },
  {
    date: `${new Date().getDate()} ${THAI_MONTHS[new Date().getMonth()]} ${new Date().getFullYear() + 543}`,
    slot: "รอบที่ 2 11.30 น. - 14.00 น.",
    room: "ห้องแล็บ 2"
  }
];

export default function BookRoom({ addBooking, notify, existingBookings = [], setPage }) {
  const [step, setStep] = useState(1);
  const [isSubmitted, setIsSubmitted] = useState(false);
  const [selectedDate, setSelectedDate] = useState(new Date());
  const [viewDate, setViewDate] = useState(new Date());
  const [slot, setSlot] = useState(null);
  const [friends, setFriends] = useState(["กิตติ ศักดิ์ (kitti_sak5187@gmail.com)"]);
  const [newFriend, setNewFriend] = useState("");
  const [room, setRoom] = useState("ห้องแล็บ 4");

  const [bookingsList, setBookingsList] = useState([
    ...initialBookings,
    ...existingBookings
  ]);

  useEffect(() => {
    setBookingsList([...initialBookings, ...existingBookings]);
  }, [existingBookings]);

  const userEmail = "somy@gmail.com";
  const steps = ["1. เลือกวัน-เวลา", "2. จัดการสมาชิก-ดูห้อง", "3. ยืนยันการจอง"];
  const formattedDate = `${selectedDate.getDate()} ${THAI_MONTHS[selectedDate.getMonth()]} ${selectedDate.getFullYear() + 543}`;

  // วันปัจจุบัน (ตัดเศษเวลาออกเพื่อเทียบแค่วัน/เดือน/ปี)
  const today = new Date();
  today.setHours(0, 0, 0, 0);

  // เช็กว่าเป็นวันที่ผ่านมาแล้วหรือไม่
  const isPastDate = (date) => {
    const target = new Date(date);
    target.setHours(0, 0, 0, 0);
    return target < today;
  };

  const isSlotBooked = (checkSlot) => {
    return bookingsList.some(
      (b) => b.date === formattedDate && b.slot === checkSlot
    );
  };

  const handleSelectDate = (date) => {
    if (isPastDate(date)) return;

    setSelectedDate(date);
    const newFormattedDate = `${date.getDate()} ${THAI_MONTHS[date.getMonth()]} ${date.getFullYear() + 543}`;
    const isCurrentSlotBooked = bookingsList.some(
      (b) => b.date === newFormattedDate && b.slot === slot
    );
    if (isCurrentSlotBooked) {
      setSlot(null);
    }
  };

  const handlePrevMonth = () => {
    setViewDate(new Date(viewDate.getFullYear(), viewDate.getMonth() - 1, 1));
  };

  const handleNextMonth = () => {
    setViewDate(new Date(viewDate.getFullYear(), viewDate.getMonth() + 1, 1));
  };

  const generateCalendarDays = () => {
    const year = viewDate.getFullYear();
    const month = viewDate.getMonth();

    const firstDayIndex = new Date(year, month, 1).getDay();
    const thaiFirstDayIndex = (firstDayIndex + 6) % 7;
    const daysInCurrentMonth = new Date(year, month + 1, 0).getDate();
    const daysInPrevMonth = new Date(year, month, 0).getDate();

    const days = [];

    for (let i = thaiFirstDayIndex - 1; i >= 0; i--) {
      days.push({
        date: new Date(year, month - 1, daysInPrevMonth - i),
        isCurrentMonth: false,
      });
    }

    for (let i = 1; i <= daysInCurrentMonth; i++) {
      days.push({
        date: new Date(year, month, i),
        isCurrentMonth: true,
      });
    }

    const remainingDays = (7 - (days.length % 7)) % 7;
    for (let i = 1; i <= remainingDays; i++) {
      days.push({
        date: new Date(year, month + 1, i),
        isCurrentMonth: false,
      });
    }

    return days;
  };

  const isSameDate = (d1, d2) => {
    if (!d1 || !d2) return false;
    return (
      d1.getDate() === d2.getDate() &&
      d1.getMonth() === d2.getMonth() &&
      d1.getFullYear() === d2.getFullYear()
    );
  };

  const formatSlotDisplay = (slotStr) => {
    if (!slotStr) return "";
    const match = slotStr.match(/(รอบที่ \d+)\s+(.+)/);
    if (match) {
      return `${match[2]} (${match[1]})`;
    }
    return slotStr;
  };

  const handleAddFriend = () => {
    const trimmed = newFriend.trim();
    if (!trimmed) return;
    if (friends.length >= 5) {
      if (notify) notify("สามารถเพิ่มเพื่อนได้สูงสุด 5 คน");
      return;
    }
    if (friends.includes(trimmed)) {
      if (notify) notify("มีอีเมลนี้ในรายการแล้ว");
      return;
    }
    setFriends([...friends, trimmed]);
    setNewFriend("");
  };

  const handleConfirmBooking = () => {
    const newBooking = { date: formattedDate, slot, room, friends };
    setBookingsList((prev) => [...prev, newBooking]);

    if (addBooking) {
      addBooking(newBooking);
    }
    if (notify) {
      notify("ยืนยันการจองห้องแล็บเรียบร้อยแล้ว");
    }
    setIsSubmitted(true);
  };

  const handleReset = () => {
    setIsSubmitted(false);
    setStep(1);
    setSlot(null);
    setRoom("ห้องแล็บ 4");
  };

  const handleBackToHome = () => {
    handleReset();
    if (setPage) {
      setPage("home");
    }
  };

  const calendarDays = generateCalendarDays();

  return (
    <div className="w-full">
      <TopBar name="สมหญิง ส." />
      <h1 className="text-base md:text-lg font-medium text-gray-900 mb-4">จองห้องแล็บ</h1>

      {/* Stepper Banner */}
      <div className="text-white rounded-2xl p-4 md:p-5 mb-6 shadow-sm" style={{ background: NAVY }}>
        <p className="text-xs text-blue-200 mb-4 text-left font-light opacity-90">
          กรุณาจองล่วงหน้าอย่างน้อย 1 ชั่วโมงก่อนเข้าใช้ห้องแล็บ
        </p>

        <div className="flex items-center justify-center max-w-lg mx-auto overflow-x-auto pb-2 md:pb-0">
          {steps.map((s, i) => {
            const isStepCompleted = isSubmitted && i === 2;
            return (
              <React.Fragment key={s}>
                <div className="flex flex-col items-center shrink-0">
                  <div
                    className="w-7 h-7 rounded-full flex items-center justify-center text-xs font-bold mb-1.5 transition-colors"
                    style={
                      isStepCompleted
                        ? { background: "#22c55e", color: "white" }
                        : step === i + 1
                        ? { background: "white", color: NAVY }
                        : step > i + 1
                        ? { background: "#93c5fd", color: "#1e3a8a" }
                        : { background: "rgba(255, 255, 255, 0.15)", color: "#e0e7ff" }
                    }
                  >
                    {isStepCompleted ? <Check size={16} strokeWidth={3} /> : `${i + 1}.`}
                  </div>
                  <span className="text-[10px] md:text-[11px] text-blue-100 whitespace-nowrap">{s}</span>
                </div>
                {i < steps.length - 1 && (
                  <div className="flex-1 min-w-[20px] h-[2px] bg-white/20 mx-2 md:mx-4 -mt-5" />
                )}
              </React.Fragment>
            );
          })}
        </div>
      </div>

      {/* STEP 1: เลือกวัน-เวลา */}
      {!isSubmitted && step === 1 && (
        <div>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6 items-start">
            <div>
              <h2 className="text-xs text-gray-500 font-medium mb-2">เลือกวันที่</h2>
              <div className="border border-gray-200 rounded-2xl overflow-hidden shadow-sm bg-white">
                <div className="text-white px-4 md:px-5 py-3.5" style={{ background: NAVY }}>
                  <div className="flex items-center justify-between text-sm font-medium mb-3">
                    <button type="button" onClick={handlePrevMonth} className="hover:opacity-75 p-1 transition-opacity">
                      <ChevronLeft size={16} />
                    </button>
                    <div className="flex items-center gap-2">
                      <span>{THAI_MONTHS[viewDate.getMonth()]} {viewDate.getFullYear() + 543}</span>
                      <CalendarIcon size={16} />
                    </div>
                    <button type="button" onClick={handleNextMonth} className="hover:opacity-75 p-1 transition-opacity">
                      <ChevronRight size={16} />
                    </button>
                  </div>
                  <div className="grid grid-cols-7 text-center text-xs text-blue-200 font-light">
                    <span>จ</span><span>อ</span><span>พ</span><span>พฤ</span><span>ศ</span><span>ส</span><span>อา</span>
                  </div>
                </div>

                <div className="p-3 md:p-4 grid grid-cols-7 gap-y-2 text-center text-xs">
                  {calendarDays.map((item, index) => {
                    const isSelected = isSameDate(item.date, selectedDate);
                    const isCurrentToday = isSameDate(item.date, today);
                    const past = isPastDate(item.date);
                    const isDisabled = !item.isCurrentMonth || past;

                    return (
                      <div key={index} className="flex justify-center items-center py-0.5">
                        <button
                          type="button"
                          onClick={() => {
                            if (!isDisabled) {
                              handleSelectDate(item.date);
                            }
                          }}
                          disabled={isDisabled}
                          className={`w-7 h-7 rounded-full flex items-center justify-center transition-all ${
                            !item.isCurrentMonth
                              ? "text-gray-300 cursor-default"
                              : past
                              ? "text-gray-300 cursor-not-allowed line-through opacity-60"
                              : isSelected
                              ? "text-white font-medium shadow"
                              : isCurrentToday
                              ? "bg-gray-200 text-gray-800 font-medium"
                              : "text-gray-700 hover:bg-gray-100"
                          }`}
                          style={item.isCurrentMonth && isSelected && !past ? { background: NAVY } : {}}
                        >
                          {item.date.getDate()}
                        </button>
                      </div>
                    );
                  })}
                </div>
              </div>
            </div>

            <div>
              <h2 className="text-xs text-gray-500 font-medium mb-2">เลือกเวลา</h2>
              <div className="border border-gray-200 rounded-2xl p-4 md:p-5 bg-white shadow-sm flex flex-col gap-4">
                {/* ช่วงเช้า */}
                <div>
                  <span className="text-xs text-gray-500 font-medium block mb-2">ช่วงเช้า</span>
                  <div className="flex flex-col gap-2">
                    {morningSlots.map((s) => {
                      const booked = isSlotBooked(s);
                      return (
                        <button
                          type="button"
                          key={s}
                          disabled={booked}
                          onClick={() => setSlot(s)}
                          className={`text-xs text-left rounded-xl px-4 py-2.5 border transition-all flex items-center justify-between ${
                            booked
                              ? "bg-gray-100 border-gray-200 text-gray-400 cursor-not-allowed"
                              : slot === s
                              ? "text-white font-medium"
                              : "border-gray-200 text-gray-700 hover:border-gray-300 bg-white"
                          }`}
                          style={!booked && slot === s ? { background: NAVY, borderColor: NAVY } : {}}
                        >
                          <span>{s}</span>
                          {booked && (
                            <span className="text-[10px] bg-gray-200 text-gray-500 font-normal px-2 py-0.5 rounded-full">
                              จองแล้ว
                            </span>
                          )}
                        </button>
                      );
                    })}
                  </div>
                </div>

                {/* ช่วงบ่าย */}
                <div>
                  <span className="text-xs text-gray-500 font-medium block mb-2">ช่วงบ่าย</span>
                  <div className="flex flex-col gap-2">
                    {afternoonSlots.map((s) => {
                      const booked = isSlotBooked(s);
                      return (
                        <button
                          type="button"
                          key={s}
                          disabled={booked}
                          onClick={() => setSlot(s)}
                          className={`text-xs text-left rounded-xl px-4 py-2.5 border transition-all flex items-center justify-between ${
                            booked
                              ? "bg-gray-100 border-gray-200 text-gray-400 cursor-not-allowed"
                              : slot === s
                              ? "text-white font-medium"
                              : "border-gray-200 text-gray-700 hover:border-gray-300 bg-white"
                          }`}
                          style={!booked && slot === s ? { background: NAVY, borderColor: NAVY } : {}}
                        >
                          <span>{s}</span>
                          {booked && (
                            <span className="text-[10px] bg-gray-200 text-gray-500 font-normal px-2 py-0.5 rounded-full">
                              จองแล้ว
                            </span>
                          )}
                        </button>
                      );
                    })}
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div className="flex justify-end mt-6">
            <button
              type="button"
              disabled={!slot}
              onClick={() => setStep(2)}
              className="text-white text-sm font-medium rounded-xl px-6 py-2.5 flex items-center gap-2 disabled:opacity-40 shadow-sm transition-opacity"
              style={{ background: NAVY }}
            >
              <span>ถัดไป</span>
              <ArrowRight size={16} />
            </button>
          </div>
        </div>
      )}

      {/* STEP 2: จัดการสมาชิก-ดูห้อง */}
      {!isSubmitted && step === 2 && (
        <Card className="p-4 md:p-6">
          <div className="text-xs text-gray-500 mb-2">เชิญเพื่อนเข้าร่วมกลุ่ม (สูงสุด 5 คน)</div>
          <div className="flex flex-col gap-2 mb-3">
            {friends.map((f, i) => (
              <div key={i} className="flex items-center justify-between border border-gray-200 rounded-lg px-3 py-2 text-xs">
                <span className="text-gray-700 truncate pr-2">{f}</span>
                <div className="flex items-center gap-2 shrink-0">
                  <Pill tone="green">เข้าร่วมแล้ว</Pill>
                  <button type="button" onClick={() => setFriends(friends.filter((x) => x !== f))} className="text-red-500 hover:text-red-700">
                    <Trash2 size={13} />
                  </button>
                </div>
              </div>
            ))}
          </div>

          <div className="flex flex-col sm:flex-row gap-2 mb-6">
            <input
              value={newFriend}
              onChange={(e) => setNewFriend(e.target.value)}
              onKeyDown={(e) => e.key === "Enter" && handleAddFriend()}
              placeholder="กรอกอีเมลเพื่อนเพื่อเชิญเข้ากลุ่ม"
              className="flex-1 text-xs border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:border-blue-500"
            />
            <button
              type="button"
              onClick={handleAddFriend}
              className="flex items-center justify-center gap-1 text-xs border border-gray-200 rounded-lg px-4 py-2 text-gray-600 hover:bg-gray-50 transition-colors"
            >
              <Plus size={13} /> เพิ่มเพื่อน
            </button>
          </div>

          <div className="text-xs text-gray-500 mb-2">ห้องที่ว่างในช่วงเวลานี้</div>
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 mb-6">
            {["ห้องแล็บ 1", "ห้องแล็บ 2", "ห้องแล็บ 3", "ห้องแล็บ 4"].map((r) => (
              <button
                key={r}
                type="button"
                onClick={() => setRoom(r)}
                className="border rounded-lg py-3 text-xs transition-colors"
                style={room === r ? { borderColor: NAVY, background: "#F4F5F8" } : { borderColor: "#e5e5e5" }}
              >
                {r}
              </button>
            ))}
          </div>

          <div className="flex justify-between gap-2">
            <button type="button" onClick={() => setStep(1)} className="text-xs text-gray-500 border border-gray-200 rounded-lg px-4 py-2 hover:bg-gray-50">
              ย้อนกลับและแก้ไข
            </button>
            <button type="button" disabled={!room} onClick={() => setStep(3)} className="text-white text-sm font-medium rounded-lg px-5 py-2.5 disabled:opacity-40" style={{ background: NAVY }}>
              ถัดไป →
            </button>
          </div>
        </Card>
      )}

      {/* STEP 3: ตรวจสอบรายละเอียดก่อนยืนยัน */}
      {!isSubmitted && step === 3 && (
        <div className="flex flex-col gap-5">
          <h2 className="text-sm font-semibold text-gray-800">ยืนยันรายละเอียดการจอง</h2>

          <div className="border border-gray-200 rounded-2xl overflow-hidden bg-white shadow-sm">
            <div className="p-4 md:p-5 text-white" style={{ background: NAVY }}>
              <h3 className="text-sm font-medium mb-1">รายละเอียดการจองของคุณ</h3>
              <p className="text-xs text-blue-200 font-light">กรุณาตรวจสอบข้อมูลให้ครบถ้วนก่อนยืนยัน</p>
            </div>

            <div className="divide-y divide-gray-100 text-xs text-gray-700">
              <div className="grid grid-cols-3 px-4 md:px-5 py-3.5 items-center">
                <span className="text-gray-500">อีเมล:</span>
                <span className="col-span-2 font-medium truncate">{userEmail}</span>
              </div>
              <div className="grid grid-cols-3 px-4 md:px-5 py-3.5 items-center">
                <span className="text-gray-500">วันที่:</span>
                <span className="col-span-2 font-medium">{formattedDate}</span>
              </div>
              <div className="grid grid-cols-3 px-4 md:px-5 py-3.5 items-center">
                <span className="text-gray-500">เวลา:</span>
                <span className="col-span-2 font-medium">{formatSlotDisplay(slot)}</span>
              </div>
              <div className="grid grid-cols-3 px-4 md:px-5 py-3.5 items-center">
                <span className="text-gray-500">ห้อง:</span>
                <span className="col-span-2 font-medium">{room || "-"}</span>
              </div>
              <div className="grid grid-cols-3 px-4 md:px-5 py-3.5 items-center">
                <span className="text-gray-500">เพื่อนร่วมกลุ่ม:</span>
                <span className="col-span-2 font-medium">
                  {friends.length > 0 ? friends.join(", ") : "-"}
                </span>
              </div>
            </div>
          </div>

          <div className="flex justify-between items-center mt-2 gap-2">
            <button
              type="button"
              onClick={() => setStep(2)}
              className="text-xs text-gray-600 bg-white border border-gray-200 rounded-xl px-4 md:px-5 py-2.5 hover:bg-gray-50 transition-colors"
            >
              ย้อนกลับและแก้ไข
            </button>
            <button
              type="button"
              onClick={handleConfirmBooking}
              className="text-white text-xs font-medium rounded-xl px-5 md:px-6 py-2.5 flex items-center gap-2 shadow-sm transition-opacity"
              style={{ background: NAVY }}
            >
              <span>ยืนยันการจอง</span>
              <Check size={16} />
            </button>
          </div>
        </div>
      )}

      {/* SUCCESS STATE */}
      {isSubmitted && (
        <Card className="p-5 md:p-8 text-center flex flex-col items-center">
          <div className="w-14 h-14 bg-emerald-500 rounded-full flex items-center justify-center text-white mb-3 shadow-md">
            <Check size={32} strokeWidth={3} />
          </div>

          <h2 className="text-base font-semibold text-gray-900 mb-1">ยืนยันการจองสำเร็จ!</h2>
          <p className="text-xs text-gray-500 mb-6">การจองของคุณได้รับการยืนยันแล้ว</p>

          <div className="w-full max-w-2xl border border-gray-200 rounded-2xl overflow-hidden bg-gray-50/50 text-left mb-6">
            <div className="p-3.5 md:p-4 bg-gray-100/70 border-b border-gray-200">
              <h3 className="text-xs font-semibold text-gray-800">รายละเอียดการจองของคุณ</h3>
            </div>
            <div className="divide-y divide-gray-200/60 text-xs text-gray-700">
              <div className="grid grid-cols-3 px-4 md:px-5 py-3">
                <span className="text-gray-500">อีเมล:</span>
                <span className="col-span-2 font-medium truncate">{userEmail}</span>
              </div>
              <div className="grid grid-cols-3 px-4 md:px-5 py-3">
                <span className="text-gray-500">วันที่:</span>
                <span className="col-span-2 font-medium">{formattedDate}</span>
              </div>
              <div className="grid grid-cols-3 px-4 md:px-5 py-3">
                <span className="text-gray-500">เวลา:</span>
                <span className="col-span-2 font-medium">{formatSlotDisplay(slot)}</span>
              </div>
              <div className="grid grid-cols-3 px-4 md:px-5 py-3">
                <span className="text-gray-500">ห้อง:</span>
                <span className="col-span-2 font-medium">{room || "-"}</span>
              </div>
              <div className="grid grid-cols-3 px-4 md:px-5 py-3">
                <span className="text-gray-500">เพื่อนร่วมกลุ่ม:</span>
                <span className="col-span-2 font-medium">
                  {friends.length > 0 ? friends.join(", ") : "-"}
                </span>
              </div>
            </div>
          </div>

          <div className="w-full max-w-2xl bg-amber-50 border border-amber-200 rounded-xl p-3.5 flex items-center gap-3 text-left mb-8">
            <div className="w-7 h-7 rounded-full bg-amber-400 text-white flex items-center justify-center shrink-0">
              <AlertTriangle size={16} />
            </div>
            <span className="text-xs text-amber-900 font-medium">
              หากคุณไม่มาใช้ห้องแล็บตามเวลาที่จองไว้ คุณจะถูกหักคะแนน 5 คะแนน
            </span>
          </div>

          <div className="flex flex-col sm:flex-row justify-end gap-3 w-full max-w-2xl">
            <button
              type="button"
              onClick={handleReset}
              className="text-xs text-gray-700 bg-white border border-gray-200 rounded-xl px-4 py-2.5 flex items-center justify-center gap-2 hover:bg-gray-50 transition-colors"
            >
              <CalendarPlus size={15} />
              <span>จองห้องแล็บเพิ่ม</span>
            </button>
            <button
              type="button"
              onClick={handleBackToHome}
              className="text-white text-xs font-medium rounded-xl px-5 py-2.5 transition-opacity"
              style={{ background: NAVY }}
            >
              กลับสู่หน้าหลัก
            </button>
          </div>
        </Card>
      )}
    </div>
  );
}