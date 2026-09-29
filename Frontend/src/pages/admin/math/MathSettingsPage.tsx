import React, { useState, useEffect } from 'react';
import { 
  Sliders, Type, Sparkles, Save, RefreshCw, RotateCcw, 
  CheckCircle2, AlertCircle, Eye, Layers, FlaskConical, 
  Compass, Atom, Dna, ZoomIn, Split, Check, Info,
  Plus, Trash2, X, Code2
} from 'lucide-react';
import { useMathConfig, MATH_PRESETS, MathEngineSettings } from '@/shared/context/MathConfigContext';
import { quizApi } from '@/shared/api/quiz-api';
import { MathRenderer } from '@/components/common/MathRenderer';
import { createLogger } from '@/shared/logger';

const logger = createLogger('MathSettingsPage');

export const MathSettingsPage: React.FC = () => {
  const { 
    settings, 
    baseFontUnit, 
    letterSpacing, 
    setBaseFontUnit, 
    setLetterSpacing, 
    updateSettings, 
    resetToDefaults,
    applyPreset,
    activePresetId
  } = useMathConfig();

  // Local state cho các thông số chi tiết
  const [localSettings, setLocalSettings] = useState<MathEngineSettings>(settings);
  const [activeTab, setActiveTab] = useState<'typography' | 'subjects' | 'presets' | 'structuralRules'>('typography');
  const [selectedSubject, setSelectedSubject] = useState<string>('MATH');
  const [customTestLatex, setCustomTestLatex] = useState<string>('Oyz \\quad Oxyz \\quad \\vec{u} \\cdot \\vec{v} = \\frac{1}{2}');
  const [saving, setSaving] = useState<boolean>(false);
  const [reloading, setReloading] = useState<boolean>(false);
  const [structuralRules, setStructuralRules] = useState<any[]>([]);
  const [loadingRules, setLoadingRules] = useState<boolean>(false);
  const [togglingRuleId, setTogglingRuleId] = useState<string | null>(null);
  const [notification, setNotification] = useState<{ type: 'success' | 'error' | 'info'; message: string } | null>(null);

  // Modal states
  const [showAddRuleModal, setShowAddRuleModal] = useState<boolean>(false);
  const [showAddTokenModal, setShowAddTokenModal] = useState<boolean>(false);
  const [tokenType, setTokenType] = useState<'symbol' | 'omml'>('symbol');

  const [newRule, setNewRule] = useState({
    id: '',
    name: '',
    priority: 85,
    anchorType: 'GEOMETRIC_LINE',
    template: '\\frac{{{numerator}}}{{{denominator}}}',
    slot1Name: 'numerator',
    slot1Pos: 'ABOVE_ANCHOR',
    slot2Name: 'denominator',
    slot2Pos: 'BELOW_ANCHOR'
  });

  const [newGlyph, setNewGlyph] = useState({
    font: 'Symbol',
    charHex: '',
    replacement: '',
    latex: '',
    desc: ''
  });

  const [newOmml, setNewOmml] = useState({
    token: '',
    latex: ''
  });

  const fetchStructuralRules = async () => {
    setLoadingRules(true);
    try {
      const res = await quizApi.getMathDictionary();
      if (res?.data?.structuralSimplificationRules) {
        setStructuralRules(res.data.structuralSimplificationRules);
      }
    } catch (err: any) {
      logger.error('Lỗi khi tải quy tắc cấu trúc:', err);
    } finally {
      setLoadingRules(false);
    }
  };

  const handleToggleRule = async (ruleId: string) => {
    setTogglingRuleId(ruleId);
    try {
      const res = await quizApi.toggleStructuralRule(ruleId);
      setStructuralRules(prev => prev.map(r => r.id === ruleId ? { ...r, enabled: !r.enabled } : r));
      showNotification('success', res?.message || `Đã cập nhật trạng thái quy tắc '${ruleId}'.`);
    } catch (err: any) {
      logger.error('Lỗi khi toggle quy tắc cấu trúc:', err);
      showNotification('error', `Không thể cập nhật quy tắc: ${err?.message || 'Lỗi mạng'}`);
    } finally {
      setTogglingRuleId(null);
    }
  };

  const handleDeleteRule = async (ruleId: string) => {
    if (!window.confirm(`Bạn có chắc chắn muốn xóa quy tắc '${ruleId}' không?`)) return;
    try {
      await quizApi.deleteStructuralRule(ruleId);
      setStructuralRules(prev => prev.filter(r => r.id !== ruleId));
      showNotification('success', `Đã xóa quy tắc '${ruleId}' thành công.`);
    } catch (err: any) {
      showNotification('error', `Không thể xóa quy tắc: ${err?.message || 'Thất bại'}`);
    }
  };

  const handleSaveNewRule = async () => {
    if (!newRule.id.trim() || !newRule.name.trim()) {
      showNotification('error', 'Vui lòng nhập đầy đủ Mã ID và Tên quy tắc.');
      return;
    }

    const payload = {
      id: newRule.id.trim(),
      name: newRule.name.trim(),
      priority: Number(newRule.priority) || 50,
      enabled: true,
      anchor: {
        type: newRule.anchorType
      },
      slots: [
        {
          name: newRule.slot1Name.trim(),
          relativePosition: newRule.slot1Pos,
          optional: false
        },
        ...(newRule.slot2Name.trim() ? [{
          name: newRule.slot2Name.trim(),
          relativePosition: newRule.slot2Pos,
          optional: false
        }] : [])
      ],
      constraints: {
        disallowPolygons: true
      },
      template: newRule.template.trim()
    };

    try {
      await quizApi.upsertStructuralRule(payload);
      showNotification('success', `Đã lưu và đồng bộ quy tắc '${newRule.id}' vào hệ thống!`);
      setShowAddRuleModal(false);
      setNewRule({
        id: '',
        name: '',
        priority: 85,
        anchorType: 'GEOMETRIC_LINE',
        template: '\\frac{{{numerator}}}{{{denominator}}}',
        slot1Name: 'numerator',
        slot1Pos: 'ABOVE_ANCHOR',
        slot2Name: 'denominator',
        slot2Pos: 'BELOW_ANCHOR'
      });
      fetchStructuralRules();
    } catch (err: any) {
      showNotification('error', `Lỗi khi lưu quy tắc: ${err?.message || 'Thất bại'}`);
    }
  };

  const handleSaveToken = async () => {
    try {
      if (tokenType === 'symbol') {
        if (!newGlyph.charHex.trim() || !newGlyph.replacement.trim()) {
          showNotification('error', 'Mã Hex và Ký tự thay thế là bắt buộc.');
          return;
        }
        await quizApi.upsertSymbolGlyph(newGlyph);
        showNotification('success', `Đã thêm/cập nhật ký hiệu '${newGlyph.charHex}' (${newGlyph.font}) vào từ điển!`);
        setNewGlyph({ font: 'Symbol', charHex: '', replacement: '', latex: '', desc: '' });
      } else {
        if (!newOmml.token.trim() || !newOmml.latex.trim()) {
          showNotification('error', 'Ký tự OMML và lệnh LaTeX là bắt buộc.');
          return;
        }
        await quizApi.upsertOmmlToken(newOmml);
        showNotification('success', `Đã thêm/cập nhật OMML token '${newOmml.token}' -> '${newOmml.latex}'!`);
        setNewOmml({ token: '', latex: '' });
      }
      setShowAddTokenModal(false);
    } catch (err: any) {
      showNotification('error', `Lỗi khi lưu từ điển: ${err?.message || 'Thất bại'}`);
    }
  };

  // Đồng bộ khi context settings thay đổi
  useEffect(() => {
    setLocalSettings(settings);
  }, [settings]);

  useEffect(() => {
    fetchStructuralRules();
  }, []);

  const showNotification = (type: 'success' | 'error' | 'info', message: string) => {
    setNotification({ type, message });
    setTimeout(() => {
      setNotification(null);
    }, 4000);
  };

  // Cập nhật giá trị tức thì cho cả local state và context (để kích hoạt CSS Variable ngay lập tức)
  const handleLetterSpacingChange = (valStr: string) => {
    setLetterSpacing(valStr);
    setLocalSettings(prev => ({ ...prev, letterSpacing: valStr }));
    updateSettings({ letterSpacing: valStr });
  };

  const handleBaseFontUnitChange = (valNum: number) => {
    setBaseFontUnit(valNum);
    setLocalSettings(prev => ({ ...prev, baseFontUnit: valNum }));
    updateSettings({ baseFontUnit: valNum });
  };

  const handleCustomParamChange = <K extends keyof MathEngineSettings>(key: K, value: MathEngineSettings[K]) => {
    setLocalSettings(prev => ({ ...prev, [key]: value }));
    updateSettings({ [key]: value });
  };

  // Lưu xuống Backend Registry vĩnh viễn (Hot-Reload)
  const handleSaveToBackend = async () => {
    setSaving(true);
    try {
      const payload = {
        ...localSettings,
        baseFontUnit,
        letterSpacing,
        lastUpdated: new Date().toISOString()
      };
      await quizApi.updateMathEngineSettings(payload);
      showNotification('success', 'Đã lưu cấu hình Typography & Math Engine vĩnh viễn vào hệ thống!');
    } catch (err: any) {
      logger.error('Lỗi khi lưu cấu hình Math Engine:', err);
      showNotification('error', 'Không thể lưu cấu hình xuống Backend: ' + (err?.message || 'Lỗi mạng hoặc quyền hạn'));
    } finally {
      setSaving(false);
    }
  };

  // Nạp lại cấu hình từ file Registry của Backend
  const handleReloadFromDisk = async () => {
    setReloading(true);
    try {
      await quizApi.reloadMathDictionary();
      const remoteSettings = await quizApi.getMathEngineSettings();
      if (remoteSettings) {
        updateSettings(remoteSettings);
        if (remoteSettings.letterSpacing) setLetterSpacing(remoteSettings.letterSpacing);
        if (remoteSettings.baseFontUnit) setBaseFontUnit(remoteSettings.baseFontUnit);
      }
      showNotification('success', 'Đã nạp lại đăng bạ từ điển và cấu hình SVG từ file đĩa thành công!');
    } catch (err: any) {
      logger.error('Lỗi khi nạp lại cấu hình:', err);
      showNotification('error', 'Không thể nạp lại cấu hình: ' + (err?.message || 'Thất bại'));
    } finally {
      setReloading(false);
    }
  };

  // Khôi phục mặc định chuẩn
  const handleReset = () => {
    if (window.confirm('Bạn có chắc chắn muốn khôi phục tất cả thông số Typography về chuẩn mặc định THPT 2026?')) {
      resetToDefaults();
      showNotification('info', 'Đã khôi phục các thông số về mặc định chuẩn.');
    }
  };

  // Xử lý áp dụng cấu hình môn học
  const handleSubjectSelect = (subCode: string) => {
    setSelectedSubject(subCode);
    const profile = localSettings.subjectProfiles?.[subCode];
    if (profile) {
      if (profile.letterSpacing) handleLetterSpacingChange(profile.letterSpacing);
      if (profile.baseFontUnit) handleBaseFontUnitChange(profile.baseFontUnit);
      showNotification('info', `Đã kích hoạt hồ sơ tối ưu cho môn ${profile.subjectName}`);
    }
  };

  const letterSpacingNum = parseFloat(letterSpacing) || 0.1;

  return (
    <div className="space-y-6 pb-12">
      {/* HEADER BẢNG ĐIỀU KHIỂN */}
      <div className="flex flex-col lg:flex-row items-start lg:items-center justify-between gap-4 bg-slate-900/90 border border-slate-800 p-6 rounded-2xl shadow-xl backdrop-blur-md">
        <div>
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-gradient-to-tr from-cyan-500 to-blue-600 flex items-center justify-center shadow-lg shadow-cyan-500/20">
              <Sliders className="w-5 h-5 text-white" />
            </div>
            <div>
              <h1 className="text-xl font-black bg-gradient-to-r from-cyan-400 via-sky-300 to-blue-400 bg-clip-text text-transparent">
                Trung Tâm Cấu Hình Typography & SVG Math Engine
              </h1>
              <p className="text-xs text-slate-400 font-medium">
                Kiểm soát khoảng cách ký tự (Letter Spacing), tỷ lệ cỡ chữ, nét vẽ vector và tối ưu hiển thị công thức toán học SVG
              </p>
            </div>
          </div>
        </div>

        {/* NÚT THAO TÁC NHANH */}
        <div className="flex flex-wrap items-center gap-2.5 w-full lg:w-auto">
          <button
            onClick={handleReloadFromDisk}
            disabled={reloading}
            className="flex items-center gap-2 px-3.5 py-2 rounded-xl text-xs font-bold bg-slate-800 hover:bg-slate-700 text-slate-200 border border-slate-700 transition-all cursor-pointer disabled:opacity-50"
            title="Nạp lại từ điển & cấu hình từ file đĩa JSON"
          >
            <RefreshCw size={14} className={reloading ? 'animate-spin text-cyan-400' : ''} />
            <span>Nạp lại Registry</span>
          </button>

          <button
            onClick={handleReset}
            className="flex items-center gap-2 px-3.5 py-2 rounded-xl text-xs font-bold bg-slate-800 hover:bg-slate-700 text-amber-300 border border-amber-500/30 transition-all cursor-pointer"
            title="Khôi phục thông số mặc định"
          >
            <RotateCcw size={14} />
            <span>Mặc định</span>
          </button>

          <button
            onClick={handleSaveToBackend}
            disabled={saving}
            className="flex items-center gap-2 px-4 py-2 rounded-xl text-xs font-black bg-gradient-to-r from-cyan-500 to-blue-600 hover:from-cyan-400 hover:to-blue-500 text-white shadow-lg shadow-cyan-500/25 transition-all cursor-pointer disabled:opacity-50"
          >
            {saving ? <RefreshCw size={14} className="animate-spin" /> : <Save size={14} />}
            <span>Lưu Hệ Thống (Hot-Reload)</span>
          </button>
        </div>
      </div>

      {/* THÔNG BÁO TRẠNG THÁI */}
      {notification && (
        <div className={`p-4 rounded-xl border flex items-center gap-3 transition-all animate-in fade-in slide-in-from-top-2 ${
          notification.type === 'success' 
            ? 'bg-emerald-950/70 border-emerald-500/50 text-emerald-200' 
            : notification.type === 'error'
            ? 'bg-rose-950/70 border-rose-500/50 text-rose-200'
            : 'bg-blue-950/70 border-blue-500/50 text-blue-200'
        }`}>
          {notification.type === 'success' && <CheckCircle2 size={18} className="text-emerald-400 flex-shrink-0" />}
          {notification.type === 'error' && <AlertCircle size={18} className="text-rose-400 flex-shrink-0" />}
          {notification.type === 'info' && <Info size={18} className="text-blue-400 flex-shrink-0" />}
          <span className="text-xs font-medium">{notification.message}</span>
        </div>
      )}

      {/* KHÔNG GIAN LÀM VIỆC CHÍNH 2 CỘT */}
      <div className="grid grid-cols-1 xl:grid-cols-12 gap-6">
        {/* CỘT TRÁI: BỘ ĐIỀU KHIỂN THÔNG SỐ (7 CỘT) */}
        <div className="xl:col-span-7 space-y-5">
          {/* TABS CHỌN KHU VỰC CẤU HÌNH */}
          <div className="flex bg-slate-900/90 p-1.5 rounded-xl border border-slate-800 gap-1.5">
            <button
              onClick={() => setActiveTab('typography')}
              className={`flex-1 flex items-center justify-center gap-2 py-2 px-3 rounded-lg text-xs font-bold transition-all cursor-pointer ${
                activeTab === 'typography'
                  ? 'bg-gradient-to-r from-cyan-600/30 to-blue-600/30 text-cyan-300 border border-cyan-500/40 shadow-sm'
                  : 'text-slate-400 hover:text-white hover:bg-slate-800/50'
              }`}
            >
              <Type size={14} />
              <span>Typography & Glyph Spacing</span>
            </button>
            <button
              onClick={() => setActiveTab('subjects')}
              className={`flex-1 flex items-center justify-center gap-2 py-2 px-3 rounded-lg text-xs font-bold transition-all cursor-pointer ${
                activeTab === 'subjects'
                  ? 'bg-gradient-to-r from-cyan-600/30 to-blue-600/30 text-cyan-300 border border-cyan-500/40 shadow-sm'
                  : 'text-slate-400 hover:text-white hover:bg-slate-800/50'
              }`}
            >
              <Compass size={14} />
              <span>Hồ Sơ Môn Học</span>
            </button>
            <button
              onClick={() => setActiveTab('presets')}
              className={`flex-1 flex items-center justify-center gap-2 py-2 px-3 rounded-lg text-xs font-bold transition-all cursor-pointer ${
                activeTab === 'presets'
                  ? 'bg-gradient-to-r from-cyan-600/30 to-blue-600/30 text-cyan-300 border border-cyan-500/40 shadow-sm'
                  : 'text-slate-400 hover:text-white hover:bg-slate-800/50'
              }`}
            >
              <Sparkles size={14} />
              <span>Bộ Cấu Hình Nhanh</span>
            </button>
            <button
              onClick={() => { setActiveTab('structuralRules'); fetchStructuralRules(); }}
              className={`flex-1 flex items-center justify-center gap-2 py-2 px-3 rounded-lg text-xs font-bold transition-all cursor-pointer ${
                activeTab === 'structuralRules'
                  ? 'bg-gradient-to-r from-purple-600/30 to-pink-600/30 text-purple-300 border border-purple-500/40 shadow-sm'
                  : 'text-slate-400 hover:text-white hover:bg-slate-800/50'
              }`}
            >
              <Layers size={14} />
              <span>Quy Tắc Cấu Trúc (P2)</span>
            </button>
          </div>

          {/* TAB 1: TYPOGRAPHY & GLYPH SPACING */}
          {activeTab === 'typography' && (
            <div className="space-y-4">
              {/* CARD 1: KHOẢNG CÁCH KÝ TỰ (LETTER SPACING) - GIẢI PHÁP CHO LỖI OYZ */}
              <div className="bg-slate-900/80 border border-slate-800 p-5 rounded-2xl shadow-lg relative overflow-hidden">
                <div className="absolute top-0 right-0 w-32 h-32 bg-cyan-500/5 rounded-full blur-2xl pointer-events-none" />
                <div className="flex items-center justify-between mb-3">
                  <div className="flex items-center gap-2.5">
                    <div className="p-2 rounded-lg bg-emerald-500/10 border border-emerald-500/20 text-emerald-400">
                      <Split size={16} />
                    </div>
                    <div>
                      <h3 className="text-sm font-bold text-white flex items-center gap-2">
                        Khoảng Cách Ký Tự (Letter Spacing)
                        <span className="text-[10px] px-2 py-0.5 rounded-full bg-emerald-950/80 border border-emerald-500/40 text-emerald-300 font-mono">
                          Xử lý dính chữ Oyz
                        </span>
                      </h3>
                      <p className="text-[11px] text-slate-400">
                        Quy định khoảng cách chèn giữa các glyph toán học nghiêng trong thẻ &lt;text&gt; của file SVG
                      </p>
                    </div>
                  </div>
                  <span className="text-base font-mono font-black text-emerald-400 bg-emerald-950/60 px-2.5 py-1 rounded-lg border border-emerald-500/30">
                    {letterSpacing}
                  </span>
                </div>

                <div className="space-y-2 mt-4">
                  <input
                    type="range"
                    min="0.00"
                    max="0.30"
                    step="0.01"
                    value={letterSpacingNum}
                    onChange={e => handleLetterSpacingChange(`${parseFloat(e.target.value).toFixed(2)}em`)}
                    className="w-full accent-emerald-400 cursor-pointer h-2 bg-slate-800 rounded-lg"
                  />
                  <div className="flex justify-between text-[10px] font-mono text-slate-400">
                    <span className="hover:text-white cursor-pointer" onClick={() => handleLetterSpacingChange('0.00em')}>0.00em (Khít)</span>
                    <span className="hover:text-white cursor-pointer" onClick={() => handleLetterSpacingChange('0.08em')}>0.08em (Gọn)</span>
                    <span className="text-emerald-400 font-bold hover:underline cursor-pointer" onClick={() => handleLetterSpacingChange('0.12em')}>0.12em (Chuẩn Toán)</span>
                    <span className="text-cyan-400 font-bold hover:underline cursor-pointer" onClick={() => handleLetterSpacingChange('0.15em')}>0.15em (Thoáng - Đề xuất)</span>
                    <span className="hover:text-white cursor-pointer" onClick={() => handleLetterSpacingChange('0.25em')}>0.25em (Rộng)</span>
                  </div>
                </div>

                {/* GỢI Ý NHANH CHO OYZ */}
                <div className="mt-4 p-3 rounded-xl bg-slate-950/60 border border-slate-800/80 flex items-start gap-2.5">
                  <Info size={14} className="text-cyan-400 flex-shrink-0 mt-0.5" />
                  <p className="text-[11px] text-slate-300 leading-relaxed">
                    <strong className="text-cyan-300">Khuyến nghị chuyên gia:</strong> Đối với các biểu thức chứa nhiều ký tự nghiêng liền kề như <code className="text-amber-300 bg-slate-800 px-1 rounded">Oyz</code>, <code className="text-amber-300 bg-slate-800 px-1 rounded">Oxyz</code>, thiết lập mức <strong className="text-emerald-300">0.12em – 0.16em</strong> sẽ đảm bảo các móc cong của chữ <span className="italic font-serif">y</span> và <span className="italic font-serif">z</span> hoàn toàn tách biệt rõ ràng, không bị chồng lấn nét.
                  </p>
                </div>
              </div>

              {/* CARD 2: TỶ LỆ CỠ CHỮ TOÁN HỌC (FONT SCALE / BASE UNIT) */}
              <div className="bg-slate-900/80 border border-slate-800 p-5 rounded-2xl shadow-lg">
                <div className="flex items-center justify-between mb-3">
                  <div className="flex items-center gap-2.5">
                    <div className="p-2 rounded-lg bg-blue-500/10 border border-blue-500/20 text-blue-400">
                      <ZoomIn size={16} />
                    </div>
                    <div>
                      <h3 className="text-sm font-bold text-white">Tỷ Lệ Phóng Cỡ Chữ (Font Scale / Base Unit)</h3>
                      <p className="text-[11px] text-slate-400">
                        Hệ số tỷ lệ giữa công thức toán học so với cỡ chữ thông thường (1.0x = 100%)
                      </p>
                    </div>
                  </div>
                  <span className="text-base font-mono font-black text-blue-400 bg-blue-950/60 px-2.5 py-1 rounded-lg border border-blue-500/30">
                    {baseFontUnit.toFixed(2)}x
                  </span>
                </div>

                <div className="space-y-2 mt-4">
                  <input
                    type="range"
                    min="1.00"
                    max="1.85"
                    step="0.05"
                    value={baseFontUnit}
                    onChange={e => handleBaseFontUnitChange(parseFloat(e.target.value))}
                    className="w-full accent-blue-400 cursor-pointer h-2 bg-slate-800 rounded-lg"
                  />
                  <div className="flex justify-between text-[10px] font-mono text-slate-400">
                    <span className="hover:text-white cursor-pointer" onClick={() => handleBaseFontUnitChange(1.0)}>1.00x (Nguyên bản)</span>
                    <span className="hover:text-white cursor-pointer" onClick={() => handleBaseFontUnitChange(1.25)}>1.25x (Gọn)</span>
                    <span className="text-blue-400 font-bold hover:underline cursor-pointer" onClick={() => handleBaseFontUnitChange(1.45)}>1.45x (Chuẩn THPT)</span>
                    <span className="hover:text-white cursor-pointer" onClick={() => handleBaseFontUnitChange(1.65)}>1.65x (Phóng to)</span>
                  </div>
                </div>
              </div>

              {/* CARD 3: ĐỘ DÀY NÉT VECTOR & DẤU NGOẶC */}
              <div className="bg-slate-900/80 border border-slate-800 p-5 rounded-2xl shadow-lg">
                <div className="flex items-center gap-2.5 mb-4">
                  <div className="p-2 rounded-lg bg-amber-500/10 border border-amber-500/20 text-amber-400">
                    <Layers size={16} />
                  </div>
                  <div>
                    <h3 className="text-sm font-bold text-white">Nét Vẽ Vector & Dấu Ngoặc Hình Học</h3>
                    <p className="text-[11px] text-slate-400">Tinh chỉnh độ sắc sảo của mũi tên vectơ, độ cong ngoặc đơn trong SVG</p>
                  </div>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  {/* Vector Stroke Ratio */}
                  <div className="bg-slate-950/50 p-3.5 rounded-xl border border-slate-800 space-y-2">
                    <div className="flex justify-between text-xs">
                      <span className="text-slate-300 font-medium">Độ dày nét Vector:</span>
                      <span className="text-amber-400 font-mono font-bold">
                        {((localSettings.vectorStrokeWidthRatio || 0.04) * 100).toFixed(0)}%
                      </span>
                    </div>
                    <input
                      type="range"
                      min="0.02"
                      max="0.10"
                      step="0.01"
                      value={localSettings.vectorStrokeWidthRatio || 0.04}
                      onChange={e => handleCustomParamChange('vectorStrokeWidthRatio', parseFloat(e.target.value))}
                      className="w-full accent-amber-400 cursor-pointer h-1.5 bg-slate-800 rounded-lg"
                    />
                    <span className="text-[9px] text-slate-500 block">Chuẩn: 4% - Mũi tên thanh mảnh, rõ nét</span>
                  </div>

                  {/* Parenthesis Bow Ratio */}
                  <div className="bg-slate-950/50 p-3.5 rounded-xl border border-slate-800 space-y-2">
                    <div className="flex justify-between text-xs">
                      <span className="text-slate-300 font-medium">Độ cong dấu ngoặc:</span>
                      <span className="text-cyan-400 font-mono font-bold">
                        {((localSettings.parenthesisBowRatio || 0.18) * 100).toFixed(0)}%
                      </span>
                    </div>
                    <input
                      type="range"
                      min="0.10"
                      max="0.30"
                      step="0.01"
                      value={localSettings.parenthesisBowRatio || 0.18}
                      onChange={e => handleCustomParamChange('parenthesisBowRatio', parseFloat(e.target.value))}
                      className="w-full accent-cyan-400 cursor-pointer h-1.5 bg-slate-800 rounded-lg"
                    />
                    <span className="text-[9px] text-slate-500 block">Chuẩn: 18% - Bao trọn phân số cân xứng</span>
                  </div>
                </div>
              </div>

              {/* CARD 4: KHOẢNG CÁCH TỪ & ĐỆM VIỀN SVG */}
              <div className="bg-slate-900/80 border border-slate-800 p-5 rounded-2xl shadow-lg">
                <h3 className="text-sm font-bold text-white mb-3 flex items-center gap-2">
                  <Sliders size={15} className="text-purple-400" />
                  Khoảng Đệm SVG & Căn Dòng (Padding & Baseline)
                </h3>
                <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                  <div className="bg-slate-950/50 p-3 rounded-xl border border-slate-800">
                    <label className="text-[11px] text-slate-400 block mb-1">Padding X</label>
                    <input
                      type="text"
                      value={localSettings.formulaPaddingX || '0.5rem'}
                      onChange={e => handleCustomParamChange('formulaPaddingX', e.target.value)}
                      className="w-full bg-slate-900 border border-slate-700 rounded-lg px-2.5 py-1.5 text-xs text-white font-mono focus:border-cyan-500 outline-none"
                    />
                  </div>
                  <div className="bg-slate-950/50 p-3 rounded-xl border border-slate-800">
                    <label className="text-[11px] text-slate-400 block mb-1">Padding Y</label>
                    <input
                      type="text"
                      value={localSettings.formulaPaddingY || '0.125rem'}
                      onChange={e => handleCustomParamChange('formulaPaddingY', e.target.value)}
                      className="w-full bg-slate-900 border border-slate-700 rounded-lg px-2.5 py-1.5 text-xs text-white font-mono focus:border-cyan-500 outline-none"
                    />
                  </div>
                  <div className="bg-slate-950/50 p-3 rounded-xl border border-slate-800">
                    <label className="text-[11px] text-slate-400 block mb-1">Margin X</label>
                    <input
                      type="text"
                      value={localSettings.formulaMarginX || '0.25rem'}
                      onChange={e => handleCustomParamChange('formulaMarginX', e.target.value)}
                      className="w-full bg-slate-900 border border-slate-700 rounded-lg px-2.5 py-1.5 text-xs text-white font-mono focus:border-cyan-500 outline-none"
                    />
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* TAB 2: HỒ SƠ THEO MÔN HỌC (SUBJECT PROFILES) */}
          {activeTab === 'subjects' && (
            <div className="space-y-4">
              <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
                {[
                  { code: 'MATH', name: 'Toán Học', icon: <Compass className="w-5 h-5" />, color: 'cyan' },
                  { code: 'PHYSICS', name: 'Vật Lý', icon: <Atom className="w-5 h-5" />, color: 'blue' },
                  { code: 'CHEMISTRY', name: 'Hóa Học', icon: <FlaskConical className="w-5 h-5" />, color: 'emerald' },
                  { code: 'BIOLOGY', name: 'Sinh Học', icon: <Dna className="w-5 h-5" />, color: 'amber' },
                ].map(sub => (
                  <button
                    key={sub.code}
                    onClick={() => handleSubjectSelect(sub.code)}
                    className={`p-4 rounded-xl border flex flex-col items-center gap-2 transition-all cursor-pointer ${
                      selectedSubject === sub.code
                        ? 'bg-slate-800 border-cyan-500 text-white shadow-lg shadow-cyan-500/10'
                        : 'bg-slate-900/60 border-slate-800 text-slate-400 hover:border-slate-700 hover:text-slate-200'
                    }`}
                  >
                    <div className={`p-2 rounded-lg ${selectedSubject === sub.code ? 'bg-cyan-500/20 text-cyan-400' : 'bg-slate-800 text-slate-400'}`}>
                      {sub.icon}
                    </div>
                    <span className="text-xs font-bold">{sub.name}</span>
                    <span className="text-[10px] font-mono text-slate-500">{sub.code}</span>
                  </button>
                ))}
              </div>

              {/* THÔNG TIN HỒ SƠ ĐANG CHỌN */}
              <div className="bg-slate-900/80 border border-slate-800 p-5 rounded-2xl">
                <h3 className="text-sm font-bold text-white mb-3">
                  Thông Số Tối Ưu Cho Môn: <span className="text-cyan-400">{localSettings.subjectProfiles?.[selectedSubject]?.subjectName || selectedSubject}</span>
                </h3>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div className="space-y-1">
                    <label className="text-xs text-slate-400">Giãn cách ký tự môn:</label>
                    <div className="flex items-center gap-2">
                      <input
                        type="text"
                        value={localSettings.subjectProfiles?.[selectedSubject]?.letterSpacing || '0.1em'}
                        onChange={e => {
                          const val = e.target.value;
                          setLocalSettings(prev => ({
                            ...prev,
                            subjectProfiles: {
                              ...prev.subjectProfiles,
                              [selectedSubject]: {
                                ...(prev.subjectProfiles?.[selectedSubject] || { subjectCode: selectedSubject, subjectName: selectedSubject, baseFontUnit: 1.4, letterSpacing: '0.1em' }),
                                letterSpacing: val
                              }
                            }
                          }));
                          handleLetterSpacingChange(val);
                        }}
                        className="bg-slate-950 border border-slate-700 rounded-lg px-3 py-2 text-xs text-white font-mono w-full"
                      />
                    </div>
                  </div>

                  <div className="space-y-1">
                    <label className="text-xs text-slate-400">Cỡ chữ (Base Font Unit):</label>
                    <div className="flex items-center gap-2">
                      <input
                        type="number"
                        step="0.05"
                        value={localSettings.subjectProfiles?.[selectedSubject]?.baseFontUnit || 1.4}
                        onChange={e => {
                          const val = parseFloat(e.target.value) || 1.4;
                          setLocalSettings(prev => ({
                            ...prev,
                            subjectProfiles: {
                              ...prev.subjectProfiles,
                              [selectedSubject]: {
                                ...(prev.subjectProfiles?.[selectedSubject] || { subjectCode: selectedSubject, subjectName: selectedSubject, baseFontUnit: 1.4, letterSpacing: '0.1em' }),
                                baseFontUnit: val
                              }
                            }
                          }));
                          handleBaseFontUnitChange(val);
                        }}
                        className="bg-slate-950 border border-slate-700 rounded-lg px-3 py-2 text-xs text-white font-mono w-full"
                      />
                    </div>
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* TAB 3: PRESETS NHANH */}
          {activeTab === 'presets' && (
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3.5">
              {MATH_PRESETS.map(preset => {
                const isActive = activePresetId === preset.id;
                return (
                  <div
                    key={preset.id}
                    onClick={() => applyPreset(preset.id)}
                    className={`p-4 rounded-xl border transition-all cursor-pointer flex flex-col justify-between ${
                      isActive
                        ? 'bg-slate-800/90 border-cyan-500 shadow-lg shadow-cyan-500/10'
                        : 'bg-slate-900/60 border-slate-800 hover:border-slate-700 hover:bg-slate-900'
                    }`}
                  >
                    <div>
                      <div className="flex items-center justify-between mb-2">
                        <div className="flex items-center gap-2">
                          <span className="text-lg">{preset.icon}</span>
                          <h4 className="text-xs font-bold text-white">{preset.name}</h4>
                        </div>
                        {isActive && (
                          <span className="p-1 rounded-full bg-cyan-500/20 text-cyan-400">
                            <Check size={12} />
                          </span>
                        )}
                      </div>
                      <p className="text-[11px] text-slate-400 leading-relaxed mb-3">
                        {preset.description}
                      </p>
                    </div>

                    <div className="flex items-center gap-2 pt-2 border-t border-slate-800 text-[10px] font-mono text-slate-400">
                      <span>Cỡ: <strong className="text-blue-400">{preset.baseFontUnit}x</strong></span>
                      <span>•</span>
                      <span>Cách chữ: <strong className="text-emerald-400">{preset.letterSpacing}</strong></span>
                    </div>
                  </div>
                );
              })}
            </div>
          )}

          {/* TAB 4: QUY TẮC CẤU TRÚC ĐỘNG (P2 DECLARATIVE RULES) */}
          {activeTab === 'structuralRules' && (
            <div className="space-y-4">
              {/* HEADER CARD */}
              <div className="bg-slate-900/80 border border-slate-800 p-5 rounded-2xl shadow-lg relative overflow-hidden">
                <div className="absolute top-0 right-0 w-40 h-40 bg-purple-500/10 rounded-full blur-3xl pointer-events-none" />
                <div className="flex items-center justify-between mb-2">
                  <div className="flex items-center gap-3">
                    <div className="p-2.5 rounded-xl bg-purple-500/15 border border-purple-500/30 text-purple-400">
                      <Layers size={18} />
                    </div>
                    <div>
                      <h3 className="text-sm font-bold text-white flex items-center gap-2">
                        <span>Hệ Quy Tắc Cấu Trúc Động (Declarative Structural Rules)</span>
                        <span className="text-[10px] px-2 py-0.5 rounded-full bg-purple-950/80 border border-purple-500/40 text-purple-300 font-mono">
                          Trụ Cột 2 & P2
                        </span>
                      </h3>
                      <p className="text-[11px] text-slate-400">
                        Chuyển đổi hình học 2D WMF sang KaTeX thuần qua tọa độ Bounding Box, cấu hình 100% không cần deploy lại code.
                      </p>
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    <button
                      onClick={() => setShowAddTokenModal(true)}
                      className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-bold bg-cyan-600/30 hover:bg-cyan-600/50 border border-cyan-500/40 text-cyan-200 transition-all cursor-pointer"
                    >
                      <Plus size={13} />
                      <span>Thêm Ký Hiệu Từ Điển</span>
                    </button>
                    <button
                      onClick={() => setShowAddRuleModal(true)}
                      className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-bold bg-purple-600/40 hover:bg-purple-600/60 border border-purple-500/50 text-purple-200 transition-all cursor-pointer"
                    >
                      <Plus size={13} />
                      <span>Thêm Quy Tắc Mới</span>
                    </button>
                    <button
                      onClick={fetchStructuralRules}
                      disabled={loadingRules}
                      className="p-2 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 transition-all cursor-pointer"
                      title="Tải lại danh sách quy tắc"
                    >
                      <RefreshCw size={14} className={loadingRules ? "animate-spin" : ""} />
                    </button>
                  </div>
                </div>
              </div>

              {/* DANH SÁCH CÁC QUY TẮC CẤU TRÚC */}
              {loadingRules ? (
                <div className="p-8 text-center bg-slate-900/60 rounded-2xl border border-slate-800">
                  <RefreshCw size={24} className="animate-spin text-purple-400 mx-auto mb-2" />
                  <p className="text-xs text-slate-400">Đang đồng bộ danh sách quy tắc từ Backend...</p>
                </div>
              ) : structuralRules.length === 0 ? (
                <div className="p-8 text-center bg-slate-900/60 rounded-2xl border border-slate-800">
                  <AlertCircle size={24} className="text-amber-400 mx-auto mb-2" />
                  <p className="text-xs text-slate-300 font-bold">Chưa nạp được quy tắc cấu trúc nào</p>
                  <p className="text-[11px] text-slate-500 mt-1">Vui lòng bấm nút 'Nạp Lại Từ Điển' để đọc từ registry.</p>
                </div>
              ) : (
                <div className="space-y-3">
                  {structuralRules.map((rule: any) => {
                    const isToggling = togglingRuleId === rule.id;
                    const getSampleLatex = (ruleId: string) => {
                      switch (ruleId) {
                        case 'RULE_SIMPLE_FRACTION':
                          return '\\sin x = \\frac{1}{2}';
                        case 'RULE_RADICAL_OVERLINE':
                          return '\\sqrt{x^2 + 1}';
                        case 'RULE_SYSTEM_OF_EQUATIONS':
                          return '\\begin{cases} 2x + y = 5 \\\\ x - y = 1 \\end{cases}';
                        case 'RULE_LIMIT_EXPRESSION':
                          return '\\lim_{x \\to 0} \\frac{\\sin x}{x} = 1';
                        case 'RULE_VECTOR_OVERLINE':
                          return '\\vec{u} + \\vec{v} = \\vec{w}';
                        default:
                          return rule.template || 'x + y = 0';
                      }
                    };

                    const getAnchorColor = (anchorType: string) => {
                      switch (anchorType) {
                        case 'GEOMETRIC_LINE':
                          return 'text-emerald-400 bg-emerald-950/60 border-emerald-500/30';
                        case 'COMPOUND':
                          return 'text-cyan-400 bg-cyan-950/60 border-cyan-500/30';
                        case 'DELIMITER_SYMBOL':
                          return 'text-purple-400 bg-purple-950/60 border-purple-500/30';
                        case 'TEXT_ANCHOR':
                          return 'text-amber-400 bg-amber-950/60 border-amber-500/30';
                        case 'VECTOR_OVERLINE':
                          return 'text-blue-400 bg-blue-950/60 border-blue-500/30';
                        default:
                          return 'text-slate-400 bg-slate-900 border-slate-700';
                      }
                    };

                    return (
                      <div
                        key={rule.id}
                        className={`p-4 rounded-xl border transition-all ${
                          rule.enabled
                            ? 'bg-slate-900/80 border-slate-800 hover:border-slate-700 shadow-md'
                            : 'bg-slate-950/40 border-slate-900 opacity-60'
                        }`}
                      >
                        <div className="flex items-start justify-between gap-3 mb-2.5">
                          <div>
                            <div className="flex items-center gap-2 mb-1">
                              <span className="text-xs font-bold text-white">{rule.name}</span>
                              <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-slate-800 text-slate-400 border border-slate-700">
                                {rule.id}
                              </span>
                              <span className={`text-[10px] font-mono px-2 py-0.5 rounded-full border ${getAnchorColor(rule.anchor?.type || '')}`}>
                                Mỏ neo: {rule.anchor?.type || 'N/A'}
                              </span>
                              <span className="text-[10px] font-mono text-slate-500">
                                Ưu tiên: {rule.priority}
                              </span>
                            </div>
                            <div className="flex flex-wrap items-center gap-1.5 mt-2">
                              {rule.slots?.map((slot: any) => (
                                <span
                                  key={slot.name}
                                  className="text-[10px] font-mono px-2 py-0.5 rounded bg-slate-800/80 border border-slate-700/60 text-slate-300"
                                >
                                  Slot: <strong className="text-cyan-400">{slot.name}</strong> ({slot.relativePosition})
                                </span>
                              ))}
                            </div>
                          </div>

                          {/* TOGGLE SWITCH & DELETE */}
                          <div className="flex items-center gap-3">
                            <div className="flex items-center gap-2">
                              <span className="text-[11px] font-mono font-medium text-slate-400">
                                {rule.enabled ? 'BẬT' : 'TẮT'}
                              </span>
                              <button
                                onClick={() => handleToggleRule(rule.id)}
                                disabled={isToggling}
                                className={`w-11 h-6 rounded-full transition-colors relative cursor-pointer ${
                                  rule.enabled ? 'bg-purple-600' : 'bg-slate-700'
                                } disabled:opacity-50`}
                              >
                                <div
                                  className={`w-4 h-4 rounded-full bg-white absolute top-1 transition-transform ${
                                    rule.enabled ? 'left-6' : 'left-1'
                                  }`}
                                />
                              </button>
                            </div>

                            <button
                              onClick={() => handleDeleteRule(rule.id)}
                              className="p-1.5 rounded-lg hover:bg-rose-950/60 text-slate-500 hover:text-rose-400 transition-colors cursor-pointer"
                              title="Xóa quy tắc này"
                            >
                              <Trash2 size={14} />
                            </button>
                          </div>
                        </div>

                        {/* MẪU TEMPLATE & PREVIEW MINH HỌA */}
                        <div className="mt-3 pt-3 border-t border-slate-800/80 grid grid-cols-1 sm:grid-cols-2 gap-3 items-center">
                          <div className="bg-slate-950/80 p-2 rounded-lg border border-slate-800 font-mono text-[11px] text-purple-300 truncate">
                            <span className="text-slate-500 select-none">Template: </span>
                            {rule.template}
                          </div>
                          <div className="bg-slate-950/80 p-2 rounded-lg border border-slate-800 flex items-center justify-between text-xs text-white">
                            <span className="text-slate-500 text-[10px] select-none">Minh họa KaTeX:</span>
                            <div className="font-serif">
                              <MathRenderer content={`$${getSampleLatex(rule.id)}$`} />
                            </div>
                          </div>
                        </div>
                      </div>
                    );
                  })}
                </div>
              )}
            </div>
          )}
        </div>

        {/* CỘT PHẢI: KÍNH LÚP LIVE PREVIEW THỜI GIAN THỰC (5 CỘT) */}
        <div className="xl:col-span-5 space-y-4">
          <div className="bg-slate-900/90 border border-slate-800 rounded-2xl p-5 shadow-xl sticky top-24 backdrop-blur-md">
            <div className="flex items-center justify-between mb-4 pb-3 border-b border-slate-800">
              <div className="flex items-center gap-2">
                <div className="w-8 h-8 rounded-lg bg-cyan-500/10 border border-cyan-500/20 flex items-center justify-center text-cyan-400">
                  <Eye size={16} />
                </div>
                <div>
                  <h3 className="text-sm font-bold text-white">Kính Lúp So Sánh & Thẩm Định Trực Quan</h3>
                  <span className="text-[10px] text-slate-400">Xem trước kết quả render thời gian thực (Zero-Lag)</span>
                </div>
              </div>
              <span className="text-[10px] font-mono text-cyan-400 bg-cyan-950/80 px-2 py-0.5 rounded-full border border-cyan-500/30">
                Live Preview
              </span>
            </div>

            {/* HỘP SO SÁNH TRỰC DIỆN LỖI OYZ */}
            <div className="space-y-3 mb-4">
              <div className="text-xs font-bold text-slate-300 flex items-center justify-between">
                <span>Trường Hợp Trọng Điểm: Biến Nghiêng $Oyz$</span>
                <span className="text-[10px] text-amber-400 font-mono">So sánh Đóng / Mở Giãn Cách</span>
              </div>

              {/* BẢN KÉM: 0.00em (DÍNH CHỮ) */}
              <div className="bg-slate-950 p-3 rounded-xl border border-rose-900/30 relative">
                <div className="flex justify-between items-center mb-1">
                  <span className="text-[10px] text-rose-400 font-bold uppercase tracking-wider">
                    ✗ Trước khi chỉnh (Dính chữ sát nhau):
                  </span>
                  <span className="text-[9px] text-slate-500 font-mono">letter-spacing: 0.00em</span>
                </div>
                <div 
                  className="py-2 px-3 bg-slate-900/80 rounded-lg text-center font-serif italic text-lg text-white border border-slate-800/80"
                  style={{ letterSpacing: '0.00em' }}
                >
                  Oyz &nbsp;&nbsp; Oxyz &nbsp;&nbsp; (P): x + y - z = 0
                </div>
                <p className="text-[9px] text-rose-400/80 mt-1">Các ký tự O, y, z chạm đuôi và đè nét lên nhau.</p>
              </div>

              {/* BẢN TỐT: KẾT QUẢ ĐANG CHỈNH HIỆN TẠI */}
              <div className="bg-slate-950 p-3.5 rounded-xl border border-emerald-500/40 relative shadow-inner">
                <div className="flex justify-between items-center mb-1">
                  <span className="text-[10px] text-emerald-400 font-bold uppercase tracking-wider flex items-center gap-1">
                    ✓ Sau khi tối ưu (Chuẩn thoáng đãng hiện tại):
                  </span>
                  <span className="text-[10px] text-emerald-400 font-mono font-bold">
                    letter-spacing: {letterSpacing}
                  </span>
                </div>
                <div 
                  className="py-2 px-3 bg-slate-900 rounded-lg text-center font-serif italic text-lg text-cyan-200 border border-emerald-500/30 transition-all duration-150"
                  style={{ letterSpacing: letterSpacing }}
                >
                  Oyz &nbsp;&nbsp; Oxyz &nbsp;&nbsp; (P): x + y - z = 0
                </div>
                <p className="text-[9px] text-emerald-400/80 mt-1">Các ký tự hoàn toàn tách bạch, thanh thoát, dễ đọc.</p>
              </div>
            </div>

            {/* TEST SUITE ĐA DẠNG: VECTOR, TÍCH PHÂN, HÓA HỌC */}
            <div className="space-y-3 pt-3 border-t border-slate-800">
              <span className="text-xs font-bold text-slate-300 block">Các Mẫu Thử Toán Học & Khoa Học Tự Nhiên:</span>

              {/* Mẫu 1: Vector */}
              <div className="bg-slate-950/60 p-3 rounded-xl border border-slate-800/80 space-y-1">
                <span className="text-[10px] text-slate-400 font-medium">1. Phép nhân vô hướng vectơ:</span>
                <div 
                  className="p-2 bg-slate-900/60 rounded-lg text-sm text-slate-100 flex items-center justify-center transition-all"
                  style={{ letterSpacing: letterSpacing }}
                >
                  <MathRenderer content="$\vec{u} \cdot \vec{v} = |\vec{u}| \cdot |\vec{v}| \cos(60^\circ) = \frac{\sqrt{3}}{2}$" />
                </div>
              </div>

              {/* Mẫu 2: Tích phân & Phân số */}
              <div className="bg-slate-950/60 p-3 rounded-xl border border-slate-800/80 space-y-1">
                <span className="text-[10px] text-slate-400 font-medium">2. Tích phân & Phân số:</span>
                <div 
                  className="p-2 bg-slate-900/60 rounded-lg text-sm text-slate-100 flex items-center justify-center transition-all"
                  style={{ letterSpacing: letterSpacing }}
                >
                  <MathRenderer content="$\int_{0}^{1} \frac{x^3 + 2x}{\sqrt{x^2 + 1}} \, dx = \frac{2\sqrt{2} - 1}{3}$" />
                </div>
              </div>

              {/* Mẫu 3: Hóa học */}
              <div className="bg-slate-950/60 p-3 rounded-xl border border-slate-800/80 space-y-1">
                <span className="text-[10px] text-slate-400 font-medium">3. Phương trình Hóa học:</span>
                <div 
                  className="p-2 bg-slate-900/60 rounded-lg text-sm text-slate-100 flex items-center justify-center transition-all"
                  style={{ letterSpacing: letterSpacing }}
                >
                  <MathRenderer content="$\text{Fe} + 2\text{HCl} \to \text{FeCl}_2 + \text{H}_2 \uparrow$" />
                </div>
              </div>

              {/* Mẫu 4: Custom Tester */}
              <div className="bg-slate-950/80 p-3 rounded-xl border border-slate-800 space-y-2">
                <label className="text-[10px] text-slate-400 font-medium flex justify-between">
                  <span>Trình kiểm thử biểu thức tùy chọn (Nhập mã LaTeX):</span>
                  <span className="text-cyan-400 font-mono">Real-time</span>
                </label>
                <input
                  type="text"
                  value={customTestLatex}
                  onChange={e => setCustomTestLatex(e.target.value)}
                  placeholder="Ví dụ: Oyz, \sqrt{x^2+y^2}..."
                  className="w-full bg-slate-900 border border-slate-700 rounded-lg px-2.5 py-1.5 text-xs text-slate-200 font-mono focus:border-cyan-500 outline-none"
                />
                <div 
                  className="p-3 bg-slate-900/90 rounded-lg text-center min-h-[44px] flex items-center justify-center border border-slate-800"
                  style={{ letterSpacing: letterSpacing }}
                >
                  <MathRenderer content={`$${customTestLatex}$`} />
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
      {/* MODAL 1: THÊM QUY TẮC CẤU TRÚC P2 MỚI */}
      {showAddRuleModal && (
        <div className="fixed inset-0 bg-black/80 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="bg-slate-900 border border-slate-700 rounded-2xl w-full max-w-xl p-6 shadow-2xl space-y-4 max-h-[90vh] overflow-y-auto">
            <div className="flex items-center justify-between pb-3 border-b border-slate-800">
              <div className="flex items-center gap-2.5">
                <div className="p-2 rounded-xl bg-purple-500/20 text-purple-400">
                  <Layers size={18} />
                </div>
                <div>
                  <h3 className="text-sm font-bold text-white">Thêm Quy Tắc Cấu Trúc Động (P2)</h3>
                  <p className="text-[11px] text-slate-400">Khai báo quy tắc bóc tách 2D WMF sang KaTeX</p>
                </div>
              </div>
              <button
                onClick={() => setShowAddRuleModal(false)}
                className="p-1 rounded-lg text-slate-400 hover:text-white hover:bg-slate-800 cursor-pointer"
              >
                <X size={16} />
              </button>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="text-[11px] text-slate-400 font-medium block mb-1">Mã Quy Tắc (ID):</label>
                <input
                  type="text"
                  placeholder="RULE_CUSTOM_FRACTION"
                  value={newRule.id}
                  onChange={e => setNewRule(prev => ({ ...prev, id: e.target.value }))}
                  className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-1.5 text-xs text-white font-mono focus:border-purple-500 outline-none"
                />
              </div>
              <div>
                <label className="text-[11px] text-slate-400 font-medium block mb-1">Độ Ưu Tiên (Priority):</label>
                <input
                  type="number"
                  min="1"
                  max="100"
                  value={newRule.priority}
                  onChange={e => setNewRule(prev => ({ ...prev, priority: parseInt(e.target.value) || 50 }))}
                  className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-1.5 text-xs text-white font-mono focus:border-purple-500 outline-none"
                />
              </div>
            </div>

            <div>
              <label className="text-[11px] text-slate-400 font-medium block mb-1">Tên Quy Tắc (Name):</label>
              <input
                type="text"
                placeholder="Phân số số học tùy chỉnh..."
                value={newRule.name}
                onChange={e => setNewRule(prev => ({ ...prev, name: e.target.value }))}
                className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-1.5 text-xs text-white focus:border-purple-500 outline-none"
              />
            </div>

            <div>
              <label className="text-[11px] text-slate-400 font-medium block mb-1">Loại Mỏ Neo (Anchor Type):</label>
              <select
                value={newRule.anchorType}
                onChange={e => setNewRule(prev => ({ ...prev, anchorType: e.target.value }))}
                className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-1.5 text-xs text-white font-mono focus:border-purple-500 outline-none cursor-pointer"
              >
                <option value="GEOMETRIC_LINE">GEOMETRIC_LINE (Đường kẻ ngang - Phân số)</option>
                <option value="COMPOUND">COMPOUND (Căn thức kèm gạch nóc)</option>
                <option value="DELIMITER_SYMBOL">DELIMITER_SYMBOL (Ngoặc nhọn hệ phương trình)</option>
                <option value="TEXT_ANCHOR">TEXT_ANCHOR (Từ khóa giới hạn lim, tích phân)</option>
                <option value="VECTOR_OVERLINE">VECTOR_OVERLINE (Mũi tên vector trên đỉnh)</option>
              </select>
            </div>

            {/* SLOTS CONFIG */}
            <div className="p-3 bg-slate-950/70 rounded-xl border border-slate-800 space-y-2">
              <span className="text-[11px] font-bold text-slate-300 block">Khai Báo Vùng Không Gian (Slots):</span>
              <div className="grid grid-cols-2 gap-2">
                <div>
                  <label className="text-[10px] text-slate-500 block mb-0.5">Tên Slot 1:</label>
                  <input
                    type="text"
                    value={newRule.slot1Name}
                    onChange={e => setNewRule(prev => ({ ...prev, slot1Name: e.target.value }))}
                    className="w-full bg-slate-900 border border-slate-800 rounded px-2.5 py-1 text-xs text-cyan-300 font-mono outline-none"
                  />
                </div>
                <div>
                  <label className="text-[10px] text-slate-500 block mb-0.5">Vị Trí Slot 1:</label>
                  <select
                    value={newRule.slot1Pos}
                    onChange={e => setNewRule(prev => ({ ...prev, slot1Pos: e.target.value }))}
                    className="w-full bg-slate-900 border border-slate-800 rounded px-2 py-1 text-xs text-slate-300 font-mono outline-none cursor-pointer"
                  >
                    <option value="ABOVE_ANCHOR">ABOVE_ANCHOR (Trên mỏ neo)</option>
                    <option value="UNDER_OVERLINE">UNDER_OVERLINE (Dưới gạch nóc)</option>
                    <option value="RIGHT_UPPER">RIGHT_UPPER (Phải phía trên)</option>
                    <option value="DIRECTLY_BELOW">DIRECTLY_BELOW (Ngay bên dưới)</option>
                  </select>
                </div>
              </div>

              <div className="grid grid-cols-2 gap-2">
                <div>
                  <label className="text-[10px] text-slate-500 block mb-0.5">Tên Slot 2 (Tùy chọn):</label>
                  <input
                    type="text"
                    value={newRule.slot2Name}
                    onChange={e => setNewRule(prev => ({ ...prev, slot2Name: e.target.value }))}
                    className="w-full bg-slate-900 border border-slate-800 rounded px-2.5 py-1 text-xs text-cyan-300 font-mono outline-none"
                  />
                </div>
                <div>
                  <label className="text-[10px] text-slate-500 block mb-0.5">Vị Trí Slot 2:</label>
                  <select
                    value={newRule.slot2Pos}
                    onChange={e => setNewRule(prev => ({ ...prev, slot2Pos: e.target.value }))}
                    className="w-full bg-slate-900 border border-slate-800 rounded px-2 py-1 text-xs text-slate-300 font-mono outline-none cursor-pointer"
                  >
                    <option value="BELOW_ANCHOR">BELOW_ANCHOR (Dưới mỏ neo)</option>
                    <option value="RIGHT_LOWER">RIGHT_LOWER (Phải phía dưới)</option>
                    <option value="RIGHT_ADJACENT">RIGHT_ADJACENT (Phải liền kề)</option>
                  </select>
                </div>
              </div>
            </div>

            <div>
              <label className="text-[11px] text-slate-400 font-medium block mb-1">Mẫu Sinh KaTeX (Template):</label>
              <input
                type="text"
                placeholder="\frac{{{numerator}}}{{{denominator}}}"
                value={newRule.template}
                onChange={e => setNewRule(prev => ({ ...prev, template: e.target.value }))}
                className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-1.5 text-xs text-purple-300 font-mono focus:border-purple-500 outline-none"
              />
            </div>

            <div className="flex justify-end gap-2.5 pt-3 border-t border-slate-800">
              <button
                onClick={() => setShowAddRuleModal(false)}
                className="px-4 py-2 rounded-xl text-xs font-semibold text-slate-400 hover:text-white hover:bg-slate-800 transition-colors cursor-pointer"
              >
                Hủy bỏ
              </button>
              <button
                onClick={handleSaveNewRule}
                className="flex items-center gap-1.5 px-4 py-2 rounded-xl text-xs font-bold bg-purple-600 hover:bg-purple-500 text-white shadow-lg shadow-purple-600/30 transition-all cursor-pointer"
              >
                <Save size={14} />
                <span>Lưu Vào Hệ Thống (DB & RAM)</span>
              </button>
            </div>
          </div>
        </div>
      )}

      {/* MODAL 2: THÊM KÝ HIỆU / OMML TOKEN MỚI */}
      {showAddTokenModal && (
        <div className="fixed inset-0 bg-black/80 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="bg-slate-900 border border-slate-700 rounded-2xl w-full max-w-md p-6 shadow-2xl space-y-4">
            <div className="flex items-center justify-between pb-3 border-b border-slate-800">
              <div className="flex items-center gap-2.5">
                <div className="p-2 rounded-xl bg-cyan-500/20 text-cyan-400">
                  <Code2 size={18} />
                </div>
                <div>
                  <h3 className="text-sm font-bold text-white">Bổ Sung Ký Hiệu Vào Từ Điển</h3>
                  <p className="text-[11px] text-slate-400">Thêm ký tự lạ từ MathType WMF hoặc Word OMML</p>
                </div>
              </div>
              <button
                onClick={() => setShowAddTokenModal(false)}
                className="p-1 rounded-lg text-slate-400 hover:text-white hover:bg-slate-800 cursor-pointer"
              >
                <X size={16} />
              </button>
            </div>

            {/* TYPE SWITCHER */}
            <div className="flex bg-slate-950 p-1 rounded-xl border border-slate-800 gap-1">
              <button
                onClick={() => setTokenType('symbol')}
                className={`flex-1 py-1.5 rounded-lg text-xs font-bold transition-all cursor-pointer ${
                  tokenType === 'symbol'
                    ? 'bg-cyan-600 text-white shadow'
                    : 'text-slate-400 hover:text-white'
                }`}
              >
                Glyph MathType (WMF)
              </button>
              <button
                onClick={() => setTokenType('omml')}
                className={`flex-1 py-1.5 rounded-lg text-xs font-bold transition-all cursor-pointer ${
                  tokenType === 'omml'
                    ? 'bg-cyan-600 text-white shadow'
                    : 'text-slate-400 hover:text-white'
                }`}
              >
                Token OMML (Word Math)
              </button>
            </div>

            {tokenType === 'symbol' ? (
              <div className="space-y-3">
                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="text-[11px] text-slate-400 font-medium block mb-1">Font Chữ:</label>
                    <select
                      value={newGlyph.font}
                      onChange={e => setNewGlyph(prev => ({ ...prev, font: e.target.value }))}
                      className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-1.5 text-xs text-white font-mono focus:border-cyan-500 outline-none cursor-pointer"
                    >
                      <option value="Symbol">Symbol</option>
                      <option value="Euclid Symbol">Euclid Symbol</option>
                      <option value="MT Extra">MT Extra</option>
                    </select>
                  </div>
                  <div>
                    <label className="text-[11px] text-slate-400 font-medium block mb-1">Mã Hex:</label>
                    <input
                      type="text"
                      placeholder="0x00F7"
                      value={newGlyph.charHex}
                      onChange={e => setNewGlyph(prev => ({ ...prev, charHex: e.target.value }))}
                      className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-1.5 text-xs text-white font-mono focus:border-cyan-500 outline-none"
                    />
                  </div>
                </div>

                <div>
                  <label className="text-[11px] text-slate-400 font-medium block mb-1">HTML Entity:</label>
                  <input
                    type="text"
                    placeholder="&#x00F7;"
                    value={newGlyph.replacement}
                    onChange={e => setNewGlyph(prev => ({ ...prev, replacement: e.target.value }))}
                    className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-1.5 text-xs text-cyan-300 font-mono focus:border-cyan-500 outline-none"
                  />
                </div>

                <div>
                  <label className="text-[11px] text-slate-400 font-medium block mb-1">Mã KaTeX / LaTeX:</label>
                  <input
                    type="text"
                    placeholder="\div"
                    value={newGlyph.latex}
                    onChange={e => setNewGlyph(prev => ({ ...prev, latex: e.target.value }))}
                    className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-1.5 text-xs text-emerald-300 font-mono focus:border-cyan-500 outline-none"
                  />
                </div>

                <div>
                  <label className="text-[11px] text-slate-400 font-medium block mb-1">Mô tả ký hiệu:</label>
                  <input
                    type="text"
                    placeholder="Dấu chia phép tính..."
                    value={newGlyph.desc}
                    onChange={e => setNewGlyph(prev => ({ ...prev, desc: e.target.value }))}
                    className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-1.5 text-xs text-white focus:border-cyan-500 outline-none"
                  />
                </div>
              </div>
            ) : (
              <div className="space-y-3">
                <div>
                  <label className="text-[11px] text-slate-400 font-medium block mb-1">Ký tự OMML (Token):</label>
                  <input
                    type="text"
                    placeholder="≈ hoặc ∓"
                    value={newOmml.token}
                    onChange={e => setNewOmml(prev => ({ ...prev, token: e.target.value }))}
                    className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-1.5 text-xs text-white font-mono focus:border-cyan-500 outline-none"
                  />
                </div>
                <div>
                  <label className="text-[11px] text-slate-400 font-medium block mb-1">Mã KaTeX / LaTeX:</label>
                  <input
                    type="text"
                    placeholder="\approx "
                    value={newOmml.latex}
                    onChange={e => setNewOmml(prev => ({ ...prev, latex: e.target.value }))}
                    className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-1.5 text-xs text-emerald-300 font-mono focus:border-cyan-500 outline-none"
                  />
                </div>
              </div>
            )}

            <div className="flex justify-end gap-2.5 pt-3 border-t border-slate-800">
              <button
                onClick={() => setShowAddTokenModal(false)}
                className="px-4 py-2 rounded-xl text-xs font-semibold text-slate-400 hover:text-white hover:bg-slate-800 transition-colors cursor-pointer"
              >
                Hủy bỏ
              </button>
              <button
                onClick={handleSaveToken}
                className="flex items-center gap-1.5 px-4 py-2 rounded-xl text-xs font-bold bg-cyan-600 hover:bg-cyan-500 text-white shadow-lg shadow-cyan-600/30 transition-all cursor-pointer"
              >
                <Save size={14} />
                <span>Lưu Vào Từ Điển Hệ Thống</span>
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default MathSettingsPage;
