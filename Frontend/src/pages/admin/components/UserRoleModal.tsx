import React, { useState, useEffect } from 'react';
import {
  X, Shield, Building2, Key, CheckCircle2, AlertTriangle,
  Lock, Unlock, Sparkles, RefreshCw, Check, Layers, UserCheck,
  Copy, ExternalLink, Link2, Send
} from 'lucide-react';
import api from '@/services/api';
import { AppRole } from '@/types/auth';

export interface AdminUserItem {
  id: string;
  email: string;
  fullName: string;
  phoneNumber?: string;
  avatarUrl?: string;
  role: string;
  orgUnitId?: string | null;
  orgUnitName?: string | null;
  orgUnitHierarchyPath?: string | null;
  isPremium: boolean;
  subscriptionTier: string;
  subscriptionExpiresAt?: string | null;
  isActive: boolean;
  failedLoginAttempts: number;
  lastLoginAt?: string | null;
  createdAt: string;
}

export interface OrgUnitFlatItem {
  id: string;
  code: string;
  name: string;
  unitType: string;
  hierarchyPath: string;
}

interface UserRoleModalProps {
  user: AdminUserItem | null;
  orgUnits: OrgUnitFlatItem[];
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
}

// Map permissions according to AppRole
const ROLE_PERMISSIONS: Record<string, string[]> = {
  [AppRole.SystemAdmin]: [
    'Quản trị chéo toàn bộ hệ sinh thái (Cross-Tenant)',
    'Quản lý tất cả tài khoản & phân quyền cấp cao',
    'Cấu hình nền tảng, thanh toán & chứng thư PKI'
  ],
  [AppRole.TenantAdmin]: [
    'Toàn quyền quản trị nhân sự & đơn vị trong tổ chức',
    'Quản trị ngân hàng câu hỏi, đề thi và kỳ thi tập trung',
    'Phân bổ gói dịch vụ VIP/Enterprise và xuất báo cáo tổng hợp'
  ],
  [AppRole.OrgUnitManager]: [
    'Quản lý nhân sự & phân công thi cử trong khối/chi nhánh',
    'Xem báo cáo tiến độ và phổ điểm của đơn vị trực thuộc',
    'Chỉ định người giám thị & trưởng nhóm khảo thí'
  ],
  [AppRole.TeamLeader]: [
    'Giám sát tiến độ làm bài của học viên trong nhóm/phòng ban',
    'Gán đề thi và phát hành mã phòng thi cho thành viên',
    'Theo dõi radar kỹ năng và phân tích tỷ lệ trả lời đúng của nhóm'
  ],
  [AppRole.Instructor]: [
    'Soạn thảo và duyệt ngân hàng câu hỏi môn học',
    'Tạo bài kiểm tra và thiết lập ma trận đề thi',
    'Giám sát phòng thi thời gian thực'
  ],
  [AppRole.Learner]: [
    'Luyện tập trắc nghiệm và thi theo lịch khảo thí',
    'Làm bài thi tương thích thích ứng AI Adaptive (nếu là VIP)',
    'Xem bảng thành tích, radar điểm và lịch sử bài thi'
  ],
  [AppRole.GuestViewer]: [
    'Quyền chỉ đọc (Read-only)',
    'Xem các bài thi hoặc tài liệu công khai'
  ]
};

export function UserRoleModal({ user, orgUnits, isOpen, onClose, onSuccess }: UserRoleModalProps) {
  if (!isOpen || !user) return null;

  const [role, setRole] = useState(user.role || AppRole.Learner);
  const [orgUnitId, setOrgUnitId] = useState(user.orgUnitId || '');
  const [subscriptionTier, setSubscriptionTier] = useState(user.subscriptionTier || 'FREE');
  const [isActive, setIsActive] = useState(user.isActive ?? true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Reset password state
  const [isResettingPassword, setIsResettingPassword] = useState(false);
  const [generatedPassword, setGeneratedPassword] = useState<string | null>(null);

  // Activation / Reset Link state
  const [isSendingLink, setIsSendingLink] = useState(false);
  const [activationLink, setActivationLink] = useState<string | null>(null);
  const [copiedLink, setCopiedLink] = useState(false);
  const [linkSuccessMsg, setLinkSuccessMsg] = useState<string | null>(null);

  useEffect(() => {
    if (user) {
      setRole(user.role || AppRole.Learner);
      setOrgUnitId(user.orgUnitId || '');
      setSubscriptionTier(user.subscriptionTier || 'FREE');
      setIsActive(user.isActive ?? true);
      setErrorMessage(null);
      setGeneratedPassword(null);
      setActivationLink(null);
      setLinkSuccessMsg(null);
      setCopiedLink(false);
    }
  }, [user]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);

    // Rule: TeamLeader must have OrgUnitId
    if (role === AppRole.TeamLeader && !orgUnitId) {
      setErrorMessage('Vai trò Trưởng nhóm (TeamLeader) bắt buộc phải gắn với một Đơn vị phòng ban (OrgUnit).');
      return;
    }

    setIsSubmitting(true);
    try {
      await api.put(`/api/admin/users/${user.id}`, {
        role,
        orgUnitId: orgUnitId || null,
        subscriptionTier,
        isActive
      });

      onSuccess();
      onClose();
    } catch (err: any) {
      const msg = err.response?.data?.message || 'Không thể cập nhật phân quyền người dùng.';
      setErrorMessage(msg);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleResetPassword = async () => {
    if (!window.confirm(`Bạn có chắc chắn muốn đặt lại mật khẩu cho tài khoản ${user.email}?`)) {
      return;
    }

    setIsResettingPassword(true);
    setErrorMessage(null);
    setActivationLink(null);
    try {
      const res = await api.post(`/api/admin/users/${user.id}/reset-password`, {});
      if (res.data?.newPassword) {
        setGeneratedPassword(res.data.newPassword);
      }
    } catch (err: any) {
      const msg = err.response?.data?.message || 'Không thể đặt lại mật khẩu.';
      setErrorMessage(msg);
    } finally {
      setIsResettingPassword(false);
    }
  };

  const handleSendActivationLink = async () => {
    setIsSendingLink(true);
    setErrorMessage(null);
    setGeneratedPassword(null);
    setLinkSuccessMsg(null);
    try {
      const res = await api.post(`/api/admin/users/${user.id}/send-activation-link`, {});
      if (res.data?.activationLink) {
        setActivationLink(res.data.activationLink);
        setLinkSuccessMsg(res.data.message || 'Đã tạo đường link kích hoạt an toàn có hiệu lực 24 giờ.');
      }
    } catch (err: any) {
      const msg = err.response?.data?.message || 'Không thể tạo đường link kích hoạt.';
      setErrorMessage(msg);
    } finally {
      setIsSendingLink(false);
    }
  };

  const copyToClipboard = (text: string) => {
    navigator.clipboard.writeText(text);
    setCopiedLink(true);
    setTimeout(() => setCopiedLink(false), 2500);
  };

  const permissions = ROLE_PERMISSIONS[role] || ROLE_PERMISSIONS[AppRole.Learner];

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-md overflow-y-auto">
      <div className="relative w-full max-w-2xl bg-slate-900 border border-slate-800 rounded-3xl p-6 sm:p-8 shadow-2xl space-y-6 text-slate-100 my-8">

        {/* Header */}
        <div className="flex items-start justify-between pb-4 border-b border-slate-800">
          <div>
            <div className="flex items-center gap-2">
              <Shield size={22} className="text-blue-400" />
              <h2 className="text-xl font-black text-white">Quản Trị Phân Quyền Scoped RBAC</h2>
            </div>
            <p className="text-xs text-slate-400 mt-1">
              Phân quyền vai trò và ủy nhiệm phạm vi quản lý theo cây tổ chức đa tầng.
            </p>
          </div>

          <button
            onClick={onClose}
            className="p-2 rounded-xl text-slate-400 hover:text-white hover:bg-slate-800 transition-colors"
          >
            <X size={20} />
          </button>
        </div>

        {/* User Summary Bar */}
        <div className="p-4 rounded-2xl bg-slate-950/80 border border-slate-800 flex items-center gap-4">
          <div className="w-12 h-12 rounded-xl bg-gradient-to-tr from-blue-600 to-purple-600 flex items-center justify-center font-black text-white text-lg uppercase shadow-md shrink-0">
            {(user.fullName || user.email).charAt(0)}
          </div>
          <div className="min-w-0 flex-1">
            <div className="font-bold text-white text-base truncate">{user.fullName}</div>
            <div className="text-xs text-slate-400 font-mono truncate">{user.email}</div>
          </div>
          <div className="text-right shrink-0">
            <span className={`px-2.5 py-1 rounded-lg text-[11px] font-black uppercase tracking-wider ${
              isActive
                ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/20'
                : 'bg-rose-500/10 text-rose-400 border border-rose-500/20'
            }`}>
              {isActive ? 'Đang hoạt động' : 'Tạm khóa'}
            </span>
          </div>
        </div>

        {errorMessage && (
          <div className="p-4 rounded-xl bg-rose-500/10 border border-rose-500/20 text-rose-400 text-xs font-semibold flex items-center gap-2">
            <AlertTriangle size={16} className="shrink-0" />
            <span>{errorMessage}</span>
          </div>
        )}

        {generatedPassword && (
          <div className="p-4 rounded-2xl bg-emerald-500/10 border border-emerald-500/30 text-emerald-300 space-y-2">
            <div className="flex items-center gap-2 font-bold text-sm">
              <CheckCircle2 size={18} className="text-emerald-400" />
              Mật khẩu mới đã được đặt lại thành công!
            </div>
            <p className="text-xs text-emerald-400/80">Vui lòng sao chép và gửi mật khẩu bảo mật này cho người dùng:</p>
            <div className="p-3 bg-slate-950 rounded-xl border border-emerald-500/30 font-mono text-base font-black text-emerald-400 select-all tracking-wider">
              {generatedPassword}
            </div>
          </div>
        )}

        {activationLink && (
          <div className="p-4 rounded-2xl bg-blue-500/10 border border-blue-500/30 text-blue-300 space-y-3 animate-in fade-in duration-200">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2 font-bold text-sm text-blue-200">
                <Link2 size={18} className="text-blue-400" />
                Link Kích Hoạt / Đổi Mật Khẩu (Hiệu lực 24 giờ)
              </div>
              <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-blue-500/20 text-blue-300 border border-blue-500/30">
                One-Time Token
              </span>
            </div>
            {linkSuccessMsg && <p className="text-xs text-slate-300">{linkSuccessMsg}</p>}
            <div className="flex items-center gap-2">
              <input
                type="text"
                readOnly
                value={activationLink}
                className="w-full bg-slate-950 px-3 py-2 rounded-xl border border-blue-500/30 font-mono text-xs text-blue-300 select-all focus:outline-none"
              />
              <button
                type="button"
                onClick={() => copyToClipboard(activationLink)}
                className="px-3.5 py-2 rounded-xl bg-blue-600 hover:bg-blue-500 text-white text-xs font-bold flex items-center gap-1.5 shrink-0 transition-colors shadow cursor-pointer"
              >
                {copiedLink ? <Check size={14} /> : <Copy size={14} />}
                {copiedLink ? 'Đã chép' : 'Sao chép'}
              </button>
            </div>
            <p className="text-[11px] text-slate-400">
              Gửi liên kết này cho cán bộ/học viên để họ tự thiết lập mật khẩu an toàn và kích hoạt tài khoản ngay lập tức.
            </p>
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-6">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-5">
            {/* Role selection */}
            <div>
              <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-2">
                Vai trò người dùng (AppRole)
              </label>
              <select
                value={role}
                onChange={(e) => setRole(e.target.value)}
                className="w-full bg-slate-950 border border-slate-800 focus:border-blue-500 rounded-xl px-4 py-3 text-white text-sm focus:outline-none transition-all cursor-pointer font-medium"
              >
                <option value={AppRole.Learner}>Học viên / Thí sinh (Learner)</option>
                <option value={AppRole.TeamLeader}>Trưởng nhóm khảo thí (TeamLeader)</option>
                <option value={AppRole.Instructor}>Giảng viên / Ra đề (Instructor)</option>
                <option value={AppRole.OrgUnitManager}>Quản lý đơn vị (OrgUnitManager)</option>
                <option value={AppRole.TenantAdmin}>Quản trị tổ chức (TenantAdmin)</option>
                <option value={AppRole.SystemAdmin}>Quản trị hệ thống (SystemAdmin)</option>
                <option value={AppRole.GuestViewer}>Khách xem (GuestViewer)</option>
              </select>
            </div>

            {/* OrgUnit selection */}
            <div>
              <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-2 flex items-center justify-between">
                <span>Đơn vị tổ chức (OrgUnit)</span>
                {role === AppRole.TeamLeader && (
                  <span className="text-rose-400 text-[10px] font-black uppercase tracking-normal">* Bắt buộc</span>
                )}
              </label>
              <select
                value={orgUnitId}
                onChange={(e) => setOrgUnitId(e.target.value)}
                className={`w-full bg-slate-950 border rounded-xl px-4 py-3 text-white text-sm focus:outline-none transition-all cursor-pointer font-medium ${
                  role === AppRole.TeamLeader && !orgUnitId
                    ? 'border-rose-500/60 focus:border-rose-500'
                    : 'border-slate-800 focus:border-blue-500'
                }`}
              >
                <option value="">-- Toàn tổ chức / Chưa gán đơn vị --</option>
                {orgUnits.map((ou) => (
                  <option key={ou.id} value={ou.id}>
                    {ou.hierarchyPath || `/${ou.code}`} — {ou.name}
                  </option>
                ))}
              </select>
            </div>

            {/* Subscription Tier */}
            <div>
              <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-2">
                Gói Thuê Bao (Subscription Tier)
              </label>
              <select
                value={subscriptionTier}
                onChange={(e) => setSubscriptionTier(e.target.value)}
                className="w-full bg-slate-950 border border-slate-800 focus:border-blue-500 rounded-xl px-4 py-3 text-white text-sm focus:outline-none transition-all cursor-pointer font-medium"
              >
                <option value="FREE">Tiêu Chuẩn (FREE)</option>
                <option value="VIP">VIP (AI Adaptive Enabled)</option>
                <option value="ENTERPRISE">Doanh Nghiệp (ENTERPRISE)</option>
              </select>
            </div>

            {/* Active Status */}
            <div>
              <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-2">
                Trạng Thái Tài Khoản
              </label>
              <button
                type="button"
                onClick={() => setIsActive(!isActive)}
                className={`w-full py-3 px-4 rounded-xl border flex items-center justify-between transition-all font-bold text-sm cursor-pointer ${
                  isActive
                    ? 'bg-emerald-500/10 border-emerald-500/30 text-emerald-300'
                    : 'bg-rose-500/10 border-rose-500/30 text-rose-300'
                }`}
              >
                <span className="flex items-center gap-2">
                  {isActive ? <Unlock size={16} /> : <Lock size={16} />}
                  {isActive ? 'Đang Kích Hoạt (Active)' : 'Đang Bị Tạm Khóa (Locked)'}
                </span>
                <span className="text-xs opacity-75 underline">Nhấp để đổi</span>
              </button>
            </div>
          </div>

          {/* Permissions Matrix Preview */}
          <div className="p-4 rounded-2xl bg-slate-950/60 border border-slate-800/80 space-y-2.5">
            <h4 className="text-xs font-bold text-slate-400 uppercase tracking-wider flex items-center gap-1.5">
              <UserCheck size={14} className="text-blue-400" />
              Xem trước đặc quyền vai trò ({role})
            </h4>
            <div className="space-y-1.5">
              {permissions.map((perm, idx) => (
                <div key={idx} className="flex items-center gap-2 text-xs text-slate-300">
                  <Check size={14} className="text-emerald-400 shrink-0" />
                  <span>{perm}</span>
                </div>
              ))}
            </div>
          </div>

          {/* Action buttons */}
          <div className="flex flex-col-reverse sm:flex-row items-center justify-between gap-4 pt-4 border-t border-slate-800">
            <div className="flex flex-wrap items-center gap-2 w-full sm:w-auto">
              <button
                type="button"
                onClick={handleSendActivationLink}
                disabled={isSendingLink}
                className="px-3.5 py-2.5 rounded-xl bg-blue-950/60 hover:bg-blue-900/60 border border-blue-500/30 text-blue-300 hover:text-white text-xs font-bold flex items-center justify-center gap-2 transition-all cursor-pointer disabled:opacity-50"
                title="Tạo đường link kích hoạt 24h để gửi cho nhân sự mới hoặc khi cần cấp lại"
              >
                {isSendingLink ? <RefreshCw size={14} className="animate-spin" /> : <Send size={14} />}
                {isSendingLink ? 'Đang tạo link...' : 'Gửi Link Kích Hoạt (24h)'}
              </button>

              <button
                type="button"
                onClick={handleResetPassword}
                disabled={isResettingPassword}
                className="px-3.5 py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 hover:text-white text-xs font-bold flex items-center justify-center gap-2 transition-all cursor-pointer disabled:opacity-50"
                title="Đặt mật khẩu ngẫu nhiên mới trực tiếp ngay trên màn hình"
              >
                {isResettingPassword ? <RefreshCw size={14} className="animate-spin" /> : <Key size={14} />}
                {isResettingPassword ? 'Đang reset...' : 'Reset trực tiếp'}
              </button>
            </div>

            <div className="flex items-center gap-3 w-full sm:w-auto justify-end">
              <button
                type="button"
                onClick={onClose}
                className="px-5 py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 font-bold text-xs transition-all cursor-pointer"
              >
                Hủy
              </button>

              <button
                type="submit"
                disabled={isSubmitting}
                className="px-6 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-500 text-white font-bold text-xs flex items-center justify-center gap-2 shadow-lg shadow-blue-500/25 transition-all cursor-pointer disabled:opacity-50"
              >
                {isSubmitting ? <RefreshCw size={14} className="animate-spin" /> : <CheckCircle2 size={14} />}
                {isSubmitting ? 'Đang lưu...' : 'Lưu Phân Quyền'}
              </button>
            </div>
          </div>
        </form>

      </div>
    </div>
  );
}
