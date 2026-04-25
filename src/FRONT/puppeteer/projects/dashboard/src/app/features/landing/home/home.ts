import { ChangeDetectionStrategy, Component, inject, signal, OnInit, OnDestroy, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser, CurrencyPipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { Plan, PropertyService, PlanService, AuthService, LoadingSpinnerComponent } from '@consultores/core';

@Component({
  selector: 'app-home',
  imports: [RouterLink, LoadingSpinnerComponent, CurrencyPipe],
  templateUrl: './home.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomeComponent implements OnInit, OnDestroy {
  private readonly propertyService = inject(PropertyService);
  private readonly planService = inject(PlanService);
  private readonly router = inject(Router);
  private readonly platformId = inject(PLATFORM_ID);
  protected readonly auth = inject(AuthService);

  // protected readonly featuredProperties = signal<FeaturedProperty[]>([]);
  // protected readonly isLoadingProperties = signal(false);
  protected readonly plans = signal<Plan[]>([]);
  protected readonly isLoadingPlans = signal(false);
  protected readonly currentYear = new Date().getFullYear();

  private pricingObserver?: IntersectionObserver;

  protected readonly features = [
    {
      icon: 'pi pi-building',
      title: 'Gestão de Imóveis',
      description: 'Cadastre, organize e publique seus imóveis com fotos, descrições e preços em poucos cliques.',
    },
    {
      icon: 'pi pi-globe',
      title: 'Portal Personalizado',
      description: 'Cada imobiliária recebe seu próprio portal com marca, cores e domínio personalizado.',
    },
    {
      icon: 'pi pi-credit-card',
      title: 'Pagamentos & Assinatura',
      description: 'Gerencie planos, perks e pagamentos integrados com Stripe em tempo real.',
    },
    {
      icon: 'pi pi-shield',
      title: 'Multi-Tenant Seguro',
      description: 'Dados isolados por tenant com autenticação OIDC, roles e permissões granulares.',
    },
    {
      icon: 'pi pi-puzzle-piece',
      title: 'Addons & Perks',
      description: 'Expanda funcionalidades com módulos opcionais: listagem na homepage, upload de vídeos e mais.',
    },
    {
      icon: 'pi pi-bolt',
      title: 'API Robusta',
      description: 'Backend .NET 9 com rate limiting, circuit breaker, caching distribuído e observabilidade.',
    },
  ];

  ngOnInit(): void {
    if (isPlatformBrowser(this.platformId)) {
      //this.loadFeaturedProperties();
      this.setupPricingObserver();
    }
  }

  ngOnDestroy(): void {
    this.pricingObserver?.disconnect();
  }

  private setupPricingObserver(): void {
    const section = document.getElementById('pricing');
    if (!section) return;

    this.pricingObserver = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting) {
          this.loadPlans();
          this.pricingObserver?.disconnect();
        }
      },
      { threshold: 0.05 },
    );
    this.pricingObserver.observe(section);
  }

  private loadPlans(): void {
    this.isLoadingPlans.set(true);
    this.planService.getPlans().subscribe({
      next: (plans) => {
        this.plans.set(plans);
        this.isLoadingPlans.set(false);
      },
      error: () => this.isLoadingPlans.set(false),
    });
  }

  // private loadFeaturedProperties(): void {
  //   this.isLoadingProperties.set(true);
  //   this.propertyService.getFeatured().subscribe({
  //     next: (response) => {
  //       this.featuredProperties.set(response.items);
  //       this.isLoadingProperties.set(false);
  //     },
  //     error: () => {
  //       this.isLoadingProperties.set(false);
  //     },
  //   });
  // }

  protected planFeatures(plan: Plan): { icon: string; label: string }[] {
    const features: { icon: string; label: string }[] = [
      { icon: 'pi pi-building', label: `Até ${plan.maxProperties} imóveis` },
    ];
    if (plan.videoUpload)      features.push({ icon: 'pi pi-video',      label: 'Upload de vídeos' });
    if (plan.aiDescriptions)   features.push({ icon: 'pi pi-bolt',       label: 'Descrições por IA' });
    if (plan.customDomain)     features.push({ icon: 'pi pi-globe',      label: 'Domínio personalizado' });
    if (plan.premiumAnalytics) features.push({ icon: 'pi pi-chart-bar',  label: 'Analytics premium' });
    return features;
  }

  protected portalUrl(slug: string, propertyId: string): string {
    const origin = globalThis.location.origin;
    const base = origin.replace(/^(https?:\/\/)/, `$1${slug}.`);
    return `${base}/imoveis/${propertyId}`;
  }

  protected scrollTo(sectionId: string): void {
    document.getElementById(sectionId)?.scrollIntoView({ behavior: 'smooth' });
  }

  protected login(): void {
    this.auth.login();
  }

  protected startSignup(planId?: string): void {
    this.router.navigate(['/onboarding/signup'], planId ? { queryParams: { plan: planId } } : {});
  }
}
