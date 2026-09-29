/**
 * @file logger.ts
 * @description [World-Class] AegisQuiz Structured Frontend Logger
 *
 * Tính năng:
 * - Log levels: debug | info | warn | error
 * - Production mode: suppress debug/info, chỉ giữ warn/error
 * - Timestamp ISO 8601 + context tag + emoji prefix
 * - Singleton pattern — import trực tiếp, không cần provider
 * - An toàn: không crash nếu console không available (SSR, workers)
 * - Tree-shakeable: production build loại bỏ debug logs
 */

type LogLevel = 'debug' | 'info' | 'warn' | 'error';

interface LogEntry {
  level: LogLevel;
  context: string;
  message: string;
  timestamp: string;
  data?: unknown[];
}

const LEVEL_ICONS: Record<LogLevel, string> = {
  debug: '🔍',
  info:  '💡',
  warn:  '⚠️',
  error: '🚨',
};

const LEVEL_PRIORITY: Record<LogLevel, number> = {
  debug: 0,
  info:  1,
  warn:  2,
  error: 3,
};

// Production: chỉ warn và error. Development: tất cả
const MIN_LEVEL: LogLevel =
  import.meta.env.PROD ? 'warn' : 'debug';

function formatMessage(entry: LogEntry): string {
  return `${LEVEL_ICONS[entry.level]} [AegisQuiz:${entry.context}] ${entry.timestamp} — ${entry.message}`;
}

function shouldLog(level: LogLevel): boolean {
  return LEVEL_PRIORITY[level] >= LEVEL_PRIORITY[MIN_LEVEL];
}

function log(level: LogLevel, context: string, message: string, ...data: unknown[]): void {
  if (!shouldLog(level)) return;

  const entry: LogEntry = {
    level,
    context,
    message,
    timestamp: new Date().toISOString(),
    data,
  };

  const formatted = formatMessage(entry);

  try {
    switch (level) {
      case 'debug':
        // eslint-disable-next-line no-console
        data.length > 0 ? console.debug(formatted, ...data) : console.debug(formatted);
        break;
      case 'info':
        // eslint-disable-next-line no-console
        data.length > 0 ? console.info(formatted, ...data) : console.info(formatted);
        break;
      case 'warn':
        // eslint-disable-next-line no-console
        data.length > 0 ? console.warn(formatted, ...data) : console.warn(formatted);
        break;
      case 'error':
        // eslint-disable-next-line no-console
        data.length > 0 ? console.error(formatted, ...data) : console.error(formatted);
        break;
    }
  } catch {
    // Silent fail — không bao giờ crash app vì logger
  }
}

/**
 * Tạo logger instance với context cụ thể (tên component/service).
 *
 * @example
 * const logger = createLogger('MathSettingsPage');
 * logger.info('Lưu cấu hình thành công');
 * logger.error('Lỗi API:', err);
 */
export function createLogger(context: string) {
  return {
    debug: (message: string, ...data: unknown[]) => log('debug', context, message, ...data),
    info:  (message: string, ...data: unknown[]) => log('info',  context, message, ...data),
    warn:  (message: string, ...data: unknown[]) => log('warn',  context, message, ...data),
    error: (message: string, ...data: unknown[]) => log('error', context, message, ...data),
  };
}

/** Logger mặc định toàn cục cho những nơi không muốn tạo context riêng */
export const logger = createLogger('AegisQuiz');

export default logger;
