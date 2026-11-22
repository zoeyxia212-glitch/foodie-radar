export type ReviewSource = 'Google' | 'Xiaohongshu' | 'Reddit' | 'OnSite';

export const SOURCE_LABEL: Record<ReviewSource, string> = {
  Google: 'Google',
  Xiaohongshu: 'RedNote',
  Reddit: 'Reddit',
  OnSite: 'Foodie Radar',
};

export interface CuisineInfo {
  key: string;
  name: string;
  flag: string;
  originLabel: string;
  nativeOrigins: string[];
}

export interface Meta {
  cuisines: CuisineInfo[];
  tastes: string[];
  areas: string[];
  origins: string[];
}

export interface RestaurantSummary {
  id: number;
  name: string;
  englishName: string;
  cuisine: string;
  cuisineName: string;
  flag: string;
  originLabel: string;
  area: string;
  suburb: string;
  priceLevel: number;
  spiceLevel: number;
  tastes: string[];
  mustOrder: string[];
  avoid: string[];
  summary: string;
  overallRating: number | null;
  nativeRating: number | null;
  reviewCount: number;
  nativeReviewCount: number;
  sourceCounts: Partial<Record<ReviewSource, number>>;
  isDemo: boolean;
  latitude: number | null;
  longitude: number | null;
  distanceKm: number | null;
}

export interface Review {
  id: number;
  source: ReviewSource;
  author: string;
  reviewerOrigin: string;
  isNative: boolean;
  rating: number;
  text: string;
  highlights: string[];
  recommendedDishes: string[];
  avoidDishes: string[];
  tastes: string[];
  spiceRating: number | null;
  priceLevel: number | null;
  helpfulCount: number;
  votedByMe: boolean;
  isMine: boolean;
  createdAt: string;
}

export interface Paged<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface RestaurantDetail {
  restaurant: RestaurantSummary;
  aiVerdict: string;
  address: string;
  website: string | null;
  phone: string | null;
  openingHours: string | null;
  osmUrl: string | null;
  reviews: Review[];
}

export type SortKey = 'native' | 'overall' | 'reviews' | 'distance';

export interface User {
  id: number;
  displayName: string;
  origin: string;
  email: string;
}

export interface AuthResponse {
  token: string;
  user: User;
}

export interface LatLon {
  lat: number;
  lon: number;
}

export interface Filters {
  cuisines: string[];
  tastes: string[];
  areas: string[];
  minSpice: number;
  nativeOnly: boolean;
  hasReviews: boolean;
  q: string;
  sort: SortKey;
}

export interface NewReview {
  rating: number;
  text: string;
  recommendedDishes: string[];
  avoidDishes: string[];
  tastes: string[];
  spiceRating: number | null;
  priceLevel: number | null;
}
