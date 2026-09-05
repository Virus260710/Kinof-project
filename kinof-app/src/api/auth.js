const API_URL = import.meta.env.VITE_API_URL ?? "http://localhost:5106";

function readStoredAuth() {
  try {
    return JSON.parse(sessionStorage.getItem("kinofAuth"));
  } catch {
    return null;
  }
}

function storeAuth(auth) {
  sessionStorage.setItem("kinofAuth", JSON.stringify(auth));
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
      };
      storeAuth(nextAuth);
      headers.Authorization = `Bearer ${nextAuth.accessToken}`;
      response = await fetch(`${API_URL}${path}`, { ...options, headers });
    } catch {
      sessionStorage.removeItem("kinofAuth");
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

export { readStoredAuth, storeAuth };
