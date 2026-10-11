import { useState, useEffect, useCallback } from 'react';
import { 
  Building2, Plus, Search, Shield, Globe, Users, 
  CheckCircle2, AlertCircle, Edit, Power, RefreshCw, Layers,
  Smartphone, Sparkles
} from 'lucide-react';
import { apiClient } from '@/shared/api/client';

interface TenantItem {
  id: string;
  code: string;
  name: string;
  logoUrl?: string;
  primaryColor?: string;
  domain?: string;
  customDomain?: string;
  plan: string;
  scaleType: string;
  maxUsers: number;
  isActive: boolean;
  createdAt: string;
  expiresAt?: string;
  currentUsersCount: number;
  orgUnitsCount: number;
  questionsCount: number;
}

interface TenantSummary {
  totalTenants: number;
  activeTenants: number;
  inactiveTenants: number;
  totalUsers: number;
  enterpriseTenants: number;
}

export function TenantsPage() {
  const [tenants, setTenants] = useState<TenantItem[]>([]);
  const [summary, setSummary] = useState<TenantSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [planFilter, setPlanFilter] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);

  // Modal states
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [selectedTenant, setSelectedTenant] = useState<TenantItem | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [feedbackMsg, setFeedbackMsg] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  // Form states for Create
  const [formCode, setFormCode] = useState('');
  const [formName, setFormName] = useState('');
  const [formPlan, setFormPlan] = useState(3); // 3 = Enterprise
  const [formScale, setFormScale] = useState(5); // 5 = Enterprise
  const [formMaxUsers, setFormMaxUsers] = useState(1000);
  const [formDomain, setFormDomain] = useState('');
  const [formCustomDomain, setFormCustomDomain] = useState('');
  const [formPrimaryColor, setFormPrimaryColor] = useState('#f59e0b');
  const [formMobileVariant, setFormMobileVariant] = useState('kids');
  const [formAdminEmail, setFormAdminEmail] = useState('');
  const [formAdminPassword, setFormAdminPassword] = useState('');
  const [formAdminName, setFormAdminName] = useState('');

  // Fetch summary
  const fetchSummary = useCallback(async () => {
    try {
      const res = await apiClient.get<TenantSummary>('/api/admin/tenants/summary');
      setSummary(res.data);
    } catch {
      // Ignored
    }
  }, []);

  // Fetch tenants
  const fetchTenants = useCallback(async () => {
    try {
      setLoading(true);
      const params = new URLSearchParams();
      params.append('page', page.toString());
      params.append('pageSize', '15');
      if (search.trim()) params.append('search', search.trim());
      if (planFilter) params.append('plan', planFilter);
      if (statusFilter !== '') params.append('isActive', statusFilter);

      const res = await apiClient.get<{
        items: TenantItem[];
        totalCount: number;
        page: number;
        totalPages: number;
      }>(`/api/admin/tenants?${params.toString()}`);

      setTenants(res.data.items || []);
      setTotalPages(res.data.totalPages || 1);
    } catch {
      setFeedbackMsg({ type: 'error', text: 'Không thể tải danh sách khách thuê. Vui lòng thử lại.' });
    } finally {
      setLoading(false);
    }
  }, [page, search, planFilter, statusFilter]);

  useEffect(() => {
    fetchSummary();
  }, [fetchSummary]);

  useEffect(() => {
    fetchTenants();
  }, [fetchTenants]);

  const handleCreateTenant = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formCode.trim() || !formName.trim()) {
      setFeedbackMsg({ type: 'error', text: 'Vui lòng điền đầy đủ Mã và Tên khách thuê.' });
      return;
    }
    try {
      setSubmitting(true);
      await apiClient.post('/api/admin/tenants', {
        code: formCode.trim().toLowerCase(),
        name: formName.trim(),
        plan: Number(formPlan),
        scaleType: Number(formScale),
        maxUsers: Number(formMaxUsers),
        domain: formDomain.trim() || undefined,
        customDomain: formCustomDomain.trim() || undefined,
        primaryColor: formPrimaryColor,
        adminEmail: formAdminEmail.trim() || undefined,
        adminPassword: formAdminPassword || undefined,
        adminFullName: formAdminName.trim() || undefined
      });

      setFeedbackMsg({ type: 'success', text: `Khởi tạo thành công khách thuê ${formName}!` });
      setIsCreateModalOpen(false);
      resetForm();
      fetchTenants();
      fetchSummary();
    } catch (err: any) {
      setFeedbackMsg({ type: 'error', text: err?.response?.data?.message || 'Lỗi khởi tạo khách thuê.' });
    } finally {
      setSubmitting(false);
    }
  };

  const handleUpdateTenant = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedTenant) return;
    try {
      setSubmitting(true);
      await apiClient.put(`/api/admin/tenants/${selectedTenant.id}`, {
        name: selectedTenant.name,
        plan: Number(selectedTenant.plan === 'Starter' ? 1 : selectedTenant.plan === 'Professional' ? 2 : selectedTenant.plan === 'Enterprise' ? 3 : 4),
        maxUsers: Number(selectedTenant.maxUsers),
        domain: selectedTenant.domain,
        customDomain: selectedTenant.customDomain,
        primaryColor: selectedTenant.primaryColor,
        isActive: selectedTenant.isActive
      });

      setFeedbackMsg({ type: 'success', text: 'Cập nhật thông tin khách thuê thành công!' });
      setIsEditModalOpen(false);
      fetchTenants();
      fetchSummary();
    } catch (err: any) {
      setFeedbackMsg({ type: 'error', text: err?.response?.data?.message || 'Lỗi cập nhật khách thuê.' });
    } finally {
      setSubmitting(false);
    }
  };

  const handleToggleActive = async (tenant: TenantItem) => {
    const action = tenant.isActive ? 'vô hiệu hóa' : 'kích hoạt lại';
    if (!confirm(`Bạn có chắc chắn muốn ${action} khách thuê "${tenant.name}" không?`)) return;

    try {
      if (tenant.isActive) {
        await apiClient.delete(`/api/admin/tenants/${tenant.id}`);
      } else {
        await apiClient.put(`/api/admin/tenants/${tenant.id}`, { isActive: true });
      }
      setFeedbackMsg({ type: 'success', text: `Đã ${action} thành công khách thuê ${tenant.name}.` });
      fetchTenants();
      fetchSummary();
    } catch (err: any) {
      setFeedbackMsg({ type: 'error', text: err?.response?.data?.message || `Lỗi khi ${action} khách thuê.` });
    }
  };

  const resetForm = () => {
    setFormCode('');
    setFormName('');
    setFormPlan(3);
    setFormScale(5);
    setFormMaxUsers(1000);
    setFormDomain('');
    setFormCustomDomain('');
    setFormPrimaryColor('#06b6d4');
    setFormAdminEmail('');
    setFormAdminPassword('');
    setFormAdminName('');
  };

  return (
    <div className="p-6 md:p-8 space-y-8 max-w-7xl mx-auto">
      {/* Top Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-3">
            <div className="p-2.5 bg-gradient-to-br from-indigo-500/20 to-cyan-500/20 rounded-2xl border border-cyan-500/30 text-cyan-400">
              <Building2 className="w-7 h-7" />
            </div>
            <div>
              <h1 className="text-2xl font-black tracking-tight text-white flex items-center gap-2">
                Quản Trị Khách Thuê (Multi-Tenant Hub)
                <span className="px-2.5 py-0.5 rounded-full text-[11px] font-bold bg-indigo-500/20 text-indigo-300 border border-indigo-500/30">
                  Global B2B
                </span>
              </h1>
              <p className="text-xs text-slate-400">
                Cô lập dữ liệu tuyệt đối giữa các tổ chức, ngân hàng, trường đại học theo chuẩn NIST SP 800-162 & SOC 2.
              </p>
            </div>
          </div>
        </div>

        <button
          onClick={() => { resetForm(); setIsCreateModalOpen(true); }}
          className="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-gradient-to-r from-cyan-500 to-blue-600 hover:from-cyan-400 hover:to-blue-500 text-white font-bold text-sm shadow-lg shadow-cyan-500/20 transition-all cursor-pointer"
        >
          <Plus size={18} />
          <span>Thêm Khách Thuê Mới</span>
        </button>
      </div>

      {/* Feedback message banner */}
      {feedbackMsg && (
        <div className={`p-4 rounded-xl flex items-center justify-between gap-3 text-sm font-medium border ${
          feedbackMsg.type === 'success' 
            ? 'bg-emerald-950/50 border-emerald-500/40 text-emerald-300' 
            : 'bg-rose-950/50 border-rose-500/40 text-rose-300'
        }`}>
          <div className="flex items-center gap-2">
            {feedbackMsg.type === 'success' ? <CheckCircle2 size={18} /> : <AlertCircle size={18} />}
            <span>{feedbackMsg.text}</span>
          </div>
          <button onClick={() => setFeedbackMsg(null)} className="text-xs opacity-70 hover:opacity-100 cursor-pointer">
            Đóng
          </button>
        </div>
      )}

      {/* KPI Stats Banner */}
      {summary && (
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
          <div className="p-5 rounded-2xl bg-slate-900/60 border border-slate-800 backdrop-blur-xl">
            <div className="flex items-center justify-between text-slate-400 mb-2">
              <span className="text-xs font-semibold">Tổng Khách Thuê</span>
              <Building2 size={18} className="text-cyan-400" />
            </div>
            <div className="text-2xl font-black text-white">{summary.totalTenants}</div>
            <div className="text-[11px] text-slate-500 mt-1">Đang hoạt động: {summary.activeTenants}</div>
          </div>

          <div className="p-5 rounded-2xl bg-slate-900/60 border border-slate-800 backdrop-blur-xl">
            <div className="flex items-center justify-between text-slate-400 mb-2">
              <span className="text-xs font-semibold">Gói Enterprise / Global</span>
              <Shield size={18} className="text-indigo-400" />
            </div>
            <div className="text-2xl font-black text-indigo-300">{summary.enterpriseTenants}</div>
            <div className="text-[11px] text-slate-500 mt-1">Chuẩn doanh nghiệp 5 tầng</div>
          </div>

          <div className="p-5 rounded-2xl bg-slate-900/60 border border-slate-800 backdrop-blur-xl">
            <div className="flex items-center justify-between text-slate-400 mb-2">
              <span className="text-xs font-semibold">Tổng Nhân Sự Khách Thuê</span>
              <Users size={18} className="text-amber-400" />
            </div>
            <div className="text-2xl font-black text-amber-300">{summary.totalUsers}</div>
            <div className="text-[11px] text-slate-500 mt-1">Toàn bộ tài khoản active</div>
          </div>

          <div className="p-5 rounded-2xl bg-slate-900/60 border border-slate-800 backdrop-blur-xl">
            <div className="flex items-center justify-between text-slate-400 mb-2">
              <span className="text-xs font-semibold">Khách Thuê Tạm Khóa</span>
              <AlertCircle size={18} className="text-rose-400" />
            </div>
            <div className="text-2xl font-black text-rose-400">{summary.inactiveTenants}</div>
            <div className="text-[11px] text-slate-500 mt-1">Hết hạn hoặc vô hiệu hóa</div>
          </div>
        </div>
      )}

      {/* Filter and Search Bar */}
      <div className="p-4 rounded-2xl bg-slate-900/60 border border-slate-800 backdrop-blur-xl flex flex-col md:flex-row items-center justify-between gap-4">
        <div className="relative w-full md:w-80">
          <Search className="absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-400" size={16} />
          <input
            type="text"
            placeholder="Tìm kiếm theo tên, mã, domain..."
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1); }}
            className="w-full pl-10 pr-4 py-2 bg-slate-950/60 border border-slate-800 rounded-xl text-sm text-white placeholder-slate-500 focus:outline-none focus:border-cyan-500 transition-colors"
          />
        </div>

        <div className="flex items-center gap-3 w-full md:w-auto flex-wrap">
          <select
            value={planFilter}
            onChange={(e) => { setPlanFilter(e.target.value); setPage(1); }}
            className="px-3.5 py-2 bg-slate-950/60 border border-slate-800 rounded-xl text-xs text-white focus:outline-none focus:border-cyan-500 cursor-pointer"
          >
            <option value="">Tất cả Gói cước</option>
            <option value="1">Starter</option>
            <option value="2">Professional</option>
            <option value="3">Enterprise</option>
            <option value="4">Global</option>
          </select>

          <select
            value={statusFilter}
            onChange={(e) => { setStatusFilter(e.target.value); setPage(1); }}
            className="px-3.5 py-2 bg-slate-950/60 border border-slate-800 rounded-xl text-xs text-white focus:outline-none focus:border-cyan-500 cursor-pointer"
          >
            <option value="">Tất cả Trạng thái</option>
            <option value="true">Đang hoạt động</option>
            <option value="false">Đã khóa / Tạm dừng</option>
          </select>

          <button
            onClick={() => { fetchTenants(); fetchSummary(); }}
            className="p-2 bg-slate-800 hover:bg-slate-700 text-slate-300 rounded-xl transition-colors cursor-pointer"
            title="Làm mới"
          >
            <RefreshCw size={16} />
          </button>
        </div>
      </div>

      {/* Tenants Table */}
      <div className="rounded-2xl border border-slate-800 bg-slate-900/60 backdrop-blur-xl overflow-hidden shadow-2xl">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm text-slate-300">
            <thead className="bg-slate-950/80 text-[11px] uppercase tracking-wider text-slate-400 border-b border-slate-800">
              <tr>
                <th className="px-6 py-4 font-bold">Khách Thuê / Tổ Chức</th>
                <th className="px-4 py-4 font-bold">Gói & Quy Mô</th>
                <th className="px-4 py-4 font-bold">Hạn Mức Nhân Sự</th>
                <th className="px-4 py-4 font-bold">Cơ Cấu OU</th>
                <th className="px-4 py-4 font-bold">Domain Định Danh</th>
                <th className="px-4 py-4 font-bold">Trạng Thái</th>
                <th className="px-6 py-4 font-bold text-right">Thao Tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800/60">
              {loading ? (
                <tr>
                  <td colSpan={7} className="px-6 py-12 text-center text-slate-500">
                    <RefreshCw className="animate-spin w-6 h-6 mx-auto mb-2 text-cyan-400" />
                    <span>Đang tải danh sách khách thuê...</span>
                  </td>
                </tr>
              ) : tenants.length === 0 ? (
                <tr>
                  <td colSpan={7} className="px-6 py-12 text-center text-slate-500">
                    <Building2 className="w-10 h-10 mx-auto mb-3 text-slate-600 opacity-40" />
                    <p className="font-semibold text-slate-400">Không tìm thấy khách thuê nào</p>
                    <p className="text-xs text-slate-600 mt-1">Hãy tạo khách thuê mới để kích hoạt môi trường làm việc</p>
                  </td>
                </tr>
              ) : (
                tenants.map((t) => (
                  <tr key={t.id} className="hover:bg-slate-800/40 transition-colors">
                    {/* Organization info */}
                    <td className="px-6 py-4">
                      <div className="flex items-center gap-3">
                        <div 
                          className="w-9 h-9 rounded-xl flex items-center justify-center font-black text-sm text-white shadow-md flex-shrink-0"
                          style={{ backgroundColor: t.primaryColor || '#06b6d4' }}
                        >
                          {t.code.substring(0, 2).toUpperCase()}
                        </div>
                        <div>
                          <div className="font-bold text-white flex items-center gap-2">
                            {t.name}
                          </div>
                          <div className="text-[11px] font-mono text-cyan-400">
                            code: {t.code}
                          </div>
                        </div>
                      </div>
                    </td>

                    {/* Plan & Scale */}
                    <td className="px-4 py-4">
                      <div className="flex flex-col gap-1">
                        <span className={`inline-flex items-center w-max px-2.5 py-0.5 rounded-full text-[10px] font-extrabold ${
                          t.plan === 'Enterprise' || t.plan === 'Global'
                            ? 'bg-indigo-500/20 text-indigo-300 border border-indigo-500/30'
                            : 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/30'
                        }`}>
                          {t.plan}
                        </span>
                        <span className="text-[10px] text-slate-400 flex items-center gap-1">
                          <Layers size={11} className="text-slate-500" />
                          {t.scaleType}
                        </span>
                      </div>
                    </td>

                    {/* Users Limit */}
                    <td className="px-4 py-4">
                      <div className="w-32">
                        <div className="flex justify-between text-[11px] mb-1">
                          <span className="font-bold text-white">{t.currentUsersCount}</span>
                          <span className="text-slate-500">/ {t.maxUsers}</span>
                        </div>
                        <div className="w-full bg-slate-800 h-1.5 rounded-full overflow-hidden">
                          <div 
                            className={`h-full rounded-full ${
                              t.currentUsersCount >= t.maxUsers ? 'bg-rose-500' : 'bg-cyan-400'
                            }`}
                            style={{ width: `${Math.min(100, (t.currentUsersCount / t.maxUsers) * 100)}%` }}
                          />
                        </div>
                      </div>
                    </td>

                    {/* Org Units count */}
                    <td className="px-4 py-4">
                      <span className="px-2.5 py-1 rounded-xl bg-slate-800/80 text-xs font-bold text-slate-300 border border-slate-700">
                        {t.orgUnitsCount} đơn vị
                      </span>
                    </td>

                    {/* Domain */}
                    <td className="px-4 py-4">
                      <div className="text-xs text-slate-300 flex items-center gap-1.5">
                        <Globe size={13} className="text-slate-500" />
                        <span>{t.customDomain || t.domain || 'Mặc định'}</span>
                      </div>
                    </td>

                    {/* Status */}
                    <td className="px-4 py-4">
                      <span className={`inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-[11px] font-bold ${
                        t.isActive 
                          ? 'bg-emerald-500/20 text-emerald-300 border border-emerald-500/30'
                          : 'bg-rose-500/20 text-rose-300 border border-rose-500/30'
                      }`}>
                        <span className={`w-1.5 h-1.5 rounded-full ${t.isActive ? 'bg-emerald-400' : 'bg-rose-400'}`} />
                        {t.isActive ? 'Hoạt động' : 'Tạm dừng'}
                      </span>
                    </td>

                    {/* Actions */}
                    <td className="px-6 py-4 text-right">
                      <div className="flex items-center justify-end gap-2">
                        <a
                          href="/m?app=kids"
                          target="_blank"
                          rel="noreferrer"
                          className="p-1.5 hover:bg-amber-500/20 text-amber-400 hover:text-amber-300 rounded-lg transition-colors cursor-pointer"
                          title="Mở ứng dụng Mobile kiểm chứng (AegisKids)"
                        >
                          <Smartphone size={16} />
                        </a>

                        <button
                          onClick={() => { setSelectedTenant({ ...t }); setIsEditModalOpen(true); }}
                          className="p-1.5 hover:bg-slate-800 text-slate-400 hover:text-white rounded-lg transition-colors cursor-pointer"
                          title="Chỉnh sửa cấu hình"
                        >
                          <Edit size={16} />
                        </button>

                        <button
                          onClick={() => handleToggleActive(t)}
                          className={`p-1.5 rounded-lg transition-colors cursor-pointer ${
                            t.isActive 
                              ? 'text-rose-400 hover:bg-rose-950/40' 
                              : 'text-emerald-400 hover:bg-emerald-950/40'
                          }`}
                          title={t.isActive ? 'Vô hiệu hóa khách thuê' : 'Kích hoạt lại'}
                        >
                          <Power size={16} />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        {totalPages > 1 && (
          <div className="px-6 py-4 border-t border-slate-800 flex items-center justify-between text-xs text-slate-400">
            <span>Trang {page} / {totalPages}</span>
            <div className="flex gap-2">
              <button
                disabled={page <= 1}
                onClick={() => setPage(p => p - 1)}
                className="px-3 py-1.5 bg-slate-800 hover:bg-slate-700 disabled:opacity-40 rounded-lg cursor-pointer"
              >
                Trước
              </button>
              <button
                disabled={page >= totalPages}
                onClick={() => setPage(p => p + 1)}
                className="px-3 py-1.5 bg-slate-800 hover:bg-slate-700 disabled:opacity-40 rounded-lg cursor-pointer"
              >
                Sau
              </button>
            </div>
          </div>
        )}
      </div>

      {/* MODAL: Tạo mới Khách thuê */}
      {isCreateModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm animate-fade-in">
          <div className="w-full max-w-2xl bg-slate-900 border border-slate-800 rounded-3xl p-6 md:p-8 shadow-2xl max-h-[90vh] overflow-y-auto">
            <h2 className="text-xl font-bold text-white flex items-center gap-2 mb-1">
              <Building2 className="text-cyan-400" size={22} />
              Khởi Tạo Khách Thuê Mới
            </h2>
            <p className="text-xs text-slate-400 mb-6">
              Hệ thống sẽ tự động tạo OU gốc "Hội sở chính" và phân bổ tài nguyên theo tiêu chuẩn Multi-Tenant.
            </p>

            <form onSubmit={handleCreateTenant} className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-slate-300 mb-1.5">Mã Khách Thuê (Code) *</label>
                  <input
                    type="text"
                    required
                    placeholder="vd: agribank, vnu, fpt"
                    value={formCode}
                    onChange={(e) => setFormCode(e.target.value.toLowerCase().replace(/[^a-z0-9_-]/g, ''))}
                    className="w-full px-4 py-2.5 bg-slate-950 border border-slate-800 rounded-xl text-sm text-white focus:outline-none focus:border-cyan-500 font-mono"
                  />
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-300 mb-1.5">Tên Tổ Chức / Doanh Nghiệp *</label>
                  <input
                    type="text"
                    required
                    placeholder="vd: Ngân hàng Agribank Việt Nam"
                    value={formName}
                    onChange={(e) => setFormName(e.target.value)}
                    className="w-full px-4 py-2.5 bg-slate-950 border border-slate-800 rounded-xl text-sm text-white focus:outline-none focus:border-cyan-500"
                  />
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-slate-300 mb-1.5">Gói Dịch Vụ (Plan)</label>
                  <select
                    value={formPlan}
                    onChange={(e) => setFormPlan(Number(e.target.value))}
                    className="w-full px-3.5 py-2.5 bg-slate-950 border border-slate-800 rounded-xl text-sm text-white focus:outline-none focus:border-cyan-500"
                  >
                    <option value={1}>Starter</option>
                    <option value={2}>Professional</option>
                    <option value={3}>Enterprise</option>
                    <option value={4}>Global</option>
                  </select>
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-300 mb-1.5">Quy Mô Cơ Cấu (Scale)</label>
                  <select
                    value={formScale}
                    onChange={(e) => setFormScale(Number(e.target.value))}
                    className="w-full px-3.5 py-2.5 bg-slate-950 border border-slate-800 rounded-xl text-sm text-white focus:outline-none focus:border-cyan-500"
                  >
                    <option value={1}>Individual (1-tier)</option>
                    <option value={2}>Team (Squad)</option>
                    <option value={3}>Classroom (Lớp học)</option>
                    <option value={4}>Organization (2-tier)</option>
                    <option value={5}>Enterprise (5-tier)</option>
                    <option value={6}>Global (Đa quốc gia)</option>
                  </select>
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-300 mb-1.5">Hạn Mức User Tối Đa</label>
                  <input
                    type="number"
                    min={10}
                    value={formMaxUsers}
                    onChange={(e) => setFormMaxUsers(Number(e.target.value))}
                    className="w-full px-4 py-2.5 bg-slate-950 border border-slate-800 rounded-xl text-sm text-white focus:outline-none focus:border-cyan-500"
                  />
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-slate-300 mb-1.5">Tên Miền Phụ (Subdomain)</label>
                  <input
                    type="text"
                    placeholder="agribank.aegisquiz.com"
                    value={formDomain}
                    onChange={(e) => setFormDomain(e.target.value)}
                    className="w-full px-4 py-2.5 bg-slate-950 border border-slate-800 rounded-xl text-sm text-white focus:outline-none focus:border-cyan-500"
                  />
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-300 mb-1.5">Màu Sắc Thương Hiệu</label>
                  <div className="flex items-center gap-3">
                    <input
                      type="color"
                      value={formPrimaryColor}
                      onChange={(e) => setFormPrimaryColor(e.target.value)}
                      className="w-10 h-10 rounded-xl bg-transparent border border-slate-800 cursor-pointer"
                    />
                    <span className="text-xs font-mono text-slate-300">{formPrimaryColor}</span>
                  </div>
                </div>
              </div>

              {/* [Metamorphic Engine] Lựa chọn biến thể giao diện Mobile */}
              <div>
                <label className="block text-xs font-semibold text-slate-300 mb-1.5 flex items-center gap-1.5">
                  <Smartphone size={13} className="text-amber-400" />
                  Định Dạng Trải Nghiệm Mobile (Metamorphic Engine)
                </label>
                <select
                  value={formMobileVariant}
                  onChange={(e) => {
                    const v = e.target.value;
                    setFormMobileVariant(v);
                    if (v === 'kids') setFormPrimaryColor('#f59e0b');
                    else if (v === 'driving') setFormPrimaryColor('#10b981');
                    else if (v === 'banking') setFormPrimaryColor('#0284c7');
                    else if (v === 'thpt') setFormPrimaryColor('#8b5cf6');
                    else if (v === 'med') setFormPrimaryColor('#e11d48');
                    else if (v === 'hse') setFormPrimaryColor('#ea580c');
                  }}
                  className="w-full px-3.5 py-2.5 bg-slate-950 border border-slate-800 rounded-xl text-xs text-white focus:outline-none focus:border-amber-400 cursor-pointer"
                >
                  <option value="kids">🌟 AegisKids — Bé Học Tiếng Anh (Gamified 3D, Bé Rồng Mascot, Sao Thưởng)</option>
                  <option value="driving">🚗 AegisDrive — Sát Hạch Lái Xe GPLX (60 Điểm Liệt & Sa Hình)</option>
                  <option value="banking">🏦 AegisBank — Khảo Thí Ngân Hàng (Tín Dụng & Basel II)</option>
                  <option value="thpt">🎓 Dehoc THPT — Luyện Thi Tốt Nghiệp & ĐGNL Quốc Gia</option>
                  <option value="med">🏥 AegisMed — Y Khoa & Dược Phẩm Lâm Sàng</option>
                  <option value="hse">🛡️ AegisSafety — An Toàn Lao Động & PCCC</option>
                </select>
              </div>

              {/* Cấp tài khoản Admin ban đầu */}
              <div className="pt-4 border-t border-slate-800">
                <h3 className="text-xs font-bold uppercase tracking-wider text-cyan-400 mb-3 flex items-center gap-1.5">
                  <Shield size={14} />
                  Khởi Tạo Quản Trị Viên TenantAdmin Đầu Tiên
                </h3>
                <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
                  <div>
                    <label className="block text-[11px] text-slate-400 mb-1">Họ Tên Admin</label>
                    <input
                      type="text"
                      placeholder="Nguyễn Văn A"
                      value={formAdminName}
                      onChange={(e) => setFormAdminName(e.target.value)}
                      className="w-full px-3.5 py-2 bg-slate-950 border border-slate-800 rounded-xl text-xs text-white focus:outline-none focus:border-cyan-500"
                    />
                  </div>
                  <div>
                    <label className="block text-[11px] text-slate-400 mb-1">Email Quản Trị</label>
                    <input
                      type="email"
                      placeholder="admin@agribank.com"
                      value={formAdminEmail}
                      onChange={(e) => setFormAdminEmail(e.target.value)}
                      className="w-full px-3.5 py-2 bg-slate-950 border border-slate-800 rounded-xl text-xs text-white focus:outline-none focus:border-cyan-500"
                    />
                  </div>
                  <div>
                    <label className="block text-[11px] text-slate-400 mb-1">Mật Khẩu Ban Đầu</label>
                    <input
                      type="password"
                      placeholder="••••••••"
                      value={formAdminPassword}
                      onChange={(e) => setFormAdminPassword(e.target.value)}
                      className="w-full px-3.5 py-2 bg-slate-950 border border-slate-800 rounded-xl text-xs text-white focus:outline-none focus:border-cyan-500"
                    />
                  </div>
                </div>
              </div>

              <div className="flex items-center justify-end gap-3 pt-6 border-t border-slate-800">
                <button
                  type="button"
                  onClick={() => setIsCreateModalOpen(false)}
                  className="px-5 py-2.5 rounded-xl border border-slate-800 hover:bg-slate-800 text-slate-300 font-semibold text-xs cursor-pointer"
                >
                  Hủy bỏ
                </button>
                <button
                  type="submit"
                  disabled={submitting}
                  className="px-6 py-2.5 rounded-xl bg-gradient-to-r from-cyan-500 to-blue-600 hover:from-cyan-400 hover:to-blue-500 disabled:opacity-50 text-white font-bold text-xs shadow-lg shadow-cyan-500/20 cursor-pointer"
                >
                  {submitting ? 'Đang khởi tạo...' : 'Xác Nhận Tạo Khách Thuê'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* MODAL: Chỉnh sửa Khách thuê */}
      {isEditModalOpen && selectedTenant && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm animate-fade-in">
          <div className="w-full max-w-lg bg-slate-900 border border-slate-800 rounded-3xl p-6 md:p-8 shadow-2xl">
            <h2 className="text-xl font-bold text-white flex items-center gap-2 mb-4">
              <Edit className="text-cyan-400" size={20} />
              Cập Nhật Khách Thuê: {selectedTenant.name}
            </h2>

            <form onSubmit={handleUpdateTenant} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold text-slate-300 mb-1.5">Tên Tổ Chức / Khách Thuê</label>
                <input
                  type="text"
                  required
                  value={selectedTenant.name}
                  onChange={(e) => setSelectedTenant({ ...selectedTenant, name: e.target.value })}
                  className="w-full px-4 py-2 bg-slate-950 border border-slate-800 rounded-xl text-sm text-white focus:outline-none focus:border-cyan-500"
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-slate-300 mb-1.5">Gói Thuê Bao</label>
                  <select
                    value={selectedTenant.plan}
                    onChange={(e) => setSelectedTenant({ ...selectedTenant, plan: e.target.value })}
                    className="w-full px-3.5 py-2 bg-slate-950 border border-slate-800 rounded-xl text-sm text-white focus:outline-none focus:border-cyan-500"
                  >
                    <option value="Starter">Starter</option>
                    <option value="Professional">Professional</option>
                    <option value="Enterprise">Enterprise</option>
                    <option value="Global">Global</option>
                  </select>
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-300 mb-1.5">Hạn Mức User</label>
                  <input
                    type="number"
                    min={10}
                    value={selectedTenant.maxUsers}
                    onChange={(e) => setSelectedTenant({ ...selectedTenant, maxUsers: Number(e.target.value) })}
                    className="w-full px-4 py-2 bg-slate-950 border border-slate-800 rounded-xl text-sm text-white focus:outline-none focus:border-cyan-500"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-300 mb-1.5">Custom Domain</label>
                <input
                  type="text"
                  placeholder="exam.agribank.com.vn"
                  value={selectedTenant.customDomain || ''}
                  onChange={(e) => setSelectedTenant({ ...selectedTenant, customDomain: e.target.value })}
                  className="w-full px-4 py-2 bg-slate-950 border border-slate-800 rounded-xl text-sm text-white focus:outline-none focus:border-cyan-500"
                />
              </div>

              <div className="flex items-center gap-3 pt-2">
                <input
                  type="checkbox"
                  id="tenantActive"
                  checked={selectedTenant.isActive}
                  onChange={(e) => setSelectedTenant({ ...selectedTenant, isActive: e.target.checked })}
                  className="w-4 h-4 rounded border-slate-800 text-cyan-500 focus:ring-cyan-500 bg-slate-950 cursor-pointer"
                />
                <label htmlFor="tenantActive" className="text-xs font-semibold text-slate-300 cursor-pointer">
                  Kích hoạt trạng thái hoạt động (Active)
                </label>
              </div>

              <div className="flex items-center justify-end gap-3 pt-6 border-t border-slate-800">
                <button
                  type="button"
                  onClick={() => setIsEditModalOpen(false)}
                  className="px-5 py-2.5 rounded-xl border border-slate-800 hover:bg-slate-800 text-slate-300 font-semibold text-xs cursor-pointer"
                >
                  Hủy bỏ
                </button>
                <button
                  type="submit"
                  disabled={submitting}
                  className="px-6 py-2.5 rounded-xl bg-gradient-to-r from-cyan-500 to-blue-600 hover:from-cyan-400 hover:to-blue-500 disabled:opacity-50 text-white font-bold text-xs shadow-lg shadow-cyan-500/20 cursor-pointer"
                >
                  {submitting ? 'Đang lưu...' : 'Lưu Thay Đổi'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
