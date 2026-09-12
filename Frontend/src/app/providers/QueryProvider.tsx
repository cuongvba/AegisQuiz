/**
 * AegisQuiz — QueryProvider
 * ==========================
 * TanStack Query client setup tập trung.
 * FSD Layer: app/providers/
 */

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
import { type ReactNode, useState } from 'react';
import { isApiError } from '@/shared/api/types';

function makeQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        // Dữ liệu không stale trong 60 giây
        staleTime:           60 * 1000,
        // Cache trong 5 phút
        gcTime:              5 * 60 * 1000,
        // Retry 1 lần, không retry với 4xx errors
        retry: (failureCount, error) => {
          if (isApiError(error) && error.status >= 400 && error.status < 500) {
            return false;
          }
          return failureCount < 1;
        },
        // Refetch khi window focus trong production only
        refetchOnWindowFocus: import.meta.env.PROD,
      },
      mutations: {
        retry: false,
      },
    },
  });
}

// Singleton pattern — 1 QueryClient cho toàn app
let browserQueryClient: QueryClient | undefined;

function getQueryClient(): QueryClient {
  if (!browserQueryClient) {
    browserQueryClient = makeQueryClient();
  }
  return browserQueryClient;
}

export function QueryProvider({ children }: { children: ReactNode }) {
  // useState để đảm bảo mỗi component tree có client riêng khi SSR
  const [queryClient] = useState(getQueryClient);

  return (
    <QueryClientProvider client={queryClient}>
      {children}
      {import.meta.env.DEV && (
        <ReactQueryDevtools initialIsOpen={false} buttonPosition="bottom-left" />
      )}
    </QueryClientProvider>
  );
}
