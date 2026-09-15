import React, { useState, useEffect, useCallback } from 'react';
import {
  Users, ShieldCheck, UserCheck, Shield, Sparkles,
  Building2, Layers, Search, Filter, RefreshCw, UserPlus,
  Lock, Unlock, ChevronLeft, ChevronRight, AlertCircle, X,
  CheckCircle2, Mail, Smartphone, Key, Download, Network
} from 'lucide-react';
import api from '@/services/api';
import { AppRole } from '@/types/auth';
import { UserRoleModal, type AdminUserItem, type OrgUnitFlatItem } from './components/UserRoleModal';

interface UserStats {
  totalUsers: number;
  activeUsers: number;
  lockedUsers: number;
  adminUsers: number;
  leaderUsers: number;
  vipUsers: number;
}

export function AdminUsersPage() {
  // Data state
  const [users, setUsers] = useState<AdminUserItem[]>([]);
  const [stats, setStats] = useState<UserStats | null>(null);
  const [orgUnits, setOrgUnits] = useState<OrgUnitFlatItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Filter & Pagination state
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedRole, setSelectedRole] = useState<string>('');
  const [selectedOrgUnit, setSelectedOrgUnit] = useState<string>('');
  const [selectedTier, setSelectedTier] = useState<string>('');
  const [selectedStatus, setSelectedStatus] = useState<string>('');
  const [page, setPage] = useState(1);
  const [pageSize] = useState(15);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);

  // Modals state
  const [selectedUserForRole, setSelectedUserForRole] = useState<AdminUserItem | null>(null);
  const [isRoleModalOpen, setIsRoleModalOpen] = useState(false);
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);

  // Create User Form state
  const [newEmail, setNewEmail] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [newFullName, setNewFullName] = useState('');
  const [newPhone, setNewPhone] = useState('');
  const [newRole, setNewRole] = useState<string>(AppRole.Learner);
  const [newOrgUnitId, setNewOrgUnitId] = useState('');
  const [newTier, setNewTier] = useState('FREE');
  const [isCreating, setIsCreating] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);

  // ── Load OrgUnits for Dropdown filter ─────────────────────────────────────
  const loadOrgUnits = useCallback(async () => {
    try {
      const res = await api.get('/api/quiz/org-units');
      if (Array.isArray(res.data)) {
        setOrgUnits(res.data);
      }
    } catch (err) {
      console.warn('Could not load org units list', err);
    }
  }, []);

  // ── Load Stats ──────────────────────────────────────────────────────────
  const loadStats = useCallback(async () => {
    try {
      const res = await api.get('/api/admin/users/stats');
      if (res.data) {
        setStats(res.data);
      }
    } catch (err) {
      console.warn('Could not load user stats', err);
    }
  }, []);

  // ── Load Users List ─────────────────────────────────────────────────────
  const loadUsers = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const params: Record<string, any> = {
        page,
        pageSize
      };

      if (searchTerm.trim()) params.search = searchTerm.trim();
      if (selectedRole) params.role = selectedRole;
      if (selectedOrgUnit) params.orgUnitId = selectedOrgUnit;
      if (selectedTier) params.tier = selectedTier;
      if (selectedStatus === 'active') params.isActive = true;
      if (selectedStatus === 'locked') params.isActive = false;

      const res = await api.get('/api/admin/users', { params });
      if (res.data) {
        setUsers(res.data.items || []);
        setTotalCount(res.data.totalCount || 0);
        setTotalPages(res.data.totalPages || 1);
      }
    } catch (err: any) {
      const msg = err.response?.data?.message || 'Không thể tải danh sách người dùng.';
      setError(msg);
    } finally {
      setLoading(false);
    }
  }, [page, pageSize, searchTerm, selectedRole, selectedOrgUnit, selectedTier, selectedStatus]);

  useEffect(() => {
    loadOrgUnits();
    loadStats();
  }, [loadOrgUnits, loadStats]);

  useEffect(() => {
    loadUsers();
  }, [loadUsers]);

  // Handle Search Input Debounce or Enter
  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setPage(1);
    loadUsers();
  };

  const handleResetFilters = () => {
    setSearchTerm('');
    setSelectedRole('');
    setSelectedOrgUnit('');
    setSelectedTier('');
    setSelectedStatus('');
    setPage(1);
  };

  const [isTreeModalOpen, setIsTreeModalOpen] = useState(false);

  // Open Role Modal
  const handleOpenRoleModal = (userItem: AdminUserItem) => {
    setSelectedUserForRole(userItem);
    setIsRoleModalOpen(true);
  };

  // Quick Toggle Active/Lock Status
  const handleQuickToggleStatus = async (userItem: AdminUserItem) => {
    const nextStatus = !userItem.isActive;
    const actionLabel = nextStatus ? 'mở khóa' : 'tạm khóa';
    if (!window.confirm(`Bạn có chắc muốn ${actionLabel} tài khoản ${userItem.email}?`)) {
      return;
    }
    try {
      await api.put(`/api/admin/users/${userItem.id}`, {
        isActive: nextStatus
      });
      loadUsers();
      loadStats();
    } catch (err: any) {
      alert(err.response?.data?.message || `Không thể ${actionLabel} tài khoản.`);
    }
  };

  // Export to CSV with UTF-8 BOM for Excel
  const handleExportCSV = () => {
    if (users.length === 0) {
      alert('Không có dữ liệu người dùng để xuất.');
      return;
    }

    const headers = [
      'Email',
      'Họ và Tên',
      'Số Điện Thoại',
      'Vai Trò',
      'Đơn Vị Phòng Ban',
      'Đường Dẫn Cây OU',
      'Gói Thuê Bao',
      'Trạng Thái',
      'Ngày Tham Gia',
      'Đăng Nhập Cuối'
    ];

    const rows = users.map((u) => [
      `"${u.email}"`,
      `"${u.fullName || ''}"`,
      `"${u.phoneNumber || ''}"`,
      `"${u.role}"`,
      `"${u.orgUnitName || 'Toàn tổ chức'}"`,
      `"${u.orgUnitHierarchyPath || ''}"`,
      `"${u.subscriptionTier || 'FREE'}"`,
      `"${u.isActive ? 'Hoạt động' : 'Bị khóa'}"`,
      `"${new Date(u.createdAt).toLocaleDateString('vi-VN')}"`,
      `"${u.lastLoginAt ? new Date(u.lastLoginAt).toLocaleString('vi-VN') : 'Chưa đăng nhập'}"`
    ]);

    const csvContent = '\uFEFF' + [headers.join(','), ...rows.map((r) => r.join(','))].join('\n');
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', `AegisQuiz_Users_${new Date().toISOString().slice(0, 10)}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  };

  // Create User Handler
  const handleCreateUser = async (e: React.FormEvent) => {
    e.preventDefault();
    setCreateError(null);

    if (newRole === AppRole.TeamLeader && !newOrgUnitId) {
      setCreateError('Vai trò Trưởng nhóm (TeamLeader) bắt buộc phải chọn Đơn vị phòng ban (OrgUnit).');
      return;
    }

    setIsCreating(true);
    try {
      await api.post('/api/admin/users', {
        email: newEmail.trim(),
        password: newPassword,
        fullName: newFullName.trim(),
        phoneNumber: newPhone.trim(),
        role: newRole,
        orgUnitId: newOrgUnitId || null,
        subscriptionTier: newTier
      });

      setIsCreateModalOpen(false);
      setNewEmail('');
      setNewPassword('');
      setNewFullName('');
      setNewPhone('');
      setNewRole(AppRole.Learner);
      setNewOrgUnitId('');
      setNewTier('FREE');

      loadUsers();
      loadStats();
    } catch (err: any) {
      const msg = err.response?.data?.message || 'Không thể tạo tài khoản người dùng.';
      setCreateError(msg);
    } finally {
      setIsCreating(false);
    }
  };

  const getRoleBadgeStyle = (r: string) => {
    switch (r) {
      case AppRole.SystemAdmin:
      case AppRole.TenantAdmin:
        return 'bg-rose-500/10 text-rose-400 border-rose-500/20';
      case AppRole.TeamLeader:
        return 'bg-cyan-500/10 text-cyan-400 border-cyan-500/20';
      case AppRole.Instructor:
      case AppRole.OrgUnitManager:
        return 'bg-purple-500/10 text-purple-400 border-purple-500/20';
      default:
        return 'bg-blue-500/10 text-blue-400 border-blue-500/20';
    }
  };

  return (
    <div className="space-y-8 text-slate-100">

      {/* ── TOP TITLE & ACTION ───────────────────────────────────────────── */}
      <div className="flex flex-col sm:flex-row justify-between sm:items-center gap-4">
        <div>
          <h1 className="text-3xl font-black text-transparent bg-clip-text bg-gradient-to-r from-blue-400 via-indigo-400 to-purple-400 tracking-tight">
            Quản Trị Người Dùng & Phân Quyền Scoped RBAC
          </h1>
          <p className="text-sm text-slate-400 mt-1 font-medium">
            Quản lý tài khoản, gán cây đơn vị tổ chức và phân quyền chuyên sâu chuẩn NIST SP 800-162.
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2.5">
          <button
            onClick={() => setIsTreeModalOpen(true)}
            className="px-3.5 py-2.5 rounded-xl bg-slate-900 hover:bg-slate-800 border border-slate-800 text-slate-300 hover:text-white font-bold text-xs flex items-center gap-2 transition-all cursor-pointer"
            title="Xem sơ đồ phân tầng cây tổ chức"
          >
            <Network size={15} className="text-purple-400" />
            <span>Sơ Đồ Cây OU</span>
          </button>

          <button
            onClick={handleExportCSV}
            className="px-3.5 py-2.5 rounded-xl bg-slate-900 hover:bg-slate-800 border border-slate-800 text-slate-300 hover:text-white font-bold text-xs flex items-center gap-2 transition-all cursor-pointer"
            title="Xuất file Excel CSV danh sách người dùng"
          >
            <Download size={15} className="text-emerald-400" />
            <span>Xuất Excel</span>
          </button>

          <button
            onClick={() => { loadUsers(); loadStats(); }}
            className="p-2.5 rounded-xl bg-slate-900 border border-slate-800 text-slate-400 hover:text-white transition-colors cursor-pointer"
            title="Làm mới"
          >
            <RefreshCw size={16} className={loading ? 'animate-spin' : ''} />
          </button>

          <button
            onClick={() => setIsCreateModalOpen(true)}
            className="px-4 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-500 text-white font-bold text-xs uppercase tracking-wider flex items-center gap-2 shadow-lg shadow-blue-500/25 transition-all cursor-pointer"
          >
            <UserPlus size={16} />
            Thêm Tài Khoản
          </button>
        </div>
      </div>

      {/* ── KPI STATS BAR ────────────────────────────────────────────────── */}
      <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-4">
        <div className="bg-slate-900/60 border border-slate-800 rounded-2xl p-4 backdrop-blur-xl">
          <div className="text-xs text-slate-400 font-bold uppercase tracking-wider">Tổng Tài Khoản</div>
          <div className="text-2xl font-black text-white mt-1">{stats?.totalUsers ?? '...'}</div>
        </div>

        <div className="bg-slate-900/60 border border-slate-800 rounded-2xl p-4 backdrop-blur-xl">
          <div className="text-xs text-emerald-400 font-bold uppercase tracking-wider">Đang Hoạt Động</div>
          <div className="text-2xl font-black text-emerald-400 mt-1">{stats?.activeUsers ?? '...'}</div>
        </div>

        <div className="bg-slate-900/60 border border-slate-800 rounded-2xl p-4 backdrop-blur-xl">
          <div className="text-xs text-rose-400 font-bold uppercase tracking-wider">Đang Bị Khóa</div>
          <div className="text-2xl font-black text-rose-400 mt-1">{stats?.lockedUsers ?? '...'}</div>
        </div>

        <div className="bg-slate-900/60 border border-slate-800 rounded-2xl p-4 backdrop-blur-xl">
          <div className="text-xs text-purple-400 font-bold uppercase tracking-wider">Quản Trị Viên</div>
          <div className="text-2xl font-black text-purple-400 mt-1">{stats?.adminUsers ?? '...'}</div>
        </div>

        <div className="bg-slate-900/60 border border-slate-800 rounded-2xl p-4 backdrop-blur-xl">
          <div className="text-xs text-cyan-400 font-bold uppercase tracking-wider">Trưởng Nhóm / GV</div>
          <div className="text-2xl font-black text-cyan-400 mt-1">{stats?.leaderUsers ?? '...'}</div>
        </div>

        <div className="bg-slate-900/60 border border-slate-800 rounded-2xl p-4 backdrop-blur-xl">
          <div className="text-xs text-amber-400 font-bold uppercase tracking-wider">VIP / Enterprise</div>
          <div className="text-2xl font-black text-amber-400 mt-1">{stats?.vipUsers ?? '...'}</div>
        </div>
      </div>

      {/* ── FILTER TOOLBAR ───────────────────────────────────────────────── */}
      <div className="bg-slate-900/60 border border-slate-800 rounded-2xl p-4 backdrop-blur-xl space-y-3">
        <form onSubmit={handleSearchSubmit} className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-6 gap-3">
          {/* Search Box */}
          <div className="lg:col-span-2 relative">
            <Search size={16} className="absolute left-3.5 top-3 text-slate-500" />
            <input
              type="text"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              placeholder="Tìm theo tên, email..."
              className="w-full bg-slate-950 border border-slate-800 focus:border-blue-500 rounded-xl pl-10 pr-4 py-2.5 text-xs text-white placeholder-slate-500 focus:outline-none transition-all font-medium"
            />
          </div>

          {/* Role Filter */}
          <div>
            <select
              value={selectedRole}
              onChange={(e) => { setSelectedRole(e.target.value); setPage(1); }}
              className="w-full bg-slate-950 border border-slate-800 focus:border-blue-500 rounded-xl px-3 py-2.5 text-xs text-slate-300 focus:outline-none transition-all cursor-pointer font-medium"
            >
              <option value="">-- Tất cả vai trò --</option>
              <option value={AppRole.SystemAdmin}>SystemAdmin</option>
              <option value={AppRole.TenantAdmin}>TenantAdmin</option>
              <option value={AppRole.TeamLeader}>TeamLeader</option>
              <option value={AppRole.Instructor}>Instructor</option>
              <option value={AppRole.OrgUnitManager}>OrgUnitManager</option>
              <option value={AppRole.Learner}>Learner</option>
              <option value={AppRole.GuestViewer}>GuestViewer</option>
            </select>
          </div>

          {/* OrgUnit Filter */}
          <div>
            <select
              value={selectedOrgUnit}
              onChange={(e) => { setSelectedOrgUnit(e.target.value); setPage(1); }}
              className="w-full bg-slate-950 border border-slate-800 focus:border-blue-500 rounded-xl px-3 py-2.5 text-xs text-slate-300 focus:outline-none transition-all cursor-pointer font-medium"
            >
              <option value="">-- Cây đơn vị (OU) --</option>
              {orgUnits.map((ou) => (
                <option key={ou.id} value={ou.id}>
                  {ou.hierarchyPath || `/${ou.code}`} ({ou.name})
                </option>
              ))}
            </select>
          </div>

          {/* Tier Filter */}
          <div>
            <select
              value={selectedTier}
              onChange={(e) => { setSelectedTier(e.target.value); setPage(1); }}
              className="w-full bg-slate-950 border border-slate-800 focus:border-blue-500 rounded-xl px-3 py-2.5 text-xs text-slate-300 focus:outline-none transition-all cursor-pointer font-medium"
            >
              <option value="">-- Gói thuê bao --</option>
              <option value="FREE">FREE</option>
              <option value="VIP">VIP</option>
              <option value="ENTERPRISE">ENTERPRISE</option>
            </select>
          </div>

          {/* Status Filter */}
          <div>
            <select
              value={selectedStatus}
              onChange={(e) => { setSelectedStatus(e.target.value); setPage(1); }}
              className="w-full bg-slate-950 border border-slate-800 focus:border-blue-500 rounded-xl px-3 py-2.5 text-xs text-slate-300 focus:outline-none transition-all cursor-pointer font-medium"
            >
              <option value="">-- Trạng thái --</option>
              <option value="active">Đang hoạt động</option>
              <option value="locked">Bị khóa</option>
            </select>
          </div>
        </form>

        {(searchTerm || selectedRole || selectedOrgUnit || selectedTier || selectedStatus) && (
          <div className="flex items-center justify-between pt-2 border-t border-slate-800/80 text-xs">
            <span className="text-slate-400">
              Đang lọc: Tìm thấy <strong className="text-white">{totalCount}</strong> kết quả
            </span>
            <button
              onClick={handleResetFilters}
              className="text-blue-400 hover:text-blue-300 font-bold transition-colors cursor-pointer"
            >
              Xóa bộ lọc
            </button>
          </div>
        )}
      </div>

      {error && (
        <div className="p-4 rounded-2xl bg-rose-500/10 border border-rose-500/20 text-rose-400 text-xs font-semibold flex items-center gap-2">
          <AlertCircle size={16} className="shrink-0" />
          <span>{error}</span>
        </div>
      )}

      {/* ── USER DATA GRID ───────────────────────────────────────────────── */}
      <div className="bg-slate-900/60 border border-slate-800 rounded-3xl overflow-hidden backdrop-blur-xl shadow-xl">
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse min-w-[900px]">
            <thead>
              <tr className="border-b border-slate-800 text-slate-400 text-[11px] font-black tracking-wider uppercase bg-slate-950/40">
                <th className="p-4">Người dùng</th>
                <th className="p-4 w-36">Vai trò (Role)</th>
                <th className="p-4 w-48">Đơn vị tổ chức (OU)</th>
                <th className="p-4 w-32">Thuê bao</th>
                <th className="p-4 w-28">Trạng thái</th>
                <th className="p-4 w-32">Đăng nhập cuối</th>
                <th className="p-4 w-28 text-right">Thao tác</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={7} className="p-10 text-center text-slate-500 text-sm">
                    <RefreshCw size={24} className="animate-spin mx-auto mb-2 text-blue-500" />
                    Đang tải danh sách người dùng...
                  </td>
                </tr>
              ) : users.length === 0 ? (
                <tr>
                  <td colSpan={7} className="p-10 text-center text-slate-500 text-sm">
                    Không tìm thấy tài khoản người dùng nào khớp với bộ lọc.
                  </td>
                </tr>
              ) : (
                users.map((item) => (
                  <tr
                    key={item.id}
                    className="border-b border-slate-850/60 hover:bg-slate-800/20 transition-colors"
                  >
                    {/* User info */}
                    <td className="p-4">
                      <div className="flex items-center gap-3">
                        <div className="w-10 h-10 rounded-xl bg-gradient-to-tr from-blue-600 to-purple-600 flex items-center justify-center font-black text-white text-xs shadow-md shrink-0 uppercase">
                          {item.fullName ? item.fullName.charAt(0) : item.email.charAt(0)}
                        </div>
                        <div className="min-w-0">
                          <div className="font-bold text-slate-100 text-sm truncate">{item.fullName}</div>
                          <div className="text-xs text-slate-400 font-mono truncate">{item.email}</div>
                        </div>
                      </div>
                    </td>

                    {/* Role badge */}
                    <td className="p-4">
                      <span className={`px-2.5 py-1 rounded-lg text-[10px] font-black uppercase tracking-wider flex items-center gap-1 w-fit border ${getRoleBadgeStyle(item.role)}`}>
                        <Shield size={10} />
                        {item.role}
                      </span>
                    </td>

                    {/* OrgUnit */}
                    <td className="p-4 text-xs">
                      {item.orgUnitName ? (
                        <div className="space-y-0.5">
                          <div className="text-slate-200 font-semibold truncate">{item.orgUnitName}</div>
                          <div className="text-[10px] text-slate-500 font-mono truncate">
                            {item.orgUnitHierarchyPath}
                          </div>
                        </div>
                      ) : (
                        <span className="text-slate-500 text-xs italic">Toàn tổ chức</span>
                      )}
                    </td>

                    {/* Subscription Tier */}
                    <td className="p-4">
                      <span className={`px-2.5 py-1 rounded-lg text-[10px] font-black uppercase tracking-wider flex items-center gap-1 w-fit border ${
                        item.isPremium || item.subscriptionTier === 'VIP' || item.subscriptionTier === 'ENTERPRISE'
                          ? 'bg-amber-500/10 text-amber-400 border-amber-500/20'
                          : 'bg-slate-800 text-slate-400 border-slate-700'
                      }`}>
                        <Sparkles size={10} />
                        {item.subscriptionTier || 'FREE'}
                      </span>
                    </td>

                    {/* Active Status */}
                    <td className="p-4">
                      <button
                        onClick={() => handleQuickToggleStatus(item)}
                        title="Bấm để nhanh chóng khóa hoặc mở khóa tài khoản này"
                        className={`px-2.5 py-1 rounded-lg text-[10px] font-black uppercase tracking-wider flex items-center gap-1 w-fit border transition-all cursor-pointer hover:scale-105 ${
                          item.isActive
                            ? 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20 hover:bg-rose-500/20 hover:text-rose-400 hover:border-rose-500/30'
                            : 'bg-rose-500/10 text-rose-400 border-rose-500/20 hover:bg-emerald-500/20 hover:text-emerald-400 hover:border-emerald-500/30'
                        }`}
                      >
                        {item.isActive ? <Unlock size={10} /> : <Lock size={10} />}
                        {item.isActive ? 'Hoạt động' : 'Đã khóa'}
                      </button>
                    </td>

                    {/* Last login */}
                    <td className="p-4 text-xs font-mono text-slate-400">
                      {item.lastLoginAt
                        ? new Date(item.lastLoginAt).toLocaleDateString('vi-VN', {
                            day: '2-digit',
                            month: '2-digit',
                            year: 'numeric'
                          })
                        : 'Chưa đăng nhập'}
                    </td>

                    {/* Actions */}
                    <td className="p-4 text-right">
                      <button
                        onClick={() => handleOpenRoleModal(item)}
                        className="px-3 py-1.5 rounded-lg bg-blue-600/10 hover:bg-blue-600 text-blue-400 hover:text-white border border-blue-500/20 text-xs font-bold transition-all cursor-pointer"
                      >
                        Phân quyền
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {/* ── PAGINATION CONTROLS ─────────────────────────────────────────── */}
        <div className="p-4 border-t border-slate-800 bg-slate-950/40 flex items-center justify-between text-xs text-slate-400">
          <div>
            Hiển thị <strong>{users.length}</strong> / <strong>{totalCount}</strong> tài khoản
          </div>

          <div className="flex items-center gap-2">
            <button
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={page <= 1}
              className="p-2 rounded-lg bg-slate-850 hover:bg-slate-800 text-slate-300 disabled:opacity-30 transition-colors cursor-pointer disabled:cursor-not-allowed"
            >
              <ChevronLeft size={16} />
            </button>
            <span className="font-bold text-slate-200">
              Trang {page} / {totalPages}
            </span>
            <button
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              disabled={page >= totalPages}
              className="p-2 rounded-lg bg-slate-850 hover:bg-slate-800 text-slate-300 disabled:opacity-30 transition-colors cursor-pointer disabled:cursor-not-allowed"
            >
              <ChevronRight size={16} />
            </button>
          </div>
        </div>
      </div>

      {/* ── ROLE & OU ASSIGNMENT MODAL ────────────────────────────────────── */}
      <UserRoleModal
        user={selectedUserForRole}
        orgUnits={orgUnits}
        isOpen={isRoleModalOpen}
        onClose={() => { setIsRoleModalOpen(false); setSelectedUserForRole(null); }}
        onSuccess={() => { loadUsers(); loadStats(); }}
      />

      {/* ── CREATE USER MODAL ────────────────────────────────────────────── */}
      {isCreateModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-md overflow-y-auto">
          <div className="relative w-full max-w-lg bg-slate-900 border border-slate-800 rounded-3xl p-6 sm:p-8 shadow-2xl space-y-6 text-slate-100">
            <div className="flex items-center justify-between pb-4 border-b border-slate-800">
              <div className="flex items-center gap-2">
                <UserPlus size={20} className="text-blue-400" />
                <h3 className="text-lg font-black text-white">Thêm Tài Khoản Người Dùng Mới</h3>
              </div>
              <button
                onClick={() => setIsCreateModalOpen(false)}
                className="p-2 text-slate-400 hover:text-white rounded-xl"
              >
                <X size={18} />
              </button>
            </div>

            {createError && (
              <div className="p-3.5 rounded-xl bg-rose-500/10 border border-rose-500/20 text-rose-400 text-xs font-semibold flex items-center gap-2">
                <AlertCircle size={16} className="shrink-0" />
                <span>{createError}</span>
              </div>
            )}

            <form onSubmit={handleCreateUser} className="space-y-4">
              <div>
                <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-1.5">
                  Email Đăng Nhập *
                </label>
                <input
                  type="email"
                  value={newEmail}
                  onChange={(e) => setNewEmail(e.target.value)}
                  required
                  placeholder="canbo@dehoc.vn"
                  className="w-full bg-slate-950 border border-slate-800 focus:border-blue-500 rounded-xl px-3.5 py-2.5 text-xs text-white focus:outline-none transition-all font-mono"
                />
              </div>

              <div>
                <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-1.5">
                  Mật Khẩu Khởi Tạo *
                </label>
                <input
                  type="password"
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                  required
                  placeholder="••••••••••••"
                  className="w-full bg-slate-950 border border-slate-800 focus:border-blue-500 rounded-xl px-3.5 py-2.5 text-xs text-white focus:outline-none transition-all font-mono"
                />
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-1.5">
                    Họ và Tên
                  </label>
                  <input
                    type="text"
                    value={newFullName}
                    onChange={(e) => setNewFullName(e.target.value)}
                    placeholder="Nguyễn Văn A"
                    className="w-full bg-slate-950 border border-slate-800 focus:border-blue-500 rounded-xl px-3.5 py-2.5 text-xs text-white focus:outline-none transition-all"
                  />
                </div>

                <div>
                  <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-1.5">
                    Số Điện Thoại
                  </label>
                  <input
                    type="text"
                    value={newPhone}
                    onChange={(e) => setNewPhone(e.target.value)}
                    placeholder="0912345678"
                    className="w-full bg-slate-950 border border-slate-800 focus:border-blue-500 rounded-xl px-3.5 py-2.5 text-xs text-white focus:outline-none transition-all"
                  />
                </div>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-1.5">
                    Vai Trò (Role)
                  </label>
                  <select
                    value={newRole}
                    onChange={(e) => setNewRole(e.target.value)}
                    className="w-full bg-slate-950 border border-slate-800 focus:border-blue-500 rounded-xl px-3 py-2.5 text-xs text-white focus:outline-none transition-all"
                  >
                    <option value={AppRole.Learner}>Learner (Học viên)</option>
                    <option value={AppRole.TeamLeader}>TeamLeader (Trưởng nhóm)</option>
                    <option value={AppRole.Instructor}>Instructor (Giảng viên)</option>
                    <option value={AppRole.TenantAdmin}>TenantAdmin (Quản trị viên)</option>
                  </select>
                </div>

                <div>
                  <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-1.5">
                    Gói Thuê Bao
                  </label>
                  <select
                    value={newTier}
                    onChange={(e) => setNewTier(e.target.value)}
                    className="w-full bg-slate-950 border border-slate-800 focus:border-blue-500 rounded-xl px-3 py-2.5 text-xs text-white focus:outline-none transition-all"
                  >
                    <option value="FREE">FREE</option>
                    <option value="VIP">VIP</option>
                    <option value="ENTERPRISE">ENTERPRISE</option>
                  </select>
                </div>
              </div>

              <div>
                <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-1.5">
                  Đơn Vị Cây Phân Cấp (OrgUnit)
                </label>
                <select
                  value={newOrgUnitId}
                  onChange={(e) => setNewOrgUnitId(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 focus:border-blue-500 rounded-xl px-3 py-2.5 text-xs text-white focus:outline-none transition-all"
                >
                  <option value="">-- Toàn tổ chức / Chưa gán đơn vị --</option>
                  {orgUnits.map((ou) => (
                    <option key={ou.id} value={ou.id}>
                      {ou.hierarchyPath || `/${ou.code}`} — {ou.name}
                    </option>
                  ))}
                </select>
              </div>

              <div className="flex justify-end gap-3 pt-4 border-t border-slate-800">
                <button
                  type="button"
                  onClick={() => setIsCreateModalOpen(false)}
                  className="px-4 py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 font-bold text-xs"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  disabled={isCreating}
                  className="px-5 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-500 text-white font-bold text-xs flex items-center gap-2 shadow-lg shadow-blue-500/25 disabled:opacity-50"
                >
                  {isCreating ? <RefreshCw size={14} className="animate-spin" /> : <CheckCircle2 size={14} />}
                  {isCreating ? 'Đang tạo...' : 'Tạo Tài Khoản'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* ── OU TREE EXPLORER MODAL ─────────────────────────────────────── */}
      {isTreeModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-md overflow-y-auto">
          <div className="relative w-full max-w-2xl bg-slate-900 border border-slate-800 rounded-3xl p-6 sm:p-8 shadow-2xl space-y-6 text-slate-100 my-8">
            <div className="flex items-center justify-between pb-4 border-b border-slate-800">
              <div className="flex items-center gap-2.5">
                <Network size={22} className="text-purple-400" />
                <div>
                  <h3 className="text-lg font-black text-white">Sơ Đồ Cây Phân Cấp Tổ Chức (5-Tier OU Tree)</h3>
                  <p className="text-xs text-slate-400">Cấu trúc phòng ban đệ quy mềm & phạm vi ủy nhiệm Scoped RBAC.</p>
                </div>
              </div>
              <button
                onClick={() => setIsTreeModalOpen(false)}
                className="p-2 text-slate-400 hover:text-white rounded-xl cursor-pointer"
              >
                <X size={18} />
              </button>
            </div>

            <div className="space-y-3 max-h-[60vh] overflow-y-auto pr-2">
              {orgUnits.length === 0 ? (
                <div className="p-8 text-center text-slate-500 text-xs">
                  Chưa có đơn vị phòng ban nào được tạo trong tổ chức.
                </div>
              ) : (
                orgUnits.map((ou) => {
                  const depth = (ou.hierarchyPath?.match(/\//g) || []).length;
                  return (
                    <div
                      key={ou.id}
                      style={{ paddingLeft: `${Math.max(0, (depth - 1) * 20)}px` }}
                      className="transition-all"
                    >
                      <div className="p-3.5 rounded-xl bg-slate-950/70 border border-slate-800/80 flex items-center justify-between hover:border-purple-500/40 hover:bg-slate-950 transition-all">
                        <div className="flex items-center gap-3">
                          <div className="w-8 h-8 rounded-lg bg-purple-500/10 border border-purple-500/20 flex items-center justify-center text-purple-400 font-bold text-xs">
                            <Layers size={14} />
                          </div>
                          <div>
                            <div className="font-bold text-sm text-slate-200">{ou.name}</div>
                            <div className="text-[11px] text-slate-500 font-mono flex items-center gap-2">
                              <span>Mã: <strong className="text-slate-400">{ou.code}</strong></span>
                              <span>Đường dẫn: <strong className="text-purple-400">{ou.hierarchyPath || `/${ou.code}`}</strong></span>
                            </div>
                          </div>
                        </div>

                        <button
                          type="button"
                          onClick={() => {
                            setSelectedOrgUnit(ou.id);
                            setIsTreeModalOpen(false);
                            setPage(1);
                          }}
                          className="px-3 py-1.5 rounded-lg bg-purple-500/10 hover:bg-purple-600 text-purple-400 hover:text-white text-xs font-bold transition-all cursor-pointer"
                        >
                          Lọc nhân sự
                        </button>
                      </div>
                    </div>
                  );
                })
              )}
            </div>

            <div className="flex justify-end pt-3 border-t border-slate-800">
              <button
                type="button"
                onClick={() => setIsTreeModalOpen(false)}
                className="px-5 py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-200 font-bold text-xs cursor-pointer"
              >
                Đóng
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default AdminUsersPage;
