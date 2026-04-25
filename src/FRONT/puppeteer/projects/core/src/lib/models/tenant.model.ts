export enum TenantStatus {
  Pending = 'Pending',
  Active = 'Active',
  Suspended = 'Suspended',
  Archived = 'Archived',
}

export enum TenantType {
  System = 'System',
  Agency = 'Agency',
}

export interface PortalBrandingContent {
  heroImageUrl?: string;
  heroHeadline?: string;
  heroSubhead?: string;
  heroCtaLabel?: string;
  heroCtaUrl?: string;
  listingIntro?: string;
  secondaryHeroImageUrl?: string;
  stat1Label?: string;
  stat1Value?: string;
  stat2Label?: string;
  stat2Value?: string;
  featureCard1Title?: string;
  featureCard1Description?: string;
  featureCard1Icon?: string;
  featureCard2Title?: string;
  featureCard2Description?: string;
  featureCard2Icon?: string;
  featureCard3Title?: string;
  featureCard3Description?: string;
  featureCard3Icon?: string;
  whatsappNumber?: string;
  heroSecondaryCta?: string;
}

export interface BrandingConfig {
  logoUrl?: string;
  primaryColor: string;
  secondaryColor: string;
  agencyDisplayName: string;
  tagline?: string;
  faviconUrl?: string;
  portalContent?: PortalBrandingContent;
}

/**
 * The three routing tiers for a tenant's public portal:
 * - 'Default' : standard starter layout (plan portal_theme default).
 * - 'Minimal' / 'Premium' : plan-driven portal shells with richer hero support.
 * - 'Custom'  : bespoke Angular sub-app for this tenant's slug (manifest → generated registry).
 *                The API returns this when `frontendOrigin` is non-empty. Same K8s `frontend` Service;
 *                absolute `frontendOrigin` also feeds OIDC allowlisting.
 *                Perk/addon changes without developer coordination will cause 402 responses
 *                in the custom frontend instead of graceful UI fallbacks.
 */
export type PortalLayoutMode = 'Default' | 'Minimal' | 'Premium' | 'Custom';

export interface Tenant {
  id: string;
  name: string;
  type: TenantType;
  slug: string;
  customDomain?: string;
  frontendOrigin?: string;
  status: TenantStatus;
  planId?: string;
  ownerUserId?: string;
  portalLayoutMode: PortalLayoutMode;
  /** Plan entitlement `portal_theme`: default | minimal | premium */
  portalTheme: string;
  contactEmail: string;
  contactPhone?: string;
  branding: BrandingConfig;
  entitlements: Record<string, unknown>;
  stripeCustomerId?: string;
  stripeSubscriptionId?: string;
  paymentStatus: string;
  lastPaymentDate?: string;
  nextBillingDate?: string;
  createdAt: string;
}

export interface Plan {
  id: string;
  name: string;
  description: string;
  pricePerMonth: number;
  currencyCode: string;
  maxProperties: number;
  videoUpload: boolean;
  aiDescriptions: boolean;
  customDomain: boolean;
  premiumAnalytics: boolean;
  portalTheme: string;
  stripePriceId?: string;
  isActive: boolean;
}

export interface CursorPage<T> {
  items: T[];
  nextCursor: string | null;
  hasNextPage: boolean;
}

export interface CreateTenantRequest {
  name: string;
  slug: string;
  contactEmail: string;
  contactPhone?: string;
  customDomain?: string;
  nextBillingDate?: string;
}

export interface UpdateTenantRequest {
  name: string;
  slug: string;
  contactEmail: string;
  contactPhone?: string;
  customDomain?: string;
  nextBillingDate?: string;
}

export interface UpdateTenantSettingsRequest {
  name: string;
  contactEmail: string;
  contactPhone?: string;
}

export interface UpdateTenantBrandingRequest {
  logoUrl?: string;
  primaryColor: string;
  secondaryColor: string;
  agencyDisplayName: string;
  tagline?: string;
  faviconUrl?: string;
  portalContent?: PortalBrandingContent;
}

export interface UpdateTenantEntitlementsRequest {
  entitlements: Record<string, unknown>;
}

export interface AssignTenantPlanRequest {
  planId: string;
}

