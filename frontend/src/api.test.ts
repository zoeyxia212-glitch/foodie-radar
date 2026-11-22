import { afterEach, describe, expect, it, vi } from 'vitest';
import { ApiError, api } from './api';
import type { Filters } from './types';

const filters: Filters = {
  cuisines: ['sichuan', 'japan'],
  tastes: ['Mala'],
  areas: [],
  minSpice: 3,
  nativeOnly: true,
  hasReviews: false,
  q: '  laksa ',
  sort: 'native',
};

function mockFetch(status: number, body: unknown) {
  const fn = vi.fn().mockResolvedValue(
    new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } }),
  );
  vi.stubGlobal('fetch', fn);
  return fn;
}

afterEach(() => vi.unstubAllGlobals());

describe('api.restaurants', () => {
  it('turns filters into the query string the backend expects', async () => {
    const fetchMock = mockFetch(200, { items: [], total: 0, page: 1, pageSize: 24 });

    await api.restaurants(filters, 2, 24, { lat: -36.8485, lon: 174.7622 });

    const url = new URL(fetchMock.mock.calls[0][0] as string, 'http://localhost');
    expect(url.pathname).toBe('/api/restaurants');
    expect(url.searchParams.getAll('cuisines')).toEqual(['sichuan', 'japan']);
    expect(url.searchParams.get('tastes')).toBe('Mala');
    expect(url.searchParams.get('minSpice')).toBe('3');
    expect(url.searchParams.get('nativeOnly')).toBe('true');
    expect(url.searchParams.has('hasReviews')).toBe(false);
    expect(url.searchParams.get('q')).toBe('laksa');
    expect(url.searchParams.get('page')).toBe('2');
    expect(url.searchParams.get('lat')).toBe('-36.84850');
  });
});

describe('error handling', () => {
  it('surfaces backend validation messages', async () => {
    mockFetch(400, { errors: { rating: ['Rating must be between 1 and 5'] } });

    await expect(api.addReview(1, {
      rating: 9, text: 'x', recommendedDishes: [], avoidDishes: [], tastes: [], spiceRating: null, priceLevel: null,
    })).rejects.toThrow('Rating must be between 1 and 5');
  });

  it('gives a friendly message and status for 401', async () => {
    mockFetch(401, {});
    const err = await api.me().catch((e) => e);
    expect(err).toBeInstanceOf(ApiError);
    expect((err as ApiError).status).toBe(401);
    expect((err as ApiError).message).toBe('Please log in first');
  });
});
