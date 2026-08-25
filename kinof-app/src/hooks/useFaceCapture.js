import { useCallback, useEffect, useRef, useState } from "react";
import { FaceDetector, FaceLandmarker, FilesetResolver } from "@mediapipe/tasks-vision";

const WASM_BASE = "https://cdn.jsdelivr.net/npm/@mediapipe/tasks-vision@latest/wasm";
const FACE_DETECTOR_MODEL =
  "https://storage.googleapis.com/mediapipe-models/face_detector/blaze_face_short_range/float16/latest/blaze_face_short_range.tflite";
const FACE_LANDMARKER_MODEL =
  "https://storage.googleapis.com/mediapipe-models/face_landmarker/face_landmarker/float16/latest/face_landmarker.task";

function eyeAspectRatio(landmarks, indices) {
  const verticalA = Math.hypot(
    landmarks[indices[1]].x - landmarks[indices[5]].x,
    landmarks[indices[1]].y - landmarks[indices[5]].y,
  );
  const verticalB = Math.hypot(
    landmarks[indices[2]].x - landmarks[indices[4]].x,
    landmarks[indices[2]].y - landmarks[indices[4]].y,
  );
  const horizontal = Math.hypot(
    landmarks[indices[0]].x - landmarks[indices[3]].x,
    landmarks[indices[0]].y - landmarks[indices[3]].y,
  );
  if (horizontal === 0) return 1;
  return (verticalA + verticalB) / (2 * horizontal);
}

function buildEmbedding(landmarks) {
  const values = [];
  for (const point of landmarks) {
    values.push(point.x, point.y, point.z ?? 0);
  }
  while (values.length < 512) values.push(0);
  const vector = values.slice(0, 512);
  const magnitude = Math.hypot(...vector) || 1;
  return vector.map((value) => value / magnitude);
}

function isFaceCentered(box, width, height) {
  const centerX = box.originX + box.width / 2;
  const centerY = box.originY + box.height / 2;
  const dx = Math.abs(centerX - width / 2) / width;
  const dy = Math.abs(centerY - height / 2) / height;
  const size = box.width / width;
  return dx < 0.12 && dy < 0.12 && size > 0.28 && size < 0.62;
}

export function useFaceCapture({ onCaptured, onError }) {
  const videoRef = useRef(null);
  const streamRef = useRef(null);
  const detectorRef = useRef(null);
  const landmarkerRef = useRef(null);
  const frameRef = useRef(null);
  const stableSinceRef = useRef(null);
  const blinkArmedRef = useRef(false);
  const blinkSeenRef = useRef(false);
  const capturedRef = useRef(false);

  const [status, setStatus] = useState("loading");
  const [hint, setHint] = useState("กำลังเตรียมกล้อง...");
  const [progress, setProgress] = useState(0);

  const stopCamera = useCallback(() => {
    if (frameRef.current) {
      cancelAnimationFrame(frameRef.current);
      frameRef.current = null;
    }
    streamRef.current?.getTracks().forEach((track) => track.stop());
    streamRef.current = null;
  }, []);

  useEffect(() => {
    let cancelled = false;

    async function start() {
      try {
        setStatus("loading");
        setHint("กำลังโหลด MediaPipe...");
        const vision = await FilesetResolver.forVisionTasks(WASM_BASE);
        const detector = await FaceDetector.createFromOptions(vision, {
          baseOptions: { modelAssetPath: FACE_DETECTOR_MODEL, delegate: "GPU" },
          runningMode: "VIDEO",
        });
        const landmarker = await FaceLandmarker.createFromOptions(vision, {
          baseOptions: { modelAssetPath: FACE_LANDMARKER_MODEL, delegate: "GPU" },
          runningMode: "VIDEO",
          numFaces: 1,
          outputFaceBlendshapes: false,
        });
        if (cancelled) return;

        detectorRef.current = detector;
        landmarkerRef.current = landmarker;

        const stream = await navigator.mediaDevices.getUserMedia({
          video: { facingMode: "user", width: { ideal: 1280 }, height: { ideal: 720 } },
          audio: false,
        });
        if (cancelled) {
          stream.getTracks().forEach((track) => track.stop());
          return;
        }

        streamRef.current = stream;
        const video = videoRef.current;
        video.srcObject = stream;
        await video.play();

        setStatus("scanning");
        setHint("จัดใบหน้าให้อยู่ในกรอบวงรี");

        let lastVideoTime = -1;
        const leftEye = [33, 160, 158, 133, 153, 144];
        const rightEye = [362, 385, 387, 263, 373, 380];

        const tick = () => {
          if (cancelled || capturedRef.current || !video.videoWidth) {
            frameRef.current = requestAnimationFrame(tick);
            return;
          }

          if (video.currentTime !== lastVideoTime) {
            lastVideoTime = video.currentTime;
            const detections = detector.detectForVideo(video, performance.now()).detections;
            const face = detections[0];

            if (!face?.boundingBox) {
              stableSinceRef.current = null;
              blinkArmedRef.current = false;
              blinkSeenRef.current = false;
              setProgress(0);
              setHint("จัดใบหน้าให้อยู่ในกรอบวงรี");
            } else {
              const box = face.boundingBox;
              const centered = isFaceCentered(box, video.videoWidth, video.videoHeight);
              if (!centered) {
                stableSinceRef.current = null;
                blinkArmedRef.current = false;
                blinkSeenRef.current = false;
                setProgress(10);
                setHint("ขยับใบหน้าให้อยู่กึ่งกลางกรอบ");
              } else if (!blinkArmedRef.current) {
                if (!stableSinceRef.current) stableSinceRef.current = performance.now();
                const stableMs = performance.now() - stableSinceRef.current;
                const nextProgress = Math.min(55, 10 + Math.floor(stableMs / 20));
                setProgress(nextProgress);
                if (stableMs > 900) {
                  blinkArmedRef.current = true;
                  setHint("กระพริบตาหนึ่งครั้งเพื่อยืนยันตัวตน");
                } else {
                  setHint("อยู่นิ่ง ๆ สักครู่...");
                }
              } else {
                const landmarkResult = landmarker.detectForVideo(video, performance.now());
                const landmarks = landmarkResult.faceLandmarks?.[0];
                if (landmarks) {
                  const leftEar = eyeAspectRatio(landmarks, leftEye);
                  const rightEar = eyeAspectRatio(landmarks, rightEye);
                  const ear = (leftEar + rightEar) / 2;
                  if (ear < 0.19) blinkSeenRef.current = true;
                  if (blinkSeenRef.current && ear > 0.24) {
                    capturedRef.current = true;
                    setStatus("capturing");
                    setProgress(100);
                    setHint("กำลังบันทึกใบหน้า...");
                    const embedding = buildEmbedding(landmarks);
                    stopCamera();
                    onCaptured?.(embedding);
                    return;
                  }
                  setProgress(Math.max(60, Math.min(95, 60 + Math.floor((0.24 - ear) * 200))));
                }
              }
            }
          }

          frameRef.current = requestAnimationFrame(tick);
        };

        frameRef.current = requestAnimationFrame(tick);
      } catch (error) {
        if (!cancelled) {
          setStatus("error");
          setHint("ไม่สามารถเปิดกล้องได้");
          onError?.(error);
        }
      }
    }

    start();
    return () => {
      cancelled = true;
      stopCamera();
    };
  }, [onCaptured, onError, stopCamera]);

  return { videoRef, status, hint, progress, stopCamera };
}
