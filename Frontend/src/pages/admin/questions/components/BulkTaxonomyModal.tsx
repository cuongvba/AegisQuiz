import React from 'react';
import { X, Tag, Sparkles, CheckCheck, Loader2, Save } from 'lucide-react';
import { learnerQuizService } from '@/services/learner-quiz.service';
import { AddDomainModalSection } from './AddDomainModalSection';
import { TaxonomyDomainSelector } from './taxonomy/TaxonomyDomainSelector';
import { TaxonomyCoordinateForm } from './taxonomy/TaxonomyCoordinateForm';
import { TaxonomyTagManager } from './taxonomy/TaxonomyTagManager';
import { useBulkTaxonomyState, FALLBACK_DOMAINS } from './taxonomy/useBulkTaxonomyState';

export { FALLBACK_DOMAINS };

export interface BulkTaxonomyApplyPayload {
  domainCode: string;
  targetLevel: string;
  assessmentPurpose: string;
  issuingOrg: string;
  benchmarkYear: number;
  benchmarkStandard: string;
  tags: string[];
  onlyUnassigned: boolean;
  savedToDatabase?: boolean;
}

export interface BulkTaxonomyModalProps {
  isOpen: boolean;
  onClose: () => void;
  onApply: (payload: BulkTaxonomyApplyPayload) => void;
  totalQuestions: number;
  processingFileName?: string;
  docxPreviewList?: any[];
  availableDomains?: any[];
  initialDomain?: string;
  showToast: (msg: string) => void;
}

export const BulkTaxonomyModal: React.FC<BulkTaxonomyModalProps> = ({
  isOpen,
  onClose,
  onApply,
  totalQuestions,
  processingFileName = '',
  docxPreviewList = [],
  availableDomains = [],
  initialDomain = 'EDUCATION',
  showToast,
}) => {
  const state = useBulkTaxonomyState({
    isOpen,
    processingFileName,
    docxPreviewList,
    availableDomains,
    initialDomain,
    showToast,
  });

  const handleApply = async () => {
    state.setIsSaving(true);
    try {
      if (state.saveToDbAsPreset && state.domain && state.domain !== 'GENERAL') {
        const presetsToSave = [
          { type: 'TARGET_LEVEL', val: state.targetLevel },
          { type: 'ASSESSMENT_PURPOSE', val: state.assessmentPurpose },
          { type: 'ISSUING_ORG', val: state.issuingOrg },
          { type: 'BENCHMARK_STANDARD', val: state.benchmarkStandard },
        ].filter(p => p.val && p.val.trim());

        for (const item of presetsToSave) {
          try {
            await learnerQuizService.createCoordinatePreset({
              domainCode: state.domain,
              coordinateType: item.type,
              presetLabel: item.val.trim(),
              synonymsJson: JSON.stringify(state.tags.slice(0, 5)),
              isDefault: false,
              displayOrder: 1,
            });
          } catch {}
        }
      }

      onApply({
        domainCode: state.domain,
        targetLevel: state.targetLevel.trim(),
        assessmentPurpose: state.assessmentPurpose.trim(),
        issuingOrg: state.issuingOrg.trim(),
        benchmarkYear: state.benchmarkYear ? parseInt(state.benchmarkYear) : new Date().getFullYear(),
        benchmarkStandard: state.benchmarkStandard.trim(),
        tags: state.tags,
        onlyUnassigned: state.onlyUnassigned,
        savedToDatabase: state.saveToDbAsPreset,
      });

      onClose();
    } catch (err: any) {
      showToast(`⚠️ Có lỗi khi áp dụng: ${err.message || 'Lỗi xử lý'}`);
    } finally {
      state.setIsSaving(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black/80 backdrop-blur-md z-[85] flex items-center justify-center p-3 sm:p-4">
      <div className="w-full max-w-3xl bg-slate-900 border border-purple-500/40 rounded-3xl p-5 sm:p-7 shadow-2xl relative text-left text-white animate-in fade-in zoom-in-95 duration-150 max-h-[92vh] overflow-y-auto">
        {/* Header Modal */}
        <div className="flex items-center justify-between pb-3 mb-4 border-b border-slate-800">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-2xl bg-gradient-to-br from-purple-500/20 to-blue-500/20 border border-purple-500/40 flex items-center justify-center text-purple-300">
              <Tag size={20} />
            </div>
            <div>
              <h3 className="text-base sm:text-lg font-black text-transparent bg-clip-text bg-gradient-to-r from-purple-300 via-indigo-300 to-blue-300">
                Gán Thẻ & Tọa Độ Đa Ngành Hàng Loạt
              </h3>
              <p className="text-[11px] text-slate-400">
                Cấu hình đồng loạt Hệ 5 Trục Tọa Độ & Thẻ thông minh cho <strong className="text-purple-300">{totalQuestions} câu hỏi</strong> đang duyệt
              </p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <button
              type="button"
              onClick={state.executeAutoSniff}
              disabled={state.isSniffing}
              className="px-3 py-1.5 rounded-xl bg-gradient-to-r from-purple-600 via-indigo-600 to-blue-600 hover:from-purple-500 hover:to-indigo-500 text-white text-xs font-bold flex items-center gap-1.5 shadow-lg shadow-purple-600/30 transition-all cursor-pointer active:scale-95 border border-purple-400/40 disabled:opacity-50"
              title="Tự động nhận diện 5 trục tọa độ & Thẻ thông minh từ Database đa ngành của Tenant"
            >
              <Sparkles size={13} className={`text-amber-300 ${state.isSniffing ? 'animate-spin' : 'animate-pulse'}`} />
              <span>{state.isSniffing ? '⚡ Đang phân tích AI...' : '⚡ Nhận diện lại bằng AI'}</span>
            </button>
            <button
              type="button"
              onClick={onClose}
              className="text-slate-400 hover:text-white p-1.5 rounded-lg hover:bg-slate-800 transition-colors cursor-pointer"
            >
              <X size={18} />
            </button>
          </div>
        </div>

        {/* Banner nhận diện thông minh */}
        {(state.targetLevel || state.assessmentPurpose || processingFileName) && (
          <div className="mb-4 p-3 bg-purple-950/40 border border-purple-500/40 rounded-2xl flex items-center justify-between text-xs text-purple-200">
            <div className="flex items-center gap-2 overflow-hidden">
              <Sparkles size={15} className="text-amber-300 shrink-0" />
              <span className="truncate">
                Nhận diện thông minh: <strong className="text-white">{state.targetLevel || processingFileName || 'Chưa định danh'}</strong>
                {state.assessmentPurpose ? ` • ${state.assessmentPurpose}` : ''} ({state.benchmarkYear})
              </span>
            </div>
            <span className="text-[10px] font-bold px-2 py-0.5 rounded-full bg-purple-900/70 border border-purple-500/50 text-purple-300 shrink-0 ml-2">
              ✓ AI Verified
            </span>
          </div>
        )}

        <div className="space-y-4">
          {/* 1. Chọn Miền Ngành Nghề Đào Tạo (Domain) */}
          <TaxonomyDomainSelector
            domain={state.domain}
            domainList={state.domainList}
            onSelectDomain={state.setDomain}
            onOpenAddDomain={() => state.setShowAddDomainModal(true)}
          />

          {/* Form thêm ngành mới (OCP) */}
          {state.showAddDomainModal && (
            <AddDomainModalSection
              onClose={() => state.setShowAddDomainModal(false)}
              onCreated={state.handleDomainCreated}
              showToast={showToast}
              existingCodes={state.domainList.map(d => d.code)}
            />
          )}

          {/* 2. Hệ Tọa Độ Tri Thức (Quy Chuẩn Định Danh) */}
          <TaxonomyCoordinateForm
            targetLevel={state.targetLevel}
            setTargetLevel={state.setTargetLevel}
            assessmentPurpose={state.assessmentPurpose}
            setAssessmentPurpose={state.setAssessmentPurpose}
            issuingOrg={state.issuingOrg}
            setIssuingOrg={state.setIssuingOrg}
            benchmarkYear={state.benchmarkYear}
            setBenchmarkYear={state.setBenchmarkYear}
            benchmarkStandard={state.benchmarkStandard}
            setBenchmarkStandard={state.setBenchmarkStandard}
          />

          {/* 3. Thẻ Thông Minh (Smart Tags) */}
          <TaxonomyTagManager
            tags={state.tags}
            suggestedTags={state.suggestedTags}
            domain={state.domain}
            onAddTag={state.handleAddTag}
            onRemoveTag={state.handleRemoveTag}
          />

          {/* Tùy chọn phạm vi & Lưu vào DB tái sử dụng (100% OCP) */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-2.5">
            <div className="p-3 bg-slate-950/60 border border-purple-500/20 rounded-2xl">
              <label className="flex items-center gap-2.5 cursor-pointer text-xs text-slate-200 select-none">
                <input
                  type="checkbox"
                  checked={state.onlyUnassigned}
                  onChange={e => state.setOnlyUnassigned(e.target.checked)}
                  className="rounded bg-slate-900 border-slate-750 text-purple-500 focus:ring-0 cursor-pointer"
                />
                <span className="font-medium text-slate-300">
                  Chỉ áp dụng cho các câu hỏi chưa được gán ngành riêng
                </span>
              </label>
            </div>

            <div className="p-3 bg-slate-950/60 border border-emerald-500/30 rounded-2xl">
              <label className="flex items-center gap-2.5 cursor-pointer text-xs text-slate-200 select-none">
                <input
                  type="checkbox"
                  checked={state.saveToDbAsPreset}
                  onChange={e => state.setSaveToDbAsPreset(e.target.checked)}
                  className="rounded bg-slate-900 border-slate-750 text-emerald-500 focus:ring-0 cursor-pointer"
                />
                <span className="font-bold text-emerald-300 flex items-center gap-1.5">
                  <Save size={12} /> Lưu vào Database để tự động nhận diện lần sau
                </span>
              </label>
            </div>
          </div>

          {/* Nút hành động */}
          <div className="flex items-center justify-end gap-3 pt-2">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2.5 rounded-xl bg-slate-800 hover:bg-slate-750 text-slate-300 text-xs font-bold transition-colors cursor-pointer"
            >
              Hủy bỏ
            </button>
            <button
              type="button"
              onClick={handleApply}
              disabled={state.isSaving}
              className="px-6 py-2.5 rounded-xl bg-gradient-to-r from-purple-600 via-indigo-600 to-blue-600 hover:from-purple-500 hover:to-blue-500 text-white text-xs font-black transition-all shadow-lg shadow-purple-950/50 flex items-center gap-1.5 cursor-pointer active:scale-95 disabled:opacity-50"
            >
              {state.isSaving ? (
                <>
                  <Loader2 size={14} className="animate-spin" /> Đang lưu tri thức...
                </>
              ) : (
                <>
                  <CheckCheck size={14} /> Áp dụng cho {totalQuestions} câu hỏi
                </>
              )}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default BulkTaxonomyModal;
