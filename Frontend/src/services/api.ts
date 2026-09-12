import axios from 'axios';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? '',
  timeout: 30000,
});

const GUID_REGEX = /^[0-9a-fA-F-]{36}$/;

function resolveTenantId(): string | null {
  const direct = localStorage.getItem('tenant_id') || localStorage.getItem('tenantId');
  if (direct && GUID_REGEX.test(direct)) return direct;

  const userRaw = localStorage.getItem('user');
  if (!userRaw) return null;
  try {
    const user = JSON.parse(userRaw) as { tenantId?: string; tenant_id?: string };
    const candidate = user.tenantId || user.tenant_id;
    if (candidate && GUID_REGEX.test(candidate)) return candidate;
  } catch {
    return null;
  }
  return null;
}

// Interceptor: tự động thêm Bearer token + X-Tenant-ID vào mọi request
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  const tenantId = resolveTenantId();
  if (tenantId) config.headers['X-Tenant-ID'] = tenantId;
  return config;
});

export default api;
