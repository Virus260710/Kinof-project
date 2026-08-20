const API_URL = import.meta.env.VITE_API_URL ?? "http://localhost:5106";

async function post(path, body) {
  const response = await fetch(`${API_URL}${path}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });

  const data = await response.json().catch(() => ({}));
  if (!response.ok) {
    throw new Error(data.message ?? "ไม่สามารถเชื่อมต่อระบบได้ กรุณาลองใหม่");
  }
  return data;
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
