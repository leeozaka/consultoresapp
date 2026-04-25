import {
  Directive,
  inject,
  input,
  TemplateRef,
  ViewContainerRef,
  effect,
} from '@angular/core';
import { EntitlementService } from '../entitlements/entitlement.service';

/**
 * Structural directive that conditionally renders its host element
 * based on whether the current tenant has the specified feature entitlement.
 *
 * @example
 * <div *appFeature="'featured_listings'">Only shown with the perk</div>
 */
//@Directive({ selector: '[appFeature]' })
// export class FeatureGateDirective {
//   private readonly tpl = inject(TemplateRef<unknown>);
//   private readonly vcr = inject(ViewContainerRef);
//   private readonly entitlements = inject(EntitlementService);

//   readonly appFeature = input.required<string>();

//   constructor() {
//     effect(() => {
//       const featureKey = this.appFeature();
//       const active = this.entitlements.hasFeature(featureKey)();

//       this.vcr.clear();
//       if (active) {
//         this.vcr.createEmbeddedView(this.tpl);
//       }
//     });
//   }
// }
