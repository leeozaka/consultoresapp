export enum TransactionType {
  Credit = 'Credit',
  Debit = 'Debit',
  Reserve = 'Reserve',
  Capture = 'Capture',
  Reversal = 'Reversal',
  Transfer = 'Transfer',
}

export enum TransactionStatus {
  Success = 'Success',
  Failed = 'Failed',
  Pending = 'Pending',
  Reversed = 'Reversed',
}

export interface TransactionRequest {
  operation: string;
  accountId: string;
  amount: number;
  currency: string;
  referenceId: string;
  destinationAccountId?: string;
  metadata?: Record<string, string>;
}

export interface TransactionResponse {
  transactionId: string;
  status: string;
  balance: number;
  reservedBalance: number;
  availableBalance: number;
  timestamp: string;
  errorMessage?: string;
  creditTransactionId?: string;
}
