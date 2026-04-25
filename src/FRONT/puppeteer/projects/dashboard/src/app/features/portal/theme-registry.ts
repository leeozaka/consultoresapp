import type { Type } from '@angular/core';
import type { Routes } from '@angular/router';

export interface PortalThemeFields {
  heroSection: boolean;
  stats: boolean;
  featureCards: boolean;
  secondaryHeroImage: boolean;
  whatsapp: boolean;
  listingIntro: boolean;
  heroSecondaryCta: boolean;
}

export interface PortalThemeDefinition {
  id: 'default' | 'minimal' | 'premium';
  name: string;
  description: string;
  previewAsset: string;
  layoutLoader: () => Promise<Type<unknown>>;
  routeLoader: () => Promise<Routes>;
  fields: PortalThemeFields;
}

/**
 * Portal theme registry — metadata only.
 * The layoutLoader / routeLoader stubs are not used in the dashboard app;
 * they exist only so the PortalThemeDefinition shape is satisfied.
 * The actual loaders live in each tenant app's own theme-registry copy.
 */
const notAvailable = () =>
  Promise.reject(new Error('Portal layouts are not available in the dashboard app'));

export const PORTAL_THEMES: PortalThemeDefinition[] = [
  {
    id: 'default',
    name: 'Starter',
    description: 'Layout básico com listagem de imóveis. Ideal para começar.',
    previewAsset: 'assets/theme-previews/default.svg',
    layoutLoader: notAvailable as () => Promise<Type<unknown>>,
    routeLoader: notAvailable as () => Promise<Routes>,
    fields: {
      heroSection: false,
      stats: false,
      featureCards: false,
      secondaryHeroImage: false,
      whatsapp: false,
      listingIntro: false,
      heroSecondaryCta: false,
    },
  },
  {
    id: 'minimal',
    name: 'Minimalista',
    description: 'Design clean com hero split e barra de estatísticas.',
    previewAsset: 'assets/theme-previews/minimal.svg',
    layoutLoader: notAvailable as () => Promise<Type<unknown>>,
    routeLoader: notAvailable as () => Promise<Routes>,
    fields: {
      heroSection: true,
      stats: true,
      featureCards: false,
      secondaryHeroImage: false,
      whatsapp: true,
      listingIntro: true,
      heroSecondaryCta: false,
    },
  },
  {
    id: 'premium',
    name: 'Premium',
    description: 'Hero full-bleed, cards de destaque e busca integrada.',
    previewAsset: 'assets/theme-previews/premium.svg',
    layoutLoader: notAvailable as () => Promise<Type<unknown>>,
    routeLoader: notAvailable as () => Promise<Routes>,
    fields: {
      heroSection: true,
      stats: true,
      featureCards: true,
      secondaryHeroImage: true,
      whatsapp: true,
      listingIntro: true,
      heroSecondaryCta: true,
    },
  },
];

export function getThemeById(id: string): PortalThemeDefinition {
  return PORTAL_THEMES.find((t) => t.id === id) ?? PORTAL_THEMES[0];
}
