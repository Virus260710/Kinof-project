import React from "react";
import TopBar from "../../components/TopBar";
import Card from "../../components/Card";
import Pill from "../../components/Pill";
import { NAVY, NAVY2, GOLD } from "../../theme";
import { scheduleRows as mockScheduleRows } from "../../data/mockData";

// ข้อมูลจำลองประวัติการหักคะแนน
const initialPenaltyHistory = [
  {
    id: 1,
    date: "2 เมษายน 2569",
    points: 5,
    reason: "ไม่มาใช้ห้องแล็บตามวัน-เวลาที่จองไว้",
  },
];

// TODO(backend): profile + score -> GET /api/users/me, schedule -> GET /api/schedule/me, penalties -> GET /api/penalties/me
export default function UserProfile({ scheduleRows = mockScheduleRows, userScore = 95, auth }) {
  const penaltyHistory = initialPenaltyHistory;
  const user = auth?.user;
  const initial = user?.firstName?.[0] ?? "ส";
  const topBarName = user ? `${user.firstName} ${user.lastName?.[0] ?? ""}.` : "ผู้ใช้งาน";

  return (
    <div className="w-full">
      <TopBar name={topBarName} />
      <h1 className="text-base md:text-lg font-medium text-gray-900 mb-4">โปรไฟล์</h1>

      {/* แบนเนอร์ต้อนรับ */}
      <div
        className="rounded-2xl text-white p-5 md:p-6 mb-6 shadow-sm"
        style={{ background: `linear-gradient(120deg, ${NAVY}, ${NAVY2})` }}
      >
        <div className="font-semibold text-sm md:text-base mb-1">สวัสดี {user?.firstName ?? "ผู้ใช้งาน"}!</div>
        <div className="text-xs text-blue-100 font-light">
          ข้อมูลและรายละเอียดโปรไฟล์ของคุณ
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4 mb-6">
        {/* การ์ดข้อมูลส่วนตัว */}
        <Card className="p-4 md:p-5 lg:col-span-2 flex flex-col justify-between">
          <div>
            <div className="flex items-center gap-3.5 mb-5">
              <div
                className="w-12 h-12 rounded-full flex items-center justify-center text-white font-bold text-base shadow-sm shrink-0"
                style={{ background: GOLD }}
              >
                {initial}
              </div>
              <div>
                <div className="font-semibold text-gray-900 text-sm mb-1">
                  {user ? `${user.firstName} ${user.lastName}` : "สมหญิง สวยงาม"}
                </div>
                <Pill tone="blue">{user?.userType === "external" ? "บุคคลภายนอก" : "นักศึกษา"}</Pill>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-y-3 text-xs border-t border-gray-100 pt-4">
              <div className="text-gray-500">อีเมล</div>
              <div className="text-gray-800 font-medium truncate pr-2">{user?.email ?? "somy@gmail.com"}</div>
              <div className="text-gray-500">ชื่อผู้ใช้</div>
              <div className="text-gray-800 font-medium">{user?.username ?? "som_ying"}</div>
              <div className="text-gray-500">ลงทะเบียนใบหน้า</div>
              <div className="text-gray-800 font-medium">{user?.faceEnrolled ? "แล้ว" : "ยังไม่ได้ลงทะเบียน"}</div>
            </div>
          </div>
        </Card>

        {/* การ์ดสถิติ, คะแนน และประวัติการหักคะแนน */}
        <Card className="p-4 md:p-5 flex flex-col gap-4">
          <div>
            <div className="text-xs text-gray-400 font-medium mb-2">
              สถิติการใช้งาน
            </div>
            <div className="flex gap-3">
              <div className="flex-1 text-center border border-gray-100 bg-gray-50/50 rounded-xl py-2.5">
                <div className="font-bold text-gray-900 text-sm">3</div>
                <div className="text-[10px] text-gray-400 mt-0.5">ครั้งที่จอง</div>
              </div>
              <div className="flex-1 text-center border border-gray-100 bg-gray-50/50 rounded-xl py-2.5">
                <div className="font-bold text-gray-900 text-sm">5</div>
                <div className="text-[10px] text-gray-400 mt-0.5">ชั่วโมงที่ใช้</div>
              </div>
            </div>
          </div>

          <div
            className="rounded-xl py-2.5 text-center text-xs font-semibold text-white shadow-sm"
            style={{ background: NAVY }}
          >
            {userScore} / 100 คะแนนการใช้งาน
          </div>

          {/* ส่วนประวัติการหักคะแนน */}
          <div>
            <div className="text-xs font-semibold text-gray-800 mb-2">
              ประวัติการหักคะแนน
            </div>
            <div className="flex flex-col gap-2">
              {penaltyHistory.length > 0 ? (
                penaltyHistory.map((item) => (
                  <div
                    key={item.id}
                    className="border border-gray-200 rounded-xl p-3.5 bg-white text-xs"
                  >
                    <div className="font-semibold text-gray-800 mb-1">
                      {item.date} - หัก {item.points} คะแนน
                    </div>
                    <div className="text-gray-600 font-light leading-relaxed">
                      สาเหตุ: {item.reason}
                    </div>
                  </div>
                ))
              ) : (
                <div className="text-xs text-gray-400 text-center py-3 border border-dashed border-gray-200 rounded-xl">
                  ไม่มีประวัติการหักคะแนน
                </div>
              )}
            </div>
          </div>
        </Card>
      </div>

      {/* ตารางเรียน */}
      <Card className="p-4 md:p-5">
        <div className="text-sm font-semibold text-gray-800 mb-3">ตารางเรียน</div>
        <div className="overflow-x-auto">
          <div className="flex flex-col gap-2 min-w-[480px]">
            {scheduleRows && scheduleRows.length > 0 ? (
              scheduleRows.map((r, i) => (
                <div
                  key={r.day || i}
                  className="flex items-center border border-gray-100 rounded-xl overflow-hidden text-xs hover:bg-gray-50/40 transition-colors"
                >
                  <div className="bg-gray-50 text-gray-600 font-medium px-4 py-3 w-28 shrink-0">
                    {r.day}
                  </div>
                  <div className="px-4 py-3 flex-1 text-gray-800 font-normal">
                    {r.course || "— ไม่มีคาบเรียน —"}
                  </div>
                  <div className="px-4 py-3 text-gray-500 w-52 shrink-0 text-right">
                    {r.time} {r.room && `· ${r.room}`}
                  </div>
                </div>
              ))
            ) : (
              <div className="text-xs text-gray-400 text-center py-6 border border-dashed border-gray-200 rounded-xl">
                ไม่พบข้อมูลตารางเรียน
              </div>
            )}
          </div>
        </div>
      </Card>
    </div>
  );
}