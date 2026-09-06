import { API_URL, apiFetch, readStoredAuth } from "./auth";

function authHeaders() {
  const auth = readStoredAuth();
  return auth?.accessToken ? { Authorization: `Bearer ${auth.accessToken}` } : {};
}

export function getAdminRooms() {
  return apiFetch("/api/admin/rooms");
}

export function createAdminRoom(payload) {
  return apiFetch("/api/admin/rooms", { method: "POST", body: JSON.stringify(payload) });
}

export function updateAdminRoom(id, payload) {
  return apiFetch(`/api/admin/rooms/${id}`, { method: "PUT", body: JSON.stringify(payload) });
}

export function deleteAdminRoom(id) {
  return apiFetch(`/api/admin/rooms/${id}`, { method: "DELETE" });
}

export function getAdminSchedules(active = true) {
  const params = active === null ? "" : `?active=${active}`;
  return apiFetch(`/api/admin/schedules${params}`);
}

export function getAdminSchedule(id) {
  return apiFetch(`/api/admin/schedules/${id}`);
}

export function createAdminSchedule(payload) {
  return apiFetch("/api/admin/schedules", { method: "POST", body: JSON.stringify(payload) });
}

export function updateAdminSchedule(id, payload) {
  return apiFetch(`/api/admin/schedules/${id}`, { method: "PUT", body: JSON.stringify(payload) });
}

export function deleteAdminSchedule(id) {
  return apiFetch(`/api/admin/schedules/${id}`, { method: "DELETE" });
}

export function addScheduleStudent(scheduleId, studentId) {
  return apiFetch(`/api/admin/schedules/${scheduleId}/enrollments`, {
    method: "POST",
    body: JSON.stringify({ studentId }),
  });
}

export function removeScheduleStudent(scheduleId, recordId, type) {
  return apiFetch(`/api/admin/schedules/${scheduleId}/enrollments/${recordId}?type=${encodeURIComponent(type)}`, {
    method: "DELETE",
  });
}

export function previewScheduleImport(file) {
  const form = new FormData();
  form.append("file", file);
  return apiFetch("/api/admin/schedules/import/preview", { method: "POST", body: form });
}

export function confirmScheduleImport(file) {
  const form = new FormData();
  form.append("file", file);
  return apiFetch("/api/admin/schedules/import/confirm", { method: "POST", body: form });
}

export async function downloadScheduleTemplate() {
  const response = await fetch(`${API_URL}/api/admin/schedules/template`, { headers: authHeaders() });
  if (!response.ok) throw new Error("ดาวน์โหลดแม่แบบไม่สำเร็จ");
  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = "kinof-schedule-template.xlsx";
  link.click();
  URL.revokeObjectURL(url);
}

export function getAdminUsers() {
  return apiFetch("/api/admin/users");
}

export function createAdminUser(payload) {
  return apiFetch("/api/admin/users", { method: "POST", body: JSON.stringify(payload) });
}

export function updateAdminUser(id, payload) {
  return apiFetch(`/api/admin/users/${id}`, { method: "PUT", body: JSON.stringify(payload) });
}

export function disableAdminUser(id) {
  return apiFetch(`/api/admin/users/${id}/disable`, { method: "POST" });
}

export function enableAdminUser(id) {
  return apiFetch(`/api/admin/users/${id}/enable`, { method: "POST" });
}

export function resendAdminInvite(id) {
  return apiFetch(`/api/admin/users/${id}/resend-invite`, { method: "POST" });
}

export function getAuditLogs({ action, page = 1, limit = 50 } = {}) {
  const params = new URLSearchParams({ page: String(page), limit: String(limit) });
  if (action) params.set("action", action);
  return apiFetch(`/api/admin/audit-logs?${params.toString()}`);
}
