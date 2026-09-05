import { apiFetch } from "./auth";

export const PROBLEM_REPORTS_API_URL = import.meta.env.VITE_API_URL ?? "http://localhost:5106";

export function problemImageUrl(url) {
  return url.startsWith("http") ? url : `${PROBLEM_REPORTS_API_URL}${url}`;
}

export async function loadProblemImage(url) {
  const auth = JSON.parse(sessionStorage.getItem("kinofAuth") ?? "null");
  const response = await fetch(problemImageUrl(url), {
    headers: auth?.accessToken ? { Authorization: `Bearer ${auth.accessToken}` } : {},
  });
  if (!response.ok) throw new Error("ไม่สามารถโหลดรูปภาพแนบได้");
  return URL.createObjectURL(await response.blob());
}

export function getMyProblemReports() {
  return apiFetch("/api/problem-reports/me");
}

export function getProblemReports() {
  return apiFetch("/api/problem-reports");
}

export function getProblemReport(id) {
  return apiFetch(`/api/problem-reports/${id}`);
}

export function createProblemReport({ category, description, files }) {
  const form = new FormData();
  form.append("category", category);
  form.append("description", description);
  files.forEach((file) => form.append("files", file));
  return apiFetch("/api/problem-reports", { method: "POST", body: form });
}

export function updateProblemReportStatus(id, status) {
  return apiFetch(`/api/problem-reports/${id}/status`, {
    method: "PATCH",
    body: JSON.stringify({ status }),
  });
}
