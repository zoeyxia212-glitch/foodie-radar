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
            {r.name} {r.isDemo && <span className="demo">Demo</span>}
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
            Authenticity · {r.nativeReviewCount} from {r.originLabel}
          </div>
        </div>
        <div className="score">
          <div className="n">{fmt(r.overallRating)}</div>
          <div className="l">Overall · {r.reviewCount} review{r.reviewCount === 1 ? '' : 's'}</div>
        </div>
      </div>

      <p className={`summary ${r.summary ? '' : 'muted'}`}>
        {r.summary || 'No summary yet. Be the first to write a review!'}
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
