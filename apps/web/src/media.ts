import type { Locale } from './content';

export type EditorialSection = 'home' | 'story' | 'ceremony' | 'dining' | 'location' | 'gallery';

export interface EditorialMediaConfig {
  heroImage: string;
  storyImage?: string;
  ceremonyImage?: string;
  diningImage?: string;
  locationImage?: string;
  galleryImages?: string[];
}

const baseUrl = import.meta.env.VITE_STORAGE_BASE_URL ?? 'http://127.0.0.1:10000/devstoreaccount1';

export function buildBlobUrl(blobName: string) {
  if (!blobName || blobName.startsWith('/') || blobName.startsWith('http://') || blobName.startsWith('https://') || blobName.startsWith('data:')) {
    return blobName;
  }

  return `${baseUrl.replace(/\/$/, '')}/${blobName.replace(/^\//, '')}`;
}

export const editorialMedia: Record<Locale, EditorialMediaConfig> = {
  en: {
    heroImage: '/hero-couple.jpg',
    storyImage: 'wedding-gallery/story.jpg',
    ceremonyImage: 'wedding-gallery/ceremony.jpg',
    diningImage: 'wedding-gallery/dining.jpg',
    locationImage: 'wedding-gallery/location.jpg',
    galleryImages: [
      'wedding-gallery/gallery-1.jpg',
      'wedding-gallery/gallery-2.jpg',
      'wedding-gallery/gallery-3.jpg',
    ],
  },
  fr: {
    heroImage: '/hero-couple.jpg',
    storyImage: 'wedding-gallery/story.jpg',
    ceremonyImage: 'wedding-gallery/ceremony.jpg',
    diningImage: 'wedding-gallery/dining.jpg',
    locationImage: 'wedding-gallery/location.jpg',
    galleryImages: [
      'wedding-gallery/gallery-1.jpg',
      'wedding-gallery/gallery-2.jpg',
      'wedding-gallery/gallery-3.jpg',
    ],
  },
};

export function getEditorialImageUrl(locale: Locale, key: 'heroImage' | 'storyImage' | 'ceremonyImage' | 'diningImage' | 'locationImage') {
  const media = editorialMedia[locale];
  const value = media[key];

  if (typeof value === 'string') {
    return buildBlobUrl(value);
  }

  return undefined;
}

export function getEditorialGalleryImages(locale: Locale) {
  const media = editorialMedia[locale];
  const values = media.galleryImages ?? [];

  return values.map((blobName) => buildBlobUrl(blobName));
}
