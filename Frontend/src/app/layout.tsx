import type { Metadata } from 'next';
import './globals.css';
import { ThemeProvider } from './quiz/ThemeProvider';

export const metadata: Metadata = {
  title: 'AegisQuiz - Nền tảng Đào tạo Thông minh',
  description: 'Hệ thống thi trắc nghiệm và giáo trình AI',
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="vi">
      <body>
        <ThemeProvider>
          {children}
        </ThemeProvider>
      </body>
    </html>
  );
}
