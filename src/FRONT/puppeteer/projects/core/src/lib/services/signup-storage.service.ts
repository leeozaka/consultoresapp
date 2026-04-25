import { Injectable, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

export interface SignupDraft {
  agencyName: string;
  slug: string;
  contactEmail: string;
  contactPhone: string;
  selectedPlanId: string | null;
  firstName: string;
  lastName: string;
  email: string;
  tenantId: string | null;
  dismissToken: string | null;
  currentStep: number;
}

const STORAGE_KEY = 'onboarding:wizard-draft';

@Injectable({ providedIn: 'root' })
export class SignupStorageService {
  private readonly platformId = inject(PLATFORM_ID);

  saveDraft(draft: SignupDraft): void {
    if (!isPlatformBrowser(this.platformId)) return;
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(draft));
    } catch {
      // quota exceeded or private browsing — silently ignore
    }
  }

  loadDraft(): SignupDraft | null {
    if (!isPlatformBrowser(this.platformId)) return null;
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return null;
      const parsed = JSON.parse(raw);
      if (!parsed || typeof parsed !== 'object' || typeof parsed.currentStep !== 'number') {
        return null;
      }
      return parsed as SignupDraft;
    } catch {
      return null;
    }
  }

  clearDraft(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    localStorage.removeItem(STORAGE_KEY);
  }

  hasPendingSignup(): boolean {
    const draft = this.loadDraft();
    return draft !== null && draft.tenantId !== null;
  }
}
