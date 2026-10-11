import { useEffect, useRef, useState, useCallback } from 'react';
import { EdgeAiVisionProctor, EdgeVisionViolation, VisionProctorConfig } from '../services/EdgeAiVisionProctor';

interface UseEdgeAiVisionProctorProps {
  videoRef: React.RefObject<HTMLVideoElement | null>;
  stream: MediaStream | null;
  isEnabled: boolean;
  onViolationDetected: (violation: EdgeVisionViolation) => void;
  config?: VisionProctorConfig;
}

export function useEdgeAiVisionProctor({
  videoRef,
  stream,
  isEnabled,
  onViolationDetected,
  config
}: UseEdgeAiVisionProctorProps) {
  const proctorRef = useRef<EdgeAiVisionProctor | null>(null);
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const intervalRef = useRef<number | null>(null);
  const [isAiMonitoring, setIsAiMonitoring] = useState(false);
  const [lastViolation, setLastViolation] = useState<EdgeVisionViolation | null>(null);

  // Khởi tạo Canvas ẩn để phân tích hình học quang học
  useEffect(() => {
    if (!canvasRef.current) {
      const canvas = document.createElement('canvas');
      canvas.width = 320;
      canvas.height = 240;
      canvasRef.current = canvas;
    }
  }, []);

  useEffect(() => {
    if (!isEnabled || !stream) {
      if (intervalRef.current) {
        window.clearInterval(intervalRef.current);
        intervalRef.current = null;
      }
      if (proctorRef.current) {
        proctorRef.current.dispose();
        proctorRef.current = null;
      }
      setIsAiMonitoring(false);
      return;
    }

    const proctor = new EdgeAiVisionProctor(config);
    proctorRef.current = proctor;

    proctor.initAudioMonitoring(stream).then(() => {
      setIsAiMonitoring(true);
    });

    // Vòng lặp suy luận AI cạnh: Quét định kỳ mỗi 1500ms
    intervalRef.current = window.setInterval(async () => {
      if (!videoRef.current || !proctorRef.current) return;

      const violation = proctorRef.current.analyzeFrame(
        videoRef.current,
        canvasRef.current || undefined
      );

      if (violation) {
        const proofHash = await proctorRef.current.generateProofHash(violation);
        violation.evidenceProofHash = proofHash;
        setLastViolation(violation);
        onViolationDetected(violation);
      }
    }, 1500);

    return () => {
      if (intervalRef.current) {
        window.clearInterval(intervalRef.current);
        intervalRef.current = null;
      }
      if (proctorRef.current) {
        proctorRef.current.dispose();
        proctorRef.current = null;
      }
      setIsAiMonitoring(false);
    };
  }, [isEnabled, stream, onViolationDetected, config, videoRef]);

  return {
    isAiMonitoring,
    lastViolation
  };
}
