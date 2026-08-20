import React, { useState, useRef } from "react";
import { Camera, Image as ImageIcon, X, Plus, Eye, Calendar, FileText } from "lucide-react";
import TopBar from "../../components/TopBar";
import Card from "../../components/Card";
import Pill from "../../components/Pill";
import { NAVY, NAVY2 } from "../../theme";

export default function UserHelp({ tickets = [], addTicket, notify }) {
  const [topic, setTopic] = useState("");
  const [detail, setDetail] = useState("");
  const [images, setImages] = useState([]); // เก็บ Array รูปภาพแนบ (Base64) Max 3
  const [showSourceModal, setShowSourceModal] = useState(false);

  // State สำหรับเก็บ Ticket ที่ผู้ใช้เลือกกดดูรายละเอียด
  const [selectedTicket, setSelectedTicket] = useState(null);
  // State สำหรับคลิกขยายดูรูปภาพเดี่ยวๆ ใน Modal รายละเอียด
  const [previewImage, setPreviewImage] = useState(null);

  const fileInputRef = useRef(null);
  const cameraInputRef = useRef(null);

  // ฟังก์ชันรองรับการเปลี่ยนไฟล์รูปภาพ (คำนวณโควตาภาพที่เหลือเพื่อป้องกันการเลือกเกิน 3 รูป)
  const handleFileChange = (e) => {
    const files = Array.from(e.target.files);
    if (!files.length) return;

    // คำนวณจำนวนรูปที่ยังเลือกเพิ่มได้
    const availableSlots = 3 - images.length;
    const filesToProcess = files.slice(0, availableSlots);

    filesToProcess.forEach((file) => {
      const reader = new FileReader();
      reader.onloadend = () => {
        setImages((prev) => {
          if (prev.length < 3) {
            return [...prev, reader.result];
          }
          return prev;
        });
      };
      reader.readAsDataURL(file);
    });

    e.target.value = "";
    setShowSourceModal(false);
  };

  const handleRemoveImage = (indexToRemove) => {
    setImages((prev) => prev.filter((_, index) => index !== indexToRemove));
  };

  const handleSubmit = () => {
    const trimmedTopic = topic.trim();
    const trimmedDetail = detail.trim();

    if (!trimmedTopic || !trimmedDetail) return;

    const newTicket = {
      id: Date.now(),
      user: "som_ying",
      title: trimmedTopic,
      room: "-",
      machine: "-",
      detail: trimmedDetail,
      images,
      status: "รอดำเนินการ",
      createdAt: new Date().toLocaleDateString("th-TH", {
        year: "numeric",
        month: "long",
        day: "numeric",
      }),
    };

    if (addTicket) {
      addTicket(newTicket);
    }

    if (notify) {
      notify("ส่งคำร้องขอความช่วยเหลือเรียบร้อยแล้ว");
    }

    setTopic("");
    setDetail("");
    setImages([]);
  };

  return (
    <div className="w-full">
      <TopBar name="สมหญิง ส." />
      <h1 className="text-base md:text-lg font-medium text-gray-900 mb-4">ช่วยเหลือ</h1>

      {/* Input ไฟล์ซ่อนไว้ */}
      <input
        type="file"
        ref={fileInputRef}
        onChange={handleFileChange}
        accept="image/*"
        multiple
        className="hidden"
      />
      <input
        type="file"
        ref={cameraInputRef}
        onChange={handleFileChange}
        accept="image/*"
        capture="environment"
        className="hidden"
      />

      <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
        {/* คอลัมน์ซ้าย: ฟอร์มส่งคำร้อง */}
        <Card className="p-4 md:p-5">
          <div
            className="rounded-xl text-white p-4 mb-4 text-xs font-light leading-relaxed shadow-sm"
            style={{ background: `linear-gradient(120deg, ${NAVY}, ${NAVY2})` }}
          >
            ต้องการความช่วยเหลือ? คุณสามารถแจ้งปัญหาเครื่องคอมและเจ้าหน้าที่จะดำเนินการให้
          </div>

          <div className="text-xs font-medium text-gray-700 mb-1">
            หัวข้อปัญหา <span className="text-blue-600">*</span>
          </div>
          <select
            value={topic}
            onChange={(e) => setTopic(e.target.value)}
            className="w-full text-xs border border-gray-200 rounded-xl px-3 py-2.5 mb-3 focus:outline-none focus:border-blue-500 bg-white"
          >
            <option value="">-- เลือกหัวข้อปัญหา --</option>
            <option value="ปัญหาการจองห้องแล็บ">ปัญหาการจองห้องแล็บ</option>
            <option value="ปัญหาโปรไฟล์/บัญชี">ปัญหาโปรไฟล์/บัญชี</option>
            <option value="ปัญหาเครื่องคอมพิวเตอร์">ปัญหาเครื่องคอมพิวเตอร์</option>
            <option value="อื่น ๆ">อื่น ๆ</option>
          </select>

          <div className="text-xs font-medium text-gray-700 mb-1">
            รายละเอียด <span className="text-blue-600">*</span>
          </div>
          <textarea
            value={detail}
            onChange={(e) => setDetail(e.target.value)}
            rows={4}
            placeholder="อธิบายปัญหาโดยรายละเอียด (แนบรูปภาพได้ด้านล่าง)"
            className="w-full text-xs border border-gray-200 rounded-xl p-3 mb-4 focus:outline-none focus:border-blue-500 resize-none bg-gray-50/30 placeholder:text-gray-300"
          />

          {/* ปุ่มเลือกแนบรูปภาพ */}
          <div className="mb-5">
            <div className="text-xs font-medium text-gray-700 mb-2">
              แนบรูปภาพเพิ่มเติม{" "}
              <span className="text-gray-400 font-normal">(เลือกได้ไม่เกิน 3 ไฟล์)</span>
            </div>

            <div className="grid grid-cols-3 gap-2 md:gap-3">
              {[0, 1, 2].map((index) => {
                const imgUrl = images[index];
                return (
                  <div key={index} className="aspect-square relative">
                    {imgUrl ? (
                      <div className="w-full h-full rounded-2xl border border-gray-200 overflow-hidden relative group">
                        <img
                          src={imgUrl}
                          alt={`attached-${index}`}
                          className="w-full h-full object-cover"
                        />
                        <button
                          type="button"
                          onClick={() => handleRemoveImage(index)}
                          className="absolute top-1.5 right-1.5 bg-black/60 hover:bg-black/80 text-white rounded-full p-1 transition-colors"
                        >
                          <X size={12} />
                        </button>
                      </div>
                    ) : (
                      <button
                        type="button"
                        onClick={() => {
                          if (images.length < 3) {
                            setShowSourceModal(true);
                          }
                        }}
                        disabled={images.length >= 3}
                        className="w-full h-full border border-gray-200 rounded-2xl flex items-center justify-center bg-gray-50/60 hover:bg-gray-100 transition-colors text-gray-400 hover:text-gray-600 disabled:opacity-40 disabled:cursor-not-allowed"
                      >
                        <Plus size={24} strokeWidth={1.5} />
                      </button>
                    )}
                  </div>
                );
              })}
            </div>
          </div>

          <button
            type="button"
            disabled={!topic.trim() || !detail.trim()}
            onClick={handleSubmit}
            className="w-full text-white text-xs font-medium rounded-xl py-3 shadow-sm disabled:opacity-40 transition-opacity hover:opacity-90"
            style={{ background: NAVY }}
          >
            ส่งคำร้องขอความช่วยเหลือ
          </button>
        </Card>

        {/* คอลัมน์ขวา: ประวัติการส่งคำร้อง */}
        <Card className="p-4 md:p-5">
          <div className="text-sm font-semibold text-gray-800 mb-3">ประวัติการส่งคำร้อง</div>
          <div className="flex flex-col gap-2.5">
            {tickets
              .filter((t) => t.user === "som_ying")
              .map((t) => (
                <div
                  key={t.id}
                  onClick={() => setSelectedTicket(t)}
                  className="group flex flex-col border border-gray-100 rounded-xl p-3.5 text-xs gap-2 bg-white hover:border-blue-300 hover:shadow-sm transition-all cursor-pointer relative"
                >
                  <div className="flex items-center justify-between">
                    <div className="text-gray-800 font-semibold group-hover:text-blue-900 flex items-center gap-1.5 truncate pr-2">
                      {t.title}
                    </div>
                    <Pill
                      tone={
                        t.status === "เสร็จสิ้น"
                          ? "green"
                          : t.status === "กำลังดำเนินการ"
                          ? "blue"
                          : "amber"
                      }
                    >
                      {t.status}
                    </Pill>
                  </div>

                  <div className="text-gray-500 text-[11px] line-clamp-2 leading-relaxed">
                    {t.detail}
                  </div>

                  {/* แสดงรูปตัวอย่างขนาดเล็กในรายการ */}
                  <div className="flex items-center justify-between mt-1 pt-1 border-t border-gray-50">
                    <div className="flex gap-1.5 items-center">
                      {t.images && t.images.length > 0 && (
                        <div className="flex gap-1">
                          {t.images.map((img, i) => (
                            <img
                              key={i}
                              src={img}
                              alt="attachment"
                              className="w-7 h-7 rounded-md object-cover border border-gray-200"
                            />
                          ))}
                        </div>
                      )}
                      {t.createdAt && (
                        <span className="text-[10px] text-gray-400 flex items-center gap-1 ml-1">
                          <Calendar size={11} /> {t.createdAt}
                        </span>
                      )}
                    </div>

                    <span className="text-[10px] text-blue-600 font-medium flex items-center gap-0.5 opacity-80 group-hover:opacity-100 group-hover:translate-x-0.5 transition-all">
                      <Eye size={12} /> ดูรายละเอียด
                    </span>
                  </div>
                </div>
              ))}

            {tickets.filter((t) => t.user === "som_ying").length === 0 && (
              <div className="text-xs text-gray-400 text-center py-12 border border-dashed border-gray-200 rounded-xl">
                ยังไม่มีประวัติการส่งคำร้อง
              </div>
            )}
          </div>
        </Card>
      </div>

      {/* 1. Modal แสดงรายละเอียดคำร้องขนาดใหญ่ */}
      {selectedTicket && (
        <div className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-3 md:p-4 backdrop-blur-sm animate-in fade-in duration-200">
          <div className="bg-white rounded-2xl w-full max-w-lg overflow-hidden shadow-2xl border border-gray-100 max-h-[90vh] flex flex-col">
            {/* Header */}
            <div className="p-4 md:p-5 text-white flex items-center justify-between shrink-0" style={{ background: NAVY }}>
              <div className="flex items-center gap-2">
                <FileText size={18} className="text-blue-200" />
                <h3 className="text-xs md:text-sm font-semibold">รายละเอียดคำร้องขอความช่วยเหลือ</h3>
              </div>
              <button
                type="button"
                onClick={() => setSelectedTicket(null)}
                className="text-white/80 hover:text-white hover:bg-white/10 p-1 rounded-full transition-colors"
              >
                <X size={18} />
              </button>
            </div>

            {/* Body */}
            <div className="p-4 md:p-6 flex flex-col gap-4 overflow-y-auto">
              <div className="flex items-start justify-between border-b border-gray-100 pb-3">
                <div>
                  <span className="text-[10px] text-gray-400 font-medium uppercase tracking-wider block mb-0.5">
                    หัวข้อปัญหา
                  </span>
                  <h4 className="text-sm md:text-base font-bold text-gray-800">{selectedTicket.title}</h4>
                </div>
                <Pill
                  tone={
                    selectedTicket.status === "เสร็จสิ้น"
                      ? "green"
                      : selectedTicket.status === "กำลังดำเนินการ"
                      ? "blue"
                      : "amber"
                  }
                >
                  {selectedTicket.status}
                </Pill>
              </div>

              <div>
                <span className="text-[10px] text-gray-400 font-medium uppercase tracking-wider block mb-1">
                  รายละเอียดปัญหา
                </span>
                <div className="bg-gray-50 border border-gray-100 rounded-xl p-3.5 md:p-4 text-xs text-gray-700 leading-relaxed whitespace-pre-line">
                  {selectedTicket.detail}
                </div>
              </div>

              {selectedTicket.images && selectedTicket.images.length > 0 && (
                <div>
                  <span className="text-[10px] text-gray-400 font-medium uppercase tracking-wider block mb-2">
                    รูปภาพที่แนบมา ({selectedTicket.images.length} รูป)
                  </span>
                  <div className="grid grid-cols-3 gap-2 md:gap-3">
                    {selectedTicket.images.map((img, index) => (
                      <div
                        key={index}
                        onClick={(e) => {
                          e.stopPropagation();
                          setPreviewImage(img);
                        }}
                        className="aspect-square rounded-xl overflow-hidden border border-gray-200 bg-gray-100 cursor-pointer group relative shadow-sm"
                      >
                        <img
                          src={img}
                          alt={`attached-large-${index}`}
                          className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-200"
                        />
                        <div className="absolute inset-0 bg-black/30 opacity-0 group-hover:opacity-100 flex items-center justify-center transition-opacity text-white text-xs gap-1 font-medium">
                          <Eye size={14} /> ขยาย
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              <div className="text-[11px] text-gray-400 border-t border-gray-100 pt-3 flex justify-between items-center">
                <span>ผู้ส่ง: สมหญิง สวยงาม (som_ying)</span>
                <span>{selectedTicket.createdAt || "ไม่ระบุวันที่"}</span>
              </div>
            </div>

            {/* Footer */}
            <div className="p-3 md:p-4 bg-gray-50 border-t border-gray-100 flex justify-end shrink-0">
              <button
                type="button"
                onClick={() => setSelectedTicket(null)}
                className="text-xs font-medium text-gray-700 bg-white border border-gray-200 hover:bg-gray-100 rounded-xl px-5 py-2 transition-colors shadow-sm"
              >
                ปิดหน้าต่าง
              </button>
            </div>
          </div>
        </div>
      )}

      {/* 2. Modal ขยายรูปภาพแบบเต็มจอ */}
      {previewImage && (
        <div
          className="fixed inset-0 bg-black/80 z-[60] flex items-center justify-center p-4 cursor-pointer animate-in fade-in duration-150"
          onClick={() => setPreviewImage(null)}
        >
          <div className="relative max-w-3xl max-h-[90vh]">
            <button
              type="button"
              onClick={() => setPreviewImage(null)}
              className="absolute -top-10 right-0 text-white hover:text-gray-300"
            >
              <X size={24} />
            </button>
            <img
              src={previewImage}
              alt="fullscreen-preview"
              className="max-w-full max-h-[85vh] rounded-xl object-contain shadow-2xl"
            />
          </div>
        </div>
      )}

      {/* 3. Modal เลือกวิธีแนบรูปภาพ */}
      {showSourceModal && (
        <div className="fixed inset-0 bg-black/40 z-50 flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl p-5 w-full max-w-xs shadow-xl animate-in fade-in zoom-in duration-150">
            <div className="flex justify-between items-center mb-4">
              <h3 className="text-xs font-semibold text-gray-800">เลือกวิธีแนบรูปภาพ</h3>
              <button
                type="button"
                onClick={() => setShowSourceModal(false)}
                className="text-gray-400 hover:text-gray-600"
              >
                <X size={16} />
              </button>
            </div>

            <div className="flex flex-col gap-2">
              <button
                type="button"
                onClick={() => {
                  cameraInputRef.current?.click();
                }}
                className="flex items-center gap-3 w-full p-3 text-xs text-gray-700 bg-gray-50 hover:bg-gray-100 rounded-xl transition-colors text-left"
              >
                <div className="w-8 h-8 rounded-lg bg-blue-100 text-blue-800 flex items-center justify-center shrink-0">
                  <Camera size={18} />
                </div>
                <div>
                  <div className="font-medium">ถ่ายรูป</div>
                  <div className="text-[10px] text-gray-400">เปิดกล้องถ่ายภาพใหม่</div>
                </div>
              </button>

              <button
                type="button"
                onClick={() => {
                  fileInputRef.current?.click();
                }}
                className="flex items-center gap-3 w-full p-3 text-xs text-gray-700 bg-gray-50 hover:bg-gray-100 rounded-xl transition-colors text-left"
              >
                <div className="w-8 h-8 rounded-lg bg-emerald-100 text-emerald-800 flex items-center justify-center shrink-0">
                  <ImageIcon size={18} />
                </div>
                <div>
                  <div className="font-medium">เลือกจากคลังภาพ / ไฟล์</div>
                  <div className="text-[10px] text-gray-400">เลือกภาพหรือไฟล์ที่มีในเครื่อง</div>
                </div>
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}