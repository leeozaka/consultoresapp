import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClientService } from '../http/api-client.service';
import { TransactionRequest, TransactionResponse } from '../models/transaction.model';

@Injectable({ providedIn: 'root' })
export class TransactionService {
  private readonly api = inject(ApiClientService);

  create(body: TransactionRequest): Observable<TransactionResponse> {
    return this.api.post<TransactionResponse>('/api/transactions', body);
  }

  createBatch(bodies: TransactionRequest[]): Observable<TransactionResponse[]> {
    return this.api.post<TransactionResponse[]>('/api/transactions/batch', bodies);
  }
}
