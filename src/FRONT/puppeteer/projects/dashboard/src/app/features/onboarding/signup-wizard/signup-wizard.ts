import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
  OnDestroy,
  PLATFORM_ID,
} from '@angular/core';
import { isPlatformBrowser, CurrencyPipe } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import {
  ReactiveFormsModule,
  FormBuilder,
  Validators,
  AbstractControl,
  ValidationErrors,
} from '@angular/forms';
import { debounceTime, distinctUntilChanged, switchMap, Subject, takeUntil } from 'rxjs';
import { OnboardingService, SignupStorageService, SignupDraft, Plan } from '@consultores/core';

function passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
  const pw = control.get('password')?.value;
  const confirm = control.get('confirmPassword')?.value;
  return pw && confirm && pw !== confirm ? { passwordMismatch: true } : null;
}

@Component({
  selector: 'app-signup-wizard',
  imports: [ReactiveFormsModule, CurrencyPipe],
  templateUrl: './signup-wizard.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SignupWizardComponent implements OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly onboarding = inject(OnboardingService);
  private readonly signupStorage = inject(SignupStorageService);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly destroy$ = new Subject<void>();

  protected readonly step = signal(0);
  protected readonly plans = signal<Plan[]>([]);
  protected readonly isLoadingPlans = signal(false);
  protected readonly isSubmitting = signal(false);
  protected readonly isDismissing = signal(false);
  protected readonly isResuming = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly slugStatus = signal<'idle' | 'checking' | 'available' | 'taken'>('idle');
  protected readonly pendingSignup = signal(false);
  protected readonly passwordRequired = signal(false);

  protected readonly agencyForm = this.fb.group({
    agencyName: ['', [Validators.required, Validators.minLength(2)]],
    slug: ['', [Validators.required, Validators.pattern(/^[a-z0-9]+(-[a-z0-9]+)*$/)]],
    contactEmail: ['', [Validators.required, Validators.email]],
    contactPhone: [''],
  });

  protected readonly accountForm = this.fb.group(
    {
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8), Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$/)]],
      confirmPassword: ['', Validators.required],
    },
    { validators: passwordMatchValidator },
  );

  protected readonly selectedPlanId = signal<string | null>(null);

  private readonly slugInput$ = new Subject<string>();
  private resumedTenantId: string | null = null;
  private dismissToken: string | null = null;

  constructor() {
    const planId = this.route.snapshot.queryParamMap.get('plan');
    if (planId) this.selectedPlanId.set(planId);

    this.loadPlans();
    this.setupSlugDebounce();
    this.tryResume();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private tryResume(): void {
    if (!isPlatformBrowser(this.platformId)) return;

    // Check for ?resume= query param (from Stripe cancel URL)
    const resumeParam = this.route.snapshot.queryParamMap.get('resume');
    if (resumeParam) {
      this.resumeFromBackend(resumeParam);
      return;
    }

    // Check localStorage draft
    const draft = this.signupStorage.loadDraft();
    if (!draft) return;

    if (draft.tenantId) {
      // Has a backend tenant — check its status
      this.isResuming.set(true);
      this.onboarding.getStatus(draft.tenantId).subscribe({
        next: (status) => {
          this.isResuming.set(false);
          if (['active', 'provisioning', 'awaiting_approval', 'payment_confirmed'].includes(status.onboardingState)) {
            this.signupStorage.clearDraft();
            this.router.navigate(['/onboarding/status', draft.tenantId]);
          } else if (status.onboardingState === 'pending_payment') {
            this.resumeFromBackend(draft.tenantId!);
          }
        },
        error: () => {
          // Tenant not found — clear draft, start fresh
          this.isResuming.set(false);
          this.signupStorage.clearDraft();
        },
      });
    } else {
      // Pre-submit draft — restore form values
      this.restoreFromDraft(draft);
    }
  }

  private resumeFromBackend(tenantId: string): void {
    this.isResuming.set(true);
    this.onboarding.getResume(tenantId).subscribe({
      next: (data) => {
        this.isResuming.set(false);
        this.resumedTenantId = tenantId;

        // Preserve dismiss token from localStorage if we have it
        const draft = this.signupStorage.loadDraft();
        this.dismissToken = draft?.dismissToken ?? null;
        this.pendingSignup.set(this.dismissToken !== null);

        // Fill forms from backend data
        this.agencyForm.patchValue({
          agencyName: data.agencyName,
          slug: data.slug,
          contactEmail: data.contactEmail,
          contactPhone: data.contactPhone ?? '',
        });
        this.selectedPlanId.set(data.planId);
        this.accountForm.patchValue({
          firstName: data.ownerFirstName,
          lastName: data.ownerLastName,
          email: data.ownerEmail,
        });
        this.slugStatus.set('available');

        // Password needs re-entry on resume
        this.passwordRequired.set(true);
        this.step.set(3);
      },
      error: () => {
        this.isResuming.set(false);
        this.signupStorage.clearDraft();
      },
    });
  }

  private restoreFromDraft(draft: SignupDraft): void {
    this.agencyForm.patchValue({
      agencyName: draft.agencyName,
      slug: draft.slug,
      contactEmail: draft.contactEmail,
      contactPhone: draft.contactPhone,
    });
    this.selectedPlanId.set(draft.selectedPlanId);
    this.accountForm.patchValue({
      firstName: draft.firstName,
      lastName: draft.lastName,
      email: draft.email,
    });
    if (draft.slug) this.slugStatus.set('available');
    this.step.set(draft.currentStep);
  }

  private buildDraft(): SignupDraft {
    const agency = this.agencyForm.getRawValue();
    const account = this.accountForm.getRawValue();
    return {
      agencyName: agency.agencyName ?? '',
      slug: agency.slug ?? '',
      contactEmail: agency.contactEmail ?? '',
      contactPhone: agency.contactPhone ?? '',
      selectedPlanId: this.selectedPlanId(),
      firstName: account.firstName ?? '',
      lastName: account.lastName ?? '',
      email: account.email ?? '',
      tenantId: this.resumedTenantId,
      dismissToken: this.dismissToken,
      currentStep: this.step(),
    };
  }

  private persistDraft(): void {
    this.signupStorage.saveDraft(this.buildDraft());
  }

  private loadPlans(): void {
    this.isLoadingPlans.set(true);
    this.onboarding.getPlans().subscribe({
      next: (plans) => {
        this.plans.set(plans);
        this.isLoadingPlans.set(false);
        if (!this.selectedPlanId() && plans.length > 0) {
          this.selectedPlanId.set(plans[0].id);
        }
      },
      error: () => this.isLoadingPlans.set(false),
    });
  }

  private setupSlugDebounce(): void {
    this.slugInput$
      .pipe(
        debounceTime(400),
        distinctUntilChanged(),
        switchMap((slug) => {
          this.slugStatus.set('checking');
          return this.onboarding.checkSlug(slug);
        }),
        takeUntil(this.destroy$),
      )
      .subscribe({
        next: (res) => this.slugStatus.set(res.available ? 'available' : 'taken'),
        error: () => this.slugStatus.set('idle'),
      });
  }

  protected onSlugInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value.toLowerCase().replace(/\s+/g, '-');
    this.agencyForm.controls.slug.setValue(value, { emitEvent: false });
    if (value.length >= 3 && this.agencyForm.controls.slug.valid) {
      this.slugInput$.next(value);
    } else {
      this.slugStatus.set('idle');
    }
  }

  protected selectPlan(planId: string): void {
    this.selectedPlanId.set(planId);
  }

  protected planFeatures(plan: Plan): { icon: string; label: string }[] {
    const features: { icon: string; label: string }[] = [
      { icon: 'pi pi-building', label: `Até ${plan.maxProperties} imóveis` },
    ];
    if (plan.videoUpload)      features.push({ icon: 'pi pi-video',     label: 'Upload de vídeos' });
    if (plan.aiDescriptions)   features.push({ icon: 'pi pi-bolt',      label: 'Descrições por IA' });
    if (plan.customDomain)     features.push({ icon: 'pi pi-globe',     label: 'Domínio personalizado' });
    if (plan.premiumAnalytics) features.push({ icon: 'pi pi-chart-bar', label: 'Analytics premium' });
    return features;
  }

  protected goToStep(n: number): void {
    this.step.set(n);
    this.persistDraft();
    if (isPlatformBrowser(this.platformId)) {
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }

  protected nextFromAgency(): void {
    if (this.agencyForm.invalid || this.slugStatus() === 'taken') return;
    this.goToStep(1);
  }

  protected nextFromPlan(): void {
    if (!this.selectedPlanId()) return;
    this.goToStep(2);
  }

  protected nextFromAccount(): void {
    if (this.accountForm.invalid) return;
    this.goToStep(3);
  }

  protected async submit(): Promise<void> {
    if (this.isSubmitting()) return;

    // On resume, password must be re-entered
    if (this.passwordRequired() && !this.accountForm.controls.password.value) {
      this.errorMessage.set('Por favor, informe sua senha novamente.');
      this.step.set(2);
      return;
    }

    const planId = this.selectedPlanId();
    if (!planId || this.agencyForm.invalid || this.accountForm.invalid) return;

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    const agency = this.agencyForm.getRawValue();
    const account = this.accountForm.getRawValue();
    const origin = isPlatformBrowser(this.platformId) ? window.location.origin : '';

    this.onboarding
      .signup({
        agencyName: agency.agencyName!,
        slug: agency.slug!,
        contactEmail: agency.contactEmail!,
        contactPhone: agency.contactPhone || null,
        planId,
        firstName: account.firstName!,
        lastName: account.lastName!,
        email: account.email!,
        password: account.password!,
        successUrl: `${origin}/onboarding/status/TENANT_ID`,
        cancelUrl: `${origin}/onboarding/signup?resume=TENANT_ID`,
      })
      .subscribe({
        next: (res) => {
          // Store tenantId + dismissToken before redirect
          this.resumedTenantId = res.tenantId;
          this.dismissToken = res.dismissToken;
          this.signupStorage.saveDraft({
            ...this.buildDraft(),
            tenantId: res.tenantId,
            dismissToken: res.dismissToken,
          });

          // Redirect to Stripe checkout
          if (isPlatformBrowser(this.platformId)) {
            globalThis.location.href = res.checkoutUrl;
          }
        },
        error: (err) => {
          this.isSubmitting.set(false);
          const msg = err?.error?.errors?.[0] ?? err?.error?.title ?? 'Ocorreu um erro. Tente novamente.';
          this.errorMessage.set(msg);
        },
      });
  }

  protected dismissSignup(): void {
    if (this.isDismissing() || !this.resumedTenantId || !this.dismissToken) return;

    this.isDismissing.set(true);
    this.errorMessage.set(null);

    this.onboarding.dismiss(this.resumedTenantId, this.dismissToken).subscribe({
      next: () => {
        this.isDismissing.set(false);
        this.signupStorage.clearDraft();
        this.resumedTenantId = null;
        this.dismissToken = null;
        this.pendingSignup.set(false);
        this.passwordRequired.set(false);

        // Reset forms
        this.agencyForm.reset();
        this.accountForm.reset();
        this.selectedPlanId.set(null);
        this.slugStatus.set('idle');
        this.step.set(0);
        this.loadPlans();
      },
      error: (err) => {
        this.isDismissing.set(false);
        const msg = err?.error?.errors?.[0] ?? err?.error?.title ?? 'Não foi possível cancelar o cadastro.';
        this.errorMessage.set(msg);
      },
    });
  }

  protected get selectedPlan(): Plan | undefined {
    return this.plans().find((p) => p.id === this.selectedPlanId());
  }
}
