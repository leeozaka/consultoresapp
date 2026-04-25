import { describe, it, expect, beforeEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { PaginationComponent } from './pagination';

describe('PaginationComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [PaginationComponent] });
  });

  it('renders when totalPages > 1', () => {
    const fixture = TestBed.createComponent(PaginationComponent);
    fixture.componentRef.setInput('totalCount', 25);
    fixture.componentRef.setInput('page', 1);
    fixture.componentRef.setInput('pageSize', 10);
    fixture.detectChanges();
    const nav = fixture.debugElement.query(By.css('nav'));
    expect(nav).not.toBeNull();
  });

  it('does not render when only 1 page', () => {
    const fixture = TestBed.createComponent(PaginationComponent);
    fixture.componentRef.setInput('totalCount', 5);
    fixture.componentRef.setInput('page', 1);
    fixture.componentRef.setInput('pageSize', 10);
    fixture.detectChanges();
    const nav = fixture.debugElement.query(By.css('nav'));
    expect(nav).toBeNull();
  });

  it('emits pageChange when a page button is clicked', () => {
    const fixture = TestBed.createComponent(PaginationComponent);
    fixture.componentRef.setInput('totalCount', 30);
    fixture.componentRef.setInput('page', 1);
    fixture.componentRef.setInput('pageSize', 10);
    fixture.detectChanges();
    const changes: number[] = [];
    fixture.componentInstance.pageChange.subscribe((p: number) => changes.push(p));
    const nextBtn = fixture.debugElement.query(By.css('[data-testid="next-page"]'));
    nextBtn?.nativeElement.click();
    fixture.detectChanges();
    expect(changes).toContain(2);
  });
});
