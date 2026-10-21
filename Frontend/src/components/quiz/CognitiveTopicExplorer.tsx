import React, { useState, useMemo, useCallback } from 'react';
import { Search, Sparkles, Layers } from 'lucide-react';
import type {
  CognitiveDomainGroupDto,
  CognitiveTopicNodeDto,
  ExamCategoryPreset,
  PracticeTopicConfig,
  TopicSummary,
} from '@/types/quiz';
import { DrivingPresetWidget } from '@/components/quiz/DrivingPresetWidget';
import { useLearnerI18n } from '@/lib/i18n';
import { DomainTabBar } from './topic-explorer/DomainTabBar';
import { TopicTreeNode } from './topic-explorer/TopicTreeNode';

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
  isDomainLocked?: boolean;
  forcedDomainCode?: string;
  t?: (key: string, params?: any) => string;
}

export const CognitiveTopicExplorer: React.FC<Props> = ({
  domainGroups,
  legacyTopics,
  selectedMap,
  activePreset,
  onSelectDrivingPreset,
  onToggleTopic,
  onUpdateTopicConfig,
  onSelectMultipleTopics,
  isDomainLocked = false,
  forcedDomainCode,
  t: propT,
}) => {
  const { t: hookT } = useLearnerI18n();
  const t = propT || hookT;
  const [selectedDomain, setSelectedDomain] = useState<string>(() => forcedDomainCode || 'ALL');
  const [searchQuery, setSearchQuery] = useState<string>('');
  const [expandedFolders, setExpandedFolders] = useState<Record<string, boolean>>({
    '2026_DOT2': true,
    '2026-DOT2': true,
  });

  const toggleExpand = useCallback((code: string) => {
    setExpandedFolders((prev) => ({ ...prev, [code]: !prev[code] }));
  }, []);

  // Tổng hợp dữ liệu Domain Groups nếu backend trả về cấu trúc phẳng
  const effectiveDomainGroups = useMemo<CognitiveDomainGroupDto[]>(() => {
    if (domainGroups && domainGroups.length > 0) return domainGroups;
    if (!legacyTopics || legacyTopics.length === 0) return [];
    return [
      {
        domainCode: 'GENERAL',
        domainName: 'Kho Tri Thức Tổng Hợp',
        totalQuestions: legacyTopics.reduce((acc, t) => acc + (t.questionCount || 0), 0),
        totalTopics: legacyTopics.length,
        rootNodes: legacyTopics.map((t) => ({
          id: (t as any).id || t.key,
          code: t.key,
          name: t.name,
          domainCode: 'GENERAL',
          depthLevel: 1,
          questionCount: t.questionCount || 0,
          scope: 'COMMUNITY',
          children: [],
        })),
      },
    ];
  }, [domainGroups, legacyTopics]);


  // Bộ lọc theo Domain hiện tại
  const activeGroups = useMemo(() => {
    const domainToFilter = forcedDomainCode || selectedDomain;
    if (domainToFilter === 'ALL') return effectiveDomainGroups;
    return effectiveDomainGroups.filter(
      (g) => g.domainCode.toUpperCase() === domainToFilter.toUpperCase()
    );
  }, [effectiveDomainGroups, selectedDomain, forcedDomainCode]);

  // Lấy toàn bộ mã lá (Leaf Topic Keys) dưới một Node
  const collectLeafKeys = useCallback((node: CognitiveTopicNodeDto): string[] => {
    if (!node.children || node.children.length === 0) return [node.code];
    return node.children.flatMap(collectLeafKeys);
  }, []);

  // Trạng thái chọn của một Node (tất cả / một phần / chưa chọn)
  const getNodeSelectionStatus = useCallback(
    (node: CognitiveTopicNodeDto) => {
      const keys = collectLeafKeys(node);
      const selCount = keys.filter((k) => selectedMap.has(k)).length;
      return {
        all: keys.length > 0 && selCount === keys.length,
        some: selCount > 0 && selCount < keys.length,
        count: selCount,
        total: keys.length,
      };
    },
    [collectLeafKeys, selectedMap]
  );

  const handleSelectTree = useCallback(
    (node: CognitiveTopicNodeDto, select: boolean) => {
      const keys = collectLeafKeys(node);
      if (onSelectMultipleTopics) {
        onSelectMultipleTopics(keys, select);
      } else {
        keys.forEach((k) => onToggleTopic(k, select));
      }
    },
    [collectLeafKeys, onSelectMultipleTopics, onToggleTopic]
  );

  const isDrivingActive = useMemo(() => {
    const d = (forcedDomainCode || selectedDomain).toUpperCase();
    return d.includes('DRIV') || d.includes('GPLX') || d.includes('LAI_XE');
  }, [forcedDomainCode, selectedDomain]);

  return (
    <div className="space-y-4">
      {/* ─── THANH ĐIỀU HƯỚNG DOMAIN VÀ THỐNG KÊ ─── */}
      <div className="rounded-2xl border border-slate-800 bg-slate-900/90 p-4 shadow-xl backdrop-blur">
        <div className="flex flex-wrap items-center justify-between gap-3 mb-4">
          <div className="flex items-center gap-2.5">
            <span className="flex h-7 w-7 items-center justify-center rounded-lg bg-cyan-600 text-white font-bold">
              <Layers className="h-4 w-4" />
            </span>
            <div>
              <h3 className="text-sm font-black text-white">{t('cognitiveTopicGraph') || 'Đồ Thị Nhận Thức Chuyên Đề Phân Tầng'}</h3>
              <p className="text-xs text-slate-400">{t('cognitiveTopicGraphDesc') || 'Chọn 1-chạm hoặc phân bổ số lượng câu hỏi linh hoạt'}</p>
            </div>
          </div>

          <div className="flex items-center gap-2 text-xs">
            <span className="px-3 py-1.5 rounded-xl bg-slate-800 text-cyan-300 font-bold border border-cyan-500/20 flex items-center gap-1.5">
              <Sparkles className="h-3.5 w-3.5 text-cyan-400" />
              <span>{t('selectingTopicsCount', { count: selectedMap.size }) || `Đang chọn ${selectedMap.size} chủ đề`}</span>
            </span>
          </div>
        </div>

        {/* Thanh lọc Domain Tabs */}
        <DomainTabBar
          domainGroups={effectiveDomainGroups}
          selectedDomain={forcedDomainCode || selectedDomain}
          onSelectDomain={setSelectedDomain}
          isDomainLocked={isDomainLocked}
          t={t}
        />

        {/* Thanh tìm kiếm */}
        <div className="relative mt-3">
          <Search className="absolute left-3 top-2.5 h-4 w-4 text-slate-400" />
          <input
            type="text"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder={t('searchTopicPlaceholder') || 'Tìm kiếm chuyên đề, mã môn hoặc nội dung bài học...'}
            className="w-full h-9 pl-9 pr-4 rounded-xl border border-slate-700 bg-slate-950 text-xs text-white placeholder-slate-500 focus:outline-hidden focus:border-cyan-500"
          />
        </div>
      </div>

      {/* ─── WIDGET CHUYÊN BIỆT ÔN THI LÁI XE GPLX (NẾU THUỘC DOMAIN LÁI XE) ─── */}
      {isDrivingActive && onSelectDrivingPreset && (
        <DrivingPresetWidget activePreset={activePreset || null} onSelectPreset={onSelectDrivingPreset} />
      )}

      {/* ─── CÂY THƯ MỤC CÁC CHUYÊN ĐỀ ─── */}
      <div className="space-y-3">
        {activeGroups.map((group) => (
          <div key={group.domainCode} className="space-y-2">
            {(group.rootNodes || group.rootTopics || []).map((rootNode) => (
              <TopicTreeNode
                key={rootNode.code}
                node={rootNode}
                isExpanded={Boolean(expandedFolders[rootNode.code])}
                onToggleExpand={toggleExpand}
                selStatus={getNodeSelectionStatus(rootNode)}
                selectedConfig={selectedMap.get(rootNode.code)}
                onSelectTree={handleSelectTree}
                onUpdateConfig={onUpdateTopicConfig}
                t={t}
                childrenNodes={
                  rootNode.children && rootNode.children.length > 0 ? (
                    <div className="space-y-2">
                      {rootNode.children.map((child) => (
                        <TopicTreeNode
                          key={child.code}
                          node={child}
                          isExpanded={Boolean(expandedFolders[child.code])}
                          onToggleExpand={toggleExpand}
                          selStatus={getNodeSelectionStatus(child)}
                          selectedConfig={selectedMap.get(child.code)}
                          onSelectTree={handleSelectTree}
                          onUpdateConfig={onUpdateTopicConfig}
                          t={t}
                        />
                      ))}
                    </div>
                  ) : null
                }
              />
            ))}
          </div>
        ))}
      </div>
    </div>
  );
};
