import React, { useEffect, useMemo, useState } from "react";
import {
  AlertTriangle,
  ArrowLeft,
  Clock3,
  Laptop,
  LogOut,
  Monitor,
  Power,
  Radar,
  Save,
  UserRound,
  Wifi,
  WifiOff,
  Wrench,
} from "lucide-react";
import Button from "../../components/Button";
import Card from "../../components/Card";
import Pill from "../../components/Pill";
import {
  trackingActivity,
  trackingRooms,
  trackingSeats,
  trackingSessions,
} from "../../data/trackingMock";
import { getDisplayName } from "../../utils/displayName";

const ROOM_META = {
  open: { label: "เปิดใช้งาน", tone: "green", border: "border-emerald-300", surface: "bg-emerald-50" },
  closed: { label: "ปิด", tone: "red", border: "border-rose-300", surface: "bg-rose-50" },
  maintenance: { label: "ปรับปรุง", tone: "amber", border: "border-amber-300", surface: "bg-amber-50" },
};

const SEAT_META = {
  in_use: { label: "กำลังใช้งาน", tone: "green", dot: "bg-emerald-500", border: "border-emerald-300", surface: "bg-emerald-50" },
  available: { label: "ว่าง", tone: "blue", dot: "bg-blue-500", border: "border-blue-300", surface: "bg-blue-50" },
  offline: { label: "ออฟไลน์", tone: "red", dot: "bg-rose-500", border: "border-rose-300", surface: "bg-rose-50" },
  maintenance: { label: "ซ่อมบำรุง", tone: "amber", dot: "bg-amber-500", border: "border-amber-300", surface: "bg-amber-50" },
};

const BULK_ACTIONS = {
  open: {
    title: "เปิดใช้งานทั้งห้อง",
    description: "เปิดรับการเข้าใช้งาน และตั้งเครื่องที่ Agent ออนไลน์เป็นสถานะว่าง",
    confirmLabel: "ยืนยันเปิดใช้งาน",
    variant: "success",
  },
  close: {
    title: "ระงับและออกจากระบบทั้งหมด",
    description: "ระงับการเข้าใช้งานใหม่และจำลองการออกจากระบบของผู้ใช้ทุกเครื่องทันที",
    confirmLabel: "ยืนยันระงับทั้งห้อง",
    variant: "danger",
  },
  maintenance: {
    title: "ปรับปรุงทั้งห้อง",
    description: "ระงับการใช้งานและตั้งเครื่องทุกเครื่องเป็นสถานะซ่อมบำรุง",
    confirmLabel: "ยืนยันปรับปรุง",
    variant: "primary",
  },
};

const formatDateTime = (value) => value
  ? new Date(value).toLocaleString("th-TH", { dateStyle: "medium", timeStyle: "short" })
  : "-";

const displayName = (user) => user?.displayName || getDisplayName(user);

export default function AdminTracking({
  notify,
  initialRoomId,
  initialSeatId,
  onOpenMonitor,
}) {
  const initialSeat = trackingSeats.find((seat) => seat.id === initialSeatId);
  const [view, setView] = useState(initialSeatId ? "seat" : initialRoomId ? "room" : "rooms");
  const [selectedRoomId, setSelectedRoomId] = useState(initialRoomId || initialSeat?.roomId || null);
  const [selectedSeatId, setSelectedSeatId] = useState(initialSeatId || null);
  const [roomStatusDrafts, setRoomStatusDrafts] = useState(
    Object.fromEntries(trackingRooms.map((room) => [room.id, room.status])),
  );
  const [seatStatusDrafts, setSeatStatusDrafts] = useState(
    Object.fromEntries(trackingSeats.map((seat) => [seat.id, seat.status])),
  );
  const [terminatedSeatIds, setTerminatedSeatIds] = useState(() => new Set());
  const [bulkAction, setBulkAction] = useState(null);

  useEffect(() => {
    const targetSeat = trackingSeats.find((seat) => seat.id === initialSeatId);
    if (targetSeat) {
      setSelectedRoomId(targetSeat.roomId);
      setSelectedSeatId(targetSeat.id);
      setView("seat");
    } else if (trackingRooms.some((room) => room.id === initialRoomId)) {
      setSelectedRoomId(initialRoomId);
      setSelectedSeatId(null);
      setView("room");
    }
  }, [initialRoomId, initialSeatId]);

  const room = trackingRooms.find((item) => item.id === selectedRoomId);
  const seat = trackingSeats.find((item) => item.id === selectedSeatId);
  const roomSeats = trackingSeats.filter((item) => item.roomId === selectedRoomId);
  const session = trackingSessions.find(
    (item) => item.seatId === selectedSeatId && !terminatedSeatIds.has(item.seatId),
  );
  const seatActivity = trackingActivity.filter((item) => item.seatId === selectedSeatId);

  const roomCounts = useMemo(() => Object.keys(SEAT_META).reduce((counts, status) => ({
    ...counts,
    [status]: roomSeats.filter((item) => seatStatusDrafts[item.id] === status).length,
  }), {}), [roomSeats, seatStatusDrafts]);

  const openRoom = (roomId) => {
    setSelectedRoomId(roomId);
    setSelectedSeatId(null);
    setView("room");
  };

  const openSeat = (seatId) => {
    setSelectedSeatId(seatId);
    setView("seat");
  };

  const saveRoom = () => notify?.(`บันทึกสถานะ ${room.name} แล้ว (ข้อมูลจำลอง)`);
  const saveSeat = () => notify?.(`บันทึกข้อมูล ${seat.label} แล้ว (ข้อมูลจำลอง)`);

  const applyBulkAction = () => {
    if (!bulkAction || !room) return;
    const targetStatus = bulkAction === "open"
      ? "available"
      : bulkAction === "close"
        ? "offline"
        : "maintenance";
    const roomStatus = bulkAction === "open"
      ? "open"
      : bulkAction === "close"
        ? "closed"
        : "maintenance";

    setRoomStatusDrafts((current) => ({ ...current, [room.id]: roomStatus }));
    setSeatStatusDrafts((current) => ({
      ...current,
      ...Object.fromEntries(roomSeats.map((item) => [
        item.id,
        bulkAction === "open" && !item.agentOnline ? "offline" : targetStatus,
      ])),
    }));

    if (bulkAction !== "open") {
      setTerminatedSeatIds((current) => {
        const next = new Set(current);
        roomSeats.forEach((item) => next.add(item.id));
        return next;
      });
    }

    notify?.(
      bulkAction === "close"
        ? `ระงับ ${room.name} และออกจากระบบผู้ใช้ทั้งหมดแล้ว (ข้อมูลจำลอง)`
        : `${BULK_ACTIONS[bulkAction].title}แล้ว (ข้อมูลจำลอง)`,
    );
    setBulkAction(null);
  };

  if (view === "rooms") {
    return (
      <Page>
        <PageTitle title="Tracking ห้องแล็บ" description="เลือกห้องเพื่อดูสถานะเครื่องและจัดการการใช้งานแบบเจาะลึก" />
        <Legend items={Object.entries(ROOM_META).map(([key, meta]) => ({ key, ...meta }))} />
        <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
          {trackingRooms.map((item) => {
            const meta = ROOM_META[roomStatusDrafts[item.id]];
            const seats = trackingSeats.filter((machine) => machine.roomId === item.id);
            const online = seats.filter((machine) => machine.agentOnline).length;
            return (
              <Card
                key={item.id}
                hoverable
                role="button"
                tabIndex={0}
                onClick={() => openRoom(item.id)}
                onKeyDown={(event) => event.key === "Enter" && openRoom(item.id)}
                className={`p-5 md:p-6 border-l-4 ${meta.border}`}
              >
                <div className="flex items-start justify-between gap-3">
                  <div>
                    <h2 className="text-lg font-bold text-ink">{item.name}</h2>
                    <p className="text-xs text-muted mt-1">{item.activeUserCount} คนกำลังใช้งาน</p>
                  </div>
                  <Pill tone={meta.tone} withDot>{meta.label}</Pill>
                </div>
                <div className="grid grid-cols-2 gap-3 mt-6">
                  <Metric label="Agent ออนไลน์" value={`${online}/${seats.length}`} icon={Wifi} />
                  <Metric label="เครื่องว่าง" value={seats.filter((machine) => machine.status === "available").length} icon={Monitor} />
                </div>
                <div className="mt-5 pt-4 border-t border-slate-100 text-xs font-semibold text-navy-800 flex items-center justify-between">
                  จัดการห้องและเครื่อง <Radar size={15} />
                </div>
              </Card>
            );
          })}
        </div>
      </Page>
    );
  }

  if (view === "room" && room) {
    return (
      <Page>
        <BackButton onClick={() => setView("rooms")}>ห้องแล็บทั้งหมด</BackButton>
        <div className="flex flex-col lg:flex-row lg:items-start lg:justify-between gap-4 mb-5">
          <PageTitle title={room.name} description="จัดการสถานะห้องและเลือกเครื่องเพื่อดูรายละเอียด" compact />
          <div className="flex flex-wrap items-center gap-2">
            <select
              value={roomStatusDrafts[room.id]}
              onChange={(event) => setRoomStatusDrafts((current) => ({ ...current, [room.id]: event.target.value }))}
              className="text-xs bg-white border border-slate-200 rounded-xl px-3 py-2.5 text-slate-700"
              aria-label="สถานะห้อง"
            >
              {Object.entries(ROOM_META).map(([key, meta]) => <option key={key} value={key}>{meta.label}</option>)}
            </select>
            <Button icon={Save} onClick={saveRoom}>บันทึกข้อมูล</Button>
          </div>
        </div>

        <Card className="p-4 mb-5">
          <div className="flex flex-wrap gap-x-5 gap-y-2">
            {Object.entries(SEAT_META).map(([key, meta]) => (
              <div key={key} className="flex items-center gap-2 text-xs text-slate-600">
                <span className={`w-2.5 h-2.5 rounded-full ${meta.dot}`} />
                {meta.label} ({roomCounts[key] || 0})
              </div>
            ))}
          </div>
        </Card>

        <Card className="p-5 mb-5 border-l-4 border-l-navy-800">
          <div className="flex flex-col xl:flex-row xl:items-center xl:justify-between gap-4">
            <div>
              <h2 className="text-sm font-bold text-ink">ควบคุมสถานะทั้งห้อง</h2>
              <p className="text-xs text-muted mt-1">
                เปลี่ยนสถานะเครื่องทั้ง {roomSeats.length} เครื่องพร้อมกัน — ขณะนี้เป็นการจำลองใน UI
              </p>
            </div>
            <div className="flex flex-wrap gap-2">
              <Button size="sm" variant="success" icon={Power} onClick={() => setBulkAction("open")}>
                เปิดใช้งานทั้งหมด
              </Button>
              <Button size="sm" variant="danger" icon={LogOut} onClick={() => setBulkAction("close")}>
                ระงับและออกจากระบบ
              </Button>
              <Button size="sm" variant="secondary" icon={Wrench} onClick={() => setBulkAction("maintenance")}>
                ปรับปรุงทั้งหมด
              </Button>
            </div>
          </div>
        </Card>

        <div className="grid grid-cols-2 sm:grid-cols-4 xl:grid-cols-5 gap-3">
          {roomSeats.map((item) => {
            const meta = SEAT_META[seatStatusDrafts[item.id]];
            const activeSession = trackingSessions.find(
              (entry) => entry.seatId === item.id && !terminatedSeatIds.has(entry.seatId),
            );
            return (
              <button
                key={item.id}
                onClick={() => openSeat(item.id)}
                className={`rounded-2xl border p-4 text-left transition-all hover:-translate-y-0.5 hover:shadow-card ${meta.border} ${meta.surface}`}
              >
                <div className="flex items-start justify-between gap-1">
                  <Laptop size={19} className="text-slate-600" />
                  {item.agentOnline ? <Wifi size={14} className="text-emerald-600" /> : <WifiOff size={14} className="text-rose-500" />}
                </div>
                <div className="font-bold text-ink mt-3">{item.label}</div>
                <div className="text-[11px] text-slate-600 mt-1">{meta.label}</div>
                {activeSession && <div className="text-[10px] text-slate-500 truncate mt-2">{displayName(activeSession.user)}</div>}
              </button>
            );
          })}
        </div>
        {bulkAction && (
          <BulkConfirm
            action={bulkAction}
            room={room}
            machineCount={roomSeats.length}
            activeSessionCount={trackingSessions.filter(
              (item) => roomSeats.some((machine) => machine.id === item.seatId)
                && !terminatedSeatIds.has(item.seatId),
            ).length}
            onCancel={() => setBulkAction(null)}
            onConfirm={applyBulkAction}
          />
        )}
      </Page>
    );
  }

  if (!seat || !room) return null;
  const seatMeta = SEAT_META[seatStatusDrafts[seat.id]];

  return (
    <Page>
      <BackButton onClick={() => setView("room")}>{room.name}</BackButton>
      <div className="flex flex-col lg:flex-row lg:items-start lg:justify-between gap-4 mb-5">
        <PageTitle title={`${seat.label} · ${room.name}`} description="ข้อมูลผู้ใช้ เซสชัน กิจกรรม และการจัดการเครื่อง" compact />
        <Button
          variant="secondary"
          icon={Radar}
          onClick={() => onOpenMonitor?.({ seatId: seat.id, roomId: room.id })}
        >
          ดูภาพรวม
        </Button>
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-3 gap-5">
        <div className="xl:col-span-2 space-y-5">
          <Card className="p-5 md:p-6">
            <div className="flex items-center justify-between gap-3 mb-5">
              <h2 className="text-sm font-bold text-ink flex items-center gap-2"><UserRound size={17} /> ผู้ใช้ปัจจุบัน</h2>
              <Pill tone={seatMeta.tone} withDot>{seatMeta.label}</Pill>
            </div>
            {session ? (
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
                <Info label="ชื่อผู้ใช้" value={displayName(session.user)} />
                <Info label="รหัสผู้ใช้" value={session.user.username} />
                <Info label="ประเภทผู้ใช้" value={session.user.userType} />
                <Info label="เริ่มเซสชัน" value={formatDateTime(session.startedAt)} className="sm:col-span-2" />
                <Info label="ระยะเวลา" value="24 นาที" />
              </div>
            ) : (
              <div className="rounded-xl border border-dashed border-slate-200 bg-slate-50 py-8 text-center text-xs text-muted">
                ไม่มีผู้ใช้กำลังใช้งานเครื่องนี้
              </div>
            )}
          </Card>

          <Card className="p-5 md:p-6 overflow-hidden">
            <div className="flex items-center gap-2 mb-4">
              <Clock3 size={17} className="text-navy-800" />
              <h2 className="text-sm font-bold text-ink">ประวัติของเครื่องนี้</h2>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full min-w-[620px] text-xs text-left">
                <thead>
                  <tr className="border-b border-slate-200 text-slate-500">
                    <th className="py-3 font-semibold">เวลา</th>
                    <th className="py-3 font-semibold">ผู้ใช้</th>
                    <th className="py-3 font-semibold">กิจกรรม</th>
                    <th className="py-3 font-semibold">ระยะเวลา</th>
                    <th className="py-3 font-semibold">สถานะ</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {seatActivity.map((activity) => (
                    <tr key={activity.id}>
                      <td className="py-3 text-slate-600">{formatDateTime(activity.at)}</td>
                      <td className="py-3 font-medium text-ink">{displayName(activity.user)}</td>
                      <td className="py-3 text-slate-700">{activity.activity}</td>
                      <td className="py-3 text-slate-600">{activity.durationMinutes || "-"} นาที</td>
                      <td className="py-3"><Pill tone={activity.suspicious ? "red" : "gray"}>{activity.suspicious ? "น่าสงสัย" : "ปกติ"}</Pill></td>
                    </tr>
                  ))}
                  {seatActivity.length === 0 && (
                    <tr><td colSpan={5} className="py-10 text-center text-muted">ยังไม่มีประวัติกิจกรรมของเครื่องนี้</td></tr>
                  )}
                </tbody>
              </table>
            </div>
          </Card>
        </div>

        <Card className="p-5 md:p-6 h-fit">
          <h2 className="text-sm font-bold text-ink mb-1">จัดการคอมพิวเตอร์</h2>
          <p className="text-xs text-muted mb-5">การเปลี่ยนแปลงนี้เป็นข้อมูลจำลองภายในหน้า</p>
          <label className="text-xs font-medium text-slate-700">สถานะเครื่อง</label>
          <select
            value={seatStatusDrafts[seat.id]}
            onChange={(event) => setSeatStatusDrafts((current) => ({ ...current, [seat.id]: event.target.value }))}
            className="w-full mt-2 text-xs bg-white border border-slate-200 rounded-xl px-3 py-2.5 text-slate-700"
          >
            {Object.entries(SEAT_META).map(([key, meta]) => <option key={key} value={key}>{meta.label}</option>)}
          </select>
          <div className="mt-4 rounded-xl bg-slate-50 border border-slate-100 p-3 flex items-center justify-between">
            <span className="text-xs text-slate-600">Tracking Agent</span>
            <Pill tone={seat.agentOnline ? "green" : "red"} withDot>{seat.agentOnline ? "ออนไลน์" : "ออฟไลน์"}</Pill>
          </div>
          <Button fullWidth icon={Save} className="mt-5" onClick={saveSeat}>บันทึกข้อมูล</Button>
        </Card>
      </div>
    </Page>
  );
}

function BulkConfirm({
  action,
  room,
  machineCount,
  activeSessionCount,
  onCancel,
  onConfirm,
}) {
  const config = BULK_ACTIONS[action];
  const isClose = action === "close";

  return (
    <div className="fixed inset-0 z-50 bg-slate-950/45 backdrop-blur-sm flex items-center justify-center p-4" role="dialog" aria-modal="true">
      <Card className="w-full max-w-lg p-6 animate-scale-in">
        <div className={`w-11 h-11 rounded-2xl flex items-center justify-center mb-4 ${isClose ? "bg-rose-50 text-rose-600" : "bg-navy-50 text-navy-800"}`}>
          {isClose ? <AlertTriangle size={21} /> : action === "open" ? <Power size={21} /> : <Wrench size={21} />}
        </div>
        <h2 className="text-lg font-bold text-ink">{config.title}</h2>
        <p className="text-xs text-muted mt-1.5 leading-relaxed">{config.description}</p>

        <div className="grid grid-cols-2 gap-3 my-5">
          <div className="rounded-xl bg-slate-50 border border-slate-100 p-3">
            <div className="text-[11px] text-muted">ห้องที่ควบคุม</div>
            <div className="text-sm font-bold text-ink mt-1">{room.name}</div>
          </div>
          <div className="rounded-xl bg-slate-50 border border-slate-100 p-3">
            <div className="text-[11px] text-muted">เครื่องที่ได้รับผลกระทบ</div>
            <div className="text-sm font-bold text-ink mt-1">{machineCount} เครื่อง</div>
          </div>
        </div>

        {isClose && activeSessionCount > 0 && (
          <div className="rounded-xl bg-rose-50 border border-rose-100 px-3.5 py-3 text-xs text-rose-700 flex items-start gap-2 mb-5">
            <LogOut size={15} className="shrink-0 mt-0.5" />
            <span>
              มีผู้ใช้กำลังเข้าสู่ระบบ {activeSessionCount} เครื่อง ระบบจะจำลองการจบเซสชันและออกจากระบบทั้งหมด
            </span>
          </div>
        )}

        <div className="flex justify-end gap-2">
          <Button variant="ghost" onClick={onCancel}>ยกเลิก</Button>
          <Button variant={config.variant} onClick={onConfirm}>{config.confirmLabel}</Button>
        </div>
      </Card>
    </div>
  );
}

function Page({ children }) {
  return <div className="w-full max-w-7xl mx-auto">{children}</div>;
}

function PageTitle({ title, description, compact = false }) {
  return (
    <div className={compact ? "" : "mb-6"}>
      <h1 className="text-xl md:text-2xl font-bold text-ink tracking-tight">{title}</h1>
      <p className="text-xs text-muted mt-1">{description}</p>
    </div>
  );
}

function BackButton({ children, onClick }) {
  return (
    <button onClick={onClick} className="mb-3 text-xs font-medium text-slate-500 hover:text-navy-800 flex items-center gap-1">
      <ArrowLeft size={14} /> {children}
    </button>
  );
}

function Legend({ items }) {
  return (
    <Card variant="flat" className="p-4 mb-5 flex flex-wrap gap-x-5 gap-y-2">
      {items.map((item) => (
        <div key={item.key} className="flex items-center gap-2 text-xs text-slate-600">
          <span className={`w-2.5 h-2.5 rounded-full ${item.surface} border ${item.border}`} /> {item.label}
        </div>
      ))}
    </Card>
  );
}

function Metric({ label, value, icon: Icon }) {
  return (
    <div className="rounded-xl bg-slate-50 border border-slate-100 p-3">
      <Icon size={14} className="text-slate-400 mb-2" />
      <div className="text-lg font-bold text-ink">{value}</div>
      <div className="text-[11px] text-muted">{label}</div>
    </div>
  );
}

function Info({ label, value, className = "" }) {
  return (
    <div className={`rounded-xl bg-slate-50 border border-slate-100 p-3 ${className}`}>
      <div className="text-[11px] text-muted">{label}</div>
      <div className="text-sm font-semibold text-ink mt-1">{value}</div>
    </div>
  );
}
