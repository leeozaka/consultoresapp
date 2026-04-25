export enum AccountStatus {
  Active = 'Active',
  Inactive = 'Inactive',
  Blocked = 'Blocked',
}

export interface Account {
  accountId: string;
  clientId: string;
  balance: number;
  reservedBalance: number;
  availableBalance: number;
  creditLimit: number;
  status: AccountStatus;
  currency: string;
}

export interface CreateAccountRequest {
  clientId: string;
  accountId?: string;
  initialBalance: number;
  creditLimit: number;
  currency: string;
}
