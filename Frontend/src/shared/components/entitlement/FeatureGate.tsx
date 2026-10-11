import React from 'react';
import { useEntitlement } from '@/shared/hooks/useEntitlement';
import { FeatureBadge } from './FeatureBadge';
import type { FeatureKeyType } from '@/types/entitlement';

interface FeatureGateProps {
  feature: FeatureKeyType;
  children: React.ReactNode;
  fallback?: React.ReactNode;
  mode?: 'lock' | 'hidden' | 'custom';
  showBadge?: boolean;
  className?: string;
}

export const FeatureGate: React.FC<FeatureGateProps> = ({
  feature,
  children,
  fallback,
  mode = 'lock',
  showBadge = true,
  className = '',
}) => {
  const { hasFeature, openPaywall } = useEntitlement();
  const isAllowed = hasFeature(feature);

  if (isAllowed) {
    return <>{children}</>;
  }

  // 1. Chế độ Hidden: Hoàn toàn không render
  if (mode === 'hidden') {
    return null;
  }

  // 2. Chế độ Custom fallback
  if (mode === 'custom' && fallback) {
    return <>{fallback}</>;
  }

  // 3. Chế độ Lock (Product-Led Growth Teaser)
  return (
    <div
      onClick={(e) => {
        e.preventDefault();
        e.stopPropagation();
        openPaywall(feature);
      }}
      className={`relative inline-block cursor-pointer group select-none ${className}`}
      title="Bấm để xem thông tin nâng cấp gói"
    >
      <div className="opacity-60 grayscale-[40%] pointer-events-none transition group-hover:opacity-80">
        {children}
      </div>

      <div className="absolute -top-2 -right-2 z-10 flex items-center gap-1 shadow-sm transition-transform group-hover:scale-110">
        {showBadge && <FeatureBadge feature={feature} size="sm" />}
      </div>
    </div>
  );
};
