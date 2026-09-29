import React, { createContext, useContext, useEffect, useState, useMemo, useCallback } from 'react';
import { quizApi } from '@/shared/api/quiz-api';

export interface SubjectMathProfile {
  subjectCode: string;
  subjectName: string;
  baseFontUnit: number;
  letterSpacing: string;
  minInlineHeight?: string;
  cropPadX?: number;
  cropPadY?: number;
}

export interface MathEngineSettings {
  baseFontUnit: number;
  letterSpacing: string;
  minInlineHeight: string;
  maxFormulaHeight: string;
  formulaPaddingX: string;
  formulaPaddingY: string;
  formulaMarginX: string;
  fontFamilySerif: string;
  vAlignSlope: number;
  vAlignIntercept: number;
  multiLineThreshold: number;
  cropPadX: number;
  cropPadY: number;
  charWidthFactor: number;
  ascenderFactor: number;
  descenderFactor: number;
  vectorHeadLengthRatio: number;
  vectorHeadSpreadRatio: number;
  vectorStrokeWidthRatio: number;
  parenthesisBowRatio: number;
  parenthesisStrokeWidthRatio: number;
  subjectProfiles: Record<string, SubjectMathProfile>;
}

export interface MathPreset {
  id: string;
  name: string;
  icon: string;
  description: string;
  baseFontUnit: number;
  letterSpacing: string;
  subjectCode?: string;
}

export const MATH_PRESETS: MathPreset[] = [
  {
    id: 'THPT_STANDARD',
    name: 'Chuẩn THPT 2026',
    icon: '⚡',
    description: 'Cỡ chữ chuẩn 1.1em (+10%), giãn cách 0.1em thoáng đãng, cân đối',
    baseFontUnit: 1.45,
    letterSpacing: '0.1em',
    subjectCode: 'MATH'
  },
  {
    id: 'CHEMISTRY',
    name: 'Môn Hóa Học',
    icon: '🧪',
    description: 'Chỉ số dưới sát, giãn cách 0.04em chuẩn công thức phân tử',
    baseFontUnit: 1.30,
    letterSpacing: '0.04em',
    subjectCode: 'CHEMISTRY'
  },
  {
    id: 'PHYSICS',
    name: 'Môn Vật Lý',
    icon: '⚛️',
    description: 'Tỷ lệ 1.40em, giãn cách 0.08em chuẩn ký hiệu và đơn vị',
    baseFontUnit: 1.40,
    letterSpacing: '0.08em',
    subjectCode: 'PHYSICS'
  },
  {
    id: 'COMPACT',
    name: 'Gọn Gàng (Mobile)',
    icon: '📱',
    description: 'Tiết kiệm không gian, phù hợp màn hình hẹp hoặc bảng nhiều cột',
    baseFontUnit: 1.20,
    letterSpacing: '0.06em'
  },
  {
    id: 'LARGE_DISPLAY',
    name: 'Phóng To Nổi Bật',
    icon: '🔍',
    description: 'Cỡ lớn 1.35em, siêu sắc nét, dễ quan sát cho học sinh ngồi xa',
    baseFontUnit: 1.65,
    letterSpacing: '0.12em'
  }
];

export interface MathConfigContextValue {
  settings: MathEngineSettings;
  baseFontUnit: number;
  letterSpacing: string;
  currentSubject: string;
  activePresetId: string | null;
  currentPreset: string | null;
  availablePresets: MathPreset[];
  setBaseFontUnit: (val: number) => void;
  setLetterSpacing: (val: string) => void;
  setSubject: (subjectCode: string) => void;
  applySubjectProfile: (subjectCode: string) => void;
  applyPreset: (presetId: string) => void;
  updateSettings: (partial: Partial<MathEngineSettings>) => void;
  resetToDefaults: () => void;
  isCustomized: boolean;
}

const DEFAULT_SETTINGS: MathEngineSettings = {
  baseFontUnit: 1.45,
  letterSpacing: '0.1em',
  minInlineHeight: '1.40em',
  maxFormulaHeight: '14em',
  formulaPaddingX: '0.5rem',
  formulaPaddingY: '0.125rem',
  formulaMarginX: '0.25rem',
  fontFamilySerif: "'Times New Roman', 'Cambria Math', serif",
  vAlignSlope: 0.48,
  vAlignIntercept: 0.12,
  multiLineThreshold: 1.8,
  cropPadX: 10.0,
  cropPadY: 6.0,
  charWidthFactor: 0.72,
  ascenderFactor: 0.85,
  descenderFactor: 0.25,
  vectorHeadLengthRatio: 0.16,
  vectorHeadSpreadRatio: 0.09,
  vectorStrokeWidthRatio: 0.04,
  parenthesisBowRatio: 0.18,
  parenthesisStrokeWidthRatio: 0.04,
  subjectProfiles: {
    MATH: { subjectCode: 'MATH', subjectName: 'Toán Học', baseFontUnit: 1.45, letterSpacing: '0.1em', cropPadX: 10, cropPadY: 6 },
    CHEMISTRY: { subjectCode: 'CHEMISTRY', subjectName: 'Hóa Học', baseFontUnit: 1.30, letterSpacing: '0.04em', cropPadX: 8, cropPadY: 5 },
    PHYSICS: { subjectCode: 'PHYSICS', subjectName: 'Vật Lý', baseFontUnit: 1.40, letterSpacing: '0.08em', cropPadX: 10, cropPadY: 6 },
    BIOLOGY: { subjectCode: 'BIOLOGY', subjectName: 'Sinh Học', baseFontUnit: 1.35, letterSpacing: '0.06em', cropPadX: 8, cropPadY: 5 }
  }
};

const STORAGE_KEY = 'aegisquiz_math_engine_config';

const MathConfigContext = createContext<MathConfigContextValue | null>(null);

export const MathConfigProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [settings, setSettings] = useState<MathEngineSettings>(DEFAULT_SETTINGS);
  const [baseFontUnit, setBaseFontUnitState] = useState<number>(() => {
    try {
      const saved = localStorage.getItem(STORAGE_KEY);
      if (saved) {
        const parsed = JSON.parse(saved);
        if (typeof parsed.baseFontUnit === 'number') return parsed.baseFontUnit;
      }
    } catch {}
    return DEFAULT_SETTINGS.baseFontUnit;
  });

  const [letterSpacing, setLetterSpacingState] = useState<string>(() => {
    try {
      const saved = localStorage.getItem(STORAGE_KEY);
      if (saved) {
        const parsed = JSON.parse(saved);
        if (typeof parsed.letterSpacing === 'string') return parsed.letterSpacing;
      }
    } catch {}
    return DEFAULT_SETTINGS.letterSpacing;
  });

  const [currentSubject, setCurrentSubject] = useState<string>('MATH');
  const [activePresetId, setActivePresetId] = useState<string | null>('THPT_STANDARD');

  // Nạp cấu hình từ API Backend khi khởi động
  useEffect(() => {
    let mounted = true;
    quizApi.getMathEngineSettings().then((remoteSettings: any) => {
      if (mounted && remoteSettings && typeof remoteSettings === 'object') {
        setSettings(prev => ({
          ...prev,
          ...remoteSettings,
          subjectProfiles: remoteSettings.subjectProfiles || prev.subjectProfiles
        }));
      }
    }).catch(() => {});
    return () => { mounted = false; };
  }, []);

  // Tự động tiêm CSS Variables vào :root để mọi công thức toán cập nhật theo thời gian thực
  useEffect(() => {
    const root = document.documentElement;
    root.style.setProperty('--math-base-unit', baseFontUnit.toString());
    root.style.setProperty('--math-letter-spacing', letterSpacing);
    root.style.setProperty('--math-pad-x', settings.formulaPaddingX);
    root.style.setProperty('--math-pad-y', settings.formulaPaddingY);
    root.style.setProperty('--math-margin-x', settings.formulaMarginX);

    // Lưu tùy chọn người dùng vào localStorage
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify({
        baseFontUnit,
        letterSpacing,
        currentSubject,
        activePresetId
      }));
    } catch {}
  }, [baseFontUnit, letterSpacing, settings, currentSubject, activePresetId]);

  const setBaseFontUnit = useCallback((val: number) => {
    const clamped = Math.max(0.8, Math.min(2.5, Math.round(val * 100) / 100));
    setBaseFontUnitState(clamped);
    setActivePresetId(null);
  }, []);

  const setLetterSpacing = useCallback((val: string) => {
    setLetterSpacingState(val);
    setActivePresetId(null);
  }, []);

  const setSubject = useCallback((subjectCode: string) => {
    setCurrentSubject(subjectCode);
    const profile = settings.subjectProfiles?.[subjectCode];
    if (profile) {
      setBaseFontUnitState(profile.baseFontUnit);
      setLetterSpacingState(profile.letterSpacing);
      const matchedPreset = MATH_PRESETS.find(p => p.subjectCode === subjectCode);
      setActivePresetId(matchedPreset ? matchedPreset.id : null);
    }
  }, [settings.subjectProfiles]);

  const applyPreset = useCallback((presetId: string) => {
    const preset = MATH_PRESETS.find(p => p.id === presetId);
    if (preset) {
      setBaseFontUnitState(preset.baseFontUnit);
      setLetterSpacingState(preset.letterSpacing);
      if (preset.subjectCode) {
        setCurrentSubject(preset.subjectCode);
      }
      setActivePresetId(preset.id);
    }
  }, []);

  const updateSettings = useCallback((partial: Partial<MathEngineSettings>) => {
    setSettings(prev => {
      const next = { ...prev, ...partial };
      if (typeof partial.baseFontUnit === 'number') {
        setBaseFontUnitState(partial.baseFontUnit);
      }
      if (typeof partial.letterSpacing === 'string') {
        setLetterSpacingState(partial.letterSpacing);
      }
      return next;
    });
  }, []);

  const resetToDefaults = useCallback(() => {
    setBaseFontUnitState(DEFAULT_SETTINGS.baseFontUnit);
    setLetterSpacingState(DEFAULT_SETTINGS.letterSpacing);
    setCurrentSubject('MATH');
    setActivePresetId('THPT_STANDARD');
    try {
      localStorage.removeItem(STORAGE_KEY);
    } catch {}
  }, []);

  const isCustomized = useMemo(() => {
    return baseFontUnit !== DEFAULT_SETTINGS.baseFontUnit || letterSpacing !== DEFAULT_SETTINGS.letterSpacing;
  }, [baseFontUnit, letterSpacing]);

  const value = useMemo(() => ({
    settings: {
      ...settings,
      baseFontUnit,
      letterSpacing
    },
    baseFontUnit,
    letterSpacing,
    currentSubject,
    activePresetId,
    currentPreset: activePresetId,
    availablePresets: MATH_PRESETS,
    setBaseFontUnit,
    setLetterSpacing,
    setSubject,
    applySubjectProfile: setSubject,
    applyPreset,
    updateSettings,
    resetToDefaults,
    isCustomized
  }), [
    settings,
    baseFontUnit,
    letterSpacing,
    currentSubject,
    activePresetId,
    setBaseFontUnit,
    setLetterSpacing,
    setSubject,
    applyPreset,
    updateSettings,
    resetToDefaults,
    isCustomized
  ]);

  return (
    <MathConfigContext.Provider value={value}>
      {children}
    </MathConfigContext.Provider>
  );
};

export const useMathConfig = (): MathConfigContextValue => {
  const context = useContext(MathConfigContext);
  if (!context) {
    // Fallback an toàn nếu component nằm ngoài provider
    return {
      settings: DEFAULT_SETTINGS,
      baseFontUnit: DEFAULT_SETTINGS.baseFontUnit,
      letterSpacing: DEFAULT_SETTINGS.letterSpacing,
      currentSubject: 'MATH',
      activePresetId: 'THPT_STANDARD',
      currentPreset: 'THPT_STANDARD',
      availablePresets: MATH_PRESETS,
      setBaseFontUnit: () => {},
      setLetterSpacing: () => {},
      setSubject: () => {},
      applySubjectProfile: () => {},
      applyPreset: () => {},
      updateSettings: () => {},
      resetToDefaults: () => {},
      isCustomized: false
    };
  }
  return context;
};
