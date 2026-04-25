import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';

@Injectable({ providedIn: 'root' })
export class ApiClientService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  private url(path: string): string {
    if (path.startsWith('http://') || path.startsWith('https://')) {
      return path;
    }
    return `${this.base}${path.startsWith('/') ? path : `/${path}`}`;
  }

  get<T>(path: string, params?: Record<string, unknown>): Observable<T> {
    const httpParams = params ? this.buildParams(params) : undefined;
    return this.http.get<T>(this.url(path), { params: httpParams });
  }

  post<T>(path: string, body: unknown): Observable<T> {
    return this.http.post<T>(this.url(path), body);
  }

  put<T>(path: string, body: unknown): Observable<T> {
    return this.http.put<T>(this.url(path), body);
  }

  patch<T>(path: string, body: unknown): Observable<T> {
    return this.http.patch<T>(this.url(path), body);
  }

  delete<T>(path: string, params?: Record<string, unknown>): Observable<T> {
    const httpParams = params ? this.buildParams(params) : undefined;
    return this.http.delete<T>(this.url(path), { params: httpParams });
  }

  postForm<T>(path: string, formData: FormData): Observable<T> {
    return this.http.post<T>(this.url(path), formData);
  }

  private buildParams(params: Record<string, unknown>): HttpParams {
    let httpParams = new HttpParams();
    for (const [key, value] of Object.entries(params)) {
      if (value !== undefined && value !== null) {
        httpParams = httpParams.set(key, String(value));
      }
    }
    return httpParams;
  }
}
