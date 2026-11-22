import { lazy, Suspense, useCallback, useEffect, useState } from 'react';
import { api } from './api';
import { useAuth } from './auth';
import AuthModal from './components/AuthModal';
import FilterPanel from './components/FilterPanel';
import RestaurantCard from './components/RestaurantCard';
import RestaurantModal from './components/RestaurantModal';
import type { Filters, LatLon, Meta, RestaurantSummary, SortKey } from './types';

// Leaflet is only downloaded when the map is opened.
const MapView = lazy(() => import('./components/MapView'));

const EMPTY: Filters = {
  cuisines: [],
  tastes: [],
  areas: [],
  minSpice: 0,
  nativeOnly: false,
  hasReviews: false,
  q: '',
  sort: 'native',
};

function useDebounced<T>(value: T, ms: number): T {
  const [v, setV] = useState(value);
  useEffect(() => {
    const t = setTimeout(() => setV(value), ms);
    return () => clearTimeout(t);
  }, [value, ms]);
  return v;
}

export default function App() {
  const { user, checking, signOut, requestLogin, loginOpen } = useAuth();
  const [meta, setMeta] = useState<Meta | null>(null);
  const [filters, setFilters] = useState<Filters>(EMPTY);
  const [list, setList] = useState<RestaurantSummary[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [openId, setOpenId] = useState<number | null>(null);
  const [view, setView] = useState<'list' | 'map'>('list');
  const [here, setHere] = useState<LatLon | null>(null);
  const [locating, setLocating] = useState(false);

  const debounced = useDebounced(filters, 250);

  useEffect(() => {
    api.meta().then(setMeta).catch((e: Error) => setError(e.message));
  }, []);

  /** Loads pages 1..upTo so a refresh keeps what the user has already scrolled through. */
  const load = useCallback(
    async (upTo: number) => {
      setLoading(true);
      try {
        const pages = await Promise.all(
          Array.from({ length: upTo }, (_, i) => api.restaurants(debounced, i + 1, undefined, here)),
        );
        setList(pages.flatMap((p) => p.items));
        setTotal(pages[0]?.total ?? 0);
        setPage(upTo);
        setError(null);
      } catch (e) {
        setError(e instanceof Error ? e.message : 'Failed to load');
      } finally {
        setLoading(false);
      }
    },
    [debounced, here],
  );

  useEffect(() => {
    load(1);
  }, [load]);

  const loadMore = async () => {
    setLoading(true);
    try {
      const next = await api.restaurants(debounced, page + 1, undefined, here);
      setList((prev) => [...prev, ...next.items]);
      setTotal(next.total);
      setPage(page + 1);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load');
    } finally {
      setLoading(false);
    }
  };

  const findNearby = () => {
    if (!('geolocation' in navigator)) {
      setError('This browser does not support location');
      return;
    }
    setLocating(true);
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        setHere({ lat: pos.coords.latitude, lon: pos.coords.longitude });
        setFilters((f) => ({ ...f, sort: 'distance' }));
        setLocating(false);
      },
      () => {
        setError('Could not get your location. Please check the browser location permission.');
        setLocating(false);
      },
      { enableHighAccuracy: false, timeout: 10000, maximumAge: 300000 },
    );
  };

  return (
    <>
      <header className="site-header">
        <div className="brand">
          <h1>Foodie <span>Radar</span></h1>
          <p>Reviews from every platform, boiled down to the bits that actually help</p>
        </div>
        <input
          className="search"
          placeholder="Search restaurants, dishes or suburbs, e.g. “laksa”, “Takapuna”"
          value={filters.q}
          onChange={(e) => setFilters({ ...filters, q: e.target.value })}
        />
        <div className="account">
          {checking ? null : user ? (
            <>
              <span className="muted small">
                {user.displayName} · from {user.origin}
              </span>
              <button type="button" className="link" onClick={signOut}>Log out</button>
            </>
          ) : (
            <button type="button" className="secondary" onClick={requestLogin}>Log in / Sign up</button>
          )}
        </div>
      </header>

      <div className="layout">
        {meta ? (
          <FilterPanel meta={meta} filters={filters} onChange={setFilters} onReset={() => setFilters(EMPTY)} />
        ) : (
          <aside className="filters muted">Loading filters…</aside>
        )}

        <main>
          <div className="topbar">
            <span className="muted">
              {loading && list.length === 0 ? 'Loading…' : `${total} restaurant${total === 1 ? '' : 's'} found`}
            </span>
            <div className="topbar-actions">
              <button type="button" className="secondary" onClick={findNearby} disabled={locating}>
                {locating ? 'Locating…' : '📍 Near me'}
              </button>
              <div className="segmented">
                <button type="button" className={view === 'list' ? 'on' : ''} onClick={() => setView('list')}>List</button>
                <button type="button" className={view === 'map' ? 'on' : ''} onClick={() => setView('map')}>Map</button>
              </div>
              <select
                value={filters.sort}
                onChange={(e) => setFilters({ ...filters, sort: e.target.value as SortKey })}
              >
                <option value="native">Sort by authenticity</option>
                <option value="overall">Sort by overall rating</option>
                <option value="reviews">Sort by most reviewed</option>
                <option value="distance" disabled={!here}>Sort by distance{here ? '' : ' (tap “Near me” first)'}</option>
              </select>
            </div>
          </div>

          {error && (
            <div className="error">
              {error}
              <button type="button" className="link" onClick={() => setError(null)}>Dismiss</button>
            </div>
          )}

          {!loading && list.length === 0 ? (
            <div className="empty">No restaurants match. Try loosening the filters 🍜</div>
          ) : view === 'map' ? (
            <Suspense fallback={<div className="empty">Loading map…</div>}>
              <MapView items={list} here={here} onOpen={setOpenId} />
            </Suspense>
          ) : (
            <div className="grid">
              {list.map((r) => (
                <RestaurantCard key={r.id} r={r} onOpen={setOpenId} />
              ))}
            </div>
          )}

          {list.length < total && (
            <div className="more">
              <button type="button" className="secondary" onClick={loadMore} disabled={loading}>
                {loading ? 'Loading…' : `Load more (${total - list.length} left)`}
              </button>
              {view === 'map' && <div className="muted small">The map shows loaded restaurants only</div>}
            </div>
          )}
        </main>
      </div>

      <footer className="site-footer">
        Restaurant data © <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noreferrer">OpenStreetMap contributors</a> (ODbL)
      </footer>

      {openId !== null && meta && (
        <RestaurantModal
          id={openId}
          meta={meta}
          defaultNative={filters.nativeOnly}
          onClose={() => setOpenId(null)}
          onChanged={() => load(page)}
        />
      )}

      {loginOpen && meta && <AuthModal origins={meta.origins} />}
    </>
  );
}
