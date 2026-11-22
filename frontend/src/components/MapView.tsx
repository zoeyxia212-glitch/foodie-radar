import 'leaflet/dist/leaflet.css';
import { CircleMarker, MapContainer, Popup, TileLayer, Tooltip } from 'react-leaflet';
import type { LatLon, RestaurantSummary } from '../types';
import { fmt } from './ui';

const AUCKLAND: [number, number] = [-36.85, 174.76];

interface Props {
  items: RestaurantSummary[];
  here: LatLon | null;
  onOpen: (id: number) => void;
}

/** Colour by authenticity score: green = loved by native reviewers, grey = no reviews yet. */
function colourFor(r: RestaurantSummary): string {
  const score = r.nativeRating ?? r.overallRating;
  if (score === null) return '#a39a91';
  if (score >= 4.3) return '#2f7d5b';
  if (score >= 3.5) return '#c98a1b';
  return '#d6452c';
}

export default function MapView({ items, here, onOpen }: Props) {
  const located = items.filter((r) => r.latitude !== null && r.longitude !== null);
  const center: [number, number] = here ? [here.lat, here.lon] : AUCKLAND;

  return (
    <div className="map-wrap">
      <MapContainer center={center} zoom={here ? 14 : 11} scrollWheelZoom className="map">
        {/* OSM's public tiles are fine for development; use a tile provider for production traffic. */}
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
          url="https://tile.openstreetmap.org/{z}/{x}/{y}.png"
        />

        {here && (
          <CircleMarker center={[here.lat, here.lon]} radius={7} pathOptions={{ color: '#4a7bd8', fillOpacity: 0.9 }}>
            <Tooltip>你在这里</Tooltip>
          </CircleMarker>
        )}

        {located.map((r) => (
          <CircleMarker
            key={r.id}
            center={[r.latitude!, r.longitude!]}
            radius={r.reviewCount > 0 ? 9 : 6}
            pathOptions={{ color: '#fff', weight: 2, fillColor: colourFor(r), fillOpacity: 0.95 }}
          >
            <Popup>
              <div className="map-popup">
                <b>{r.flag} {r.name}</b>
                <div className="muted small">
                  {r.cuisineName} · {r.suburb || r.area}
                  {r.distanceKm !== null && ` · ${r.distanceKm} km`}
                </div>
                <div className="small">
                  正宗度 {fmt(r.nativeRating)} · 综合 {fmt(r.overallRating)} · {r.reviewCount} 条评价
                </div>
                <button type="button" className="link" onClick={() => onOpen(r.id)}>查看详情 →</button>
              </div>
            </Popup>
          </CircleMarker>
        ))}
      </MapContainer>

      <div className="map-legend">
        <span><i style={{ background: '#2f7d5b' }} />4.3+</span>
        <span><i style={{ background: '#c98a1b' }} />3.5–4.2</span>
        <span><i style={{ background: '#d6452c' }} />&lt;3.5</span>
        <span><i style={{ background: '#a39a91' }} />暂无评价</span>
        {located.length < items.length && <span className="muted">{items.length - located.length} 家没有坐标</span>}
      </div>
    </div>
  );
}
