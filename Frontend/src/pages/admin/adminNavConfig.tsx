import React from 'react';
import {
  LayoutDashboard,
  FileQuestion,
  BookOpen,
  Users,
  CreditCard,
  FileText,
  Atom,
  Brain,
  Zap,
  Network,
  Building2,
  Palette
} from 'lucide-react';
import { FeatureKey, type FeatureKeyType } from '@/types/entitlement';

export interface AdminNavDef {
  href: string;
  i18nKey: string;
  defaultLabel: string;
  icon: React.ReactNode;
  adminOnly?: boolean;
  feature?: FeatureKeyType;
}

export const ADMIN_NAV_ITEMS: AdminNavDef[] = [
  { href: '/admin', i18nKey: 'nav.overview', defaultLabel: 'Tổng quan', icon: <LayoutDashboard size={18} /> },
  { href: '/admin/questions', i18nKey: 'nav.questionBank', defaultLabel: 'Ngân hàng Câu hỏi', icon: <FileQuestion size={18} /> },
  { href: '/admin/exams', i18nKey: 'nav.exams', defaultLabel: 'Đề thi', icon: <FileText size={18} /> },
  { href: '/admin/tenants', i18nKey: 'nav.tenants', defaultLabel: 'Khách thuê & Nhãn trắng', icon: <Building2 size={18} />, adminOnly: true },
  { href: '/admin/white-label', i18nKey: 'nav.whiteLabel', defaultLabel: 'Bản sắc Thương hiệu (Theming)', icon: <Palette size={18} />, adminOnly: true, feature: FeatureKey.WhiteLabelTheming },
  { href: '/admin/org-units', i18nKey: 'nav.orgUnits', defaultLabel: 'Cây Tổ chức O(1)', icon: <Network size={18} />, adminOnly: true, feature: FeatureKey.HierarchyTree },
  { href: '/admin/notebooks', i18nKey: 'nav.notebooks', defaultLabel: 'Giáo trình (PDF)', icon: <BookOpen size={18} />, adminOnly: true },
  { href: '/admin/symposium', i18nKey: 'nav.symposium', defaultLabel: 'Đại Hội Thảo AI', icon: <Atom size={18} />, feature: FeatureKey.AiSymposium },
  { href: '/admin/ai-settings', i18nKey: 'nav.aiSettings', defaultLabel: 'Bầy Sói AI & FinOps', icon: <Brain size={18} />, adminOnly: true, feature: FeatureKey.AiSymposium },
  { href: '/admin/prompt-forge', i18nKey: 'nav.promptForge', defaultLabel: 'Xưởng Prompt Tầng 0', icon: <Zap size={18} />, adminOnly: true, feature: FeatureKey.AiSymposium },
  { href: '/admin/users', i18nKey: 'nav.users', defaultLabel: 'Người dùng', icon: <Users size={18} />, adminOnly: true },
  { href: '/admin/payments', i18nKey: 'nav.payments', defaultLabel: 'Doanh thu', icon: <CreditCard size={18} />, adminOnly: true },
];
