import { apiFetch } from "./auth";

export function getNavBadges() {
  return apiFetch("/api/nav-badges");
}
