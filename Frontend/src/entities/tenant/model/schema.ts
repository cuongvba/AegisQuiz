/**
 * AegisQuiz — Tenant Entity Model + Zod Schemas
 * ===============================================
 * SSOT cho Tenant domain model với runtime validation.
 * FSD Layer: entities/tenant/model/
 */

import { z } from 'zod';

// ── Tenant Scale ──────────────────────────────────────────────────────────────

export const TenantScaleSchema = z.enum([
  'individual',
  'team',
  'classroom',
  'organization',
  'enterprise',
  'global',
]);

export type TenantScale = z.infer<typeof TenantScaleSchema>;

// ── Domain Option Schema ──────────────────────────────────────────────────────

export const DomainOptionSchema = z.object({
  code:             z.string().min(1),
  name:             z.string().min(1),
  description:      z.string().optional(),
  icon:             z.string().optional(),
  colorBadge:       z.string().optional(),
  isSystemStandard: z.boolean().optional(),
  parentDomainCode: z.string().optional(),
});

export type DomainOption = z.infer<typeof DomainOptionSchema>;

// ── Theme Config Schema ───────────────────────────────────────────────────────

export const TenantThemeSchema = z.object({
  brandPrimary:      z.string().default('#06b6d4'),
  brandPrimaryDark:  z.string().default('#0891b2'),
  brandPrimaryLight: z.string().default('#cffafe'),
  brandSecondary:    z.string().default('#3b82f6'),
  brandAccent:       z.string().default('#f59e0b'),
  fontHeading:       z.string().default("'Inter', system-ui, sans-serif"),
  fontBody:          z.string().default("'Inter', system-ui, sans-serif"),
  radiusButton:      z.string().default('9999px'),
  radiusCard:        z.string().default('1rem'),
  radiusInput:       z.string().default('0.75rem'),
  tenantId:          z.string().default('default'),
  locale:            z.string().default('vi'),
  isRTL:             z.boolean().default(false),
});

export type TenantThemeConfig = z.infer<typeof TenantThemeSchema>;

// ── Tenant Config Schema ──────────────────────────────────────────────────────

export const TenantConfigSchema = z.object({
  tenantId:   z.string().uuid(),
  tenantName: z.string().min(1),
  scaleType:  TenantScaleSchema.default('enterprise'),
  theme:      TenantThemeSchema,
  domains:    z.array(DomainOptionSchema).default([]),
  logoUrl:    z.string().url().optional(),
  locale:     z.string().default('vi'),
  isRTL:      z.boolean().default(false),
});

export type TenantConfig = z.infer<typeof TenantConfigSchema>;

// ── Safe parse ────────────────────────────────────────────────────────────────

export function parseTenantConfigSafe(raw: unknown): TenantConfig | null {
  const result = TenantConfigSchema.safeParse(raw);
  if (!result.success && import.meta.env.DEV) {
    console.warn('[TenantConfig] Validation failed:', result.error.flatten());
  }
  return result.success ? result.data : null;
}
