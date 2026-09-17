import { apiFetch } from "./auth";

export function getBehavior() {
  return apiFetch("/api/behavior");
}
