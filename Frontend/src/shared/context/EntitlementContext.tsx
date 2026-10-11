import React, { createContext, useContext, useEffect, useState, useCallback, useMemo } from 'react';
import type { 
  EntitlementsResponse, 
  FeatureKeyType, 
  TenantPlan, 
  TenantScaleType,
  TenantQuotaLimits 
} from '@/types/entitlement';
import { entitlementService } from '@/services/entitlementService';

interface EntitlementContextType {
  entitlements: EntitlementsResponse | null;
  isLoading: boolean;
  error: string | null;
  plan: TenantPlan;
  scaleType: TenantScaleType;
  quotas: TenantQuotaLimits | null;
  
  // Ma trận 4 tầng
  hasFeature: (feature: FeatureKeyType) => boolean;
  canUser: (capability: string) => boolean;
  isDomainActive: (domainCode: string) => boolean;
  
  // Paywall & Monetization Trigger
  activePaywallFeature: FeatureKeyType | null;
  openPaywall: (feature: FeatureKeyType) => void;
  closePaywall: () => void;
  
  refresh: () => Promise<void>;
}

const DEFAULT_QUOTAS: TenantQuotaLimits = {
  maxUsers: 10,
  maxQuestionsPerMonth: 200,
  maxConcurrentArenaSeats: 0,
  aiTokensMonthly: 0,
  storageLimitMb: 500,
};

const EntitlementContext = createContext<EntitlementContextType | null>(null);

export const EntitlementProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [entitlements, setEntitlements] = useState<EntitlementsResponse | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [activePaywallFeature, setActivePaywallFeature] = useState<FeatureKeyType | null>(null);

  const fetchEntitlements = useCallback(async () => {
    try {
      setIsLoading(true);
      setError(null);
      const data = await entitlementService.getCurrentEntitlements();
      setEntitlements(data);
    } catch (err: any) {
      console.warn('[EntitlementProvider] Không thể lấy entitlements, dùng fallback Community mode:', err);
      setError(err?.message || 'Failed to fetch entitlements');
      // Fallback Community / Starter
      setEntitlements({
        tenant: {
          id: '00000000-0000-0000-0000-000000000001',
          code: 'community',
          name: 'Aegis Community Platform',
          plan: 'Starter',
          scaleType: 'Individual',
          isCommunity: true,
        },
        features: ['transpiler:moodle', 'math:vector_wmf'],
        quotas: DEFAULT_QUOTAS,
        domains: ['general', 'education'],
        user: {
          role: 'Learner',
          capabilities: ['questions:read', 'practice:access'],
        },
      });
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchEntitlements();
  }, [fetchEntitlements]);

  const hasFeature = useCallback((feature: FeatureKeyType): boolean => {
    if (!entitlements) return false;
    return entitlements.features.includes(feature);
  }, [entitlements]);

  const canUser = useCallback((capability: string): boolean => {
    if (!entitlements) return false;
    return entitlements.user.capabilities.includes(capability);
  }, [entitlements]);

  const isDomainActive = useCallback((domainCode: string): boolean => {
    if (!entitlements) return true;
    return entitlements.domains.includes(domainCode);
  }, [entitlements]);

  const openPaywall = useCallback((feature: FeatureKeyType) => {
    setActivePaywallFeature(feature);
  }, []);

  const closePaywall = useCallback(() => {
    setActivePaywallFeature(null);
  }, []);

  const plan = entitlements?.tenant.plan || 'Starter';
  const scaleType = entitlements?.tenant.scaleType || 'Individual';
  const quotas = entitlements?.quotas || DEFAULT_QUOTAS;

  const value = useMemo(() => ({
    entitlements,
    isLoading,
    error,
    plan,
    scaleType,
    quotas,
    hasFeature,
    canUser,
    isDomainActive,
    activePaywallFeature,
    openPaywall,
    closePaywall,
    refresh: fetchEntitlements,
  }), [
    entitlements,
    isLoading,
    error,
    plan,
    scaleType,
    quotas,
    hasFeature,
    canUser,
    isDomainActive,
    activePaywallFeature,
    openPaywall,
    closePaywall,
    fetchEntitlements,
  ]);

  return (
    <EntitlementContext.Provider value={value}>
      {children}
    </EntitlementContext.Provider>
  );
};

export function useEntitlementContext(): EntitlementContextType {
  const ctx = useContext(EntitlementContext);
  if (!ctx) {
    throw new Error('useEntitlementContext must be used within an EntitlementProvider');
  }
  return ctx;
}
