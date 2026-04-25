import { describe, it, expect, beforeEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { AppErrorHandlerService } from './error-handler.service';

describe('AppErrorHandlerService', () => {
  let service: AppErrorHandlerService;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [AppErrorHandlerService] });
    service = TestBed.inject(AppErrorHandlerService);
  });

  it('starts with an empty notifications list', () => {
    expect(service.notifications()).toEqual([]);
  });

  it('maps a 401 HttpError to an authentication message', () => {
    const err = new HttpErrorResponse({ status: 401 });
    service.handleError(err);
    const notifications = service.notifications();
    expect(notifications.length).toBe(1);
    expect(notifications[0].severity).toBe('warn');
    expect(notifications[0].summary).toContain('autenticação');
  });

  it('maps a 403 HttpError to an authorization message', () => {
    const err = new HttpErrorResponse({ status: 403 });
    service.handleError(err);
    expect(service.notifications()[0].summary).toContain('permissão');
  });

  it('maps a 404 HttpError to a not-found message', () => {
    const err = new HttpErrorResponse({ status: 404 });
    service.handleError(err);
    expect(service.notifications()[0].summary).toContain('encontrado');
  });

  it('maps a 500 HttpError to a server error message', () => {
    const err = new HttpErrorResponse({ status: 500 });
    service.handleError(err);
    expect(service.notifications()[0].severity).toBe('error');
  });

  it('can dismiss a notification by id', () => {
    service.handleError(new HttpErrorResponse({ status: 500 }));
    const id = service.notifications()[0].id;
    service.dismiss(id);
    expect(service.notifications()).toEqual([]);
  });
});
