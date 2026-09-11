# KINOF Face Service

FastAPI service สำหรับสร้าง InsightFace `buffalo_l` embedding 512 มิติจากภาพที่ Backend ส่งมา ภาพอยู่ในหน่วยความจำเฉพาะระหว่างประมวลผลและไม่ถูกบันทึกลงดิสก์หรือฐานข้อมูล

## Requirements

- Python 3.10 หรือ 3.11 (64-bit)
- RAM อย่างน้อย 4 GB
- อินเทอร์เน็ตในครั้งแรกเพื่อดาวน์โหลดโมเดล `buffalo_l`

## Run

```powershell
cd C:\Users\User\Desktop\Kinof-project\face-service
py -3.11 -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install -r requirements.txt
python -m uvicorn app.main:app --host 127.0.0.1 --port 8001
```

ครั้งแรก InsightFace จะดาวน์โหลดโมเดลไว้ใน user cache ของเครื่อง จากนั้นตรวจสถานะที่ `http://localhost:8001/health`

## API

`POST /api/v1/embeddings` รับ multipart field ชื่อ `image` (`image/jpeg`, `image/png` หรือ `image/webp`) และคืน:

```json
{
  "embedding": [0.0123, -0.0456]
}
```

ระบบจะปฏิเสธภาพที่ไม่มีใบหน้า มีมากกว่าหนึ่งใบหน้า หรือมีขนาดเกิน 5 MB

Environment variables:

- `INSIGHTFACE_MODEL` ค่าเริ่มต้น `buffalo_l`
- `INSIGHTFACE_DET_SIZE` ค่าเริ่มต้น `640`
