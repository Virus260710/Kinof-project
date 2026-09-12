import asyncio
import os
from contextlib import asynccontextmanager

import cv2
import numpy as np
from fastapi import FastAPI, File, HTTPException, UploadFile
from insightface.app import FaceAnalysis
from pydantic import BaseModel
from starlette.concurrency import run_in_threadpool

MAX_IMAGE_BYTES = 5 * 1024 * 1024
SUPPORTED_CONTENT_TYPES = {"image/jpeg", "image/png", "image/webp"}

face_analyzer: FaceAnalysis | None = None
inference_lock = asyncio.Lock()


class EmbeddingResponse(BaseModel):
    embedding: list[float]


def create_analyzer() -> FaceAnalysis:
    model_name = os.getenv("INSIGHTFACE_MODEL", "buffalo_l")
    detector_size = int(os.getenv("INSIGHTFACE_DET_SIZE", "640"))
    analyzer = FaceAnalysis(
        name=model_name,
        providers=["CPUExecutionProvider"],
    )
    analyzer.prepare(ctx_id=-1, det_size=(detector_size, detector_size))
    return analyzer


@asynccontextmanager
async def lifespan(_: FastAPI):
    global face_analyzer
    face_analyzer = await run_in_threadpool(create_analyzer)
    yield
    face_analyzer = None


app = FastAPI(
    title="KINOF Face Service",
    version="1.0.0",
    lifespan=lifespan,
)


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ready" if face_analyzer is not None else "loading"}


def extract_embedding(image_bytes: bytes) -> list[float]:
    if face_analyzer is None:
        raise HTTPException(status_code=503, detail="Face Service ยังไม่พร้อมใช้งาน")

    encoded = np.frombuffer(image_bytes, dtype=np.uint8)
    image = cv2.imdecode(encoded, cv2.IMREAD_COLOR)
    if image is None:
        raise HTTPException(status_code=400, detail="ไม่สามารถอ่านข้อมูลภาพได้")

    faces = face_analyzer.get(image)
    if not faces:
        raise HTTPException(
            status_code=422,
            detail="ไม่พบใบหน้าในภาพ กรุณาจัดใบหน้าให้อยู่ในกรอบแล้วลองใหม่",
        )
    if len(faces) > 1:
        raise HTTPException(
            status_code=422,
            detail="พบมากกว่าหนึ่งใบหน้า กรุณาอยู่ในภาพเพียงคนเดียว",
        )

    embedding = np.asarray(faces[0].normed_embedding, dtype=np.float32)
    if embedding.shape != (512,) or not np.isfinite(embedding).all():
        raise HTTPException(status_code=500, detail="โมเดลส่งผลลัพธ์ไม่ถูกต้อง")

    norm = float(np.linalg.norm(embedding))
    if norm <= 0:
        raise HTTPException(status_code=500, detail="โมเดลส่งผลลัพธ์ไม่ถูกต้อง")

    return (embedding / norm).tolist()


@app.post("/api/v1/embeddings", response_model=EmbeddingResponse)
async def create_embedding(image: UploadFile = File(...)) -> EmbeddingResponse:
    if image.content_type not in SUPPORTED_CONTENT_TYPES:
        raise HTTPException(
            status_code=415,
            detail="รองรับเฉพาะภาพ JPEG, PNG หรือ WebP",
        )

    image_bytes = await image.read(MAX_IMAGE_BYTES + 1)
    await image.close()
    if not image_bytes or len(image_bytes) > MAX_IMAGE_BYTES:
        raise HTTPException(
            status_code=413,
            detail="ภาพต้องมีขนาดไม่เกิน 5 MB",
        )

    async with inference_lock:
        embedding = await run_in_threadpool(extract_embedding, image_bytes)
    return EmbeddingResponse(embedding=embedding)
