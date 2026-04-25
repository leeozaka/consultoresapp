import { PaginationParams } from './pagination.model';

export enum PropertyType {
  House = 'House',
  Apartment = 'Apartment',
  Commercial = 'Commercial',
  Land = 'Land',
  Garage = 'Garage',
  Studio = 'Studio',
}

export enum ListingType {
  Sale = 'Sale',
  Rent = 'Rent',
  Both = 'Both',
}

export enum PropertyStatus {
  Draft = 'Draft',
  Active = 'Active',
  UnderOffer = 'UnderOffer',
  Sold = 'Sold',
  Rented = 'Rented',
  Archived = 'Archived',
}

export interface PropertyImage {
  key: string;
  url?: string;
  thumbnailUrl?: string;
  mediumUrl?: string;
  order: number;
  isProcessed: boolean;
  originalFileName: string;
}

export interface Property {
  id: string;
  tenantId: string;
  title: string;
  description?: string;
  price: number;
  currency: string;
  city: string;
  state: string;
  country: string;
  zipCode?: string;
  address?: string;
  neighbourhood?: string;
  bedrooms: number;
  bathrooms: number;
  parkingSpaces?: number;
  areaSqMeters?: number;
  propertyType: PropertyType;
  listingType: ListingType;
  status: PropertyStatus;
  attributes?: Record<string, unknown>;
  images: PropertyImage[];
  contactPhone?: string;
  agentId?: string;
  publishedAtUtc?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreatePropertyRequest {
  title: string;
  price: number;
  city: string;
  state: string;
  propertyType: PropertyType;
  listingType: ListingType;
  bedrooms: number;
  bathrooms: number;
  description?: string;
  country?: string;
  zipCode?: string;
  address?: string;
  neighbourhood?: string;
  parkingSpaces?: number;
  areaSqMeters?: number;
  attributes?: Record<string, unknown>;
  contactPhone?: string;
}

export interface UpdatePropertyRequest {
  title: string;
  price: number;
  city: string;
  state: string;
  propertyType: PropertyType;
  listingType: ListingType;
  bedrooms: number;
  bathrooms: number;
  description?: string;
  zipCode?: string;
  address?: string;
  neighbourhood?: string;
  parkingSpaces?: number;
  areaSqMeters?: number;
  attributes?: Record<string, unknown>;
  contactPhone?: string;
}

export interface FeaturedProperty {
  property: Property;
  tenantSlug: string;
  tenantName: string;
  tenantLogoUrl?: string;
}

export interface PropertySearchRequest extends PaginationParams {
  city?: string;
  neighbourhood?: string;
  minPrice?: number;
  maxPrice?: number;
  minBedrooms?: number;
  propertyType?: PropertyType;
  listingType?: ListingType;
  status?: PropertyStatus;
}

export interface LocationSuggestion {
  label: string;
  type: 'city' | 'neighbourhood';
  city: string;
  state: string;
}
