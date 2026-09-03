export function getDisplayName(user) {
  if (user?.userType === "admin") return "ผู้ดูแลระบบ";
  if (!user?.firstName) return "ผู้ใช้งาน";

  const lastInitial = user.lastName?.trim()?.[0];
  return lastInitial ? `${user.firstName} ${lastInitial}.` : user.firstName;
}
