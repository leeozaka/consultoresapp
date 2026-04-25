import { Property, PropertyStatus, PropertyType, ListingType } from '../../models/property.model';
import { Tenant, TenantStatus, TenantType, BrandingConfig } from '../../models/tenant.model';
import { CurrentUser } from '../../models/auth.model';
import { Account, AccountStatus } from '../../models/account.model';

export function createMockProperty(overrides: Partial<Property> = {}): Property {
  return {
    id: 'prop-1',
    tenantId: 'tenant-1',
    title: 'Apartamento 3 quartos no Centro',
    description: 'Ótimo apartamento com varanda',
    price: 450000,
    currency: 'BRL',
    city: 'São Paulo',
    state: 'SP',
    country: 'BR',
    zipCode: '01310-100',
    address: 'Av. Paulista, 1000',
    bedrooms: 3,
    bathrooms: 2,
    parkingSpaces: 1,
    areaSqMeters: 85,
    propertyType: PropertyType.Apartment,
    listingType: ListingType.Sale,
    status: PropertyStatus.Active,
    attributes: {},
    images: [],
    agentId: 'agent-1',
    publishedAtUtc: '2025-01-15T10:00:00Z',
    createdAt: '2025-01-10T08:00:00Z',
    updatedAt: '2025-01-15T10:00:00Z',
    ...overrides,
  };
}

const defaultBranding: BrandingConfig = {
  primaryColor: '#1A73E8',
  secondaryColor: '#F5A623',
  agencyDisplayName: 'Imobiliária Demo',
  tagline: 'Encontre o imóvel dos seus sonhos',
};

export function createMockTenant(overrides: Partial<Tenant> = {}): Tenant {
  return {
    id: 'tenant-1',
    name: 'Imobiliária Demo',
    type: TenantType.Agency,
    slug: 'demo',
    status: TenantStatus.Active,
    portalLayoutMode: 'Minimal',
    portalTheme: 'minimal',
    contactEmail: 'contato@demo.consultor.app',
    branding: defaultBranding,
    entitlements: { featured_listings: true },
    paymentStatus: 'none',
    createdAt: '2024-06-01T00:00:00Z',
    ...overrides,
  };
}

export function createMockUser(overrides: Partial<CurrentUser> = {}): CurrentUser {
  return {
    id: 'user-1',
    email: 'admin@demo.consultor.app',
    firstName: 'João',
    lastName: 'Silva',
    tenantId: 'tenant-1',
    roles: ['TenantAdmin'],
    ...overrides,
  };
}

export function createMockAccount(overrides: Partial<Account> = {}): Account {
  return {
    accountId: 'acc-1',
    clientId: 'client-1',
    balance: 10000,
    reservedBalance: 500,
    availableBalance: 9500,
    creditLimit: 5000,
    status: AccountStatus.Active,
    currency: 'BRL',
    ...overrides,
  };
}
