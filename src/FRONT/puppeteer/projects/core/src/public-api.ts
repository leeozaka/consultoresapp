/* @consultores/core — shared library */

// ── Auth ──────────────────────────────────────────────────────────────
export { AuthService } from './lib/auth/auth.service';
export { authGuard } from './lib/auth/auth.guard';
export { roleGuard } from './lib/auth/role.guard';
export { authInterceptor } from './lib/auth/auth.interceptor';

// ── HTTP ──────────────────────────────────────────────────────────────
export { ApiClientService } from './lib/http/api-client.service';
export {
  snakeCaseInterceptor,
  toSnakeCase,
  toCamelCase,
  convertKeysToSnakeCase,
  convertKeysToCamelCase,
} from './lib/http/snake-case.interceptor';
export { SSR_API_ORIGIN } from './lib/http/ssr-api-origin.token';
export { ssrOriginInterceptor } from './lib/http/ssr-origin.interceptor';

// ── Tenant ────────────────────────────────────────────────────────────
export { TenantContextService } from './lib/tenant/tenant-context.service';
export { BrandingApplierService } from './lib/tenant/branding-applier.service';
export { tenantAccessInterceptor } from './lib/tenant/tenant-access.interceptor';

// ── Entitlements ──────────────────────────────────────────────────────
export { EntitlementService } from './lib/entitlements/entitlement.service';

// ── Errors ────────────────────────────────────────────────────────────
export { AppErrorHandlerService } from './lib/errors/error-handler.service';
export type { AppNotification, NotificationSeverity } from './lib/errors/error-handler.service';
export { ChunkErrorHandler } from './lib/errors/chunk-error-handler';

// ── Theme ─────────────────────────────────────────────────────────────
export { ThemeService } from './lib/theme/theme.service';

// ── Services ──────────────────────────────────────────────────────────
export { AccountService } from './lib/services/account.service';
export { AddonService } from './lib/services/addon.service';
export { BillingService } from './lib/services/billing.service';
export { ImageBlobCacheService } from './lib/services/image-blob-cache.service';
export { ImageCompressionService } from './lib/services/image-compression.service';
export type { CompressionOptions } from './lib/services/image-compression.service';
export { ImageProcessedSseService } from './lib/services/image-processed-sse.service';
export type { ImageProcessedEvent } from './lib/services/image-processed-sse.service';
export { OnboardingService } from './lib/services/onboarding.service';
export type {
  SignupRequest,
  SignupResponse,
  SignupResumeResponse,
  SlugAvailabilityResponse,
  OnboardingStatusResponse,
} from './lib/services/onboarding.service';
export { PaymentSseService } from './lib/services/payment-sse.service';
export type { PaymentEvent } from './lib/services/payment-sse.service';
export { PlanService } from './lib/services/plan.service';
export type { CreatePlanRequest } from './lib/services/plan.service';
export { PropertyService } from './lib/services/property.service';
export { SignupStorageService } from './lib/services/signup-storage.service';
export type { SignupDraft } from './lib/services/signup-storage.service';
export { TenantService } from './lib/services/tenant.service';
export type { SiteBuildStatusEvent } from './lib/services/tenant.service';
export { ThreeJsService } from './lib/services/three-js.service';
export { TransactionService } from './lib/services/transaction.service';
export { UserAdminService } from './lib/services/user-admin.service';
export type {
  UserRecord,
  CreateUserRequest,
  UpdateUserRequest,
} from './lib/services/user-admin.service';

// ── Models ────────────────────────────────────────────────────────────
export type {
  Account,
  CreateAccountRequest,
} from './lib/models/account.model';
export { AccountStatus } from './lib/models/account.model';

export type {
  CurrentUser,
  LoginRequest,
} from './lib/models/auth.model';
export type { UserRole } from './lib/models/auth.model';

export type {
  BillingOverview,
  BillingTransaction,
  BillingPortalSessionResponse,
  ChangePlanResponse,
} from './lib/models/billing.model';

export type {
  PaginatedResponse,
  PaginationParams,
} from './lib/models/pagination.model';
export { LIST_FETCH_PAGE_SIZE } from './lib/models/pagination.model';

export type {
  Property,
  PropertyImage,
  CreatePropertyRequest,
  UpdatePropertyRequest,
  PropertySearchRequest,
  FeaturedProperty,
  LocationSuggestion,
} from './lib/models/property.model';
export { PropertyStatus, PropertyType, ListingType } from './lib/models/property.model';

export type {
  Tenant,
  BrandingConfig,
  Plan,
  CursorPage,
  CreateTenantRequest,
  UpdateTenantRequest,
  UpdateTenantBrandingRequest,
  UpdateTenantSettingsRequest,
  UpdateTenantEntitlementsRequest,
  AssignTenantPlanRequest,
  PortalBrandingContent,
} from './lib/models/tenant.model';
export { TenantStatus, TenantType } from './lib/models/tenant.model';
export type { PortalLayoutMode } from './lib/models/tenant.model';

export type {
  TransactionRequest,
  TransactionResponse,
} from './lib/models/transaction.model';
export { TransactionStatus, TransactionType } from './lib/models/transaction.model';

// ── Components ────────────────────────────────────────────────────────
export { LoadingSpinnerComponent } from './lib/components/loading-spinner/loading-spinner';
export type { SpinnerSize } from './lib/components/loading-spinner/loading-spinner';
export { PaginationComponent } from './lib/components/pagination/pagination';
export { PriceDisplayComponent } from './lib/components/price-display/price-display';
export { ToastComponent } from './lib/components/toast/toast';
export { ImageGalleryComponent } from './lib/components/image-gallery/image-gallery';
export { PhotoStagingAreaComponent } from './lib/components/photo-staging-area/photo-staging-area';
export type { StagedFile } from './lib/components/photo-staging-area/photo-staging-area';
export { WebGLBackgroundComponent } from './lib/components/webgl-background/webgl-background.component';

// ── Pipes ─────────────────────────────────────────────────────────────
export { PropertyStatusPipe } from './lib/pipes/property-status.pipe';
export { RelativeTimePipe } from './lib/pipes/relative-time.pipe';
export { TenantStatusPipe } from './lib/pipes/tenant-status.pipe';

// ── Directives ────────────────────────────────────────────────────────
export { CachedSrcDirective } from './lib/directives/cached-src.directive';
// FeatureGateDirective is currently commented out in source

// ── Environments ──────────────────────────────────────────────────────
export type { AppEnvironment, AppOidcConfig } from './lib/environments/app-environment';
export { environment } from './lib/environments/environment';

// ── SSR ───────────────────────────────────────────────────────────────
export { getSsrAllowedHosts } from './lib/ssr/ssr-hosts';

// ── Testing ───────────────────────────────────────────────────────────
export { renderComponent } from './lib/testing/test-utils';
export {
  createMockAuthService,
  createMockTenantContextService,
  createMockPropertyService,
} from './lib/testing/mock-services';
export {
  createMockProperty,
  createMockTenant,
  createMockUser,
  createMockAccount,
} from './lib/testing/fixtures';
