/**
 * AegisQuiz — usePermission Hook
 * ================================
 * `can(action, subject)` permission interface.
 * Wrap useAuthContext để cung cấp declarative permission checks.
 *
 * FSD Layer: shared/hooks/
 *
 * Cách dùng:
 *   const { can, cannot } = usePermission();
 *   if (can('write', 'questions')) { ... }
 *   <Show when={can('access', 'admin')}> ... </Show>
 */

import { useAuthContext }   from '@/app/providers/AuthProvider';
import { hasPermission }    from '@/types/auth';
import type { PermissionType } from '@/types/auth';

type Action  = 'read' | 'write' | 'create' | 'assign' | 'manage' | 'access' | 'view';
type Subject =
  | 'questions'
  | 'exams'
  | 'team'
  | 'users'
  | 'reports'
  | 'practice'
  | 'admin';

const ACTION_SUBJECT_MAP: Partial<Record<`${Action}:${Subject}`, PermissionType>> = {
  'read:questions':   'questions:read',
  'write:questions':  'questions:write',
  'create:exams':     'exams:create',
  'assign:exams':     'exams:assign',
  'view:team':        'team:view_members',
  'view:reports':     'team:view_progress',
  'manage:team':      'team:manage_members',
  'manage:users':     'users:manage',
  'read:reports':     'reports:team',
  'access:practice':  'practice:access',
  'access:admin':     'admin:access',
  // Unmapped — fallback to false
  'create:questions': 'questions:write',
  'read:exams':       'questions:read',
  'write:exams':      'exams:create',
  'manage:exams':     'exams:create',
  'create:team':      'team:manage_members',
  'read:team':        'team:view_members',
  'write:team':       'team:manage_members',
  'assign:team':      'team:manage_members',
  'create:users':     'users:manage',
  'read:users':       'users:manage',
  'write:users':      'users:manage',
  'assign:users':     'users:manage',
  'assign:reports':   'reports:team',
  'create:reports':   'reports:team',
  'write:reports':    'reports:team',
  'manage:reports':   'reports:team',
  'read:practice':    'practice:access',
  'write:practice':   'practice:access',
  'create:practice':  'practice:access',
  'assign:practice':  'practice:access',
  'manage:practice':  'practice:access',
  'read:admin':       'admin:access',
  'write:admin':      'admin:access',
  'create:admin':     'admin:access',
  'assign:admin':     'admin:access',
  'manage:admin':     'admin:access',
  'view:questions':   'questions:read',
  'view:exams':       'exams:create',
  'view:users':       'users:manage',
  'view:practice':    'practice:access',
  'view:admin':       'admin:access',
};

export interface UsePermissionReturn {
  /** Kiểm tra permission theo (action, subject) pattern */
  can:    (action: Action, subject: Subject) => boolean;
  /** Nghịch đảo của can() */
  cannot: (action: Action, subject: Subject) => boolean;
  /** Raw permission check */
  hasPermission: (permission: PermissionType) => boolean;
}

export function usePermission(): UsePermissionReturn {
  const { role } = useAuthContext();

  const can = (action: Action, subject: Subject): boolean => {
    const key = `${action}:${subject}` as keyof typeof ACTION_SUBJECT_MAP;
    const permission = ACTION_SUBJECT_MAP[key];
    if (!permission) return false;
    return hasPermission(role, permission);
  };

  return {
    can,
    cannot: (action, subject) => !can(action, subject),
    hasPermission: (perm) => hasPermission(role, perm),
  };
}

/**
 * Show component — render children chỉ khi có permission.
 * Dùng như conditional rendering wrapper.
 */
export function Show({
  when,
  fallback = null,
  children,
}: {
  when:      boolean;
  fallback?: React.ReactNode;
  children:  React.ReactNode;
}): React.ReactElement | null {
  return when ? (children as React.ReactElement) : (fallback as React.ReactElement | null);
}
