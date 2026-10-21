import React from 'react';
import { Compass, Award, Building2, Calendar } from 'lucide-react';

export interface TaxonomyCoordinateFormProps {
  targetLevel: string;
  setTargetLevel: (val: string) => void;
  assessmentPurpose: string;
  setAssessmentPurpose: (val: string) => void;
  issuingOrg: string;
  setIssuingOrg: (val: string) => void;
  benchmarkYear: string;
  setBenchmarkYear: (val: string) => void;
  benchmarkStandard: string;
  setBenchmarkStandard: (val: string) => void;
}

export const TaxonomyCoordinateForm: React.FC<TaxonomyCoordinateFormProps> = ({
  targetLevel,
  setTargetLevel,
  assessmentPurpose,
  setAssessmentPurpose,
  issuingOrg,
  setIssuingOrg,
  benchmarkYear,
  setBenchmarkYear,
  benchmarkStandard,
  setBenchmarkStandard,
}) => {
  return (
    <div className="space-y-3 bg-slate-950/60 border border-slate-800/80 rounded-2xl p-4">
      <div className="text-[11px] font-extrabold uppercase text-slate-400 flex items-center gap-1.5">
        <Compass size={13} className="text-blue-400" /> 2. Hệ Tọa Độ Tri Thức (Quy chuẩn định danh)
      </div>
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs">
        <div className="space-y-1">
          <label className="text-[10px] text-slate-400 font-bold flex items-center gap-1">
            <Award size={11} className="text-blue-400" /> Cấp bậc / Khối lớp
          </label>
          <input
            type="text"
            value={targetLevel}
            onChange={e => setTargetLevel(e.target.value)}
            placeholder="VD: Lớp 12, Senior RM, Bác sĩ CKI..."
            className="w-full bg-slate-900 border border-slate-750 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-blue-500"
          />
        </div>
        <div className="space-y-1">
          <label className="text-[10px] text-slate-400 font-bold flex items-center gap-1">
            🎯 Mục đích sát hạch / Đánh giá
          </label>
          <input
            type="text"
            value={assessmentPurpose}
            onChange={e => setAssessmentPurpose(e.target.value)}
            placeholder="VD: Thi tốt nghiệp THPT, Tuân thủ AML định kỳ..."
            className="w-full bg-slate-900 border border-slate-750 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-blue-500"
          />
        </div>
        <div className="space-y-1">
          <label className="text-[10px] text-slate-400 font-bold flex items-center gap-1">
            <Building2 size={11} className="text-emerald-400" /> Đơn vị / Cơ quan ban hành
          </label>
          <input
            type="text"
            value={issuingOrg}
            onChange={e => setIssuingOrg(e.target.value)}
            placeholder="VD: Bộ Giáo dục và Đào tạo, Vietcombank, Cục ĐBVN..."
            className="w-full bg-slate-900 border border-slate-750 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-blue-500"
          />
        </div>
        <div className="space-y-1">
          <label className="text-[10px] text-slate-400 font-bold flex items-center gap-1">
            <Calendar size={11} className="text-amber-400" /> Năm & Chuẩn mực quy chiếu
          </label>
          <div className="flex gap-2">
            <input
              type="number"
              value={benchmarkYear}
              onChange={e => setBenchmarkYear(e.target.value)}
              placeholder="2026"
              className="w-24 bg-slate-900 border border-slate-750 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-blue-500"
            />
            <input
              type="text"
              value={benchmarkStandard}
              onChange={e => setBenchmarkStandard(e.target.value)}
              placeholder="Chương trình GDPT 2018 / Thông tư 41..."
              className="flex-1 bg-slate-900 border border-slate-750 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-blue-500"
            />
          </div>
        </div>
      </div>
    </div>
  );
};
