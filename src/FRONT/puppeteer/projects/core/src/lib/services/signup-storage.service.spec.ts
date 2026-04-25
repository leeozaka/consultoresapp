import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { PLATFORM_ID } from '@angular/core';
import { SignupStorageService, SignupDraft } from './signup-storage.service';

function buildDraft(overrides: Partial<SignupDraft> = {}): SignupDraft {
  return {
    agencyName: 'Minha Imob',
    slug: 'minha-imob',
    contactEmail: 'contato@imob.com.br',
    contactPhone: '(11) 99999-9999',
    selectedPlanId: 'plan-1',
    firstName: 'João',
    lastName: 'Silva',
    email: 'joao@imob.com.br',
    tenantId: null,
    dismissToken: null,
    currentStep: 1,
    ...overrides,
  };
}

describe('SignupStorageService', () => {
  let service: SignupStorageService;
  let store: Record<string, string>;
  let mockStorage: Storage;

  beforeEach(() => {
    store = {};
    mockStorage = {
      getItem: vi.fn((key: string) => store[key] ?? null),
      setItem: vi.fn((key: string, value: string) => { store[key] = value; }),
      removeItem: vi.fn((key: string) => { delete store[key]; }),
      clear: vi.fn(() => { store = {}; }),
      key: vi.fn(() => null),
      length: 0,
    };
    Object.defineProperty(window, 'localStorage', { value: mockStorage, writable: true, configurable: true });

    TestBed.configureTestingModule({
      providers: [SignupStorageService, { provide: PLATFORM_ID, useValue: 'browser' }],
    });
    service = TestBed.inject(SignupStorageService);
  });

  afterEach(() => vi.restoreAllMocks());

  it('saveDraft() + loadDraft() round-trips correctly', () => {
    const draft = buildDraft();
    service.saveDraft(draft);
    expect(service.loadDraft()).toEqual(draft);
  });

  it('loadDraft() returns null when nothing is stored', () => {
    expect(service.loadDraft()).toBeNull();
  });

  it('loadDraft() returns null for corrupt JSON', () => {
    store['onboarding:wizard-draft'] = '{invalid json';
    expect(service.loadDraft()).toBeNull();
  });

  it('loadDraft() returns null for invalid structure (missing currentStep)', () => {
    store['onboarding:wizard-draft'] = '{"agencyName":"X"}';
    expect(service.loadDraft()).toBeNull();
  });

  it('clearDraft() removes the stored draft', () => {
    service.saveDraft(buildDraft());
    service.clearDraft();
    expect(service.loadDraft()).toBeNull();
  });

  it('hasPendingSignup() returns true when draft has tenantId', () => {
    service.saveDraft(buildDraft({ tenantId: 'tenant-123' }));
    expect(service.hasPendingSignup()).toBe(true);
  });

  it('hasPendingSignup() returns false when draft has no tenantId', () => {
    service.saveDraft(buildDraft());
    expect(service.hasPendingSignup()).toBe(false);
  });

  it('hasPendingSignup() returns false when no draft exists', () => {
    expect(service.hasPendingSignup()).toBe(false);
  });
});

describe('SignupStorageService (server)', () => {
  let service: SignupStorageService;

  beforeEach(() => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [SignupStorageService, { provide: PLATFORM_ID, useValue: 'server' }],
    });
    service = TestBed.inject(SignupStorageService);
  });

  it('loadDraft() returns null on SSR', () => {
    expect(service.loadDraft()).toBeNull();
  });

  it('hasPendingSignup() returns false on SSR', () => {
    expect(service.hasPendingSignup()).toBe(false);
  });
});
