import React, { useState, useEffect } from 'react';
import {
  X, Network, Building, GitFork, Plus, Edit2, Trash2, Check, CheckCircle2,
  ChevronRight, ChevronDown, Layers, ShieldCheck, Tag, Sparkles, RefreshCw, AlertCircle
} from 'lucide-react';
import { learnerQuizService } from '@/services/learner-quiz.service';
import { useAdaptiveTenant } from '@/hooks/useAdaptiveTenant';

interface TenantGovernanceModalProps {
  isOpen: boolean;
  onClose: () => void;
}

const UNIT_TYPES = [
  { value: 1, label: 'Hội sở chính / Ban Lãnh đạo (HeadOffice)', badge: 'bg-rose-500/20 text-rose-300 border-rose-500/30' },
  { value: 2, label: 'Khu vực / Phân hiệu / Vùng (Region)', badge: 'bg-amber-500/20 text-amber-300 border-amber-500/30' },
  { value: 3, label: 'Chi nhánh / Trường thành viên (Branch)', badge: 'bg-blue-500/20 text-blue-300 border-blue-500/30' },
  { value: 4, label: 'Phòng ban / Khoa chuyên môn (Department)', badge: 'bg-emerald-500/20 text-emerald-300 border-emerald-500/30' },
  { value: 5, label: 'Lớp học / Tổ nhóm nghiệp vụ (Classroom)', badge: 'bg-purple-500/20 text-purple-300 border-purple-500/30' },
];

export const TenantGovernanceModal: React.FC<TenantGovernanceModalProps> = ({ isOpen, onClose }) => {
  const { refreshDomains } = useAdaptiveTenant();
  const [activeTab, setActiveTab] = useState<'ORG_UNITS' | 'DOMAINS' | 'PRESETS'>('ORG_UNITS');

  // ── Tab 1: Organization Units State ──
  const [orgTree, setOrgTree] = useState<any[]>([]);
  const [loadingTree, setLoadingTree] = useState(false);
  const [expandedNodes, setExpandedNodes] = useState<Record<string, boolean>>({});
  const [showUnitModal, setShowUnitModal] = useState(false);
  const [selectedParentId, setSelectedParentId] = useState<string | null>(null);
  const [unitCode, setUnitCode] = useState('');
  const [unitName, setUnitName] = useState('');
  const [unitType, setUnitType] = useState<number>(3);
  const [unitEmail, setUnitEmail] = useState('');
  const [unitPhone, setUnitPhone] = useState('');

  // ── Tab 2: Dynamic Domains State ──
  const [domainCatalog, setDomainCatalog] = useState<any[]>([]);
  const [loadingCatalog, setLoadingCatalog] = useState(false);
  const [savingDomains, setSavingDomains] = useState(false);
  const [showAddDomainModal, setShowAddDomainModal] = useState(false);
  const [newDomainCode, setNewDomainCode] = useState('');
  const [newDomainName, setNewDomainName] = useState('');
  const [newDomainDesc, setNewDomainDesc] = useState('');
  const [newDomainIcon, setNewDomainIcon] = useState('Layers');

  // ── Tab 3: Presets State ──
  const [presets, setPresets] = useState<any[]>([]);
  const [loadingPresets, setLoadingPresets] = useState(false);
  const [presetDomain, setPresetDomain] = useState('BANKING');
  const [presetType, setPresetType] = useState('TARGET_LEVEL');
  const [presetLabel, setPresetLabel] = useState('');
  const [presetSynonyms, setPresetSynonyms] = useState('');

  const [toast, setToast] = useState<string | null>(null);

  const showNotification = (msg: string) => {
    setToast(msg);
    setTimeout(() => setToast(null), 3000);
  };

  // Load tree
  const loadTree = async () => {
    setLoadingTree(true);
    try {
      const tree = await learnerQuizService.getOrgUnitsTree();
      setOrgTree(tree);
      // Auto expand roots
      const exp: Record<string, boolean> = {};
      tree.forEach((node: any) => { exp[node.id] = true; });
      setExpandedNodes(exp);
    } catch {
      showNotification('Không thể tải cây tổ chức.');
    } finally {
      setLoadingTree(false);
    }
  };

  // Load domains catalog
  const loadCatalog = async () => {
    setLoadingCatalog(true);
    try {
      const cat = await learnerQuizService.getDomainCatalog();
      setDomainCatalog(cat);
    } catch {
      showNotification('Không thể tải danh mục ngành.');
    } finally {
      setLoadingCatalog(false);
    }
  };

  // Load presets
  const loadPresets = async () => {
    setLoadingPresets(true);
    try {
      const list = await learnerQuizService.getCoordinatePresets(presetDomain, presetType);
      setPresets(list);
    } catch {
      showNotification('Không thể tải bộ tọa độ.');
    } finally {
      setLoadingPresets(false);
    }
  };

  useEffect(() => {
    if (!isOpen) return;
    if (activeTab === 'ORG_UNITS') loadTree();
    else if (activeTab === 'DOMAINS') loadCatalog();
    else if (activeTab === 'PRESETS') loadPresets();
  }, [isOpen, activeTab, presetDomain, presetType]);

  const toggleExpand = (id: string) => {
    setExpandedNodes(prev => ({ ...prev, [id]: !prev[id] }));
  };

  // Create Unit
  const handleSaveUnit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!unitCode || !unitName) {
      alert('Vui lòng nhập Mã và Tên đơn vị');
      return;
    }
    try {
      await learnerQuizService.createOrgUnit({
        parentId: selectedParentId,
        code: unitCode,
        name: unitName,
        unitType: unitType,
        email: unitEmail,
        phoneNumber: unitPhone
      });
      showNotification('Đã thêm đơn vị tổ chức mới thành công!');
      setShowUnitModal(false);
      setUnitCode('');
      setUnitName('');
      setUnitEmail('');
      setUnitPhone('');
      loadTree();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Lỗi thêm đơn vị');
    }
  };

  // Toggle Domain Enablement
  const handleToggleDomain = (code: string) => {
    setDomainCatalog(prev => prev.map(d => d.code === code ? { ...d, isEnabled: !d.isEnabled } : d));
  };

  // Save Domain Configs
  const handleSaveDomainConfigs = async () => {
    setSavingDomains(true);
    try {
      const configs = domainCatalog.map(d => ({
        domainCode: d.code,
        isEnabled: d.isEnabled,
        customDisplayName: d.customDisplayName || undefined,
        displayOrder: d.displayOrder || 0
      }));
      await learnerQuizService.updateTenantDomainConfigs(configs);
      showNotification('Đã lưu cấu hình danh mục ngành của Tenant!');
      await refreshDomains();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Lỗi lưu cấu hình ngành');
    } finally {
      setSavingDomains(false);
    }
  };

  // Create Custom Domain
  const handleCreateDomain = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newDomainCode || !newDomainName) {
      alert('Vui lòng nhập Mã ngành và Tên ngành');
      return;
    }
    try {
      await learnerQuizService.createCustomDomain({
        code: newDomainCode,
        name: newDomainName,
        description: newDomainDesc,
        icon: newDomainIcon
      });
      showNotification('Đã tạo ngành đặc thù mới thành công!');
      setShowAddDomainModal(false);
      setNewDomainCode('');
      setNewDomainName('');
      setNewDomainDesc('');
      loadCatalog();
      await refreshDomains();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Lỗi tạo ngành mới');
    }
  };

  // Create Preset
  const handleCreatePreset = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!presetLabel) {
      alert('Vui lòng nhập Tên tọa độ');
      return;
    }
    try {
      const synonymsArray = presetSynonyms
        ? presetSynonyms.split(',').map(s => s.trim()).filter(Boolean)
        : [];

      await learnerQuizService.createCoordinatePreset({
        domainCode: presetDomain,
        coordinateType: presetType,
        presetLabel: presetLabel.trim(),
        synonymsJson: synonymsArray.length > 0 ? JSON.stringify(synonymsArray) : undefined
      });
      showNotification('Đã thêm tọa độ tri thức & từ điển đồng nghĩa!');
      setPresetLabel('');
      setPresetSynonyms('');
      loadPresets();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Lỗi thêm tọa độ');
    }
  };

  // Delete Preset
  const handleDeletePreset = async (id: string) => {
    if (!window.confirm('Bạn có chắc muốn xóa tọa độ này?')) return;
    try {
      await learnerQuizService.deleteCoordinatePreset(id);
      showNotification('Đã xóa tọa độ thành công');
      loadPresets();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Lỗi xóa tọa độ');
    }
  };

  if (!isOpen) return null;

  // Render tree recursive
  const renderTreeNode = (node: any, level: number = 0) => {
    const isExpanded = !!expandedNodes[node.id];
    const hasChildren = node.children && node.children.length > 0;
    const typeObj = UNIT_TYPES.find(t => t.value === (typeof node.unitType === 'number' ? node.unitType : 3)) || UNIT_TYPES[2];

    return (
      <div key={node.id} className="relative">
        <div
          className={`flex items-center justify-between p-2.5 rounded-xl border transition-all my-1 hover:bg-slate-800/60 ${
            level === 0 ? 'bg-slate-900/90 border-slate-700 shadow-sm' : 'bg-slate-950/40 border-slate-800/80'
          }`}
          style={{ marginLeft: `${level * 20}px` }}
        >
          <div className="flex items-center gap-2 min-w-0">
            {hasChildren ? (
              <button
                type="button"
                onClick={() => toggleExpand(node.id)}
                className="p-1 rounded hover:bg-slate-700 text-slate-400 hover:text-white transition-colors"
              >
                {isExpanded ? <ChevronDown size={15} /> : <ChevronRight size={15} />}
              </button>
            ) : (
              <span className="w-5" />
            )}

            <Building size={16} className={level === 0 ? 'text-amber-400' : 'text-blue-400'} />

            <div className="truncate">
              <span className="font-bold text-xs text-white mr-2">{node.name}</span>
              <span className="text-[10px] px-1.5 py-0.5 rounded bg-slate-800 text-slate-400 font-mono">
                {node.code}
              </span>
            </div>

            <span className={`text-[10px] px-2 py-0.5 rounded-full border ${typeObj.badge} hidden sm:inline-block`}>
              {typeObj.label.split(' ')[0]}
            </span>
          </div>

          <div className="flex items-center gap-1">
            <button
              type="button"
              onClick={() => {
                setSelectedParentId(node.id);
                setUnitType(Math.min((node.unitType || 1) + 1, 5));
                setShowUnitModal(true);
              }}
              className="px-2 py-1 bg-blue-600/30 hover:bg-blue-600/50 text-blue-300 rounded text-[11px] font-medium flex items-center gap-1 border border-blue-500/30"
              title="Thêm đơn vị con trực thuộc"
            >
              <Plus size={12} /> <span className="hidden sm:inline">Thêm con</span>
            </button>
          </div>
        </div>

        {hasChildren && isExpanded && (
          <div className="border-l border-slate-800/80 ml-4">
            {node.children.map((child: any) => renderTreeNode(child, level + 1))}
          </div>
        )}
      </div>
    );
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/75 backdrop-blur-md animate-in fade-in duration-200">
      <div className="relative w-full max-w-4xl bg-slate-900 border border-slate-800 rounded-2xl shadow-2xl overflow-hidden flex flex-col max-h-[90vh]">
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-slate-800 bg-gradient-to-r from-blue-900/30 via-purple-900/30 to-slate-900">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-blue-600 to-indigo-600 flex items-center justify-center text-white shadow-lg shadow-blue-500/20">
              <Network size={20} />
            </div>
            <div>
              <h2 className="text-base font-bold text-white flex items-center gap-2">
                Quản Trị Phân Tầng Đa Cấp & Ngành Nghề Động
                <span className="px-2 py-0.5 rounded-full text-[10px] font-extrabold uppercase bg-purple-500/20 text-purple-300 border border-purple-500/30">
                  Kỳ quan 16
                </span>
              </h2>
              <p className="text-xs text-slate-400">
                Tùy biến cây phân cấp đơn vị, cấu hình danh mục ngành và bộ tọa độ tri thức đặc thù
              </p>
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="p-2 rounded-xl text-slate-400 hover:text-white hover:bg-slate-800 transition-colors"
          >
            <X size={20} />
          </button>
        </div>

        {/* Navigation Tabs */}
        <div className="flex items-center gap-2 px-6 pt-3 border-b border-slate-800 bg-slate-950/40">
          <button
            type="button"
            onClick={() => setActiveTab('ORG_UNITS')}
            className={`pb-3 px-3 text-xs font-bold flex items-center gap-1.5 border-b-2 transition-all cursor-pointer ${
              activeTab === 'ORG_UNITS'
                ? 'border-blue-500 text-blue-400'
                : 'border-transparent text-slate-400 hover:text-slate-200'
            }`}
          >
            <GitFork size={14} /> 1. Cây Phân Cấp Đơn Vị (5-Tier Tree)
          </button>

          <button
            type="button"
            onClick={() => setActiveTab('DOMAINS')}
            className={`pb-3 px-3 text-xs font-bold flex items-center gap-1.5 border-b-2 transition-all cursor-pointer ${
              activeTab === 'DOMAINS'
                ? 'border-purple-500 text-purple-400'
                : 'border-transparent text-slate-400 hover:text-slate-200'
            }`}
          >
            <Layers size={14} /> 2. Danh Mục Ngành Động (Domains)
          </button>

          <button
            type="button"
            onClick={() => setActiveTab('PRESETS')}
            className={`pb-3 px-3 text-xs font-bold flex items-center gap-1.5 border-b-2 transition-all cursor-pointer ${
              activeTab === 'PRESETS'
                ? 'border-emerald-500 text-emerald-400'
                : 'border-transparent text-slate-400 hover:text-slate-200'
            }`}
          >
            <Tag size={14} /> 3. Bộ Tọa Độ & Từ Điển AI (Presets)
          </button>
        </div>

        {/* Content Body */}
        <div className="p-6 overflow-y-auto flex-1 space-y-4">
          {/* TAB 1: CÂY TỔ CHỨC */}
          {activeTab === 'ORG_UNITS' && (
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <div>
                  <h3 className="text-xs font-extrabold uppercase tracking-wider text-slate-300">
                    Cấu Trúc Cây Phân Cấp Đơn Vị Trực Thuộc
                  </h3>
                  <p className="text-[11px] text-slate-400">
                    Phục vụ phân vùng ngân hàng đề thi và phân tầng quản trị (Hội sở, Chi nhánh, Phòng ban)
                  </p>
                </div>
                <div className="flex items-center gap-2">
                  <button
                    type="button"
                    onClick={loadTree}
                    className="p-1.5 bg-slate-800 hover:bg-slate-700 text-slate-300 rounded-lg text-xs flex items-center gap-1"
                  >
                    <RefreshCw size={13} className={loadingTree ? 'animate-spin' : ''} />
                  </button>
                  <button
                    type="button"
                    onClick={() => {
                      setSelectedParentId(null);
                      setUnitType(1);
                      setShowUnitModal(true);
                    }}
                    className="px-3 py-1.5 bg-blue-600 hover:bg-blue-500 text-white rounded-xl text-xs font-bold flex items-center gap-1.5 shadow-md shadow-blue-900/30"
                  >
                    <Plus size={14} /> Thêm Đơn Vị Gốc (HQ)
                  </button>
                </div>
              </div>

              {loadingTree ? (
                <div className="py-12 text-center text-xs text-slate-500">Đang tải cây phân cấp...</div>
              ) : orgTree.length === 0 ? (
                <div className="p-8 border border-dashed border-slate-800 rounded-2xl text-center space-y-3">
                  <Building size={32} className="mx-auto text-slate-600" />
                  <div className="text-xs font-bold text-slate-300">Chưa thiết lập cây tổ chức</div>
                  <p className="text-[11px] text-slate-500 max-w-md mx-auto">
                    Tổ chức của bạn đang ở chế độ phẳng. Hãy bấm <b>Thêm Đơn Vị Gốc</b> để phân nhánh Hội sở, Chi nhánh và Phòng ban.
                  </p>
                </div>
              ) : (
                <div className="space-y-1">{orgTree.map(root => renderTreeNode(root, 0))}</div>
              )}
            </div>
          )}

          {/* TAB 2: QUẢN LÝ NGÀNH ĐỘNG */}
          {activeTab === 'DOMAINS' && (
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <div>
                  <h3 className="text-xs font-extrabold uppercase tracking-wider text-slate-300">
                    Cấu Hình Ngành Nghề Cho Phép Tại Tổ Chức
                  </h3>
                  <p className="text-[11px] text-slate-400">
                    Chỉ những ngành được BẬT mới xuất hiện trong các modal tạo câu hỏi và đề thi
                  </p>
                </div>
                <div className="flex items-center gap-2">
                  <button
                    type="button"
                    onClick={() => setShowAddDomainModal(true)}
                    className="px-3 py-1.5 bg-purple-600 hover:bg-purple-500 text-white rounded-xl text-xs font-bold flex items-center gap-1.5 shadow-md shadow-purple-900/30"
                  >
                    <Plus size={14} /> Tạo Ngành Đặc Thù Mới
                  </button>
                  <button
                    type="button"
                    onClick={handleSaveDomainConfigs}
                    disabled={savingDomains}
                    className="px-4 py-1.5 bg-emerald-600 hover:bg-emerald-500 text-white rounded-xl text-xs font-bold flex items-center gap-1.5 shadow-md shadow-emerald-900/30 disabled:opacity-50"
                  >
                    <Check size={14} /> {savingDomains ? 'Đang lưu...' : 'Lưu Thay Đổi'}
                  </button>
                </div>
              </div>

              {loadingCatalog ? (
                <div className="py-12 text-center text-xs text-slate-500">Đang tải danh mục ngành...</div>
              ) : (
                <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                  {domainCatalog.map((dom: any) => (
                    <div
                      key={dom.code}
                      className={`p-3.5 rounded-xl border transition-all flex items-start justify-between ${
                        dom.isEnabled
                          ? 'bg-slate-900/90 border-purple-500/40 shadow-sm'
                          : 'bg-slate-950/40 border-slate-800 opacity-60'
                      }`}
                    >
                      <div className="flex items-start gap-3">
                        <div
                          className="w-9 h-9 rounded-xl flex items-center justify-center text-white font-bold text-sm shrink-0"
                          style={{ backgroundColor: dom.colorBadge || '#2563eb' }}
                        >
                          {dom.code.slice(0, 2)}
                        </div>
                        <div>
                          <div className="flex items-center gap-2">
                            <span className="font-bold text-xs text-white">{dom.name}</span>
                            <span className="text-[10px] px-1.5 py-0.5 rounded bg-slate-800 text-slate-400 font-mono">
                              {dom.code}
                            </span>
                          </div>
                          <p className="text-[11px] text-slate-400 mt-1 line-clamp-2">{dom.description}</p>
                          <div className="mt-2 flex items-center gap-2">
                            <span className="text-[10px] px-2 py-0.5 rounded bg-slate-800/80 text-slate-300">
                              {dom.isSystemStandard ? '🌐 Chuẩn Toàn Cầu' : '🏢 Ngành Riêng Tenant'}
                            </span>
                          </div>
                        </div>
                      </div>

                      <button
                        type="button"
                        onClick={() => handleToggleDomain(dom.code)}
                        className={`px-3 py-1 rounded-full text-xs font-bold transition-all ${
                          dom.isEnabled
                            ? 'bg-emerald-500/20 text-emerald-300 border border-emerald-500/40'
                            : 'bg-slate-800 text-slate-400 border border-slate-700'
                        }`}
                      >
                        {dom.isEnabled ? 'ĐANG BẬT' : 'TẮT'}
                      </button>
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}

          {/* TAB 3: BỘ TỌA ĐỘ TRI THỨC & TỪ ĐIỂN AI */}
          {activeTab === 'PRESETS' && (
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <div>
                  <h3 className="text-xs font-extrabold uppercase tracking-wider text-slate-300">
                    Bộ Tọa Độ Tri Thức & Từ Điển Nhận Diện AI
                  </h3>
                  <p className="text-[11px] text-slate-400">
                    Định nghĩa cấp bậc, cơ quan ban hành và chuẩn đánh giá kèm từ đồng nghĩa để AI nhận diện tự động khi import
                  </p>
                </div>
              </div>

              {/* Filters */}
              <div className="flex flex-wrap items-center gap-3 p-3 bg-slate-950/60 rounded-xl border border-slate-800">
                <div className="flex items-center gap-2">
                  <span className="text-xs text-slate-400">Ngành:</span>
                  <select
                    value={presetDomain}
                    onChange={e => setPresetDomain(e.target.value)}
                    className="bg-slate-900 border border-slate-700 rounded-lg px-2.5 py-1 text-xs text-white font-bold"
                  >
                    <option value="BANKING">Tài chính - Ngân hàng</option>
                    <option value="EDUCATION">Giáo dục & Học thuật</option>
                    <option value="HEALTHCARE">Y tế - Sức khỏe</option>
                    <option value="IT_SECURITY">An toàn TT & CNTT</option>
                    <option value="HSE">An toàn LĐ & Môi trường</option>
                  </select>
                </div>

                <div className="flex items-center gap-2">
                  <span className="text-xs text-slate-400">Trục tọa độ:</span>
                  <select
                    value={presetType}
                    onChange={e => setPresetType(e.target.value)}
                    className="bg-slate-900 border border-slate-700 rounded-lg px-2.5 py-1 text-xs text-white font-bold"
                  >
                    <option value="TARGET_LEVEL">Cấp bậc / Trình độ (Target Level)</option>
                    <option value="ISSUING_ORG">Cơ quan / Ban chuyên môn (Issuing Org)</option>
                    <option value="BENCHMARK_STANDARD">Tiêu chuẩn / Quy chuẩn (Standard)</option>
                    <option value="ASSESSMENT_PURPOSE">Mục đích sát hạch (Purpose)</option>
                  </select>
                </div>
              </div>

              {/* Form Add Preset */}
              <form onSubmit={handleCreatePreset} className="p-4 bg-slate-950/40 border border-slate-800 rounded-xl space-y-3">
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  <div>
                    <label className="block text-[11px] font-bold text-slate-400 mb-1">Tên Tọa Độ / Chức danh</label>
                    <input
                      type="text"
                      value={presetLabel}
                      onChange={e => setPresetLabel(e.target.value)}
                      placeholder="e.g. Tín dụng Doanh nghiệp, GDV, Bác sĩ CKI..."
                      className="w-full px-3 py-1.5 bg-slate-900 border border-slate-700 rounded-lg text-xs text-white"
                    />
                  </div>
                  <div>
                    <label className="block text-[11px] font-bold text-slate-400 mb-1">Từ đồng nghĩa cho AI (cách nhau dấu phẩy)</label>
                    <input
                      type="text"
                      value={presetSynonyms}
                      onChange={e => setPresetSynonyms(e.target.value)}
                      placeholder="e.g. KHDN, TDDN, Corporate Credit..."
                      className="w-full px-3 py-1.5 bg-slate-900 border border-slate-700 rounded-lg text-xs text-white"
                    />
                  </div>
                </div>
                <div className="flex justify-end">
                  <button
                    type="submit"
                    className="px-4 py-1.5 bg-emerald-600 hover:bg-emerald-500 text-white rounded-xl text-xs font-bold flex items-center gap-1.5"
                  >
                    <Plus size={14} /> Thêm Tọa Độ Mới
                  </button>
                </div>
              </form>

              {/* Presets List */}
              {loadingPresets ? (
                <div className="py-8 text-center text-xs text-slate-500">Đang tải danh sách...</div>
              ) : presets.length === 0 ? (
                <div className="py-8 text-center text-xs text-slate-500">Chưa có tọa độ nào được cấu hình cho mục này.</div>
              ) : (
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                  {presets.map((p: any) => {
                    let synList: string[] = [];
                    try {
                      if (p.synonymsJson) synList = JSON.parse(p.synonymsJson);
                    } catch {}

                    return (
                      <div key={p.id} className="p-3 bg-slate-900/80 border border-slate-800 rounded-xl flex items-center justify-between">
                        <div>
                          <div className="font-bold text-xs text-white">{p.presetLabel}</div>
                          {synList.length > 0 && (
                            <div className="flex flex-wrap gap-1 mt-1">
                              {synList.map((s, idx) => (
                                <span key={idx} className="text-[10px] px-1.5 py-0.2 rounded bg-purple-950/60 text-purple-300 border border-purple-800/40">
                                  {s}
                                </span>
                              ))}
                            </div>
                          )}
                        </div>
                        <button
                          type="button"
                          onClick={() => handleDeletePreset(p.id)}
                          className="p-1.5 text-slate-400 hover:text-rose-400 transition-colors"
                          title="Xóa tọa độ"
                        >
                          <Trash2 size={14} />
                        </button>
                      </div>
                    );
                  })}
                </div>
              )}
            </div>
          )}
        </div>

        {/* Toast Notification */}
        {toast && (
          <div className="absolute bottom-4 right-4 bg-blue-600 text-white px-4 py-2 rounded-xl text-xs font-bold shadow-xl flex items-center gap-2 animate-in slide-in-from-bottom">
            <CheckCircle2 size={16} /> {toast}
          </div>
        )}
      </div>

      {/* MODAL: THÊM ĐƠN VỊ TỔ CHỨC */}
      {showUnitModal && (
        <div className="fixed inset-0 z-60 flex items-center justify-center p-4 bg-black/80">
          <div className="bg-slate-900 border border-slate-800 rounded-2xl max-w-md w-full p-6 space-y-4 shadow-2xl">
            <h3 className="text-sm font-bold text-white flex items-center gap-2">
              <Building size={16} className="text-blue-400" /> Thêm Đơn Vị Tổ Chức Mới
            </h3>

            <form onSubmit={handleSaveUnit} className="space-y-3">
              <div>
                <label className="block text-[11px] font-bold text-slate-400 mb-1">Cấp Đơn Vị</label>
                <select
                  value={unitType}
                  onChange={e => setUnitType(Number(e.target.value))}
                  className="w-full bg-slate-950 border border-slate-700 rounded-lg px-3 py-2 text-xs text-white"
                >
                  {UNIT_TYPES.map(t => (
                    <option key={t.value} value={t.value}>{t.label}</option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-[11px] font-bold text-slate-400 mb-1">Mã Đơn Vị (Viết liền, không dấu)</label>
                <input
                  type="text"
                  value={unitCode}
                  onChange={e => setUnitCode(e.target.value.toUpperCase())}
                  placeholder="e.g. CN_DONGDA, KHOA_CNTT"
                  className="w-full bg-slate-950 border border-slate-700 rounded-lg px-3 py-2 text-xs text-white font-mono"
                  required
                />
              </div>

              <div>
                <label className="block text-[11px] font-bold text-slate-400 mb-1">Tên Đơn Vị</label>
                <input
                  type="text"
                  value={unitName}
                  onChange={e => setUnitName(e.target.value)}
                  placeholder="e.g. Chi nhánh Đống Đa, Khoa Công nghệ Thông tin"
                  className="w-full bg-slate-950 border border-slate-700 rounded-lg px-3 py-2 text-xs text-white"
                  required
                />
              </div>

              <div className="flex justify-end gap-2 pt-2">
                <button
                  type="button"
                  onClick={() => setShowUnitModal(false)}
                  className="px-3 py-1.5 bg-slate-800 text-slate-300 rounded-xl text-xs"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  className="px-4 py-1.5 bg-blue-600 hover:bg-blue-500 text-white rounded-xl text-xs font-bold shadow-md shadow-blue-900/30"
                >
                  Lưu Đơn Vị
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* MODAL: THÊM NGÀNH ĐẶC THÙ */}
      {showAddDomainModal && (
        <div className="fixed inset-0 z-60 flex items-center justify-center p-4 bg-black/80">
          <div className="bg-slate-900 border border-slate-800 rounded-2xl max-w-md w-full p-6 space-y-4 shadow-2xl">
            <h3 className="text-sm font-bold text-white flex items-center gap-2">
              <Layers size={16} className="text-purple-400" /> Tạo Ngành / Tiểu Ngành Đặc Thù
            </h3>

            <form onSubmit={handleCreateDomain} className="space-y-3">
              <div>
                <label className="block text-[11px] font-bold text-slate-400 mb-1">Mã Ngành (Code)</label>
                <input
                  type="text"
                  value={newDomainCode}
                  onChange={e => setNewDomainCode(e.target.value.toUpperCase())}
                  placeholder="e.g. AGRI_CREDIT, CARDIOLOGY"
                  className="w-full bg-slate-950 border border-slate-700 rounded-lg px-3 py-2 text-xs text-white font-mono"
                  required
                />
              </div>

              <div>
                <label className="block text-[11px] font-bold text-slate-400 mb-1">Tên Ngành Nghề</label>
                <input
                  type="text"
                  value={newDomainName}
                  onChange={e => setNewDomainName(e.target.value)}
                  placeholder="e.g. Tín dụng Nông nghiệp Nông thôn"
                  className="w-full bg-slate-950 border border-slate-700 rounded-lg px-3 py-2 text-xs text-white"
                  required
                />
              </div>

              <div>
                <label className="block text-[11px] font-bold text-slate-400 mb-1">Mô Tả Chuyên Môn</label>
                <textarea
                  value={newDomainDesc}
                  onChange={e => setNewDomainDesc(e.target.value)}
                  placeholder="Mô tả phạm vi nghiệp vụ và chuẩn kiến thức..."
                  className="w-full bg-slate-950 border border-slate-700 rounded-lg px-3 py-2 text-xs text-white h-20"
                />
              </div>

              <div className="flex justify-end gap-2 pt-2">
                <button
                  type="button"
                  onClick={() => setShowAddDomainModal(false)}
                  className="px-3 py-1.5 bg-slate-800 text-slate-300 rounded-xl text-xs"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  className="px-4 py-1.5 bg-purple-600 hover:bg-purple-500 text-white rounded-xl text-xs font-bold shadow-md shadow-purple-900/30"
                >
                  Tạo Ngành Mới
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
