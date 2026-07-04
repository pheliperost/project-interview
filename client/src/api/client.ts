import type { AuthResponse, KanbanStatus, Task, TaskFilters, TaskListResponse } from './types';

const TOKEN_KEY = 'simple_tasks_token';
const EMAIL_KEY = 'simple_tasks_email';
const EXPIRES_KEY = 'simple_tasks_expires';

export class ApiError extends Error {
  status: number;

  constructor(message: string, status: number) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
  }
}

let unauthorizedHandler: (() => void) | null = null;

export function setUnauthorizedHandler(handler: (() => void) | null) {
  unauthorizedHandler = handler;
}

function notifyUnauthorized() {
  unauthorizedHandler?.();
}

function decodeJwtExp(token: string): number | null {
  try {
    const payload = token.split('.')[1];
    if (!payload) return null;
    const json = JSON.parse(atob(payload.replace(/-/g, '+').replace(/_/g, '/'))) as { exp?: number };
    return typeof json.exp === 'number' ? json.exp : null;
  } catch {
    return null;
  }
}

export function getToken() {
  return localStorage.getItem(TOKEN_KEY);
}

export function isTokenValid() {
  const token = getToken();
  if (!token) return false;

  const storedExpires = localStorage.getItem(EXPIRES_KEY);
  if (storedExpires && Date.now() >= new Date(storedExpires).getTime()) return false;

  const jwtExp = decodeJwtExp(token);
  if (jwtExp !== null && Date.now() >= jwtExp * 1000) return false;

  return true;
}

export function setAuth(token: string, email: string, expiresAt?: string) {
  localStorage.setItem(TOKEN_KEY, token);
  localStorage.setItem(EMAIL_KEY, email);
  if (expiresAt) localStorage.setItem(EXPIRES_KEY, expiresAt);
}

export function clearAuth() {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(EMAIL_KEY);
  localStorage.removeItem(EXPIRES_KEY);
}

export function getEmail() {
  return localStorage.getItem(EMAIL_KEY);
}

function isPublicAuthPath(path: string) {
  const base = path.split('?')[0];
  return base === '/api/auth/login' || base === '/api/auth/register';
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const token = getToken();
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(options.headers as Record<string, string>),
  };
  if (token) {
    if (!isTokenValid()) {
      clearAuth();
      notifyUnauthorized();
      throw new ApiError('Session expired. Please sign in again.', 401);
    }
    headers.Authorization = `Bearer ${token}`;
  }

  const response = await fetch(path, { ...options, headers });
  if (!response.ok) {
    const body = await response.json().catch(() => ({}));
    if (response.status === 401 && !isPublicAuthPath(path)) {
      clearAuth();
      notifyUnauthorized();
    }
    throw new ApiError(body.error ?? response.statusText, response.status);
  }
  if (response.status === 204) return undefined as T;
  const data = await response.json();

  const pathWithoutQuery = path.split('?')[0];
  if (pathWithoutQuery === '/api/tasks' && data && typeof data === 'object' && 'items' in data) {
    const list = data as { items: Record<string, unknown>[] };
    return { ...list, items: list.items.map((item) => normalizeTask(item)) } as T;
  }
  if (path.includes('/tasks') && data && typeof data === 'object' && 'id' in data) {
    return normalizeTask(data) as T;
  }
  return data;
}

const STATUS_BY_NUMBER: Record<number, import('./types').KanbanStatus> = {
  0: 'Todo',
  1: 'InProgress',
  2: 'OnHold',
  3: 'InReview',
  4: 'Completed',
  5: 'Cancelled',
};

const PRIORITY_BY_NUMBER: Record<number, import('./types').TaskPriority> = {
  0: 'Low',
  1: 'Medium',
  2: 'High',
  3: 'Urgent',
};

function normalizeTask(raw: Record<string, unknown>): import('./types').Task {
  const status = raw.status;
  const priority = raw.priority;
  const task = raw as unknown as import('./types').Task;
  return {
    ...task,
    status:
      typeof status === 'number'
        ? STATUS_BY_NUMBER[status]
        : (status as import('./types').KanbanStatus),
    priority:
      typeof priority === 'number'
        ? PRIORITY_BY_NUMBER[priority]
        : (priority as import('./types').TaskPriority),
  };
}

export const api = {
  register: (email: string, password: string) =>
    request<AuthResponse>('/api/auth/register', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    }),

  login: (email: string, password: string) =>
    request<AuthResponse>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    }),

  logout: () => request<void>('/api/auth/logout', { method: 'POST' }),

  getTasks: async (filters: TaskFilters) => {
    const params = new URLSearchParams();
    if (filters.search.trim()) params.set('search', filters.search.trim());
    filters.statuses.forEach((s) => params.append('status', s));
    if (filters.createdFrom) params.set('createdFrom', filters.createdFrom);
    if (filters.createdTo) params.set('createdTo', filters.createdTo);
    if (filters.updatedFrom) params.set('updatedFrom', filters.updatedFrom);
    if (filters.updatedTo) params.set('updatedTo', filters.updatedTo);
    const qs = params.toString();
    const list = await request<TaskListResponse>(`/api/tasks${qs ? `?${qs}` : ''}`);
    return list.items;
  },

  createTask: (body: {
    title: string;
    description?: string;
    priority?: string;
    dueDate?: string;
  }) =>
    request<Task>('/api/tasks', { method: 'POST', body: JSON.stringify(body) }),

  updateTask: (
    id: string,
    body: {
      title: string;
      description?: string;
      status: KanbanStatus;
      priority: string;
      dueDate?: string | null;
    },
  ) => request<Task>(`/api/tasks/${id}`, { method: 'PUT', body: JSON.stringify(body) }),

  deleteTask: (id: string) => request<void>(`/api/tasks/${id}`, { method: 'DELETE' }),

  reactivate: (id: string) =>
    request<Task>(`/api/tasks/${id}/reactivate`, { method: 'POST' }),
};

export function buildDateRange(
  preset: TaskFilters['createdPreset'],
  from?: string,
  to?: string,
): { from?: string; to?: string } {
  if (preset === 'any') return {};
  const now = new Date();
  const startOfDay = (d: Date) =>
    new Date(d.getFullYear(), d.getMonth(), d.getDate()).toISOString();
  const endOfDay = (d: Date) =>
    new Date(d.getFullYear(), d.getMonth(), d.getDate(), 23, 59, 59).toISOString();

  if (preset === 'today') {
    return { from: startOfDay(now), to: endOfDay(now) };
  }
  if (preset === 'last7') {
    const fromDate = new Date(now);
    fromDate.setDate(fromDate.getDate() - 7);
    return { from: startOfDay(fromDate), to: endOfDay(now) };
  }
  if (preset === 'last30') {
    const fromDate = new Date(now);
    fromDate.setDate(fromDate.getDate() - 30);
    return { from: startOfDay(fromDate), to: endOfDay(now) };
  }
  if (preset === 'custom') {
    return {
      from: from ? startOfDay(new Date(from)) : undefined,
      to: to ? endOfDay(new Date(to)) : undefined,
    };
  }
  return {};
}
