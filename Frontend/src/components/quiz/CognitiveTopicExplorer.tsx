import React, { useState, useMemo } from 'react';
import {
  Folder,
  FolderOpen,
  ChevronRight,
  ChevronDown,
  Search,
  Sparkles,
  Globe2,
  Building2,
  Layers,
  CheckSquare,
  Square,
  Info,
  Users
} from 'lucide-react';
import type {
  CognitiveDomainGroupDto,
  CognitiveTopicNodeDto,
  ExamCategoryPreset,
  PracticeTopicConfig,
  TopicSummary
} from '@/types/quiz';
import { DrivingPresetWidget } from '@/components/quiz/DrivingPresetWidget';

interface Props {
  domainGroups?: CognitiveDomainGroupDto[];
  legacyTopics: TopicSummary[];
  topicConfigs: PracticeTopicConfig[];
  selectedMap: Map<string, PracticeTopicConfig>;
  activePreset?: ExamCategoryPreset | null;
  onSelectDrivingPreset?: (preset: ExamCategoryPreset, count: number, mins: number) => void;
  onToggleTopic: (topicKey: string, checked: boolean) => void;
  onUpdateTopicConfig: (topicKey: string, patch: Partial<PracticeTopicConfig>) => void;
  onSelectMultipleTopics?: (topicKeys: string[], select: boolean) => void;
  t: (key: string) => string;
}

const DOMAIN_ICONS: Record<string, string> = {
  BANKING: '🏦',
  DRIVING: '🚗',
  GOV_DRIVING: '🚗',
  EDUCATION: '🎓',
  IT: '💻',
  HEALTHCARE: '🏥',
  GENERAL: '🌐'
};

const isDrivingDomain = (code?: string): boolean => {
  if (!code) return false;
  const upper = code.toUpperCase();
  return upper.includes('DRIV') || upper.includes('LAI_XE') || upper.includes('GPLX');
};

export const CognitiveTopicExplorer: React.FC<Props> = ({
  domainGroups,
  legacyTopics,
  selectedMap,
  activePreset,
  onSelectDrivingPreset,
  onToggleTopic,
  onUpdateTopicConfig,
  onSelectMultipleTopics,
}) => {
  const [selectedDomain, setSelectedDomain] = useState<string>('ALL');
  const [selectedScope, setSelectedScope] = useState<string>(() => new URLSearchParams(window.location.search).get('scope') || 'ALL');
  const [searchQuery, setSearchQuery] = useState<string>('');
  const [expandedFolders, setExpandedFolders] = useState<Record<string, boolean>>({
    '2026_DOT2': true,
    '2026-DOT2': true
  });

  const toggleExpand = (code: string) => {
    setExpandedFolders(prev => ({ ...prev, [code]: !prev[code] }));
  };

  // Nếu Backend chưa có Domain Groups, tự động tổng hợp từ legacyTopics
  const effectiveDomainGroups = useMemo<CognitiveDomainGroupDto[]>(() => {
    if (domainGroups && domainGroups.length > 0) {
      return domainGroups.map(g => {
        const rawRoots = g.rootNodes ?? g.rootTopics ?? [];
        const roots: CognitiveTopicNodeDto[] = Array.isArray(rawRoots) ? rawRoots : [];
        const countTopics = (nodes: CognitiveTopicNodeDto[]): number => {
          let c = 0;
          for (const n of nodes) {
            c += 1 + (n.children && Array.isArray(n.children) ? countTopics(n.children) : 0);
          }
          return c;
        };
        const totalQ = g.totalQuestions ?? g.totalQuestionCount ?? 0;
        const totalT = g.totalTopics ?? countTopics(roots);

        return {
          ...g,
          rootNodes: roots,
          rootTopics: roots,
          totalQuestions: totalQ,
          totalQuestionCount: totalQ,
          totalTopics: totalT
        };
      });
    }

    // Fallback: Group legacyTopics theo logic tiền tố
    const bankingTopics: CognitiveTopicNodeDto[] = [];
    const drivingTopics: CognitiveTopicNodeDto[] = [];
    const eduTopics: CognitiveTopicNodeDto[] = [];
    const generalTopics: CognitiveTopicNodeDto[] = [];

    // Tìm xem có node 2026-DOT2 cha không
    const dot2Parent = legacyTopics.find(t => t.code.toUpperCase().includes('2026') && !t.code.includes('_CHUYEN_DE_') && !t.code.match(/_\d+_/));
    const dot2Children = legacyTopics.filter(t => t.code !== dot2Parent?.code && (t.name.includes('2026-DOT2') || t.name.includes('Đợt 2')));

    for (const t of legacyTopics) {
      const isDot2Child = dot2Children.some(c => c.code === t.code);
      if (isDot2Child) continue; // sẽ đưa vào children của dot2Parent

      const upper = t.code.toUpperCase();
      const node: CognitiveTopicNodeDto = {
        id: t.code,
        name: t.name,
        code: t.code,
        scope: 'COMMUNITY',
        domainCode: 'GENERAL',
        depthLevel: 1,
        questionCount: t.questionCount,
        children: []
      };

      if (dot2Parent && t.code === dot2Parent.code) {
        node.domainCode = 'BANKING';
        node.children = dot2Children.map(c => ({
          id: c.code,
          name: c.name.replace(/^.*?→\s*/, ''),
          code: c.code,
          scope: 'COMMUNITY',
          domainCode: 'BANKING',
          depthLevel: 2,
          parentId: node.id,
          questionCount: c.questionCount,
          children: []
        }));
        bankingTopics.push(node);
      } else if (upper.includes('GPLX') || upper.includes('LAI_XE') || t.name.includes('Lái xe')) {
        node.domainCode = 'DRIVING';
        drivingTopics.push(node);
      } else if (upper.includes('TNPT') || upper.includes('TIENG_ANH') || upper.includes('TOAN')) {
        node.domainCode = 'EDUCATION';
        eduTopics.push(node);
      } else {
        generalTopics.push(node);
      }
    }

    const groups: CognitiveDomainGroupDto[] = [];
    if (bankingTopics.length > 0) {
      groups.push({
        domainCode: 'BANKING',
        domainName: 'Ngân hàng & Tài chính Enterprise',
        totalTopics: bankingTopics.reduce((s, r) => s + 1 + r.children.length, 0),
        totalQuestions: bankingTopics.reduce((s, r) => s + r.questionCount + r.children.reduce((cs, c) => cs + c.questionCount, 0), 0),
        rootNodes: bankingTopics,
        rootTopics: bankingTopics
      });
    }
    if (drivingTopics.length > 0) {
      groups.push({
        domainCode: 'DRIVING',
        domainName: 'Sát hạch Lái xe Quốc gia (Cục Đường Bộ)',
        totalTopics: drivingTopics.length,
        totalQuestions: drivingTopics.reduce((s, r) => s + r.questionCount, 0),
        rootNodes: drivingTopics,
        rootTopics: drivingTopics
      });
    }
    if (eduTopics.length > 0) {
      groups.push({
        domainCode: 'EDUCATION',
        domainName: 'Khảo thí & Giáo dục Quốc gia',
        totalTopics: eduTopics.length,
        totalQuestions: eduTopics.reduce((s, r) => s + r.questionCount, 0),
        rootNodes: eduTopics,
        rootTopics: eduTopics
      });
    }
    if (generalTopics.length > 0) {
      groups.push({
        domainCode: 'GENERAL',
        domainName: 'Chuyên đề Cộng đồng Khác',
        totalTopics: generalTopics.length,
        totalQuestions: generalTopics.reduce((s, r) => s + r.questionCount, 0),
        rootNodes: generalTopics,
        rootTopics: generalTopics
      });
    }

    return groups;
  }, [domainGroups, legacyTopics]);

  // Bộ lọc theo Domain và Scope
  const filteredGroups = useMemo(() => {
    return effectiveDomainGroups
      .filter(g => selectedDomain === 'ALL' || (g.domainCode || '').toUpperCase() === selectedDomain.toUpperCase())
      .map(group => {
        let roots: CognitiveTopicNodeDto[] = group.rootNodes || group.rootTopics || [];
        if (!Array.isArray(roots)) roots = [];

        // Lọc theo Scope
        if (selectedScope !== 'ALL') {
          roots = roots.filter(r => (r.scope || 'COMMUNITY').toUpperCase() === selectedScope.toUpperCase());
        }

        // Lọc theo Search Query
        if (searchQuery.trim()) {
          const q = searchQuery.toLowerCase().trim();
          roots = roots.map(root => {
            const matchRoot = (root.name || '').toLowerCase().includes(q) || (root.code || '').toLowerCase().includes(q);
            const filteredChildren = (root.children || []).filter(c => (c.name || '').toLowerCase().includes(q) || (c.code || '').toLowerCase().includes(q));
            if (matchRoot || filteredChildren.length > 0) {
              return {
                ...root,
                children: matchRoot ? (root.children || []) : filteredChildren
              };
            }
            return null;
          }).filter(Boolean) as CognitiveTopicNodeDto[];
        }

        return {
          ...group,
          rootNodes: roots,
          rootTopics: roots
        };
      })
      .filter(g => (g.rootNodes || []).length > 0);
  }, [effectiveDomainGroups, selectedDomain, selectedScope, searchQuery]);

  // Đếm tổng số chủ đề & câu hỏi toàn hệ thống
  const allDomainsStats = useMemo(() => {
    let topics = 0;
    let questions = 0;
    for (const g of effectiveDomainGroups) {
      topics += (g.totalTopics || 0);
      questions += (g.totalQuestions ?? g.totalQuestionCount ?? 0);
    }
    return { topics, questions };
  }, [effectiveDomainGroups]);

  // Hàm chọn toàn bộ một cây thư mục
  const handleSelectTree = (node: CognitiveTopicNodeDto, select: boolean) => {
    const keysToUpdate: string[] = [node.code];
    if (node.children && Array.isArray(node.children) && node.children.length > 0) {
      for (const child of node.children) {
        keysToUpdate.push(child.code);
      }
    }

    if (onSelectMultipleTopics) {
      onSelectMultipleTopics(keysToUpdate, select);
    } else {
      for (const key of keysToUpdate) {
        onToggleTopic(key, select);
      }
    }
  };

  // Kiểm tra trạng thái chọn của một cây thư mục
  const getNodeSelectionStatus = (node: CognitiveTopicNodeDto) => {
    const isRootSelected = selectedMap.has(node.code);
    const children = Array.isArray(node.children) ? node.children : [];
    if (children.length === 0) {
      return { all: isRootSelected, some: false, count: isRootSelected ? 1 : 0 };
    }

    const childSelectedCount = children.filter(c => selectedMap.has(c.code)).length;
    const all = childSelectedCount === children.length && (isRootSelected || children.length > 0);
    const some = childSelectedCount > 0 && !all;

    return { all, some, count: childSelectedCount + (isRootSelected ? 1 : 0) };
  };

  return (
    <div className="space-y-4">
      {/* ─── THANH ĐIỀU HƯỚNG LĨNH VỰC (DOMAIN PILLS & STATS) ─── */}
      <div className="rounded-2xl border border-slate-200/90 bg-white/80 p-4 shadow-sm backdrop-blur">
        <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
          <div>
            <div className="flex items-center gap-2">
              <span className="flex h-6 w-6 items-center justify-center rounded-lg bg-cyan-600 text-white shadow-xs text-xs font-bold">
                <Layers className="h-3.5 w-3.5" />
              </span>
              <h3 className="text-sm font-black text-slate-900 tracking-tight">
                Đồ Thị Nhận Thức Chuyên Đề (Universal Cognitive Taxonomy)
              </h3>
            </div>
            <p className="mt-0.5 text-xs text-slate-500">
              Phân cấp đa lĩnh vực, 10.000+ chủ đề • Thư mục Đợt thi gom nhóm chuẩn mực • Lựa chọn 1-chạm thông minh
            </p>
          </div>

          <div className="flex items-center gap-2">
            <span className="inline-flex items-center gap-1.5 rounded-xl border border-cyan-200 bg-cyan-50 px-3 py-1.5 text-xs font-bold text-cyan-800">
              <Sparkles className="h-3.5 w-3.5 text-cyan-600" />
              <span>{allDomainsStats.topics} Chuyên đề</span>
              <span className="text-slate-300">•</span>
              <span>{allDomainsStats.questions.toLocaleString()} Câu hỏi</span>
            </span>
          </div>
        </div>

        {/* BỘ LỌC DOMAIN & SCOPE */}
        <div className="mt-3 flex flex-wrap items-center gap-2 border-t border-slate-100 pt-3">
          <button
            type="button"
            onClick={() => setSelectedDomain('ALL')}
            className={`inline-flex items-center gap-1.5 rounded-xl px-3 py-1.5 text-xs font-bold transition-all ${selectedDomain === 'ALL'
                ? 'bg-slate-900 text-white shadow-sm ring-2 ring-slate-900/20'
                : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
              }`}
          >
            <span>🌐</span>
            <span>Tất Cả Lĩnh Vực</span>
            <span className="ml-1 rounded-md bg-white/20 px-1.5 py-0.2 text-[10px]">{allDomainsStats.topics}</span>
          </button>

          {effectiveDomainGroups.map(group => {
            const isSelected = selectedDomain.toUpperCase() === group.domainCode.toUpperCase();
            const icon = DOMAIN_ICONS[group.domainCode.toUpperCase()] || '📁';
            return (
              <button
                key={group.domainCode}
                type="button"
                onClick={() => setSelectedDomain(group.domainCode)}
                className={`inline-flex items-center gap-1.5 rounded-xl px-3 py-1.5 text-xs font-bold transition-all ${isSelected
                    ? 'bg-cyan-600 text-white shadow-sm ring-2 ring-cyan-500/30'
                    : 'bg-slate-100 text-slate-700 hover:bg-cyan-50 hover:text-cyan-700'
                  }`}
              >
                <span>{icon}</span>
                <span>{group.domainName.split('(')[0].trim()}</span>
                <span className={`ml-1 rounded-md px-1.5 py-0.2 text-[10px] ${isSelected ? 'bg-white/20' : 'bg-slate-200 text-slate-600'}`}>
                  {group.totalTopics}
                </span>
              </button>
            );
          })}

          <div className="ml-auto flex items-center gap-2">
            {/* Lọc Scope: Cộng đồng vs Doanh nghiệp */}
            <div className="inline-flex rounded-xl border border-slate-200 bg-slate-50 p-0.5 text-xs font-semibold">
              <button
                type="button"
                onClick={() => setSelectedScope('ALL')}
                className={`rounded-lg px-2 py-1 transition ${selectedScope === 'ALL' ? 'bg-white text-slate-900 shadow-xs' : 'text-slate-500 hover:text-slate-800'}`}
              >
                Mọi gói
              </button>
              <button
                type="button"
                onClick={() => setSelectedScope('COMMUNITY')}
                className={`inline-flex items-center gap-1 rounded-lg px-2 py-1 transition ${selectedScope === 'COMMUNITY' ? 'bg-white text-cyan-700 shadow-xs font-bold' : 'text-slate-500 hover:text-slate-800'}`}
                title="Gói Cộng Đồng Mở"
              >
                <Globe2 className="h-3 w-3" />
                Cộng đồng
              </button>
              <button
                type="button"
                onClick={() => setSelectedScope('TENANT')}
                className={`inline-flex items-center gap-1 rounded-lg px-2 py-1 transition ${selectedScope === 'TENANT' ? 'bg-white text-indigo-700 shadow-xs font-bold' : 'text-slate-500 hover:text-slate-800'}`}
                title="Dữ liệu Doanh Nghiệp Nội Bộ"
              >
                <Building2 className="h-3 w-3" />
                Doanh nghiệp
              </button>
              <button
                type="button"
                onClick={() => setSelectedScope('TEAM')}
                className={`inline-flex items-center gap-1 rounded-lg px-2 py-1 transition ${selectedScope === 'TEAM' ? 'bg-white text-emerald-700 shadow-xs font-bold' : 'text-slate-500 hover:text-slate-800'}`}
                title="Ngân hàng câu hỏi riêng của Team"
              >
                <Users className="h-3 w-3" />
                Nhóm của tôi
              </button>
            </div>
          </div>
        </div>

        {/* THANH TÌM KIẾM MỜ (FUZZY SEARCH) */}
        <div className="mt-3 relative">
          <Search className="absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
          <input
            type="text"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder="🔍 Tìm nhanh theo tên chuyên đề, đợt thi, mã chủ đề (vd: 2026, tín dụng, B2, tiếng anh)..."
            className="w-full rounded-xl border border-slate-200 bg-slate-50/70 pl-9 pr-4 py-2 text-xs text-slate-800 placeholder-slate-400 outline-none transition focus:border-cyan-500 focus:bg-white focus:ring-2 focus:ring-cyan-200"
          />
          {searchQuery && (
            <button
              type="button"
              onClick={() => setSearchQuery('')}
              className="absolute right-3 top-2 text-xs text-slate-400 hover:text-slate-600 font-bold"
            >
              ✕ Xóa
            </button>
          )}
        </div>
      </div>

      {/* ─── DANH SÁCH CÁC NHÓM LĨNH VỰC & THƯ MỤC CÂY (TREE ACCORDIONS) ─── */}
      <div className="space-y-4">
        {filteredGroups.length === 0 ? (
          <div className="rounded-2xl border border-dashed border-slate-300 bg-slate-50/50 p-8 text-center">
            <Info className="mx-auto h-8 w-8 text-slate-400" />
            <p className="mt-2 text-sm font-bold text-slate-700">Không tìm thấy chuyên đề phù hợp</p>
            <p className="mt-1 text-xs text-slate-500">Vui lòng thử từ khóa tìm kiếm khác hoặc chuyển sang danh mục "Tất Cả Lĩnh Vực".</p>
          </div>
        ) : (
          filteredGroups.map(group => {
            const icon = DOMAIN_ICONS[group.domainCode.toUpperCase()] || '📁';
            return (
              <section
                key={group.domainCode}
                className="rounded-2xl border border-slate-200 bg-white/95 p-4 shadow-sm transition"
              >
                {/* Domain Header */}
                <div className="flex items-center justify-between border-b border-slate-100 pb-3">
                  <div className="flex items-center gap-2">
                    <span className="text-xl">{icon}</span>
                    <div>
                      <h4 className="text-sm font-black text-slate-900 tracking-tight">
                        {group.domainName}
                      </h4>
                      <p className="text-[11px] text-slate-500">
                        Mã Domain: <code className="rounded bg-slate-100 px-1 text-slate-700 font-mono font-bold">{group.domainCode}</code>
                      </p>
                    </div>
                  </div>

                  <span className="rounded-full bg-slate-100 px-2.5 py-0.5 text-xs font-bold text-slate-700">
                    {(group.rootNodes || group.rootTopics || []).length} cây chuyên đề
                  </span>
                </div>

                {/* MA TRẬN SÁT HẠCH LÁI XE QUỐC GIA - ĐẶT ĐÚNG TRONG DOMAIN DRIVING */}
                {isDrivingDomain(group.domainCode) && onSelectDrivingPreset && (
                  <DrivingPresetWidget
                    activePreset={activePreset ?? null}
                    onSelectPreset={onSelectDrivingPreset}
                  />
                )}

                {/* Danh sách các Node trong Domain */}
                <div className="mt-3 space-y-3">
                  {(group.rootNodes || group.rootTopics || []).map(rootNode => {
                    const hasChildren = Array.isArray(rootNode.children) && rootNode.children.length > 0;
                    const isExpanded = Boolean(expandedFolders[rootNode.code]);
                    const selStatus = getNodeSelectionStatus(rootNode);
                    const selected = selectedMap.get(rootNode.code);

                    return (
                      <div
                        key={rootNode.code}
                        className={`rounded-xl border transition-all ${selStatus.all || selStatus.some
                            ? 'border-cyan-300 bg-cyan-50/30 shadow-xs'
                            : 'border-slate-200 bg-slate-50/40 hover:border-slate-300'
                          }`}
                      >
                        {/* Root Header Item */}
                        <div className="flex items-center justify-between p-3">
                          <div className="flex items-center gap-2.5 min-w-0 flex-1">
                            {/* Nút đóng mở thư mục nếu có con */}
                            {hasChildren ? (
                              <button
                                type="button"
                                onClick={() => toggleExpand(rootNode.code)}
                                className="flex h-7 w-7 shrink-0 items-center justify-center rounded-lg border border-slate-200 bg-white text-slate-600 hover:bg-slate-100 hover:text-cyan-700 transition"
                                title={isExpanded ? 'Thu gọn thư mục' : 'Mở rộng thư mục'}
                              >
                                {isExpanded ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}
                              </button>
                            ) : (
                              <div className="w-7 shrink-0" />
                            )}

                            {/* Checkbox chọn Root / Toàn bộ Folder */}
                            <button
                              type="button"
                              onClick={() => handleSelectTree(rootNode, !selStatus.all)}
                              className="flex items-center gap-2 text-left cursor-pointer group"
                            >
                              <span className="shrink-0 text-cyan-600">
                                {selStatus.all ? (
                                  <CheckSquare className="h-5 w-5 text-cyan-600 fill-cyan-100" />
                                ) : selStatus.some ? (
                                  <span className="flex h-5 w-5 items-center justify-center rounded border-2 border-cyan-600 bg-cyan-600 text-white text-[10px] font-black">
                                    -
                                  </span>
                                ) : (
                                  <Square className="h-5 w-5 text-slate-300 group-hover:text-slate-400" />
                                )}
                              </span>

                              {hasChildren ? (
                                <FolderOpen className={`h-5 w-5 shrink-0 ${selStatus.all || selStatus.some ? 'text-amber-500' : 'text-slate-400'}`} />
                              ) : (
                                <Folder className={`h-5 w-5 shrink-0 ${selStatus.all ? 'text-cyan-500' : 'text-slate-400'}`} />
                              )}
                            </button>

                            {/* Tên và thông tin Folder */}
                            <div className="min-w-0 flex-1">
                              <div className="flex items-center gap-2 flex-wrap">
                                <span
                                  className="text-xs font-black text-slate-900 hover:text-cyan-700 cursor-pointer"
                                  onClick={() => hasChildren ? toggleExpand(rootNode.code) : handleSelectTree(rootNode, !selStatus.all)}
                                >
                                  {rootNode.name}
                                </span>

                                {rootNode.scope === 'COMMUNITY' ? (
                                  <span className="inline-flex items-center gap-0.5 rounded bg-emerald-50 px-1.5 py-0.2 text-[10px] font-bold text-emerald-700 border border-emerald-200">
                                    <Globe2 className="h-2.5 w-2.5" /> Gói Cộng Đồng
                                  </span>
                                ) : (
                                  <span className="inline-flex items-center gap-0.5 rounded bg-indigo-50 px-1.5 py-0.2 text-[10px] font-bold text-indigo-700 border border-indigo-200">
                                    <Building2 className="h-2.5 w-2.5" /> Doanh Nghiệp
                                  </span>
                                )}

                                {hasChildren && (
                                  <span className="rounded bg-sky-100 px-2 py-0.2 text-[10px] font-black text-sky-800">
                                    {(rootNode.children || []).length} chuyên đề con
                                  </span>
                                )}
                              </div>

                              <p className="text-[11px] text-slate-500 truncate mt-0.5">
                                Mã: <code className="font-mono text-slate-600">{rootNode.code}</code> • {rootNode.questionCount.toLocaleString()} câu hỏi
                              </p>
                            </div>
                          </div>

                          {/* Thao tác nhanh cho Folder Cha */}
                          {hasChildren && (
                            <div className="flex items-center gap-2 shrink-0 ml-2">
                              <button
                                type="button"
                                onClick={() => handleSelectTree(rootNode, !selStatus.all)}
                                className={`rounded-xl px-2.5 py-1 text-[11px] font-bold transition border ${selStatus.all
                                    ? 'bg-rose-50 text-rose-700 border-rose-200 hover:bg-rose-100'
                                    : 'bg-white text-cyan-700 border-cyan-200 hover:bg-cyan-50 shadow-2xs'
                                  }`}
                              >
                                {selStatus.all ? 'Bỏ chọn cả cây' : '1-Chạm Chọn Tất Cả'}
                              </button>
                            </div>
                          )}
                        </div>

                        {/* Cấu hình câu hỏi nếu là Node đơn lẻ được chọn */}
                        {!hasChildren && selected && (
                          <div className="border-t border-slate-200 bg-white p-3 rounded-b-xl">
                            <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
                              <label className="text-[11px] font-bold uppercase tracking-wider text-slate-500 block">
                                Số lượng câu
                                <input
                                  type="number"
                                  min={1}
                                  value={selected.numberOfQuestions}
                                  onChange={(e) => onUpdateTopicConfig(rootNode.code, { numberOfQuestions: Math.max(1, Number(e.target.value) || 1) })}
                                  className="mt-1 h-8 w-full rounded-lg border border-slate-300 bg-white px-2 text-xs"
                                />
                              </label>

                              <label className="flex items-center gap-1.5 text-[11px] font-bold uppercase tracking-wider text-slate-600 mt-5">
                                <input
                                  type="checkbox"
                                  checked={selected.isRandom}
                                  onChange={(e) => onUpdateTopicConfig(rootNode.code, { isRandom: e.target.checked })}
                                  className="h-4 w-4 rounded border-slate-300"
                                />
                                Lấy ngẫu nhiên
                              </label>

                              {!selected.isRandom && (
                                <label className="text-[11px] font-bold uppercase tracking-wider text-slate-500 block">
                                  Bắt đầu từ câu
                                  <input
                                    type="number"
                                    min={1}
                                    value={selected.startIndex}
                                    onChange={(e) => onUpdateTopicConfig(rootNode.code, { startIndex: Math.max(1, Number(e.target.value) || 1) })}
                                    className="mt-1 h-8 w-full rounded-lg border border-slate-300 bg-white px-2 text-xs"
                                  />
                                </label>
                              )}
                            </div>
                          </div>
                        )}

                        {/* DANH SÁCH CÁC CHUYÊN ĐỀ CON (CHILDREN ACCORDION) */}
                        {hasChildren && isExpanded && (
                          <div className="border-t border-slate-200/80 bg-white/90 p-3 rounded-b-xl space-y-2">
                            <div className="flex items-center justify-between px-1 mb-2">
                              <span className="text-[11px] font-bold uppercase tracking-wider text-slate-500">
                                Danh sách các chuyên đề trực thuộc {rootNode.name}
                              </span>
                              <span className="text-[11px] text-slate-500">
                                Đã chọn {selStatus.count}/{(rootNode.children || []).length} chuyên đề
                              </span>
                            </div>

                            <div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-3">
                              {(rootNode.children || []).map(child => {
                                const childSelected = selectedMap.get(child.code);
                                return (
                                  <div
                                    key={child.code}
                                    className={`rounded-lg border p-2.5 transition ${childSelected
                                        ? 'border-cyan-400 bg-cyan-50/70 shadow-2xs ring-1 ring-cyan-300/50'
                                        : 'border-slate-200 bg-white hover:border-slate-300 hover:bg-slate-50/50'
                                      }`}
                                  >
                                    <div className="flex items-start gap-2">
                                      <input
                                        type="checkbox"
                                        checked={Boolean(childSelected)}
                                        onChange={(e) => onToggleTopic(child.code, e.target.checked)}
                                        className="mt-0.5 h-4 w-4 shrink-0 rounded border-slate-300 text-cyan-600 focus:ring-cyan-500"
                                      />
                                      <div className="min-w-0 flex-1">
                                        <p className="truncate text-xs font-bold text-slate-900" title={child.name}>
                                          {child.name}
                                        </p>
                                        <p className="text-[10px] text-slate-500 truncate">
                                          <code className="font-mono">{child.code}</code> • {child.questionCount} Qs
                                        </p>
                                      </div>
                                    </div>

                                    {/* Cấu hình câu hỏi riêng khi chuyên đề con được chọn */}
                                    {childSelected && (
                                      <div className="mt-2.5 border-t border-cyan-200/60 pt-2 grid grid-cols-2 gap-1.5">
                                        <div>
                                          <label className="text-[10px] font-semibold text-slate-600 block">
                                            Số câu:
                                          </label>
                                          <input
                                            type="number"
                                            min={1}
                                            value={childSelected.numberOfQuestions}
                                            onChange={(e) => onUpdateTopicConfig(child.code, { numberOfQuestions: Math.max(1, Number(e.target.value) || 1) })}
                                            className="h-7 w-full rounded border border-slate-300 bg-white px-1.5 text-xs"
                                          />
                                        </div>
                                        <div className="flex items-end pb-1">
                                          <label className="flex items-center gap-1 text-[10px] font-semibold text-slate-600">
                                            <input
                                              type="checkbox"
                                              checked={childSelected.isRandom}
                                              onChange={(e) => onUpdateTopicConfig(child.code, { isRandom: e.target.checked })}
                                              className="h-3.5 w-3.5 rounded border-slate-300 text-cyan-600"
                                            />
                                            Random
                                          </label>
                                        </div>
                                      </div>
                                    )}
                                  </div>
                                );
                              })}
                            </div>
                          </div>
                        )}
                      </div>
                    );
                  })}
                </div>
              </section>
            );
          })
        )}
      </div>
    </div>
  );
};
