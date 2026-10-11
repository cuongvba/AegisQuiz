import { useEntitlementContext } from '@/shared/context/EntitlementContext';
import { 
  FEATURE_METADATA_CATALOG, 
  type FeatureKeyType, 
  type FeatureMetadata 
} from '@/types/entitlement';

export function useEntitlement() {
  const context = useEntitlementContext();

  const isFeatureLocked = (feature: FeatureKeyType): boolean => {
    return !context.hasFeature(feature);
  };

  const getFeatureMetadata = (feature: FeatureKeyType): FeatureMetadata => {
    return FEATURE_METADATA_CATALOG[feature] ?? {
      key: feature,
      name: feature,
      description: 'Tính năng chuyên biệt mở rộng của nền tảng.',
      requiredPlan: 'Professional',
      badge: 'PRO',
    };
  };

  const isStarter = context.plan === 'Starter';
  const isProfessional = context.plan === 'Professional';
  const isEnterprise = context.plan === 'Enterprise' || context.plan === 'Global';

  return {
    ...context,
    isFeatureLocked,
    getFeatureMetadata,
    isStarter,
    isProfessional,
    isEnterprise,
  };
}
