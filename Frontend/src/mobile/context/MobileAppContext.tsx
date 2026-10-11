import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';
import { MOBILE_APP_VARIANTS, DEFAULT_MOBILE_VARIANT, MobileAppVariant } from '../config/mobile-app-variants';
import { offlineExamService } from '@/services/offline-exam.service';

interface MobileAppContextType {
  variant: MobileAppVariant;
  setVariantId: (id: string) => void;
  allVariants: MobileAppVariant[];
  isSwitcherOpen: boolean;
  setIsSwitcherOpen: (open: boolean) => void;
  downloadedCount: number;
  refreshDownloadedCount: () => Promise<void>;
  triggerHaptic: (pattern?: number | number[]) => void;
}

const MobileAppContext = createContext<MobileAppContextType | undefined>(undefined);

export const MobileAppProvider: React.FC<{ initialVariantId?: string; children: React.ReactNode }> = ({
  initialVariantId,
  children,
}) => {
  const [variant, setVariant] = useState<MobileAppVariant>(() => {
    // 1. Kiểm tra tham số truyền vào
    if (initialVariantId && MOBILE_APP_VARIANTS[initialVariantId.toLowerCase()]) {
      return MOBILE_APP_VARIANTS[initialVariantId.toLowerCase()];
    }

    // 2. Kiểm tra Query Param ?app=...
    if (typeof window !== 'undefined') {
      const params = new URLSearchParams(window.location.search);
      const appQuery = params.get('app');
      if (appQuery && MOBILE_APP_VARIANTS[appQuery.toLowerCase()]) {
        return MOBILE_APP_VARIANTS[appQuery.toLowerCase()];
      }

      // 3. Kiểm tra Subdomain (driving.dehoc.vn, banking.dehoc.vn, kids.dehoc.vn...)
      const host = window.location.hostname.toLowerCase();
      if (host.includes('kids') || host.includes('tienganh') || host.includes('treem')) return MOBILE_APP_VARIANTS.kids;
      if (host.includes('laixe') || host.includes('driving')) return MOBILE_APP_VARIANTS.driving;
      if (host.includes('nganhang') || host.includes('banking')) return MOBILE_APP_VARIANTS.banking;
      if (host.includes('thpt') || host.includes('toan')) return MOBILE_APP_VARIANTS.thpt;
      if (host.includes('yduoc') || host.includes('med')) return MOBILE_APP_VARIANTS.med;
      if (host.includes('antoan') || host.includes('pccc') || host.includes('hse')) return MOBILE_APP_VARIANTS.hse;

      // 4. Kiểm tra localStorage
      const saved = localStorage.getItem('aegis_mobile_app_variant');
      if (saved && MOBILE_APP_VARIANTS[saved]) {
        return MOBILE_APP_VARIANTS[saved];
      }
    }

    return DEFAULT_MOBILE_VARIANT;
  });

  const [isSwitcherOpen, setIsSwitcherOpen] = useState<boolean>(false);
  const [downloadedCount, setDownloadedCount] = useState<number>(0);

  const refreshDownloadedCount = useCallback(async () => {
    try {
      const list = await offlineExamService.listDownloadedExams();
      setDownloadedCount(list.length);
    } catch {
      setDownloadedCount(0);
    }
  }, []);

  useEffect(() => {
    refreshDownloadedCount();
  }, [refreshDownloadedCount]);

  const setVariantId = useCallback((id: string) => {
    const target = MOBILE_APP_VARIANTS[id.toLowerCase()];
    if (target) {
      setVariant(target);
      localStorage.setItem('aegis_mobile_app_variant', target.id);
      setIsSwitcherOpen(false);
    }
  }, []);

  const triggerHaptic = useCallback((pattern: number | number[] = 15) => {
    if (typeof window !== 'undefined' && 'vibrate' in navigator) {
      try {
        navigator.vibrate(pattern);
      } catch {
        // Ignored
      }
    }
  }, []);

  return (
    <MobileAppContext.Provider
      value={{
        variant,
        setVariantId,
        allVariants: Object.values(MOBILE_APP_VARIANTS),
        isSwitcherOpen,
        setIsSwitcherOpen,
        downloadedCount,
        refreshDownloadedCount,
        triggerHaptic,
      }}
    >
      {children}
    </MobileAppContext.Provider>
  );
};

export function useMobileApp() {
  const context = useContext(MobileAppContext);
  if (!context) {
    throw new Error('useMobileApp must be used within a MobileAppProvider');
  }
  return context;
}
