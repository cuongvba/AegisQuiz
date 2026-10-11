import React from 'react';
import {
  Landmark,
  GraduationCap,
  HeartPulse,
  Car,
  Layers,
  HardHat,
  ShieldCheck,
  Code,
  Globe,
  BookOpen,
  Cpu,
  Award,
  Compass,
  Briefcase,
  Stethoscope,
  Scale,
  Flame,
  Shield,
  Zap,
  Sparkles,
  Folder,
  FileQuestion,
  type LucideIcon,
} from 'lucide-react';

const ICON_MAP: Record<string, LucideIcon> = {
  landmark: Landmark,
  graduationcap: GraduationCap,
  heartpulse: HeartPulse,
  car: Car,
  layers: Layers,
  hardhat: HardHat,
  shieldcheck: ShieldCheck,
  code: Code,
  globe: Globe,
  bookopen: BookOpen,
  cpu: Cpu,
  award: Award,
  compass: Compass,
  briefcase: Briefcase,
  stethoscope: Stethoscope,
  scale: Scale,
  flame: Flame,
  shield: Shield,
  zap: Zap,
  sparkles: Sparkles,
  folder: Folder,
  filequestion: FileQuestion,
};

interface DynamicDomainIconProps {
  icon?: string;
  size?: number;
  className?: string;
}

/**
 * [Living Design Component] Render Icon linh hoạt cho Miền Ngành Nghề (Dynamic Domain)
 * - Tự động nhận diện chuỗi tên Icon Lucide (Landmark, GraduationCap, Car...) và render SVG chuẩn
 * - Hỗ trợ Emoji nguyên bản (🏦, 🎓, 🏥, 🚗...)
 * - Fallback thanh lịch về Layers/Folder nếu không khớp
 */
export const DynamicDomainIcon: React.FC<DynamicDomainIconProps> = ({
  icon,
  size = 16,
  className = '',
}) => {
  if (!icon || !icon.trim()) {
    return <Layers size={size} className={className} />;
  }

  const trimmed = icon.trim();

  // Kiểm tra nếu là Emoji (Unicode Extended Pictographic)
  if (/\p{Extended_Pictographic}/u.test(trimmed)) {
    return (
      <span
        style={{ fontSize: `${size}px`, lineHeight: 1 }}
        className={`inline-flex items-center justify-center select-none ${className}`}
        aria-hidden="true"
      >
        {trimmed}
      </span>
    );
  }

  // Chuẩn hóa tên icon không phân biệt chữ hoa / thường
  const normalizedKey = trimmed.toLowerCase().replace(/[^a-z0-9]/g, '');
  const IconComponent = ICON_MAP[normalizedKey];

  if (IconComponent) {
    return <IconComponent size={size} className={className} aria-hidden="true" />;
  }

  return <Layers size={size} className={className} aria-hidden="true" />;
};

export default DynamicDomainIcon;
