import { useState, type FormEvent, type KeyboardEvent } from 'react';
import { api } from '../api';
import { useAuth } from '../auth';
import type { Meta } from '../types';
import { SPICE_LABELS } from './ui';

interface Props {
  restaurantId: number;
  meta: Meta;
  /** Dishes already known for this restaurant, offered as one-tap suggestions. */
  knownDishes: string[];
  alreadyReviewed: boolean;
  onCreated: () => void;
}

const MAX_DISHES = 5;

function DishPicker({
  label,
  value,
  onChange,
  suggestions,
  tone,
}: {
  label: string;
  value: string[];
  onChange: (v: string[]) => void;
  suggestions: string[];
  tone: 'good' | 'bad';
}) {
  const [draft, setDraft] = useState('');

  const add = (dish: string) => {
    const d = dish.trim();
    if (!d || d.length > 30 || value.length >= MAX_DISHES) return;
    if (value.some((x) => x.toLowerCase() === d.toLowerCase())) return;
    onChange([...value, d]);
  };

  const onKey = (e: KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter' || e.key === ',' || e.key === '，') {
      e.preventDefault();
      add(draft);
      setDraft('');
    }
  };

  const unused = suggestions.filter((s) => !value.includes(s));

  return (
    <div className="picker">
      <span className="picker-label">{label}</span>
      <div className="chips">
        {value.map((d) => (
          <button key={d} type="button" className={`chip on ${tone}`} onClick={() => onChange(value.filter((x) => x !== d))}>
            {d} ×
          </button>
        ))}
        {value.length < MAX_DISHES && (
          <input
            className="chip-input"
            placeholder="Type a dish, press Enter"
            value={draft}
            maxLength={30}
            onChange={(e) => setDraft(e.target.value)}
            onKeyDown={onKey}
            onBlur={() => {
              add(draft);
              setDraft('');
            }}
          />
        )}
      </div>
      {unused.length > 0 && value.length < MAX_DISHES && (
        <div className="suggest">
          {unused.slice(0, 6).map((s) => (
            <button key={s} type="button" className="chip ghost" onClick={() => add(s)}>
              + {s}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}

export default function ReviewForm({ restaurantId, meta, knownDishes, onCreated, alreadyReviewed }: Props) {
  const { user, requestLogin } = useAuth();
  const [rating, setRating] = useState(5);
  const [text, setText] = useState('');
  const [recommended, setRecommended] = useState<string[]>([]);
  const [avoid, setAvoid] = useState<string[]>([]);
  const [tastes, setTastes] = useState<string[]>([]);
  const [spice, setSpice] = useState<number | null>(null);
  const [priceLevel, setPriceLevel] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const toggleTaste = (t: string) =>
    setTastes((prev) => (prev.includes(t) ? prev.filter((x) => x !== t) : [...prev, t]));

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);
    setSaving(true);
    try {
      await api.addReview(restaurantId, {
        rating,
        text,
        recommendedDishes: recommended,
        avoidDishes: avoid,
        tastes,
        spiceRating: spice,
        priceLevel,
      });
      setText('');
      setRecommended([]);
      setAvoid([]);
      setTastes([]);
      setSpice(null);
      setPriceLevel(null);
      onCreated();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not submit');
    } finally {
      setSaving(false);
    }
  };

  if (!user) {
    return (
      <div className="review-form locked">
        <h3>✍️ Write a review</h3>
        <p className="muted">Log in to write a review. Where you’re from feeds the native-reviewer score.</p>
        <button type="button" className="primary" onClick={requestLogin}>Log in / Sign up</button>
      </div>
    );
  }

  if (alreadyReviewed) {
    return (
      <div className="review-form locked">
        <p className="muted">✅ You’ve already reviewed this place. Thanks!</p>
      </div>
    );
  }

  return (
    <form className="review-form" onSubmit={submit}>
      <h3>✍️ Write a review</h3>
      <p className="muted small">
        Posting as <b>{user.displayName}</b> (from {user.origin})
      </p>

      <div className="form-row single">
        <select value={rating} onChange={(e) => setRating(Number(e.target.value))}>
          {[5, 4, 3, 2, 1].map((n) => (
            <option key={n} value={n}>{'★'.repeat(n)}</option>
          ))}
        </select>
      </div>

      <DishPicker label="👍 Order" value={recommended} onChange={setRecommended} suggestions={knownDishes} tone="good" />
      <DishPicker label="👎 Skip" value={avoid} onChange={setAvoid} suggestions={knownDishes} tone="bad" />

      <div className="picker">
        <span className="picker-label">👅 Taste</span>
        <div className="chips">
          {meta.tastes.map((t) => (
            <button key={t} type="button" className={`chip spicy ${tastes.includes(t) ? 'on' : ''}`} onClick={() => toggleTaste(t)}>
              {t}
            </button>
          ))}
        </div>
      </div>

      <div className="picker">
        <span className="picker-label">🌶 Spice</span>
        <div className="chips">
          {[0, 1, 2, 3, 4, 5].map((n) => (
            <button key={n} type="button" className={`chip spicy ${spice === n ? 'on' : ''}`} onClick={() => setSpice(spice === n ? null : n)}>
              {n === 0 ? 'None' : SPICE_LABELS[n].replace('+', '')}
            </button>
          ))}
        </div>
      </div>

      <div className="picker">
        <span className="picker-label">💰 Per person</span>
        <div className="chips">
          {[
            [1, '$ <$20'],
            [2, '$$ $20–40'],
            [3, '$$$ $40+'],
          ].map(([n, label]) => (
            <button
              key={n}
              type="button"
              className={`chip ${priceLevel === n ? 'on' : ''}`}
              onClick={() => setPriceLevel(priceLevel === n ? null : (n as number))}
            >
              {label}
            </button>
          ))}
        </div>
      </div>

      <textarea
        placeholder="Tell us about specific dishes, flavour, portions… the more specific, the more useful"
        value={text}
        maxLength={2000}
        rows={3}
        onChange={(e) => setText(e.target.value)}
        required
      />
      {error && <div className="error">{error}</div>}
      <button type="submit" className="primary" disabled={saving}>
        {saving ? 'Submitting…' : 'Submit review'}
      </button>
    </form>
  );
}
