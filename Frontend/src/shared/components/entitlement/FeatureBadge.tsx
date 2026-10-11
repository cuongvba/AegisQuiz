import React from 'react';
import type { FeatureKeyType } from '@/types/entitlement';
import { FEATURE_METADATA_CATALOG } from '@/types/entitlement';

interface FeatureBadgeProps {
  feature?: FeatureKeyType;
  badge?: 'STARTER' | 'PRO' | 'ENTERPRISE' | 'GLOBAL';
  className?: string;
  size?: 'sm' | 'md';
}

export const FeatureBadge: React.FC<FeatureBadgeProps> = ({
  feature,
  badge: explicitBadge,
  className = '',
  size = 'sm',
}) => {
  const badgeType = explicitBadge || (feature ? FEATURE_METADATA_CATALOG[feature]?.badge : 'PRO') || 'PRO';

  if (badgeType === 'STARTER') return null;

  const sizeClasses = size === 'sm' ? 'px-1.5 py-0.5 text-[10px]' : 'px-2.5 py-1 text-xs';

  const badgeStyles = {
    PRO: 'bg-gradient-to-r from-amber-500 via-orange-500 to-amber-600 text-white shadow-amber-500/20 shadow-sm border border-amber-300/30',
    ENTERPRISE: 'bg-gradient-to-r from-indigo-600 via-purple-600 to-pink-600 text-white shadow-purple-500/25 shadow-sm border border-purple-300/30',
    GLOBAL: 'bg-gradient-to-r from-cyan-500 via-blue-600 to-indigo-700 text-white shadow-cyan-500/30 shadow-sm border border-cyan-300/30',
  }[badgeType as 'PRO' | 'ENTERPRISE' | 'GLOBAL'] || 'bg-amber-500 text-white';

  return (
    <span
      className={`inline-flex items-center gap-1 font-bold tracking-wider uppercase rounded-full select-none ${sizeClasses} ${badgeStyles} ${className}`}
    >
      <span className="w-1.5 h-1.5 rounded-full bg-white animate-pulse" />
      {badgeType}
    </span>
  );
};
