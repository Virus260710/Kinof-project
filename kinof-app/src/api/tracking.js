import { apiFetch } from "./auth";

/** Today in the lab's timezone, as YYYY-MM-DD, regardless of the browser locale. */
export function bangkokDate(offsetDays = 0) {
  const now = new Date();
  now.setDate(now.getDate() + offsetDays);
  return new Intl.DateTimeFormat("en-CA", {
    timeZone: "Asia/Bangkok",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).format(now);
}

export function getTrackingSummary() {
  return apiFetch("/api/admin/tracking/summary");
}

export function getTrackingRooms() {
  return apiFetch("/api/admin/tracking/rooms");
}

export function getTrackingSeats(roomId) {
  return apiFetch(`/api/admin/tracking/seats?roomId=${encodeURIComponent(roomId)}`);
}

export function getTrackingActivity({ roomId, date, type } = {}) {
  const params = new URLSearchParams();
  if (roomId && roomId !== "all") params.set("roomId", roomId);
  if (date) params.set("date", date);
  if (type) params.set("type", type);
  const query = params.toString();
  return apiFetch(`/api/admin/tracking/activity${query ? `?${query}` : ""}`);
}

export function getSeatActivity(seatId, limit = 50) {
  return apiFetch(`/api/admin/tracking/seats/${seatId}/activity?limit=${limit}`);
}

export function updateRoomStatus(roomId, status) {
  return apiFetch(`/api/admin/tracking/rooms/${roomId}/status`, {
    method: "PUT",
    body: JSON.stringify({ status }),
  });
}

export function bulkRoomAction(roomId, action) {
  return apiFetch(`/api/admin/tracking/rooms/${roomId}/bulk-action`, {
    method: "POST",
    body: JSON.stringify({ action }),
  });
}

export function forceSeatLogout(seatId) {
  return apiFetch(`/api/admin/tracking/seats/${seatId}/logout`, { method: "POST" });
}

export function getWebsiteBlacklist() {
  return apiFetch("/api/admin/tracking/website-blacklist");
}

export function addWebsiteBlacklist({ domain, category, reason } = {}) {
  return apiFetch("/api/admin/tracking/website-blacklist", {
    method: "POST",
    body: JSON.stringify({ domain, category, reason }),
  });
}

export function removeWebsiteBlacklist(id) {
  return apiFetch(`/api/admin/tracking/website-blacklist/${id}`, { method: "DELETE" });
}

export function getWebsiteBlacklistCategories() {
  return apiFetch("/api/admin/tracking/website-blacklist/categories");
}

export function importWebsiteBlacklistCategory({ category, limit } = {}) {
  return apiFetch("/api/admin/tracking/website-blacklist/import", {
    method: "POST",
    body: JSON.stringify({ category, limit }),
  });
}

export function removeWebsiteBlacklistCategory(category) {
  return apiFetch(`/api/admin/tracking/website-blacklist/categories/${encodeURIComponent(category)}`, {
    method: "DELETE",
  });
}

export function getProgramBlacklist() {
  return apiFetch("/api/admin/tracking/program-blacklist");
}

export function addProgramBlacklist({ processName, category, reason } = {}) {
  return apiFetch("/api/admin/tracking/program-blacklist", {
    method: "POST",
    body: JSON.stringify({ processName, category, reason }),
  });
}

export function removeProgramBlacklist(id) {
  return apiFetch(`/api/admin/tracking/program-blacklist/${id}`, { method: "DELETE" });
}

export function getProgramAllowlist() {
  return apiFetch("/api/admin/tracking/program-allowlist");
}

export function addProgramAllowlist({ processName, displayName, category } = {}) {
  return apiFetch("/api/admin/tracking/program-allowlist", {
    method: "POST",
    body: JSON.stringify({ processName, displayName, category }),
  });
}

export function removeProgramAllowlist(id) {
  return apiFetch(`/api/admin/tracking/program-allowlist/${id}`, { method: "DELETE" });
}

export function getUnknownPrograms({ roomId, date } = {}) {
  const params = new URLSearchParams();
  if (roomId && roomId !== "all") params.set("roomId", roomId);
  if (date) params.set("date", date);
  const query = params.toString();
  return apiFetch(`/api/admin/tracking/unknown-programs${query ? `?${query}` : ""}`);
}

export function getTrackingAgents(roomId) {
  const query = roomId ? `?roomId=${encodeURIComponent(roomId)}` : "";
  return apiFetch(`/api/admin/agents${query}`);
}

export function createTrackingAgent(seatId) {
  return apiFetch("/api/admin/agents", {
    method: "POST",
    body: JSON.stringify({ seatId }),
  });
}

export function getBehaviorReviews({ roomId } = {}) {
  const params = new URLSearchParams();
  if (roomId && roomId !== "all") params.set("roomId", roomId);
  const query = params.toString();
  return apiFetch(`/api/admin/behavior/reviews${query ? `?${query}` : ""}`).then((data) => {
    if (Array.isArray(data)) return { items: data, handledKeys: [], clearedKeys: [] };
    return {
      items: data?.items ?? [],
      handledKeys: data?.handledKeys ?? [],
      clearedKeys: data?.clearedKeys ?? [],
    };
  });
}

export function clearBehaviorReview(reviewId) {
  return apiFetch(`/api/admin/behavior/reviews/${reviewId}/clear`, { method: "POST" });
}

export function penalizeBehaviorReview(reviewId) {
  return apiFetch(`/api/admin/behavior/reviews/${reviewId}/penalize`, { method: "POST" });
}

export function blockFlaggedActivity(body) {
  return apiFetch("/api/admin/behavior/block", {
    method: "POST",
    body: JSON.stringify(body),
  });
}

export function clearFlaggedActivity(body) {
  return apiFetch("/api/admin/behavior/clear", {
    method: "POST",
    body: JSON.stringify(body),
  });
}
