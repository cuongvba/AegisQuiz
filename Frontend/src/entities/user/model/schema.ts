/**
 * AegisQuiz — User Entity Model + Zod Schemas
 * =============================================
 * SSOT cho User domain model.
 * Migrate từ: src/types/auth.ts (vẫn giữ file cũ để backward compat)
 *
 * FSD Layer: entities/user/model/
 */

import { z } from 'zod';

// ── Zod Schemas ─────────────────────────────────────────────────────────────

export const AppRoleSchema = z.enum([
  'SystemAdmin',
  'TenantAdmin',
  'ContentManager',
  'Instructor',
  'OrgUnitManager',
  'TeamLeader',
  'Learner',
  'GuestViewer',
]);

export type AppRoleType = z.infer<typeof AppRoleSchema>;

export const UserProfileSchema = z.object({
  id:          z.string().min(1),
  name:        z.string().optional(),
  email:       z.string().email().optional(),
  role:        AppRoleSchema.default('GuestViewer'),
  isPremium:   z.boolean().default(false),
  tenantId:    z.string().uuid().optional(),
  orgUnitId:   z.string().uuid().optional(),
  orgUnitName: z.string().optional(),
  ou:          z.string().optional(),
});

export type UserProfile = z.infer<typeof UserProfileSchema>;

// ── Permission Schema ────────────────────────────────────────────────────────

export const PermissionSchema = z.enum([
  'questions:write',
  'questions:read',
  'exams:create',
  'exams:assign',
  'team:view_members',
  'team:view_progress',
  'team:manage_members',
  'users:manage',
  'reports:team',
  'practice:access',
  'admin:access',
]);

export type PermissionType = z.infer<typeof PermissionSchema>;

// ── Safe parse helper ────────────────────────────────────────────────────────

/**
 * Parse user JSON từ localStorage an toàn với Zod validation.
 * Trả về null nếu data không hợp lệ thay vì throw.
 */
export function parseUserSafe(raw: unknown): UserProfile | null {
  // Normalize legacy role aliases
  if (raw && typeof raw === 'object') {
    const obj = raw as Record<string, unknown>;
    if (obj['role'] === 'admin')   obj['role'] = 'TenantAdmin';
    if (obj['role'] === 'student') obj['role'] = 'Learner';
    if (obj['role'] === 'viewer')  obj['role'] = 'GuestViewer';
  }

  const result = UserProfileSchema.safeParse(raw);
  return result.success ? result.data : null;
}
