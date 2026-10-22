import React from 'react';

interface FlagIconProps {
  code: string;
  className?: string;
}

export const FlagIcon: React.FC<FlagIconProps> = ({ code, className = 'w-5 h-3.5' }) => {
  const norm = code?.toLowerCase().trim();

  switch (norm) {
    case 'vi': // Vietnam
      return (
        <svg viewBox="0 0 30 20" className={className} xmlns="http://www.w3.org/2000/svg">
          <rect width="30" height="20" fill="#DA251D" />
          <polygon
            points="15,4 16.5,8.8 21.5,8.8 17.5,11.8 19,16.5 15,13.6 11,16.5 12.5,11.8 8.5,8.8 13.5,8.8"
            fill="#FFFF00"
          />
        </svg>
      );

    case 'en': // United Kingdom (Union Jack)
      return (
        <svg viewBox="0 0 60 30" className={className} xmlns="http://www.w3.org/2000/svg">
          <clipPath id="uk-clip">
            <rect width="60" height="30" />
          </clipPath>
          <g clipPath="url(#uk-clip)">
            <rect width="60" height="30" fill="#012169" />
            <path d="M0,0 L60,30 M60,0 L0,30" stroke="#FFFFFF" strokeWidth="6" />
            <path d="M0,0 L60,30 M60,0 L0,30" stroke="#C8102E" strokeWidth="3" />
            <path d="M30,0 v30 M0,15 h60" stroke="#FFFFFF" strokeWidth="10" />
            <path d="M30,0 v30 M0,15 h60" stroke="#C8102E" strokeWidth="6" />
          </g>
        </svg>
      );

    case 'zh': // China
      return (
        <svg viewBox="0 0 30 20" className={className} xmlns="http://www.w3.org/2000/svg">
          <rect width="30" height="20" fill="#DE2910" />
          {/* Main Star */}
          <polygon points="5,2 5.9,4.8 8.8,4.8 6.5,6.5 7.4,9.2 5,7.6 2.6,9.2 3.5,6.5 1.2,4.8 4.1,4.8" fill="#FFDE00" />
          {/* 4 Little Stars */}
          <circle cx="10" cy="2.5" r="0.9" fill="#FFDE00" />
          <circle cx="12" cy="4.5" r="0.9" fill="#FFDE00" />
          <circle cx="12" cy="7.5" r="0.9" fill="#FFDE00" />
          <circle cx="10" cy="9.5" r="0.9" fill="#FFDE00" />
        </svg>
      );

    case 'es': // Spain
      return (
        <svg viewBox="0 0 30 20" className={className} xmlns="http://www.w3.org/2000/svg">
          <rect width="30" height="5" fill="#AA151B" />
          <rect y="5" width="30" height="10" fill="#F1BF00" />
          <rect y="15" width="30" height="5" fill="#AA151B" />
          {/* Minimal Crown/Coat */}
          <circle cx="7" cy="10" r="2.2" fill="#AA151B" opacity="0.8" />
        </svg>
      );

    case 'fr': // France
      return (
        <svg viewBox="0 0 30 20" className={className} xmlns="http://www.w3.org/2000/svg">
          <rect width="10" height="20" fill="#002654" />
          <rect x="10" width="10" height="20" fill="#FFFFFF" />
          <rect x="20" width="10" height="20" fill="#CE1126" />
        </svg>
      );

    case 'ru': // Russia
      return (
        <svg viewBox="0 0 30 20" className={className} xmlns="http://www.w3.org/2000/svg">
          <rect width="30" height="6.66" fill="#FFFFFF" />
          <rect y="6.66" width="30" height="6.66" fill="#0039A6" />
          <rect y="13.33" width="30" height="6.67" fill="#D52B1E" />
        </svg>
      );

    case 'hi': // India
      return (
        <svg viewBox="0 0 30 20" className={className} xmlns="http://www.w3.org/2000/svg">
          <rect width="30" height="6.66" fill="#FF9933" />
          <rect y="6.66" width="30" height="6.66" fill="#FFFFFF" />
          <rect y="13.33" width="30" height="6.67" fill="#138808" />
          {/* Ashoka Chakra */}
          <circle cx="15" cy="10" r="2.3" fill="none" stroke="#000080" strokeWidth="0.8" />
          <circle cx="15" cy="10" r="0.6" fill="#000080" />
        </svg>
      );

    case 'pt': // Brazil (Português)
      return (
        <svg viewBox="0 0 30 20" className={className} xmlns="http://www.w3.org/2000/svg">
          <rect width="30" height="20" fill="#009C3B" />
          <polygon points="15,2.5 27,10 15,17.5 3,10" fill="#FFDF00" />
          <circle cx="15" cy="10" r="4" fill="#002776" />
          <path d="M11.5,10.5 Q15,8.5 18.5,11" stroke="#FFFFFF" strokeWidth="0.8" fill="none" />
        </svg>
      );

    case 'id': // Indonesia
      return (
        <svg viewBox="0 0 30 20" className={className} xmlns="http://www.w3.org/2000/svg">
          <rect width="30" height="10" fill="#CE1126" />
          <rect y="10" width="30" height="10" fill="#FFFFFF" />
        </svg>
      );

    case 'lo': // Laos
      return (
        <svg viewBox="0 0 30 20" className={className} xmlns="http://www.w3.org/2000/svg">
          <rect width="30" height="5" fill="#CE1126" />
          <rect y="5" width="30" height="10" fill="#002868" />
          <rect y="15" width="30" height="5" fill="#CE1126" />
          <circle cx="15" cy="10" r="3.6" fill="#FFFFFF" />
        </svg>
      );

    case 'ja': // Japan
      return (
        <svg viewBox="0 0 30 20" className={className} xmlns="http://www.w3.org/2000/svg">
          <rect width="30" height="20" fill="#FFFFFF" />
          <circle cx="15" cy="10" r="6" fill="#BC002D" />
        </svg>
      );

    case 'ko': // South Korea
      return (
        <svg viewBox="0 0 30 20" className={className} xmlns="http://www.w3.org/2000/svg">
          <rect width="30" height="20" fill="#FFFFFF" />
          {/* Taegeuk Symbol */}
          <circle cx="15" cy="10" r="4.5" fill="#CD2E3A" />
          <path d="M15,5.5 A4.5,4.5 0 0,0 15,14.5 A2.25,2.25 0 0,0 15,10 A2.25,2.25 0 0,1 15,5.5" fill="#0047A0" />
          {/* Trigram dots/bars */}
          <rect x="6.5" y="4" width="2" height="4" fill="#000000" transform="rotate(-35 7.5 6)" />
          <rect x="21.5" y="4" width="2" height="4" fill="#000000" transform="rotate(35 22.5 6)" />
          <rect x="6.5" y="12" width="2" height="4" fill="#000000" transform="rotate(35 7.5 14)" />
          <rect x="21.5" y="12" width="2" height="4" fill="#000000" transform="rotate(-35 22.5 14)" />
        </svg>
      );

    case 'ar': // Saudi Arabia / Arabic
      return (
        <svg viewBox="0 0 30 20" className={className} xmlns="http://www.w3.org/2000/svg">
          <rect width="30" height="20" fill="#006C35" />
          {/* Arabic Calligraphy & Sword Representation */}
          <rect x="7" y="6.5" width="16" height="2.2" fill="#FFFFFF" rx="0.5" opacity="0.9" />
          <polygon points="6,12.5 24,12.5 22,11.5 24,13.5 6,13" fill="#FFFFFF" />
          <rect x="8" y="11" width="1.2" height="4" fill="#FFFFFF" />
        </svg>
      );

    default:
      return (
        <svg viewBox="0 0 30 20" className={className} xmlns="http://www.w3.org/2000/svg">
          <rect width="30" height="20" fill="#1E293B" />
          <text x="15" y="13" textAnchor="middle" fill="#94A3B8" fontSize="8" fontWeight="bold">
            {code.toUpperCase()}
          </text>
        </svg>
      );
  }
};
