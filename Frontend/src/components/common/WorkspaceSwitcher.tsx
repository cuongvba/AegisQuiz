import { useState, useEffect, useRef } from 'react';
import { Building2, User, ChevronDown, Check, ArrowRightLeft, Sparkles, Shield, Plus } from 'lucide-react';
import { apiClient } from '@/shared/api/client';
import { useAuthContext } from '@/app/providers/AuthProvider';
import { createLogger } from '@/shared/logger';
const logger = createLogger('WorkspaceSwitcher');


export interface WorkspaceItem {
  tenantId: string;
  tenantCode: string;
  tenantName: string;
  logoUrl?: string | null;
  primaryColor?: string | null;
  role: string;
  isPersonal: boolean;
  isCurrent: boolean;
}

export function WorkspaceSwitcher() {
  const { user } = useAuthContext();
  const [workspaces, setWorkspaces] = useState<WorkspaceItem[]>([]);
  const [isOpen, setIsOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [switching, setSwitching] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  // Tải danh sách không gian
  const fetchWorkspaces = async () => {
    if (!user) return;
    setLoading(true);
    try {
      const res = await apiClient.get<WorkspaceItem[]>('/api/auth/workspaces');
      if (Array.isArray(res.data)) {
        setWorkspaces(res.data);
      }
    } catch (err) {
      logger.warn('Không thể lấy danh sách workspaces:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchWorkspaces();
  }, [user?.tenantId]);

  // Click outside to close
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  // Chuyển đổi Workspace 1-chạm
  const handleSwitchWorkspace = async (targetTenantId: string) => {
    if (switching) return;
    setSwitching(true);
    try {
      const res = await apiClient.post<{ token: string; user: any; activeWorkspace: any }>(
        '/api/auth/switch-workspace',
        { targetTenantId }
      );

      if (res.data?.token) {
        localStorage.setItem('auth_token', res.data.token);
        if (res.data.user) {
          localStorage.setItem('auth_user', JSON.stringify(res.data.user));
        }
        // Làm mới trang tức thì với ngữ cảnh bảo mật mới
        window.location.reload();
      }
    } catch (err: any) {
      alert(err?.response?.data?.message || 'Không thể chuyển đổi không gian làm việc.');
      setSwitching(false);
    }
  };

  const currentWs = workspaces.find(w => w.isCurrent) || {
    tenantId: user?.tenantId || '',
    tenantCode: user?.tenantCode || 'DEFAULT',
    tenantName: user?.tenantName || 'Tổ chức',
    role: user?.role || 'Learner',
    isPersonal: false,
    isCurrent: true
  };

  return (
    <div className="relative select-none" ref={dropdownRef}>
      {/* Switcher Trigger Button */}
      <button
        type="button"
        onClick={() => {
          if (!isOpen) fetchWorkspaces();
          setIsOpen(!isOpen);
        }}
        className="flex items-center gap-2 px-3 py-1.5 rounded-xl bg-slate-900/90 hover:bg-slate-800 border border-slate-800 hover:border-cyan-500/40 text-left transition-all cursor-pointer shadow-sm group"
        title="Chuyển đổi không gian làm việc (Workspace Switcher)"
      >
        <div className={`w-7 h-7 rounded-lg flex items-center justify-center shrink-0 border ${
          currentWs.isPersonal 
            ? 'bg-emerald-500/10 text-emerald-400 border-emerald-500/30' 
            : 'bg-cyan-500/10 text-cyan-400 border-cyan-500/30'
        }`}>
          {currentWs.isPersonal ? <User size={14} /> : <Building2 size={14} />}
        </div>

        <div className="min-w-0 max-w-[140px] sm:max-w-[180px]">
          <p className="text-[10px] text-slate-400 font-bold uppercase tracking-wider truncate flex items-center gap-1">
            <span>{currentWs.isPersonal ? 'Cá nhân' : 'Doanh nghiệp'}</span>
            <span className="text-[9px] text-cyan-400 font-mono">({currentWs.role})</span>
          </p>
          <p className="text-xs font-black text-white truncate leading-tight">
            {currentWs.tenantName}
          </p>
        </div>

        <ChevronDown size={14} className={`text-slate-400 group-hover:text-cyan-300 transition-transform duration-200 ${isOpen ? 'rotate-180' : ''}`} />
      </button>

      {/* Dropdown Menu */}
      {isOpen && (
        <div className="absolute left-0 mt-2 w-72 sm:w-80 rounded-2xl bg-slate-900/95 border border-slate-800 shadow-2xl backdrop-blur-xl p-2 z-50 animate-in fade-in slide-in-from-top-2 duration-150">
          <div className="p-2.5 pb-2 border-b border-slate-800/80">
            <div className="flex items-center justify-between">
              <span className="text-xs font-bold text-slate-300 uppercase tracking-wider flex items-center gap-1.5">
                <ArrowRightLeft size={13} className="text-cyan-400" />
                Không Gian Làm Việc
              </span>
              <span className="text-[10px] px-1.5 py-0.5 rounded bg-cyan-500/10 text-cyan-300 font-mono">
                {workspaces.length} Không gian
              </span>
            </div>
            <p className="text-[11px] text-slate-400 mt-0.5">
              Chuyển đổi tức thì giữa các tổ chức và không gian cá nhân
            </p>
          </div>

          {/* List of Workspaces */}
          <div className="max-h-64 overflow-y-auto py-1 space-y-1">
            {loading ? (
              <div className="p-4 text-center text-xs text-slate-500">Đang tải không gian...</div>
            ) : workspaces.length === 0 ? (
              <div className="p-4 text-center text-xs text-slate-500">Chưa có không gian phụ thuộc.</div>
            ) : (
              workspaces.map(ws => {
                const isSelected = ws.isCurrent;
                return (
                  <button
                    key={ws.tenantId}
                    type="button"
                    disabled={isSelected || switching}
                    onClick={() => handleSwitchWorkspace(ws.tenantId)}
                    className={`w-full flex items-center justify-between p-2.5 rounded-xl text-left transition-all cursor-pointer ${
                      isSelected
                        ? 'bg-cyan-500/10 border border-cyan-500/30'
                        : 'hover:bg-slate-800/70 border border-transparent'
                    }`}
                  >
                    <div className="flex items-center gap-2.5 min-w-0 flex-1">
                      <div className={`w-8 h-8 rounded-xl flex items-center justify-center shrink-0 border ${
                        ws.isPersonal
                          ? 'bg-emerald-500/10 text-emerald-400 border-emerald-500/30'
                          : 'bg-blue-500/10 text-blue-400 border-blue-500/30'
                      }`}>
                        {ws.isPersonal ? <User size={15} /> : <Building2 size={15} />}
                      </div>

                      <div className="min-w-0 flex-1">
                        <div className="flex items-center gap-1.5">
                          <p className="text-xs font-bold text-white truncate">{ws.tenantName}</p>
                          {ws.isPersonal && (
                            <span className="text-[9px] font-bold px-1.5 py-0.2 rounded bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">
                              Cá nhân
                            </span>
                          )}
                        </div>
                        <p className="text-[10px] text-slate-400 font-mono truncate">
                          Mã: {ws.tenantCode} • Quyền: <span className="text-cyan-300">{ws.role}</span>
                        </p>
                      </div>
                    </div>

                    {isSelected ? (
                      <Check size={16} className="text-cyan-400 shrink-0 ml-2" />
                    ) : (
                      <ArrowRightLeft size={13} className="text-slate-600 group-hover:text-slate-400 shrink-0 ml-2" />
                    )}
                  </button>
                );
              })
            )}
          </div>

          {/* Footer actions */}
          <div className="pt-2 mt-1 border-t border-slate-800/80 p-1 flex items-center justify-between text-[11px] text-slate-400">
            <span className="flex items-center gap-1 text-slate-400">
              <Shield size={12} className="text-emerald-400" />
              Chủ quyền dữ liệu cách ly
            </span>
          </div>
        </div>
      )}
    </div>
  );
}

