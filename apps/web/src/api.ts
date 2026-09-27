export type ApiEnvelope<T> = {
  success: boolean;
  message: string;
  data?: T;
  errorCode?: string;
  timestamp?: string;
};

const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? '/api').replace(/\/$/, '');

export async function apiRequest<T>(path: string, options: RequestInit = {}, householdId?: string | null): Promise<T> {
  const normalizedPath = path.replace(/^\/+/, '');
  const url = `${API_BASE_URL}/${normalizedPath}`;
  const headers = new Headers(options.headers ?? {});

  if (!headers.has('Content-Type') && options.body && typeof options.body === 'string') {
    headers.set('Content-Type', 'application/json');
  }

  if (householdId) {
    headers.set('x-household-id', householdId);
  }

  const response = await fetch(url, { ...options, headers });
  const payload = await response.json().catch(() => null) as ApiEnvelope<T> | T | null;

  if (!response.ok) {
    const message = typeof payload === 'object' && payload && 'message' in payload ? String((payload as { message?: string }).message ?? 'Request failed') : 'Request failed';
    throw new Error(message);
  }

  if (payload && typeof payload === 'object' && 'success' in payload) {
    const envelope = payload as ApiEnvelope<T>;
    if (!envelope.success) {
      throw new Error(envelope.message ?? 'Request failed');
    }

    return (envelope.data ?? undefined) as T;
  }

  return payload as T;
}
