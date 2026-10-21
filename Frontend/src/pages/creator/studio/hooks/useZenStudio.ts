import { useState, useEffect, useCallback } from 'react';
import axios from 'axios';
import {
  ZenUploadResult,
  EducatorPartnershipInfo,
  ZenRoomLaunchResult,
  ZenQuestionPatch,
} from '../types/zen-studio.types';

export function useZenStudio() {
  const [partnership, setPartnership] = useState<EducatorPartnershipInfo | null>(null);
  const [isUploading, setIsUploading] = useState(false);
  const [uploadResult, setUploadResult] = useState<ZenUploadResult | null>(null);
  const [isLaunching, setIsLaunching] = useState(false);
  const [launchResult, setLaunchResult] = useState<ZenRoomLaunchResult | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Fetch partnership on mount
  useEffect(() => {
    let isMounted = true;
    axios
      .get<EducatorPartnershipInfo>('/api/creator/studio/partnership')
      .then((res) => {
        if (isMounted && res.data) {
          setPartnership(res.data);
        }
      })
      .catch((err) => {
        console.warn('Could not fetch partnership:', err);
      });
    return () => {
      isMounted = false;
    };
  }, []);

  // Upload Word docx file
  const handleUploadFile = useCallback(async (file: File) => {
    setIsUploading(true);
    setErrorMessage(null);
    try {
      const formData = new FormData();
      formData.append('file', file);

      const response = await axios.post<ZenUploadResult>(
        '/api/creator/studio/upload',
        formData,
        {
          headers: {
            'Content-Type': 'multipart/form-data',
          },
        }
      );

      if (response.data && response.data.success) {
        setUploadResult(response.data);
      } else {
        setErrorMessage('Tải tệp thất bại.');
      }
    } catch (err: any) {
      console.error('Zen Studio Upload error:', err);
      setErrorMessage(
        err.response?.data?.message || 'Có lỗi xảy ra khi bóc tách tệp đề thi.'
      );
    } finally {
      setIsUploading(false);
    }
  }, []);

  // Launch realtime exam room
  const handleLaunchRoom = useCallback(async () => {
    if (!uploadResult) return;
    setIsLaunching(true);
    try {
      const response = await axios.post<{
        success: boolean;
        pinCode: string;
        roomId: string;
        title: string;
        joinUrl: string;
        qrCodeData: string;
        launchedAt: string;
      }>('/api/creator/studio/launch', {
        title: uploadResult.examTitle,
        totalQuestions: uploadResult.totalQuestions,
        subjectCode: uploadResult.suggestedTaxonomy?.domainCode,
      });

      if (response.data && response.data.success) {
        setLaunchResult(response.data);
      }
    } catch (err: any) {
      console.error('Launch room error:', err);
      setErrorMessage('Không thể khởi tạo phòng thi realtime.');
    } finally {
      setIsLaunching(false);
    }
  }, [uploadResult]);

  const handleReset = useCallback(() => {
    setUploadResult(null);
    setLaunchResult(null);
    setErrorMessage(null);
  }, []);

  const handleCloseModal = useCallback(() => {
    setLaunchResult(null);
  }, []);

  const updateQuestion = useCallback((order: number, patch: ZenQuestionPatch) => {
    setUploadResult((prev) =>
      prev
        ? {
            ...prev,
            questions: prev.questions.map((q) =>
              q.order === order ? { ...q, ...patch, edited: true } : q,
            ),
          }
        : prev,
    );
  }, []);

  return {
    partnership,
    isUploading,
    uploadResult,
    isLaunching,
    launchResult,
    errorMessage,
    handleUploadFile,
    handleLaunchRoom,
    handleReset,
    handleCloseModal,
    updateQuestion,
  };
}
