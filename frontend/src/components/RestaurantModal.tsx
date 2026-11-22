import { useCallback, useEffect, useState } from 'react';
import { api } from '../api';
import { useAuth } from '../auth';
import type { Meta, RestaurantDetail, Review, ReviewSource } from '../types';
import { SOURCE_LABEL } from '../types';
import ReviewForm from './ReviewForm';
import { Chili, Highlighted, SourceBadge, Stars, fmt, metaLine, price } from './ui';

interface Props {
  id: number;
  meta: Meta;
  defaultNative: boolean;
  onClose: () => void;
  onChanged: () => void;
}

type Tab = 'all' | 'native' | ReviewSource;
type ReviewSort = 'helpful' | 'newest' | 'high' | 'low';
const SOURCES: ReviewSource[] = ['Google', 'Xiaohongshu', 'Reddit', 'OnSite'];

const SORTERS: Record<ReviewSort, (a: Review, b: Review) => number> = {
  helpful: (a, b) => b.helpfulCount - a.helpfulCount || b.createdAt.localeCompare(a.createdAt),
  newest: (a, b) => b.createdAt.localeCompare(a.createdAt),
  high: (a, b) => b.rating - a.rating,
  low: (a, b) => a.rating - b.rating,
};

function ReviewItem({ v, onHelpful }: { v: Review; onHelpful: (id: number) => void }) {
  const voted = v.votedByMe;
  return (
    <div className="review">
      <div className="review-meta">
        <SourceBadge source={v.source} />
        <b>{v.author}</b>
        {v.isMine && <span className="demo">我</span>}
        <span className={`origin ${v.isNative ? 'native' : ''}`}>来自 {v.reviewerOrigin}</span>
        <Stars n={v.rating} />
        <span className="muted">{new Date(v.createdAt).toLocaleDateString('zh-CN')}</span>
      </div>
      <p>
        <Highlighted text={v.text} phrases={v.highlights} />
      </p>
      {(v.recommendedDishes.length > 0 || v.avoidDishes.length > 0 || v.tastes.length > 0 || v.spiceRating !== null) && (
        <div className="review-tags">
          {v.recommendedDishes.map((d) => (
            <span key={`r-${d}`} className="dish">👍 {d}</span>
          ))}
          {v.avoidDishes.map((d) => (
            <span key={`a-${d}`} className="dish bad">{d}</span>
          ))}
          {v.tastes.map((t) => (
            <span key={`t-${t}`} className="tag">{t}</span>
          ))}
          {v.spiceRating !== null && <Chili level={v.spiceRating} />}
        </div>
      )}
      {v.isMine ? (
        v.helpfulCount > 0 && <span className="helpful on">👍 {v.helpfulCount} 人觉得有用</span>
      ) : (
        <button type="button" className={`helpful ${voted ? 'on' : ''}`} disabled={voted} onClick={() => onHelpful(v.id)}>
          👍 有用{v.helpfulCount > 0 && ` · ${v.helpfulCount}`}
        </button>
      )}
    </div>
  );
}

export default function RestaurantModal({ id, meta, defaultNative, onClose, onChanged }: Props) {
  const [detail, setDetail] = useState<RestaurantDetail | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [tab, setTab] = useState<Tab>(defaultNative ? 'native' : 'all');
  const [sort, setSort] = useState<ReviewSort>('helpful');

  const { user, requestLogin } = useAuth();

  // Reload when the user signs in/out so "voted by me" / "is mine" flags are right.
  const load = useCallback(() => {
    api.restaurant(id).then(setDetail).catch((e: Error) => setError(e.message));
  }, [id, user?.id]); // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(load, [load]);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && onClose();
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [onClose]);

  const markHelpful = async (reviewId: number) => {
    if (!user) {
      requestLogin();
      return;
    }
    try {
      const { helpfulCount } = await api.helpful(reviewId);
      setDetail((d) =>
        d && {
          ...d,
          reviews: d.reviews.map((v) => (v.id === reviewId ? { ...v, helpfulCount, votedByMe: true } : v)),
        },
      );
    } catch (e) {
      setError(e instanceof Error ? e.message : '操作失败');
    }
  };

  const r = detail?.restaurant;
  const reviews = (detail?.reviews ?? [])
    .filter((v) => (tab === 'all' ? true : tab === 'native' ? v.isNative : v.source === tab))
    .sort(SORTERS[sort]);

  const knownDishes = r
    ? [...new Set([...r.mustOrder, ...r.avoid, ...(detail?.reviews ?? []).flatMap((v) => [...v.recommendedDishes, ...v.avoidDishes])])]
    : [];

  return (
    <div className="mask" onClick={(e) => e.target === e.currentTarget && onClose()}>
      <div className="modal" role="dialog" aria-modal="true">
        {error && <div className="error">{error}</div>}
        {!r ? (
          !error && <div className="muted">加载中…</div>
        ) : (
          <>
            <div className="modal-head">
              <div>
                <h2>
                  {r.flag} {r.name} {r.isDemo && <span className="demo">示例</span>}
                </h2>
                <div className="meta">
                  {metaLine(r.englishName !== r.name ? r.englishName : '', r.cuisineName, r.suburb || r.area, price(r.priceLevel))}
                </div>
              </div>
              <button className="close" onClick={onClose} aria-label="关闭">×</button>
            </div>

            <div className="info">
              {detail?.address && <div>📍 {detail.address}</div>}
              {detail?.openingHours && <div>🕐 {detail.openingHours}</div>}
              {detail?.phone && <div>📞 {detail.phone}</div>}
              <div className="links">
                {detail?.website && /^https?:\/\//i.test(detail.website) && (
                  <a href={detail.website} target="_blank" rel="noreferrer">官网</a>
                )}
                {detail?.osmUrl && <a href={detail.osmUrl} target="_blank" rel="noreferrer">在 OpenStreetMap 查看</a>}
              </div>
            </div>

            <div className="ai">
              💡 <b>一句话结论：</b>{detail?.aiVerdict || '评价还不够多，暂时无法生成结论。'}
              {r.summary && <div className="ai-summary">{r.summary}</div>}
            </div>

            {(r.mustOrder.length > 0 || r.avoid.length > 0) && (
              <div className="dishes" style={{ marginTop: 12 }}>
                {r.mustOrder.map((d) => (
                  <span key={d} className="dish">👍 {d}</span>
                ))}
                {r.avoid.map((d) => (
                  <span key={d} className="dish bad">{d}</span>
                ))}
              </div>
            )}

            <div className="bars">
              {SOURCES.map((s) => {
                const n = r.sourceCounts[s] ?? 0;
                return (
                  <div className="bar-row" key={s}>
                    <span>{SOURCE_LABEL[s]}</span>
                    <div className="bar">
                      <i className={`s-${s}`} style={{ width: `${r.reviewCount ? (n / r.reviewCount) * 100 : 0}%` }} />
                    </div>
                    <span>{n}</span>
                  </div>
                );
              })}
            </div>

            <div className="scores">
              <div className="score native">
                <div className="n">{fmt(r.nativeRating)}</div>
                <div className="l">{r.originLabel}评价者打分</div>
              </div>
              <div className="score">
                <div className="n">{fmt(r.overallRating)}</div>
                <div className="l">全部评价者打分</div>
              </div>
            </div>

            <div className="review-toolbar">
              <div className="chips">
                {(['all', 'native', ...SOURCES] as Tab[]).map((t) => (
                  <button key={t} className={`chip ${tab === t ? 'on' : ''}`} onClick={() => setTab(t)}>
                    {t === 'all' ? '全部' : t === 'native' ? `只看${r.originLabel}` : SOURCE_LABEL[t]}
                  </button>
                ))}
              </div>
              <select value={sort} onChange={(e) => setSort(e.target.value as ReviewSort)}>
                <option value="helpful">最有用</option>
                <option value="newest">最新</option>
                <option value="high">评分最高</option>
                <option value="low">评分最低</option>
              </select>
            </div>

            {reviews.length === 0 ? (
              <div className="empty small">这个分类下暂时没有评价</div>
            ) : (
              reviews.map((v) => <ReviewItem key={v.id} v={v} onHelpful={markHelpful} />)
            )}

            <ReviewForm
              restaurantId={r.id}
              meta={meta}
              knownDishes={knownDishes}
              alreadyReviewed={(detail?.reviews ?? []).some((v) => v.isMine)}
              onCreated={() => {
                load();
                onChanged();
              }}
            />
          </>
        )}
      </div>
    </div>
  );
}
