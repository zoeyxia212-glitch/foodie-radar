import type { Filters, Meta } from '../types';
import { SPICE_LABELS } from './ui';

interface Props {
  meta: Meta;
  filters: Filters;
  onChange: (f: Filters) => void;
  onReset: () => void;
}

type ListKey = 'cuisines' | 'tastes' | 'areas';

export default function FilterPanel({ meta, filters, onChange, onReset }: Props) {
  const toggle = (key: ListKey, value: string) => {
    const current = filters[key];
    const next = current.includes(value) ? current.filter((v) => v !== value) : [...current, value];
    onChange({ ...filters, [key]: next });
  };

  const chip = (key: ListKey, value: string, label: string, extra = '') => (
    <button
      key={value}
      type="button"
      className={`chip ${extra} ${filters[key].includes(value) ? 'on' : ''}`}
      onClick={() => toggle(key, value)}
    >
      {label}
    </button>
  );

  return (
    <aside className="filters">
      <section>
        <h3>🌏 Cuisine / country</h3>
        <div className="chips">{meta.cuisines.map((c) => chip('cuisines', c.key, `${c.flag} ${c.name}`))}</div>
      </section>

      <section>
        <h3>👅 Taste</h3>
        <div className="chips">{meta.tastes.map((t) => chip('tastes', t, t, 'spicy'))}</div>
      </section>

      <section>
        <h3>🌶 Minimum spice</h3>
        <input
          type="range"
          min={0}
          max={5}
          value={filters.minSpice}
          onChange={(e) => onChange({ ...filters, minSpice: Number(e.target.value) })}
        />
        <div className="range-labels">
          <span>Any</span>
          <b>{SPICE_LABELS[filters.minSpice]}</b>
          <span>Extra hot</span>
        </div>
      </section>

      <section>
        <h3>📍 Area</h3>
        <div className="chips">{meta.areas.map((a) => chip('areas', a, a))}</div>
      </section>

      <section>
        <label className="toggle">
          <input
            type="checkbox"
            checked={filters.nativeOnly}
            onChange={(e) => onChange({ ...filters, nativeOnly: e.target.checked })}
          />
          <div>
            <b>Native reviewers only</b>
            <small>Only show places reviewed by people from the cuisine’s home country, and open their reviews first</small>
          </div>
        </label>
      </section>

      <section>
        <label className="toggle plain">
          <input
            type="checkbox"
            checked={filters.hasReviews}
            onChange={(e) => onChange({ ...filters, hasReviews: e.target.checked })}
          />
          <div>
            <b>Only places with reviews</b>
          </div>
        </label>
      </section>

      <button type="button" className="reset" onClick={onReset}>
        Clear filters
      </button>
    </aside>
  );
}
