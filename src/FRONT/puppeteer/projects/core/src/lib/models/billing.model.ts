import { Plan } from './tenant.model';

export interface BillingTransaction {
  id: string;
  type: string;
  status: string;
  amount: number;
  currency: string;
  occurredAt: string;
  description: string;
  hostedInvoiceUrl?: string;
}

export interface BillingOverview {
  paymentStatus: string;
  subscriptionStatus: string;
  planId?: string;
  planName?: string;
  stripeCustomerId?: string;
  stripeSubscriptionId?: string;
  lastPaymentDate?: string;
  nextPaymentDate?: string;
  gracePeriodEndsAt?: string;
  availabilityEndsAt?: string;
  hasRecurringPayment: boolean;
  canManageBilling: boolean;
  recentTransactions: BillingTransaction[];
}

export interface BillingPortalSessionResponse {
  url: string;
}

export interface ChangePlanResponse {
  plan: Plan;
  requiresCheckout: boolean;
  checkoutUrl?: string;
  updatedInPlace: boolean;
  message: string;
}
