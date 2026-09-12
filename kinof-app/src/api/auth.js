export const API_URL = import.meta.env.VITE_API_URL ?? "http://localhost:5106";

const AUTH_KEY = "kinofAuth";
const SESSION_ID_KEY = "kinofSessionId";
const TAB_SESSION_KEY = "kinofTabSessionId";
const TAKEN_OVER_KEY = "kinofSessionTakenOver";

function parseAuthJson(raw) {
  if (!raw) return null;
  try {
    return JSON.parse(raw);
  } catch {
    return null;
  }
}

export function peekStoredAuth() {
  return parseAuthJson(localStorage.getItem(AUTH_KEY) ?? sessionStorage.getItem(AUTH_KEY));
}

function readStoredAuth() {
  try {
    if (sessionStorage.getItem(TAKEN_OVER_KEY)) return null;

    const fromLocal = localStorage.getItem(AUTH_KEY);
    const fromSession = sessionStorage.getItem(AUTH_KEY);
    const parsed = parseAuthJson(fromLocal ?? fromSession);
    if (!parsed) return null;
    if (!fromLocal) {
      localStorage.setItem(AUTH_KEY, JSON.stringify(parsed));
      sessionStorage.removeItem(AUTH_KEY);
      if (parsed.sessionId) localStorage.setItem(SESSION_ID_KEY, parsed.sessionId);
    }

    const tabSessionId = sessionStorage.getItem(TAB_SESSION_KEY);
    if (tabSessionId && parsed.sessionId && tabSessionId !== parsed.sessionId) {
      return null;
    }
    if (!tabSessionId && parsed.sessionId) {
      sessionStorage.setItem(TAB_SESSION_KEY, parsed.sessionId);
    }
    return parsed;
  } catch {
    return null;
  }
}

function storeAuth(auth) {
  localStorage.setItem(AUTH_KEY, JSON.stringify(auth));
  sessionStorage.removeItem(AUTH_KEY);
  if (auth?.sessionId && localStorage.getItem(SESSION_ID_KEY) !== auth.sessionId) {
    localStorage.setItem(SESSION_ID_KEY, auth.sessionId);
  }
}

export function beginBrowserSession(auth) {
  const sessionId = crypto.randomUUID();
  const next = { ...auth, sessionId };
  sessionStorage.removeItem(TAKEN_OVER_KEY);
  sessionStorage.setItem(TAB_SESSION_KEY, sessionId);
  storeAuth(next);
  localStorage.setItem(SESSION_ID_KEY, sessionId);
  return next;
}

export function ensureBrowserSession(auth) {
  if (!auth) return null;
  const sessionId = auth.sessionId || localStorage.getItem(SESSION_ID_KEY) || crypto.randomUUID();
  const next = { ...auth, sessionId };
  sessionStorage.removeItem(TAKEN_OVER_KEY);
  sessionStorage.setItem(TAB_SESSION_KEY, sessionId);
  storeAuth(next);
  localStorage.setItem(SESSION_ID_KEY, sessionId);
  return next;
}

export function adoptBrowserSession() {
  sessionStorage.removeItem(TAKEN_OVER_KEY);
  const parsed = peekStoredAuth();
  if (!parsed?.accessToken) return null;
  return ensureBrowserSession(parsed);
}

export function clearStoredAuth() {
  localStorage.removeItem(AUTH_KEY);
  localStorage.removeItem(SESSION_ID_KEY);
  sessionStorage.removeItem(AUTH_KEY);
  sessionStorage.removeItem(TAB_SESSION_KEY);
  sessionStorage.removeItem(TAKEN_OVER_KEY);
}

export function markSessionTakenOver() {
  sessionStorage.setItem(TAKEN_OVER_KEY, "1");
  sessionStorage.removeItem(TAB_SESSION_KEY);
}

export function isSessionTakenOver() {
  return sessionStorage.getItem(TAKEN_OVER_KEY) === "1";
}

export function getTabSessionId() {
  return sessionStorage.getItem(TAB_SESSION_KEY);
}

export function getBrowserSessionId() {
  return localStorage.getItem(SESSION_ID_KEY);
}

async function parseResponse(response) {
  const data = await response.json().catch(() => ({}));
  if (!response.ok) {
    throw new Error(data.message ?? "ไม่สามารถเชื่อมต่อระบบได้ กรุณาลองใหม่");
  }
  return data;
}

async function post(path, body, token) {
  const headers = { "Content-Type": "application/json" };
  if (token) headers.Authorization = `Bearer ${token}`;

  const response = await fetch(`${API_URL}${path}`, {
    method: "POST",
    headers,
    body: JSON.stringify(body),
  });
  return parseResponse(response);
}

async function get(path, token) {
  const headers = {};
  if (token) headers.Authorization = `Bearer ${token}`;

  const response = await fetch(`${API_URL}${path}`, { headers });
  return parseResponse(response);
}

export async function apiFetch(path, options = {}) {
  const auth = readStoredAuth();
  const headers = {
    ...(options.headers ?? {}),
  };
  if (!(options.body instanceof FormData)) headers["Content-Type"] = "application/json";
  if (auth?.accessToken) {
    headers.Authorization = `Bearer ${auth.accessToken}`;
  }

  let response = await fetch(`${API_URL}${path}`, {
    ...options,
    headers,
  });

  if (response.status === 401 && auth?.refreshToken) {
    try {
      const refreshed = await post("/api/auth/refresh", { refreshToken: auth.refreshToken });
      const nextAuth = {
        accessToken: refreshed.accessToken,
        refreshToken: refreshed.refreshToken,
        user: refreshed.user,
        sessionId: auth.sessionId,
      };
      storeAuth(nextAuth);
      headers.Authorization = `Bearer ${nextAuth.accessToken}`;
      response = await fetch(`${API_URL}${path}`, { ...options, headers });
    } catch {
      clearStoredAuth();
      throw new Error("เซสชันหมดอายุ กรุณาเข้าสู่ระบบใหม่");
    }
  }

  return parseResponse(response);
}

export function login(username, password) {
  return post("/api/auth/login", { username, password });
}

export function register(details) {
  return post("/api/auth/register", details);
}

export function verifyEmailOtp(userId, code) {
  return post("/api/auth/verify-email-otp", { userId, code });
}

export function resendEmailOtp(userId) {
  return post("/api/auth/resend-email-otp", { userId });
}

export function refreshToken(refreshTokenValue) {
  return post("/api/auth/refresh", { refreshToken: refreshTokenValue });
}

export function forgotPassword(email) {
  return post("/api/auth/forgot-password", { email });
}

export function resetPassword(token, newPassword) {
  return post("/api/auth/reset-password", { token, newPassword });
}

export function getMe() {
  return apiFetch("/api/auth/me");
}

export function registerFace(imageBase64) {
  return apiFetch("/api/auth/register/face", {
    method: "POST",
    body: JSON.stringify({ imageBase64 }),
  });
}

export function requestEntryOtp(roomId) {
  return apiFetch("/api/auth/entry-otp/request", {
    method: "POST",
    body: JSON.stringify({ roomId: roomId || null }),
  });
}

export function resendEntryOtp(roomId) {
  return apiFetch("/api/auth/entry-otp/resend", {
    method: "POST",
    body: JSON.stringify({ roomId: roomId || null }),
  });
}

export function getActiveEntryOtp() {
  return apiFetch("/api/auth/entry-otp/active");
}

export { readStoredAuth, storeAuth };
