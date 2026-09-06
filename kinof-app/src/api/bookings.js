import { apiFetch } from "./auth";

export function getRooms() {
  return apiFetch("/api/rooms");
}

export function getAvailableRooms(startTime, endTime) {
  const params = new URLSearchParams({
    startTime: startTime.toISOString(),
    endTime: endTime.toISOString(),
  });
  return apiFetch(`/api/rooms/available?${params.toString()}`);
}

export function getMyBookings() {
  return apiFetch("/api/bookings/me");
}

export function createBooking({ roomId, startTime, endTime, inviteeUserIds = [] }) {
  return apiFetch("/api/bookings", {
    method: "POST",
    body: JSON.stringify({ roomId, startTime, endTime, inviteeUserIds }),
  });
}

export function searchUsers(query) {
  return apiFetch(`/api/invitations/users?query=${encodeURIComponent(query)}`);
}

export function getMyInvitations() {
  return apiFetch("/api/invitations/me");
}

export function acceptInvitation(id) {
  return apiFetch(`/api/invitations/${id}/accept`, {
    method: "POST",
    body: JSON.stringify({ confirm: true }),
  });
}

export function declineInvitation(id) {
  return apiFetch(`/api/invitations/${id}/decline`, { method: "POST" });
}

export function mapBookingRow(booking) {
  return {
    id: booking.id,
    roomId: booking.roomId,
    room: booking.room,
    building: booking.building,
    date: formatThaiDate(booking.startTime),
    slot: formatSlotLabel(booking.startTime, booking.endTime),
    startTime: parseStoredDate(booking.startTime).toISOString(),
    endTime: parseStoredDate(booking.endTime).toISOString(),
    status: booking.status,
    createdAt: booking.createdAt,
  };
}

export const BOOKING_SLOTS = [
  { id: 1, label: "รอบที่ 1  09.00 น. - 11.30 น.", startHour: 9, startMinute: 0, endHour: 11, endMinute: 30 },
  { id: 2, label: "รอบที่ 2  11.30 น. - 14.00 น.", startHour: 11, startMinute: 30, endHour: 14, endMinute: 0 },
  { id: 3, label: "รอบที่ 3  14.00 น. - 16.30 น.", startHour: 14, startMinute: 0, endHour: 16, endMinute: 30 },
  { id: 4, label: "รอบที่ 4  16.30 น. - 19.00 น.", startHour: 16, startMinute: 30, endHour: 19, endMinute: 0 },
];

export function slotToRange(dateValue, slot) {
  const start = new Date(dateValue);
  start.setHours(slot.startHour, slot.startMinute, 0, 0);
  const end = new Date(dateValue);
  end.setHours(slot.endHour, slot.endMinute, 0, 0);
  return { start, end };
}

export function formatThaiDate(dateValue) {
  return new Intl.DateTimeFormat("th-TH", {
    day: "numeric",
    month: "short",
    year: "numeric",
  }).format(parseStoredDate(dateValue));
}

export function parseStoredDate(dateValue) {
  if (typeof dateValue === "string" && !/[zZ]|[+-]\d{2}:?\d{2}$/.test(dateValue)) {
    return new Date(`${dateValue}Z`);
  }
  return new Date(dateValue);
}

export function formatSlotLabel(startTime, endTime) {
  const fmt = new Intl.DateTimeFormat("th-TH", { hour: "2-digit", minute: "2-digit" });
  return `${fmt.format(parseStoredDate(startTime))} - ${fmt.format(parseStoredDate(endTime))}`;
}
