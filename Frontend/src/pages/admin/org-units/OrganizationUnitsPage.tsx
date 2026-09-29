import { useState, useEffect, useCallback, useMemo } from 'react';
import { 
  Network, Plus, Search, ChevronRight, ChevronDown, 
  FolderTree, Building2, MapPin, GitBranch, Users, 
  Edit, ArrowRightLeft, Trash2, CheckCircle2, AlertCircle, 
  RefreshCw, Layers, Shield, FolderPlus, HelpCircle
} from 'lucide-react';
import { apiClient } from '@/shared/api/client';
import { useAuthContext } from '@/app/providers/AuthProvider';

export interface TenantOption {
  id: string;
  code: string;
  name: string;
  plan: string;
  maxUsers: number;
  currentUsersCount: number;
  isActive: boolean;
}

export interface OrgUnitNode {
  id: string;
  code: string;
  name: string;
  unitType: number | string;
  unitTypeValue?: number;
  depthLevel?: number;
  taxonomyCategory?: string | null;
  parentId?: string | null;
  hierarchyPath: string;
  sortOrder: number;
  isActive: boolean;
  userCount: number;
  children: OrgUnitNode[];
}

export interface OrgUnitFlatItem {
  id: string;
  code: string;
  name: string;
  unitType: number | string;
  unitTypeValue?: number;
  depthLevel?: number;
  taxonomyCategory?: string | null;
  parentId?: string | null;
  hierarchyPath: string;
  sortOrder: number;
  isActive: boolean;
  userCount: number;
}

const UNIT_TYPE_CONFIG: Record<number, { label: string; badgeClass: string; icon: any }> = {
  0: {
    label: 'Tập đoàn mẹ / Holding Corp',
    badgeClass: 'bg-indigo-500/10 text-indigo-400 border-indigo-500/30',
    icon: Building2
  },
  1: {
    label: 'Hội sở chính (HQ)',
    badgeClass: 'bg-purple-500/10 text-purple-400 border-purple-500/30',
    icon: Building2
  },
  2: {
    label: 'Khu vực / Vùng (Region / APAC / EMEA)',
    badgeClass: 'bg-blue-500/10 text-blue-400 border-blue-500/30',
    icon: MapPin
  },
  3: {
    label: 'Chi nhánh / Khối / OpCo (Branch)',
    badgeClass: 'bg-cyan-500/10 text-cyan-400 border-cyan-500/30',
    icon: GitBranch
  },
  4: {
    label: 'Phòng ban / Khoa / Trung tâm (Department)',
    badgeClass: 'bg-amber-500/10 text-amber-400 border-amber-500/30',
    icon: FolderTree
  },
  5: {
    label: 'Lớp học / Tổ nhóm / Nhóm dự án (Classroom)',
    badgeClass: 'bg-emerald-500/10 text-emerald-400 border-emerald-500/30',
    icon: Users
  },
  6: {
    label: 'Khối kinh doanh / Ngành hàng (Sector)',
    badgeClass: 'bg-violet-500/10 text-violet-400 border-violet-500/30',
    icon: Layers
  },
  7: {
    label: 'Ban chuyên môn / Khối chức năng (Division)',
    badgeClass: 'bg-sky-500/10 text-sky-400 border-sky-500/30',
    icon: GitBranch
  },
  8: {
    label: 'Bộ phận / Phòng chức năng (Section)',
    badgeClass: 'bg-yellow-500/10 text-yellow-400 border-yellow-500/30',
    icon: FolderTree
  },
  9: {
    label: 'Đội Agile / Nhóm đặc nhiệm (Squad / Taskforce)',
    badgeClass: 'bg-rose-500/10 text-rose-400 border-rose-500/30',
    icon: Users
  },
  99: {
    label: 'Đơn vị tùy chỉnh (Custom Taxonomy)',
    badgeClass: 'bg-teal-500/10 text-teal-400 border-teal-500/30',
    icon: Shield
  }
};

export function OrganizationUnitsPage() {
  const { user } = useAuthContext();
  const isSystemAdmin = user?.role === 'SystemAdmin';
  const [tenantsList, setTenantsList] = useState<TenantOption[]>([]);
  const [selectedTenantId, setSelectedTenantId] = useState<string>('');

  const [treeData, setTreeData] = useState<OrgUnitNode[]>([]);
  const [flatData, setFlatData] = useState<OrgUnitFlatItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [expandedIds, setExpandedIds] = useState<Set<string>>(new Set());

  // Modals state
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [isMoveModalOpen, setIsMoveModalOpen] = useState(false);
  const [selectedNode, setSelectedNode] = useState<OrgUnitNode | null>(null);
  const [parentPresetId, setParentPresetId] = useState<string | null>(null);

  // Form states
  const [formCode, setFormCode] = useState('');
  const [formName, setFormName] = useState('');
  const [formUnitType, setFormUnitType] = useState(4); // Default to Department
  const [formTaxonomyCategory, setFormTaxonomyCategory] = useState('');
  const [formParentId, setFormParentId] = useState<string>('');
  const [formSortOrder, setFormSortOrder] = useState(0);
  const [formIsActive, setFormIsActive] = useState(true);
  const [moveTargetParentId, setMoveTargetParentId] = useState<string>('');

  const [submitting, setSubmitting] = useState(false);
  const [feedbackMsg, setFeedbackMsg] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  // Load Tenants List for SysAdmin
  useEffect(() => {
    if (isSystemAdmin) {
      apiClient.get<TenantOption[]>('/api/admin/tenants')
        .then(res => {
          if (Array.isArray(res.data) && res.data.length > 0) {
            setTenantsList(res.data);
            setSelectedTenantId(prev => prev || user?.tenantId || res.data[0].id);
          }
        })
        .catch(err => console.error('Lỗi khi tải danh sách khách thuê:', err));
    }
  }, [isSystemAdmin, user?.tenantId]);

  // Auto clear feedback after 5s
  useEffect(() => {
    if (feedbackMsg) {
      const timer = setTimeout(() => setFeedbackMsg(null), 5000);
      return () => clearTimeout(timer);
    }
  }, [feedbackMsg]);

  // Helper: Dựng cây từ danh sách phẳng (fallback)
  const buildTreeFromFlats = (items: OrgUnitFlatItem[]): OrgUnitNode[] => {
    const nodeMap = new Map<string, OrgUnitNode>();
    items.forEach(it => {
      nodeMap.set(it.id, { ...it, children: [] });
    });
    const roots: OrgUnitNode[] = [];
    items.forEach(it => {
      const node = nodeMap.get(it.id)!;
      if (it.parentId && nodeMap.has(it.parentId)) {
        nodeMap.get(it.parentId)!.children.push(node);
      } else {
        roots.push(node);
      }
    });
    return roots;
  };

  // Helper: Trải phẳng cây (fallback)
  const flattenTreeNodes = (nodes: OrgUnitNode[]): OrgUnitFlatItem[] => {
    const result: OrgUnitFlatItem[] = [];
    const traverse = (items: OrgUnitNode[]) => {
      for (const item of items) {
        result.push({
          id: item.id,
          code: item.code,
          name: item.name,
          unitType: item.unitType,
          unitTypeValue: item.unitTypeValue,
          depthLevel: item.depthLevel,
          taxonomyCategory: item.taxonomyCategory,
          parentId: item.parentId,
          hierarchyPath: item.hierarchyPath,
          sortOrder: item.sortOrder,
          isActive: item.isActive,
          userCount: item.userCount || 0
        });
        if (item.children && item.children.length > 0) {
          traverse(item.children);
        }
      }
    };
    traverse(nodes);
    return result;
  };

  // Load Tree & Flat Data
  const loadData = useCallback(async (tenantIdOverride?: string) => {
    setLoading(true);
    try {
      let trees: OrgUnitNode[] = [];
      let flats: OrgUnitFlatItem[] = [];

      const targetTenant = tenantIdOverride !== undefined ? tenantIdOverride : selectedTenantId;
      const queryParam = targetTenant ? `?tenantId=${targetTenant}` : '';

      try {
        const treeRes = await apiClient.get<OrgUnitNode[]>(`/api/quiz/org-units/tree${queryParam}`);
        if (Array.isArray(treeRes.data)) trees = treeRes.data;
      } catch (e) {
        console.warn('Could not fetch tree directly, will fallback from flat', e);
      }

      try {
        const flatRes = await apiClient.get<OrgUnitFlatItem[]>(`/api/quiz/org-units/flat${queryParam}`);
        if (Array.isArray(flatRes.data)) flats = flatRes.data;
      } catch {
        try {
          const flatRes2 = await apiClient.get<OrgUnitFlatItem[]>(`/api/quiz/org-units${queryParam}`);
          if (Array.isArray(flatRes2.data)) flats = flatRes2.data;
        } catch (e) {
          console.warn('Could not fetch flat list directly', e);
        }
      }

      // Bi-directional recovery
      if (trees.length === 0 && flats.length > 0) {
        trees = buildTreeFromFlats(flats);
      } else if (flats.length === 0 && trees.length > 0) {
        flats = flattenTreeNodes(trees);
      }

      setTreeData(trees);
      setFlatData(flats);

      // Auto expand root & 1st level nodes
      const initialExpanded = new Set<string>();
      const addKeys = (nodes: OrgUnitNode[], depth: number) => {
        for (const n of nodes) {
          if (depth <= 2) initialExpanded.add(n.id);
          if (n.children && n.children.length > 0) {
            addKeys(n.children, depth + 1);
          }
        }
      };
      addKeys(trees, 1);
      setExpandedIds(initialExpanded);
    } catch (err: any) {
      console.error('Failed to load OUs:', err);
      setFeedbackMsg({
        type: 'error',
        text: err?.response?.data?.message || 'Không thể tải sơ đồ cơ cấu tổ chức'
      });
    } finally {
      setLoading(false);
    }
  }, [selectedTenantId]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  // Expand / Collapse Toggles
  const toggleExpand = (id: string) => {
    setExpandedIds(prev => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const expandAll = () => {
    const all = new Set<string>();
    flatData.forEach(d => all.add(d.id));
    setExpandedIds(all);
  };

  const collapseAll = () => {
    setExpandedIds(new Set());
  };

  // KPIs
  const totalUnits = flatData.length;
  const totalAssignedUsers = flatData.reduce((acc, cur) => acc + (cur.userCount || 0), 0);
  const activeUnits = flatData.filter(u => u.isActive).length;

  // Open Create Modal
  const handleOpenAddModal = (presetParentId?: string) => {
    setFormCode('');
    setFormName('');
    setFormUnitType(presetParentId ? 4 : 1);
    setFormTaxonomyCategory('');
    setFormParentId(presetParentId || '');
    setFormSortOrder(0);
    setFormIsActive(true);
    setParentPresetId(presetParentId || null);
    setIsAddModalOpen(true);
  };

  // Submit Create
  const handleCreateSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formCode.trim() || !formName.trim()) {
      setFeedbackMsg({ type: 'error', text: 'Vui lòng nhập đầy đủ mã và tên đơn vị.' });
      return;
    }

    setSubmitting(true);
    try {
      await apiClient.post('/api/quiz/org-units', {
        tenantId: isSystemAdmin && selectedTenantId ? selectedTenantId : undefined,
        code: formCode.trim().toUpperCase(),
        name: formName.trim(),
        unitType: Number(formUnitType),
        taxonomyCategory: formTaxonomyCategory.trim() || undefined,
        parentId: formParentId || null,
        sortOrder: Number(formSortOrder) || 0,
        isActive: formIsActive
      });

      setFeedbackMsg({ type: 'success', text: `Đã tạo mới đơn vị "${formName}" thành công!` });
      setIsAddModalOpen(false);
      loadData(selectedTenantId);
    } catch (err: any) {
      setFeedbackMsg({
        type: 'error',
        text: err?.response?.data?.message || 'Lỗi khi tạo mới đơn vị tổ chức.'
      });
    } finally {
      setSubmitting(false);
    }
  };

  // Open Edit Modal
  const handleOpenEditModal = (node: OrgUnitNode) => {
    setSelectedNode(node);
    setFormName(node.name);
    setFormUnitType(typeof node.unitTypeValue === 'number' ? node.unitTypeValue : Number(node.unitType) || 4);
    setFormTaxonomyCategory(node.taxonomyCategory || '');
    setFormSortOrder(node.sortOrder);
    setFormIsActive(node.isActive);
    setIsEditModalOpen(true);
  };

  // Submit Edit
  const handleEditSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedNode) return;

    setSubmitting(true);
    try {
      await apiClient.put(`/api/quiz/org-units/${selectedNode.id}`, {
        name: formName.trim(),
        unitType: Number(formUnitType),
        taxonomyCategory: formTaxonomyCategory.trim() || undefined,
        sortOrder: Number(formSortOrder) || 0,
        isActive: formIsActive
      });

      setFeedbackMsg({ type: 'success', text: `Đã cập nhật thông tin đơn vị "${formName}" thành công!` });
      setIsEditModalOpen(false);
      loadData();
    } catch (err: any) {
      setFeedbackMsg({
        type: 'error',
        text: err?.response?.data?.message || 'Lỗi khi cập nhật đơn vị tổ chức.'
      });
    } finally {
      setSubmitting(false);
    }
  };

  // Open Move Modal
  const handleOpenMoveModal = (node: OrgUnitNode) => {
    setSelectedNode(node);
    setMoveTargetParentId(node.parentId || '');
    setIsMoveModalOpen(true);
  };

  // Submit Move (Re-parenting)
  const handleMoveSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedNode) return;

    setSubmitting(true);
    try {
      await apiClient.post(`/api/quiz/org-units/${selectedNode.id}/move`, {
        newParentId: moveTargetParentId || null,
        newSortOrder: selectedNode.sortOrder
      });

      setFeedbackMsg({
        type: 'success',
        text: `Đã di chuyển nhánh "${selectedNode.name}" và tự động cập nhật toàn bộ cây phân cấp!`
      });
      setIsMoveModalOpen(false);
      loadData();
    } catch (err: any) {
      setFeedbackMsg({
        type: 'error',
        text: err?.response?.data?.message || 'Không thể di chuyển đơn vị (phát hiện vòng lặp hoặc ngoài phạm vi).'
      });
    } finally {
      setSubmitting(false);
    }
  };

  // Delete Unit
  const handleDeleteUnit = async (node: OrgUnitNode) => {
    if (node.children && node.children.length > 0) {
      alert(`Đơn vị "${node.name}" đang có ${node.children.length} đơn vị cấp dưới. Bạn cần xóa hoặc di chuyển các đơn vị con trước.`);
      return;
    }

    if (node.userCount > 0) {
      alert(`Đơn vị "${node.name}" đang có ${node.userCount} người dùng trực thuộc. Bạn cần chuyển nhân sự sang đơn vị khác trước khi xóa.`);
      return;
    }

    if (!window.confirm(`Bạn có chắc chắn muốn xóa đơn vị "${node.name}" (${node.code})? Hành động này không thể hoàn tác.`)) {
      return;
    }

    try {
      await apiClient.delete(`/api/quiz/org-units/${node.id}`);
      setFeedbackMsg({ type: 'success', text: `Đã xóa đơn vị "${node.name}" thành công!` });
      loadData();
    } catch (err: any) {
      setFeedbackMsg({
        type: 'error',
        text: err?.response?.data?.message || 'Không thể xóa đơn vị tổ chức.'
      });
    }
  };

  // Eligible Parent Candidates for Move Modal (preventing self and subtrees)
  const eligibleParents = useMemo(() => {
    if (!selectedNode) return flatData;
    return flatData.filter(item => {
      if (item.id === selectedNode.id) return false;
      // If item is a child of selectedNode (starts with selectedNode.hierarchyPath)
      if (item.hierarchyPath && selectedNode.hierarchyPath && item.hierarchyPath.startsWith(selectedNode.hierarchyPath)) {
        return false;
      }
      return true;
    });
  }, [flatData, selectedNode]);

  // Filter tree nodes recursively when searching
  const filterNode = (node: OrgUnitNode, query: string): OrgUnitNode | null => {
    const matchesQuery = 
      node.name.toLowerCase().includes(query) || 
      node.code.toLowerCase().includes(query) ||
      (node.hierarchyPath && node.hierarchyPath.toLowerCase().includes(query));

    const filteredChildren = (node.children || [])
      .map(child => filterNode(child, query))
      .filter((c): c is OrgUnitNode => c !== null);

    if (matchesQuery || filteredChildren.length > 0) {
      return {
        ...node,
        children: filteredChildren
      };
    }
    return null;
  };

  const filteredTree = useMemo(() => {
    if (!search.trim()) return treeData;
    const q = search.trim().toLowerCase();
    return treeData
      .map(node => filterNode(node, q))
      .filter((n): n is OrgUnitNode => n !== null);
  }, [treeData, search]);

  // Recursive Tree Node Renderer
  const renderTreeNode = (node: OrgUnitNode, depth = 0) => {
    const isExpanded = expandedIds.has(node.id);
    const hasChildren = node.children && node.children.length > 0;
    const typeKey = Number(node.unitTypeValue ?? node.unitType) || 4;
    const typeConf = UNIT_TYPE_CONFIG[typeKey] || UNIT_TYPE_CONFIG[4];
    const IconComp = typeConf.icon;

    return (
      <div key={node.id} className="relative select-none">
        {/* Branch row card */}
        <div 
          className={`group flex items-center justify-between p-3 my-1.5 rounded-2xl border transition-all duration-200 ${
            node.isActive
              ? 'bg-slate-900/80 hover:bg-slate-800/90 border-slate-800/80 hover:border-slate-700 shadow-sm'
              : 'bg-slate-950/60 border-slate-800/40 opacity-60'
          }`}
          style={{ marginLeft: `${depth * 24}px` }}
        >
          {/* Left section: expand arrow, icon, name, code, path */}
          <div className="flex items-center gap-2.5 min-w-0 flex-1">
            {/* Expand / Collapse Button */}
            {hasChildren ? (
              <button
                type="button"
                onClick={() => toggleExpand(node.id)}
                className="w-6 h-6 flex items-center justify-center rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-400 hover:text-white transition-colors cursor-pointer shrink-0"
              >
                {isExpanded ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
              </button>
            ) : (
              <div className="w-6 h-6 flex items-center justify-center text-slate-600 shrink-0">
                <span className="w-1.5 h-1.5 rounded-full bg-slate-700" />
              </div>
            )}

            {/* Type Icon */}
            <div className={`w-8 h-8 rounded-xl flex items-center justify-center shrink-0 border ${typeConf.badgeClass}`}>
              <IconComp size={15} />
            </div>

            {/* Title & Path */}
            <div className="min-w-0 flex-1">
              <div className="flex items-center gap-2 flex-wrap">
                <span className="font-black text-white text-sm tracking-tight truncate">
                  {node.name}
                </span>
                <span className="font-mono text-[11px] px-1.5 py-0.5 rounded bg-slate-800 text-cyan-300 font-bold border border-slate-700 shrink-0">
                  {node.code}
                </span>
                <span className="font-mono text-[10px] px-2 py-0.5 rounded-full bg-gradient-to-r from-blue-500/20 to-purple-500/20 text-blue-300 font-bold border border-blue-500/30 shrink-0">
                  Tầng {node.depthLevel || (node.hierarchyPath ? node.hierarchyPath.split('/').filter(Boolean).length : depth + 1)}
                </span>
                <span className={`text-[10px] font-bold px-2 py-0.5 rounded-full border shrink-0 ${typeConf.badgeClass}`}>
                  {typeConf.label}
                </span>
                {node.taxonomyCategory && (
                  <span className="text-[10px] font-bold px-2 py-0.5 rounded-full bg-cyan-500/10 text-cyan-300 border border-cyan-500/30 shrink-0">
                    🏷️ {node.taxonomyCategory}
                  </span>
                )}
                {!node.isActive && (
                  <span className="text-[10px] font-bold px-2 py-0.5 rounded-full bg-rose-500/10 text-rose-400 border border-rose-500/20 shrink-0">
                    Tạm khóa
                  </span>
                )}
              </div>
              <div className="text-[11px] text-slate-500 font-mono truncate mt-0.5">
                {node.hierarchyPath || `/${node.code}`}
              </div>
            </div>
          </div>

          {/* Right section: member count badge & action buttons */}
          <div className="flex items-center gap-2 shrink-0 ml-3">
            {/* User count badge */}
            <div className="flex items-center gap-1.5 px-2.5 py-1 rounded-xl bg-slate-950 border border-slate-800 text-slate-300 text-xs font-bold" title="Số lượng nhân sự trực thuộc">
              <Users size={13} className="text-blue-400" />
              <span>{node.userCount}</span>
            </div>

            {/* Action buttons (always visible or revealed on group hover) */}
            <div className="flex items-center gap-1">
              {/* + Con button */}
              <button
                type="button"
                onClick={() => handleOpenAddModal(node.id)}
                className="p-1.5 rounded-xl bg-slate-800/80 hover:bg-emerald-600/30 text-slate-300 hover:text-emerald-300 border border-slate-700/60 hover:border-emerald-500/40 text-xs font-bold transition-all cursor-pointer flex items-center gap-1"
                title="Thêm đơn vị con trực thuộc"
              >
                <Plus size={13} />
                <span className="hidden sm:inline text-[11px]">Con</span>
              </button>

              {/* Edit button */}
              <button
                type="button"
                onClick={() => handleOpenEditModal(node)}
                className="p-1.5 rounded-xl bg-slate-800/80 hover:bg-blue-600/30 text-slate-300 hover:text-blue-300 border border-slate-700/60 hover:border-blue-500/40 text-xs font-bold transition-all cursor-pointer flex items-center gap-1"
                title="Sửa thông tin đơn vị"
              >
                <Edit size={13} />
                <span className="hidden sm:inline text-[11px]">Sửa</span>
              </button>

              {/* Move button (Re-parenting) */}
              <button
                type="button"
                onClick={() => handleOpenMoveModal(node)}
                className="p-1.5 rounded-xl bg-slate-800/80 hover:bg-amber-600/30 text-slate-300 hover:text-amber-300 border border-slate-700/60 hover:border-amber-500/40 text-xs font-bold transition-all cursor-pointer flex items-center gap-1"
                title="Di chuyển nhánh / Đổi cấp cha"
              >
                <ArrowRightLeft size={13} />
                <span className="hidden sm:inline text-[11px]">Chuyển</span>
              </button>

              {/* Delete button */}
              <button
                type="button"
                onClick={() => handleDeleteUnit(node)}
                className="p-1.5 rounded-xl bg-slate-800/80 hover:bg-rose-600/30 text-slate-400 hover:text-rose-300 border border-slate-700/60 hover:border-rose-500/40 text-xs font-bold transition-all cursor-pointer"
                title="Xóa đơn vị (yêu cầu không còn nhánh con và nhân sự)"
              >
                <Trash2 size={13} />
              </button>
            </div>
          </div>
        </div>

        {/* Render child nodes recursively */}
        {hasChildren && isExpanded && (
          <div className="relative">
            {/* Indentation line */}
            <div 
              className="absolute left-0 top-0 bottom-2 w-px bg-slate-800/80" 
              style={{ left: `${depth * 24 + 15}px` }} 
            />
            {node.children.map(child => renderTreeNode(child, depth + 1))}
          </div>
        )}
      </div>
    );
  };

  return (
    <div className="space-y-8 max-w-7xl mx-auto pb-12">
      {/* Header & Breadcrumb */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-2 text-cyan-400 font-bold text-xs uppercase tracking-wider mb-1">
            <Shield size={14} />
            <span>Enterprise Hierarchy & Scoped RBAC</span>
          </div>
          <h1 className="text-3xl font-black text-white tracking-tight flex items-center gap-3">
            <Network className="text-cyan-400" size={30} />
            Cơ Cấu Tổ Chức & Đơn Vị (Org Units)
          </h1>
          <p className="text-sm text-slate-400 mt-1 max-w-2xl">
            Quản lý sơ đồ phả hệ tổ chức đa tầng (5-Tier: Hội sở → Vùng → Chi nhánh → Phòng ban → Lớp học). 
            Phân quyền chỉ được gán và quản lý trong phạm vi cây phả hệ trực thuộc.
          </p>
        </div>

        {/* Global actions */}
        <div className="flex items-center gap-3">
          <button
            type="button"
            onClick={() => loadData()}
            disabled={loading}
            className="px-3.5 py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 hover:text-white text-xs font-bold flex items-center gap-1.5 transition-all cursor-pointer disabled:opacity-50"
          >
            <RefreshCw size={14} className={loading ? 'animate-spin' : ''} />
            Làm mới
          </button>

          <button
            type="button"
            onClick={() => handleOpenAddModal()}
            className="px-4 py-2.5 rounded-xl bg-gradient-to-r from-cyan-500 to-blue-600 hover:from-cyan-400 hover:to-blue-500 text-white text-xs font-bold flex items-center gap-2 shadow-lg shadow-cyan-500/20 transition-all cursor-pointer"
          >
            <Plus size={15} />
            Tạo Đơn Vị Mới
          </button>
        </div>
      </div>

      {/* Multi-Tenant Context Bar */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 p-4 rounded-2xl bg-slate-900/90 border border-cyan-500/20 shadow-xl backdrop-blur-md">
        {isSystemAdmin ? (
          <div className="flex flex-wrap items-center gap-3 w-full sm:w-auto">
            <div className="flex items-center gap-2 text-cyan-400 font-bold text-xs uppercase tracking-wider">
              <Building2 size={16} />
              <span>Khách thuê đang quản trị:</span>
            </div>
            <select
              value={selectedTenantId}
              onChange={(e) => {
                setSelectedTenantId(e.target.value);
                loadData(e.target.value);
              }}
              className="bg-slate-950 border border-slate-700 hover:border-cyan-500 text-white font-bold text-xs rounded-xl px-3.5 py-2 focus:border-cyan-500 focus:outline-none cursor-pointer transition-colors"
            >
              {tenantsList.map(t => (
                <option key={t.id} value={t.id}>
                  🏢 {t.name} ({t.code.toUpperCase()}) — Gói {t.plan} [{t.currentUsersCount}/{t.maxUsers} users]
                </option>
              ))}
            </select>
            <span className="text-[11px] font-mono px-2 py-0.5 rounded-full bg-cyan-500/10 text-cyan-300 font-bold border border-cyan-500/30">
              {tenantsList.length} Doanh nghiệp
            </span>
          </div>
        ) : (
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-xl bg-blue-500/10 text-blue-400 border border-blue-500/20">
              <Building2 size={18} />
            </div>
            <div>
              <p className="text-xs text-slate-400 font-medium">Tổ chức trực thuộc:</p>
              <h4 className="text-sm font-black text-white">
                {user?.tenantName || 'Dehoc Academy'}
              </h4>
            </div>
            <span className="text-[10px] font-bold px-2 py-0.5 rounded-full bg-blue-500/10 text-blue-300 border border-blue-500/20 uppercase">
              {user?.subscriptionTier || 'ENTERPRISE'}
            </span>
          </div>
        )}

        <div className="flex items-center gap-2 text-xs text-slate-400">
          <Shield size={14} className="text-cyan-400" />
          <span>Ranh giới: <strong className="text-slate-200 font-mono">Scoped Isolated</strong></span>
        </div>
      </div>

      {/* Feedback Banner */}
      {feedbackMsg && (
        <div className={`p-4 rounded-2xl border flex items-center gap-3 animate-in fade-in duration-200 ${
          feedbackMsg.type === 'success'
            ? 'bg-emerald-500/10 border-emerald-500/30 text-emerald-300'
            : 'bg-rose-500/10 border-rose-500/30 text-rose-300'
        }`}>
          {feedbackMsg.type === 'success' ? <CheckCircle2 size={18} /> : <AlertCircle size={18} />}
          <span className="text-sm font-semibold">{feedbackMsg.text}</span>
        </div>
      )}

      {/* KPI Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-5">
        <div className="p-5 rounded-3xl bg-slate-900/60 border border-slate-800/80 backdrop-blur-md relative overflow-hidden group">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-xs font-bold text-slate-400 uppercase tracking-wider">Tổng Đơn Vị / Phòng Ban</p>
              <h3 className="text-3xl font-black text-white mt-1 tracking-tight">{totalUnits}</h3>
              <p className="text-xs text-slate-500 mt-1">Đã khởi tạo trong tổ chức</p>
            </div>
            <div className="w-12 h-12 rounded-2xl bg-cyan-500/10 border border-cyan-500/20 flex items-center justify-center text-cyan-400">
              <FolderTree size={24} />
            </div>
          </div>
        </div>

        <div className="p-5 rounded-3xl bg-slate-900/60 border border-slate-800/80 backdrop-blur-md relative overflow-hidden group">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-xs font-bold text-slate-400 uppercase tracking-wider">Nhân Sự Đã Gán Vị Trí</p>
              <h3 className="text-3xl font-black text-blue-400 mt-1 tracking-tight">{totalAssignedUsers}</h3>
              <p className="text-xs text-slate-500 mt-1">Cán bộ & học viên trực thuộc</p>
            </div>
            <div className="w-12 h-12 rounded-2xl bg-blue-500/10 border border-blue-500/20 flex items-center justify-center text-blue-400">
              <Users size={24} />
            </div>
          </div>
        </div>

        <div className="p-5 rounded-3xl bg-slate-900/60 border border-slate-800/80 backdrop-blur-md relative overflow-hidden group">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-xs font-bold text-slate-400 uppercase tracking-wider">Đơn Vị Đang Hoạt Động</p>
              <h3 className="text-3xl font-black text-emerald-400 mt-1 tracking-tight">{activeUnits} / {totalUnits}</h3>
              <p className="text-xs text-slate-500 mt-1">Đầy đủ quyền khảo thí</p>
            </div>
            <div className="w-12 h-12 rounded-2xl bg-emerald-500/10 border border-emerald-500/20 flex items-center justify-center text-emerald-400">
              <CheckCircle2 size={24} />
            </div>
          </div>
        </div>
      </div>

      {/* Main Tree Container */}
      <div className="bg-slate-900/40 border border-slate-800/80 rounded-3xl p-6 backdrop-blur-md space-y-6">
        {/* Controls bar: search, expand/collapse all */}
        <div className="flex flex-col sm:flex-row items-center justify-between gap-4 pb-4 border-b border-slate-800/80">
          <div className="relative w-full sm:w-96">
            <Search className="absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-500" size={16} />
            <input
              type="text"
              placeholder="Tìm kiếm theo mã, tên đơn vị hoặc path..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="w-full bg-slate-950/80 border border-slate-800 rounded-xl pl-10 pr-4 py-2 text-sm text-white placeholder-slate-500 focus:outline-none focus:border-cyan-500 transition-colors"
            />
          </div>

          <div className="flex items-center gap-2 w-full sm:w-auto justify-end">
            <button
              type="button"
              onClick={expandAll}
              className="px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-semibold transition-colors cursor-pointer"
            >
              Mở rộng tất cả
            </button>
            <button
              type="button"
              onClick={collapseAll}
              className="px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-semibold transition-colors cursor-pointer"
            >
              Thu gọn tất cả
            </button>
          </div>
        </div>

        {/* Tree Content */}
        {loading ? (
          <div className="p-16 text-center text-slate-500 flex flex-col items-center justify-center gap-3">
            <RefreshCw size={28} className="animate-spin text-cyan-400" />
            <p className="text-sm font-semibold">Đang nạp sơ đồ cơ cấu tổ chức...</p>
          </div>
        ) : filteredTree.length === 0 ? (
          <div className="p-16 text-center text-slate-500 flex flex-col items-center justify-center gap-3">
            <Network size={36} className="text-slate-600" />
            <p className="text-base font-bold text-slate-400">Không tìm thấy đơn vị nào phù hợp</p>
            <p className="text-xs text-slate-600 max-w-sm">
              Bạn có thể tạo mới đơn vị gốc (Hội sở / Trụ sở chính) để bắt đầu xây dựng cây phả hệ tổ chức.
            </p>
            <button
              type="button"
              onClick={() => handleOpenAddModal()}
              className="mt-2 px-4 py-2 rounded-xl bg-cyan-600 hover:bg-cyan-500 text-white text-xs font-bold transition-all cursor-pointer flex items-center gap-1.5"
            >
              <Plus size={14} /> Thêm Đơn Vị Gốc
            </button>
          </div>
        ) : (
          <div className="space-y-1">
            {filteredTree.map(node => renderTreeNode(node, 0))}
          </div>
        )}
      </div>

      {/* Guide Note */}
      <div className="p-5 rounded-2xl bg-cyan-950/20 border border-cyan-500/20 text-xs text-slate-400 flex items-start gap-3">
        <HelpCircle size={18} className="text-cyan-400 shrink-0 mt-0.5" />
        <div className="space-y-1">
          <p className="text-cyan-300 font-bold">Nguyên lý phân quyền Scoped RBAC & Materialized Path</p>
          <p>
            • Mỗi đơn vị có mã đường dẫn phân cấp (ví dụ: <code className="text-cyan-300 font-mono">/HQ/MB/HN</code>). 
            Khi một Quản lý (OrgUnitManager) hoặc Trưởng nhóm (TeamLeader) được gán vào đơn vị nào, họ chỉ có quyền xem, tạo và quản trị nhân sự thuộc nhánh con của đơn vị đó.
          </p>
          <p>
            • Khi di chuyển nhánh (Re-parenting), hệ thống sẽ tự động cập nhật đệ quy toàn bộ tiền tố <code className="text-cyan-300 font-mono">HierarchyPath</code> của tất cả các nhánh và nhân sự con cháu, đồng thời ngăn chặn tuyệt đối tình trạng vòng lặp (vòng tuần hoàn vào chính nhánh con).
          </p>
        </div>
      </div>

      {/* MODAL 1: ADD NEW ORG UNIT */}
      {isAddModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-md overflow-y-auto">
          <div className="relative w-full max-w-lg bg-slate-900 border border-slate-800 rounded-3xl p-6 sm:p-8 shadow-2xl space-y-6 text-slate-100 my-8">
            <div className="flex items-center justify-between pb-4 border-b border-slate-800">
              <div className="flex items-center gap-2.5">
                <div className="p-2 rounded-xl bg-cyan-500/10 text-cyan-400 border border-cyan-500/20">
                  <FolderPlus size={20} />
                </div>
                <div>
                  <h2 className="text-lg font-black text-white">Thêm Đơn Vị Tổ Chức</h2>
                  <p className="text-xs text-slate-400">Tạo mới nút phân cấp trong cây tổ chức</p>
                </div>
              </div>
              <button
                type="button"
                onClick={() => setIsAddModalOpen(false)}
                className="text-slate-400 hover:text-white p-1 rounded-lg"
              >
                ✕
              </button>
            </div>

            <form onSubmit={handleCreateSubmit} className="space-y-4">
              {/* Tenant context display */}
              <div className="p-3 rounded-xl bg-slate-950/80 border border-slate-800 flex items-center justify-between text-xs">
                <span className="text-slate-400 font-medium">Tổ chức thụ hưởng đơn vị:</span>
                <span className="font-bold text-cyan-300 flex items-center gap-1.5">
                  <Building2 size={13} />
                  {isSystemAdmin 
                    ? (tenantsList.find(t => t.id === selectedTenantId)?.name || 'Tenant đã chọn')
                    : (user?.tenantName || 'Dehoc Academy')}
                </span>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-1.5">
                    Mã Đơn Vị *
                  </label>
                  <input
                    type="text"
                    required
                    placeholder="VD: HN, P_CNTT, KHOI_BANLE"
                    value={formCode}
                    onChange={(e) => setFormCode(e.target.value.toUpperCase())}
                    className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2.5 text-white font-mono text-xs focus:outline-none focus:border-cyan-500 uppercase"
                  />
                </div>

                <div>
                  <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-1.5">
                    Loại Đơn Vị Phân Cấp *
                  </label>
                  <select
                    value={formUnitType}
                    onChange={(e) => setFormUnitType(Number(e.target.value))}
                    className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2.5 text-white text-xs focus:outline-none focus:border-cyan-500 cursor-pointer font-medium"
                  >
                    <option value={0}>0. Tập đoàn mẹ / Holding Corp</option>
                    <option value={1}>1. Hội sở chính (HQ / Trụ sở chính)</option>
                    <option value={2}>2. Khu vực / Vùng (Region / APAC / EMEA)</option>
                    <option value={3}>3. Chi nhánh / Khối / Pháp nhân (Branch / OpCo)</option>
                    <option value={4}>4. Phòng ban / Khoa / Trung tâm (Department)</option>
                    <option value={5}>5. Lớp học / Tổ nhóm / Đội dự án (Classroom)</option>
                    <option value={6}>6. Khối kinh doanh / Ngành hàng (Sector)</option>
                    <option value={7}>7. Ban chuyên môn / Khối chức năng (Division)</option>
                    <option value={8}>8. Bộ phận chức năng vi mô (Section)</option>
                    <option value={9}>9. Đội Agile / Nhóm đặc nhiệm (Squad / Taskforce)</option>
                    <option value={99}>99. Đơn vị tùy chỉnh (Custom Taxonomy)</option>
                  </select>
                </div>
              </div>

              {/* Dynamic Depth Level & Custom Taxonomy Input */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-1.5 flex items-center justify-between">
                    <span>Tên Gọi Danh Mục Tùy Biến</span>
                    <span className="text-[10px] text-cyan-400 font-normal">Tùy chọn</span>
                  </label>
                  <input
                    type="text"
                    placeholder="VD: Phân viện, Trung tâm R&D, Lab..."
                    value={formTaxonomyCategory}
                    onChange={(e) => setFormTaxonomyCategory(e.target.value)}
                    className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2.5 text-white text-xs focus:outline-none focus:border-cyan-500"
                  />
                </div>

                <div>
                  <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-1.5">
                    Độ Sâu Cấu Trúc (Depth Level)
                  </label>
                  <div className="w-full bg-slate-950/80 border border-slate-800 rounded-xl px-3.5 py-2.5 flex items-center justify-between">
                    <span className="text-xs text-slate-400">Vị trí nút:</span>
                    <span className="font-mono text-xs font-black text-cyan-400">
                      Tầng {formParentId ? ((flatData.find(u => u.id === formParentId)?.depthLevel ?? 1) + 1) : 1} (Vô hạn)
                    </span>
                  </div>
                </div>
              </div>

              <div>
                <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-1.5">
                  Tên Đơn Vị *
                </label>
                <input
                  type="text"
                  required
                  placeholder="VD: Khối Khảo thí & Đào tạo, Phân viện Nghiên cứu AI..."
                  value={formName}
                  onChange={(e) => setFormName(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2.5 text-white text-xs focus:outline-none focus:border-cyan-500"
                />
              </div>

              <div>
                <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-1.5">
                  Đơn Vị Cấp Cha Trực Thuộc
                </label>
                <select
                  value={formParentId}
                  onChange={(e) => setFormParentId(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2.5 text-white text-xs focus:outline-none focus:border-cyan-500 cursor-pointer"
                >
                  <option value="">-- Là đơn vị gốc cao nhất (Root Holding / HQ) --</option>
                  {flatData.map((ou) => (
                    <option key={ou.id} value={ou.id}>
                      {ou.hierarchyPath || `/${ou.code}`} — {ou.name} [Tầng {ou.depthLevel || 1}]
                    </option>
                  ))}
                </select>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-1.5">
                    Thứ Tự Sắp Xếp
                  </label>
                  <input
                    type="number"
                    value={formSortOrder}
                    onChange={(e) => setFormSortOrder(Number(e.target.value))}
                    className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2.5 text-white text-xs focus:outline-none focus:border-cyan-500"
                  />
                </div>

                <div>
                  <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-1.5">
                    Trạng Thái
                  </label>
                  <button
                    type="button"
                    onClick={() => setFormIsActive(!formIsActive)}
                    className={`w-full py-2.5 px-3.5 rounded-xl border text-xs font-bold flex items-center justify-between cursor-pointer ${
                      formIsActive
                        ? 'bg-emerald-500/10 border-emerald-500/30 text-emerald-300'
                        : 'bg-rose-500/10 border-rose-500/30 text-rose-300'
                    }`}
                  >
                    <span>{formIsActive ? 'Hoạt động' : 'Tạm khóa'}</span>
                    <span className="text-[10px] opacity-75">Đổi</span>
                  </button>
                </div>
              </div>

              <div className="flex items-center justify-end gap-3 pt-4 border-t border-slate-800">
                <button
                  type="button"
                  onClick={() => setIsAddModalOpen(false)}
                  className="px-4 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-bold transition-all cursor-pointer"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  disabled={submitting}
                  className="px-5 py-2 rounded-xl bg-cyan-600 hover:bg-cyan-500 text-white text-xs font-bold flex items-center gap-1.5 shadow-lg shadow-cyan-500/20 transition-all cursor-pointer disabled:opacity-50"
                >
                  {submitting ? <RefreshCw size={14} className="animate-spin" /> : <CheckCircle2 size={14} />}
                  Tạo Đơn Vị
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* MODAL 2: EDIT ORG UNIT */}
      {isEditModalOpen && selectedNode && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-md overflow-y-auto">
          <div className="relative w-full max-w-lg bg-slate-900 border border-slate-800 rounded-3xl p-6 sm:p-8 shadow-2xl space-y-6 text-slate-100 my-8">
            <div className="flex items-center justify-between pb-4 border-b border-slate-800">
              <div className="flex items-center gap-2.5">
                <div className="p-2 rounded-xl bg-blue-500/10 text-blue-400 border border-blue-500/20">
                  <Edit size={20} />
                </div>
                <div>
                  <h2 className="text-lg font-black text-white">Chỉnh Sửa Đơn Vị</h2>
                  <p className="text-xs text-slate-400 font-mono">{selectedNode.code} — {selectedNode.hierarchyPath}</p>
                </div>
              </div>
              <button
                type="button"
                onClick={() => setIsEditModalOpen(false)}
                className="text-slate-400 hover:text-white p-1 rounded-lg"
              >
                ✕
              </button>
            </div>

            <form onSubmit={handleEditSubmit} className="space-y-4">
              <div>
                <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-1.5">
                  Tên Đơn Vị *
                </label>
                <input
                  type="text"
                  required
                  value={formName}
                  onChange={(e) => setFormName(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2.5 text-white text-xs focus:outline-none focus:border-blue-500"
                />
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-1.5">
                    Loại Đơn Vị
                  </label>
                  <select
                    value={formUnitType}
                    onChange={(e) => setFormUnitType(Number(e.target.value))}
                    className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2.5 text-white text-xs focus:outline-none focus:border-blue-500 cursor-pointer"
                  >
                    <option value={0}>0. Tập đoàn mẹ / Holding Corp</option>
                    <option value={1}>1. Hội sở chính (HQ)</option>
                    <option value={2}>2. Khu vực / Vùng</option>
                    <option value={3}>3. Chi nhánh / Khối</option>
                    <option value={4}>4. Phòng ban / Bộ phận</option>
                    <option value={5}>5. Lớp học / Đội nhóm</option>
                    <option value={6}>6. Khối kinh doanh (Sector)</option>
                    <option value={7}>7. Ban chuyên môn (Division)</option>
                    <option value={8}>8. Bộ phận vi mô (Section)</option>
                    <option value={9}>9. Đội Agile (Squad)</option>
                    <option value={99}>99. Đơn vị tùy chỉnh (Custom)</option>
                  </select>
                </div>

                <div>
                  <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-1.5">
                    Tên Gọi Danh Mục Tùy Biến
                  </label>
                  <input
                    type="text"
                    placeholder="VD: Phân viện, Lab..."
                    value={formTaxonomyCategory}
                    onChange={(e) => setFormTaxonomyCategory(e.target.value)}
                    className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2.5 text-white text-xs focus:outline-none focus:border-blue-500"
                  />
                </div>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-1.5">
                    Thứ Tự Sắp Xếp
                  </label>
                  <input
                    type="number"
                    value={formSortOrder}
                    onChange={(e) => setFormSortOrder(Number(e.target.value))}
                    className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2.5 text-white text-xs focus:outline-none focus:border-blue-500"
                  />
                </div>

                <div>
                  <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-1.5">
                    Trạng Thái
                  </label>
                  <button
                    type="button"
                    onClick={() => setFormIsActive(!formIsActive)}
                    className={`w-full py-2.5 px-3.5 rounded-xl border text-xs font-bold flex items-center justify-between cursor-pointer ${
                      formIsActive
                        ? 'bg-emerald-500/10 border-emerald-500/30 text-emerald-300'
                        : 'bg-rose-500/10 border-rose-500/30 text-rose-300'
                    }`}
                  >
                    <span>{formIsActive ? 'Hoạt động' : 'Tạm khóa'}</span>
                    <span className="text-[10px] opacity-75">Đổi</span>
                  </button>
                </div>
              </div>

              <div className="flex items-center justify-end gap-3 pt-4 border-t border-slate-800">
                <button
                  type="button"
                  onClick={() => setIsEditModalOpen(false)}
                  className="px-4 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-bold transition-all cursor-pointer"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  disabled={submitting}
                  className="px-5 py-2 rounded-xl bg-blue-600 hover:bg-blue-500 text-white text-xs font-bold flex items-center gap-1.5 shadow-lg shadow-blue-500/20 transition-all cursor-pointer disabled:opacity-50"
                >
                  {submitting ? <RefreshCw size={14} className="animate-spin" /> : <CheckCircle2 size={14} />}
                  Lưu Thay Đổi
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* MODAL 3: MOVE / RE-PARENT ORG UNIT */}
      {isMoveModalOpen && selectedNode && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-md overflow-y-auto">
          <div className="relative w-full max-w-lg bg-slate-900 border border-slate-800 rounded-3xl p-6 sm:p-8 shadow-2xl space-y-6 text-slate-100 my-8">
            <div className="flex items-center justify-between pb-4 border-b border-slate-800">
              <div className="flex items-center gap-2.5">
                <div className="p-2 rounded-xl bg-amber-500/10 text-amber-400 border border-amber-500/20">
                  <ArrowRightLeft size={20} />
                </div>
                <div>
                  <h2 className="text-lg font-black text-white">Di Chuyển Cây Phân Cấp (Re-parent)</h2>
                  <p className="text-xs text-slate-400">Thay đổi đơn vị cấp cha cho nhánh này</p>
                </div>
              </div>
              <button
                type="button"
                onClick={() => setIsMoveModalOpen(false)}
                className="text-slate-400 hover:text-white p-1 rounded-lg"
              >
                ✕
              </button>
            </div>

            <div className="p-4 rounded-2xl bg-slate-950 border border-slate-800 space-y-2">
              <div className="text-xs text-slate-400">Nhánh đang chọn:</div>
              <div className="font-bold text-white text-sm flex items-center gap-2">
                <span>{selectedNode.name}</span>
                <span className="font-mono text-cyan-300 text-xs font-bold px-1.5 py-0.5 rounded bg-slate-800">
                  {selectedNode.code}
                </span>
              </div>
              <div className="text-xs font-mono text-slate-500">{selectedNode.hierarchyPath}</div>
            </div>

            <form onSubmit={handleMoveSubmit} className="space-y-4">
              <div>
                <label className="block text-xs font-bold text-slate-300 uppercase tracking-wider mb-1.5">
                  Chọn Đơn Vị Cấp Cha Mới *
                </label>
                <select
                  value={moveTargetParentId}
                  onChange={(e) => setMoveTargetParentId(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2.5 text-white text-xs focus:outline-none focus:border-amber-500 cursor-pointer"
                >
                  <option value="">-- Đưa lên làm Đơn vị gốc cao nhất (Root HQ) --</option>
                  {eligibleParents.map((ou) => (
                    <option key={ou.id} value={ou.id}>
                      {ou.hierarchyPath || `/${ou.code}`} — {ou.name}
                    </option>
                  ))}
                </select>
                <p className="text-[11px] text-slate-500 mt-1.5">
                  * Danh sách đã tự động lọc bỏ chính đơn vị này và toàn bộ các nhánh con cháu của nó để ngăn ngừa vòng lặp vô tận.
                </p>
              </div>

              <div className="p-3.5 rounded-xl bg-amber-500/10 border border-amber-500/20 text-amber-300 text-xs flex items-start gap-2">
                <AlertCircle size={16} className="shrink-0 mt-0.5" />
                <span>
                  Hệ thống sẽ đệ quy tự động tính toán và cập nhật lại toàn bộ đường dẫn phân cấp (HierarchyPath) của nhánh này cùng tất cả các đơn vị và nhân sự con cháu trực thuộc.
                </span>
              </div>

              <div className="flex items-center justify-end gap-3 pt-4 border-t border-slate-800">
                <button
                  type="button"
                  onClick={() => setIsMoveModalOpen(false)}
                  className="px-4 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-bold transition-all cursor-pointer"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  disabled={submitting}
                  className="px-5 py-2 rounded-xl bg-amber-600 hover:bg-amber-500 text-white text-xs font-bold flex items-center gap-1.5 shadow-lg shadow-amber-500/20 transition-all cursor-pointer disabled:opacity-50"
                >
                  {submitting ? <RefreshCw size={14} className="animate-spin" /> : <ArrowRightLeft size={14} />}
                  Xác Nhận Di Chuyển
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}

export default OrganizationUnitsPage;
