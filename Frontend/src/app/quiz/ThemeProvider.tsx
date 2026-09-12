'use client';
import { createContext, useContext, useState, ReactNode } from 'react';

// ===== THEME ENGINE =====
// Logic Quiz Engine dùng chung, chỉ khác "bộ da" (Skin)
export type ThemeMode = 'pro' | 'kid' | 'paper';

interface ThemeContextType {
  theme: ThemeMode;
  setTheme: (theme: ThemeMode) => void;
}

const ThemeContext = createContext<ThemeContextType>({ theme: 'pro', setTheme: () => {} });

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [theme, setTheme] = useState<ThemeMode>('pro');
  return (
    <ThemeContext.Provider value={{ theme, setTheme }}>
      {children}
    </ThemeContext.Provider>
  );
}

export function useTheme() {
  return useContext(ThemeContext);
}

// ===== THEME CONFIG =====
export const themeConfig = {
  pro: {
    bg: 'bg-gray-950',
    card: 'bg-gray-900 border-gray-800',
    text: 'text-white',
    accent: 'text-blue-400',
    button: 'bg-blue-600 hover:bg-blue-700',
    font: 'font-sans',
  },
  kid: {
    bg: 'bg-gradient-to-br from-yellow-100 via-pink-100 to-blue-100',
    card: 'bg-white border-4 border-yellow-300 shadow-xl',
    text: 'text-gray-800',
    accent: 'text-pink-500',
    button: 'bg-gradient-to-r from-pink-500 to-purple-500 hover:from-pink-600 hover:to-purple-600 text-white',
    font: 'font-[Comic_Neue]',
  },
  paper: {
    bg: 'bg-white',
    card: 'bg-white border border-black',
    text: 'text-black',
    accent: 'text-black',
    button: 'bg-white border-2 border-black text-black hover:bg-gray-100',
    font: 'font-serif',
  },
};
