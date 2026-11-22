import type { RestaurantSummary, ReviewSource } from '../types';
import { Chili, SourceBadge, fmt, metaLine, price } from './ui';

interface Props {
  r: RestaurantSummary;
  onOpen: (id: number) => void;
}

export default function RestaurantCard({ r, onOpen }: Props) {
  const sources = Object.entries(r.sourceCounts) as [ReviewSource, number][];

  return (
    <article className="card" onClick={() => onOpen(r.id)}>
      <div className="card-top">
        <div>
          <h2>
            {r.name} {r.isDemo && <span className="demo">示例</span>}
          </h2>
          <div className="meta">
            {metaLine(
              r.englishName !== r.name ? r.englishName : '',
              r.cuisineName,
              r.suburb || r.area,
              price(r.priceLevel),
              r.distanceKm !== null ? `📍 ${r.distanceKm} km` : '',
            )}
          </div>
        </div>
        <div className="flag">{r.flag}</div>
      </div>

      <div className="scores">
        <div className="score native">
          <div className="n">{fmt(r.nativeRating)}</div>
          <div className="l">
            正宗度 · {r.originLabel}评价者 {r.nativeReviewCount} 条
          </div>
        </div>
        <div className="score">
          <div className="n">{fmt(r.overallRating)}</div>
          <div className="l">综合评分 · 共 {r.reviewCount} 条</div>
        </div>
      </div>

      <p className={`summary ${r.summary ? '' : 'muted'}`}>
        {r.summary || '还没有评价摘要，来写第一条评价吧'}
      </p>

      {(r.mustOrder.length > 0 || r.avoid.length > 0) && <div className="dishes">
        {r.mustOrder.map((d) => (
          <span key={d} className="dish">👍 {d}</span>
        ))}
        {r.avoid.map((d) => (
          <span key={d} className="dish bad">{d}</span>
        ))}
      </div>}

      <div className="tags">
        {r.tastes.map((t) => (
          <span key={t} className="tag">{t}</span>
        ))}
      </div>

      <div className="card-foot">
        <Chili level={r.spiceLevel} />
        <span className="srcs">
          {sources.map(([s, n]) => (
            <SourceBadge key={s} source={s} count={n} />
          ))}
        </span>
      </div>
    </article>
  );
}
