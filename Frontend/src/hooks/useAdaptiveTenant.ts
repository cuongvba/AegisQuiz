import { useState, useEffect, useMemo, useCallback } from 'react';
import { learnerQuizService } from '../services/learner-quiz.service';

export type TenantScale = 'individual' | 'team' | 'classroom' | 'organization' | 'enterprise' | 'global';

export interface DynamicDomainOption {
  code: string;
  name: string;
  description?: string;
  icon?: string;
  colorBadge?: string;
  isSystemStandard?: boolean;
  parentDomainCode?: string;
}

export const DEFAULT_FALLBACK_DOMAINS: DynamicDomainOption[] = [
  { code: 'BANKING', name: 'Tài chính - Ngân hàng', icon: 'Landmark', colorBadge: '#059669', isSystemStandard: true },
  { code: 'HEALTHCARE', name: 'Y tế - Sức khỏe', icon: 'HeartPulse', colorBadge: '#dc2626', isSystemStandard: true },
  { code: 'EDUCATION', name: 'Giáo dục & Học thuật', icon: 'GraduationCap', colorBadge: '#2563eb', isSystemStandard: true },
  { code: 'IT_SECURITY', name: 'An toàn TT & CNTT', icon: 'ShieldCheck', colorBadge: '#0284c7', isSystemStandard: true },
  { code: 'HSE', name: 'An toàn - Môi trường LĐ', icon: 'HardHat', colorBadge: '#d97706', isSystemStandard: true },
  { code: 'GOV_DRIVING', name: 'Sát hạch Lái xe Quốc gia', icon: 'Car', colorBadge: '#7c3aed', isSystemStandard: true },
  { code: 'GENERAL', name: 'Tổng hợp / Đại cương', icon: 'Layers', colorBadge: '#4b5563', isSystemStandard: true },
];

let cachedDomains: DynamicDomainOption[] | null = null;
let isFetchingDomains = false;
const listeners: Array<(domains: DynamicDomainOption[]) => void> = [];

export function useAdaptiveTenant() {
  const [scaleType, setScaleType] = useState<TenantScale>('enterprise');
  const [tenantName, setTenantName] = useState<string>('Hệ thống Khảo thí');
  const [domains, setDomains] = useState<DynamicDomainOption[]>(cachedDomains || DEFAULT_FALLBACK_DOMAINS);
  const [isLoadingDomains, setIsLoadingDomains] = useState<boolean>(!cachedDomains);

  useEffect(() => {
    // 1. Phân tích thông tin tenant từ localStorage / Auth
    try {
      const userRaw = localStorage.getItem('user');
      if (userRaw) {
        const user = JSON.parse(userRaw);
        if (user.tenantScale) {
          setScaleType(user.tenantScale.toLowerCase() as TenantScale);
        } else if (user.scaleType) {
          setScaleType(user.scaleType.toLowerCase() as TenantScale);
        }
        if (user.tenantName) {
          setTenantName(user.tenantName);
        }
      }
    } catch {
      // Fallback enterprise default
    }

    // 2. Fetch danh mục ngành động
    if (cachedDomains) {
      setDomains(cachedDomains);
      setIsLoadingDomains(false);
      return;
    }

    const listener = (newDomains: DynamicDomainOption[]) => {
      setDomains(newDomains);
      setIsLoadingDomains(false);
    };
    listeners.push(listener);

    if (!isFetchingDomains) {
      isFetchingDomains = true;
      learnerQuizService.getDomains()
        .then((data) => {
          if (Array.isArray(data) && data.length > 0) {
            cachedDomains = data;
          } else {
            cachedDomains = DEFAULT_FALLBACK_DOMAINS;
          }
          listeners.forEach((fn) => fn(cachedDomains!));
        })
        .catch(() => {
          cachedDomains = DEFAULT_FALLBACK_DOMAINS;
          listeners.forEach((fn) => fn(cachedDomains!));
        })
        .finally(() => {
          isFetchingDomains = false;
        });
    }

    return () => {
      const idx = listeners.indexOf(listener);
      if (idx !== -1) listeners.splice(idx, 1);
    };
  }, []);

  const refreshDomains = useCallback(async () => {
    setIsLoadingDomains(true);
    try {
      const data = await learnerQuizService.getDomains();
      if (Array.isArray(data) && data.length > 0) {
        cachedDomains = data;
        setDomains(data);
      }
    } catch {
      // Keep existing
    } finally {
      setIsLoadingDomains(false);
    }
  }, []);

  // Tiệm tiến độ phức tạp (Progressive Disclosure)
  const isSimpleMode = useMemo(() => scaleType === 'individual' || scaleType === 'team', [scaleType]);
  const isEducationMode = useMemo(() => scaleType === 'classroom' || scaleType === 'organization', [scaleType]);
  const isEnterpriseMode = useMemo(() => scaleType === 'enterprise' || scaleType === 'global', [scaleType]);

  return {
    scaleType,
    tenantName,
    domains,
    isLoadingDomains,
    refreshDomains,
    isSimpleMode,
    isEducationMode,
    isEnterpriseMode,
  };
}
