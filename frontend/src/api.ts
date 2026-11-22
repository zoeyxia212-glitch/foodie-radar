import type {
  AuthResponse,
  Filters,
  LatLon,
  Meta,
  NewReview,
  Paged,
  RestaurantDetail,
  RestaurantSummary,
  Review,
  User,
} from './types';

const BASE = import.meta.env.VITE_API_BASE ?? '';
export const PAGE_SIZE = 24;

// ---------- token storage ----------
// localStorage keeps the user signed in across tabs. For a public launch, consider an
// httpOnly cookie instead so injected scripts can't read the token.
const TOKEN_KEY = 'foodie-radar-token';
export const tokenStore = {
  get: (): string | null => {
    try {
      return localStorage.getItem(TOKEN_KEY);
    } catch {
      return null;
    }
  },
  set: (t: string | null) => {
    try {
      if (t) localStorage.setItem(TOKEN_KEY, t);
      else localStorage.removeItem(TOKEN_KEY);
    } catch {
      /* storage unavailable */
    }
  },
};

export class ApiError extends Error {
  constructor(message: string, public status: number) {
    super(message);
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const token = tokenStore.get();
  const res = await fetch(`${BASE}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init?.headers,
    },
  });
  if (!res.ok) {
    let message =
      res.status === 401 ? 'Please log in first' : res.status === 429 ? 'Too many requests, please try again shortly' : `Request failed (${res.status})`;
    try {
      const body = await res.json();
      if (body?.errors) message = Object.values(body.errors as Record<string, string[]>).flat().join('；');
      else if (body?.message) message = body.message;
    } catch {
      /* not JSON */
    }
    throw new ApiError(message, res.status);
  }
  return res.json() as Promise<T>;
}

const post = <T>(path: string, body?: unknown) =>
  request<T>(path, { method: 'POST', body: body === undefined ? undefined : JSON.stringify(body) });

export const api = {
  meta: () => request<Meta>('/api/meta'),

  restaurants: (f: Filters, page: number, pageSize = PAGE_SIZE, here?: LatLon | null) => {
    const p = new URLSearchParams();
    f.cuisines.forEach((c) => p.append('cuisines', c));
    f.tastes.forEach((t) => p.append('tastes', t));
    f.areas.forEach((a) => p.append('areas', a));
    if (f.minSpice > 0) p.set('minSpice', String(f.minSpice));
    if (f.nativeOnly) p.set('nativeOnly', 'true');
    if (f.hasReviews) p.set('hasReviews', 'true');
    if (f.q.trim()) p.set('q', f.q.trim());
    if (here) {
      p.set('lat', here.lat.toFixed(5));
      p.set('lon', here.lon.toFixed(5));
    }
    p.set('sort', f.sort);
    p.set('page', String(page));
    p.set('pageSize', String(pageSize));
    return request<Paged<RestaurantSummary>>(`/api/restaurants?${p}`);
  },

  restaurant: (id: number) => request<RestaurantDetail>(`/api/restaurants/${id}`),

  addReview: (id: number, body: NewReview) => post<Review>(`/api/restaurants/${id}/reviews`, body),

  helpful: (reviewId: number) => post<{ helpfulCount: number }>(`/api/reviews/${reviewId}/helpful`),

  register: (body: { email: string; password: string; displayName: string; origin: string }) =>
    post<AuthResponse>('/api/auth/register', body),

  login: (body: { email: string; password: string }) => post<AuthResponse>('/api/auth/login', body),

  me: () => request<User>('/api/auth/me'),
};
