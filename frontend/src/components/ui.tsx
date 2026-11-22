import type { ReactNode } from 'react';
import type { ReviewSource } from '../types';
import { SOURCE_LABEL } from '../types';

export const SPICE_LABELS = ['不限', '微辣+', '小辣+', '中辣+', '大辣+', '变态辣'];

export function Chili({ level }: { level: number }) {
  if (level === 0) return <span className="muted small">不辣</span>;
  return (
    <span className="chilis" title={`辣度 ${level}/5`}>
      {'🌶'.repeat(level)}
      <span className="faded">{'🌶'.repeat(5 - level)}</span>
    </span>
  );
}

export function SourceBadge({ source, count }: { source: ReviewSource; count?: number }) {
  return (
    <span className={`src s-${source}`}>
      {SOURCE_LABEL[source]}
      {count !== undefined && ` ${count}`}
    </span>
  );
}

export function Stars({ n }: { n: number }) {
  return <span className="stars">{'★'.repeat(n) + '☆'.repeat(5 - n)}</span>;
}

export const price = (level: number) => '$'.repeat(Math.max(0, level));

/** "English name · 川菜 · Takapuna · $$" — skips empty parts */
export const metaLine = (...parts: (string | undefined)[]) => parts.filter(Boolean).join(' · ');

export const fmt = (n: number | null) => (n === null ? '—' : n.toFixed(1));

const escapeRegex = (s: string) => s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');

/** Wraps the extracted key phrases in <mark>. */
export function Highlighted({ text, phrases }: { text: string; phrases: string[] }) {
  const list = phrases.filter(Boolean);
  if (list.length === 0) return <>{text}</>;
  const re = new RegExp(`(${list.map(escapeRegex).join('|')})`, 'gi');
  const lower = list.map((p) => p.toLowerCase());
  const parts: ReactNode[] = text.split(re).map((part, i) =>
    lower.includes(part.toLowerCase()) ? <mark key={i}>{part}</mark> : part,
  );
  return <>{parts}</>;
}
