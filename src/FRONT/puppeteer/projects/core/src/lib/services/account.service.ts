import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClientService } from '../http/api-client.service';
import { Account, CreateAccountRequest } from '../models/account.model';

@Injectable({ providedIn: 'root' })
export class AccountService {
  private readonly api = inject(ApiClientService);

  create(body: CreateAccountRequest): Observable<Account> {
    return this.api.post<Account>('/api/accounts', body);
  }

  getBalance(accountId: string): Observable<Account> {
    return this.api.get<Account>(`/api/accounts/${accountId}/balance`);
  }
}
