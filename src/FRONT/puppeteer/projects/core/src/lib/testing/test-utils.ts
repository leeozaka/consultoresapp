import { Type } from '@angular/core';
import { TestBed, TestBedStatic } from '@angular/core/testing';
import { ComponentFixture } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

/**
 * Renders a standalone component for unit testing with minimal providers.
 * Automatically adds router and HTTP testing providers.
 */
export async function renderComponent<T>(
  component: Type<T>,
  options: {
    imports?: unknown[];
    providers?: unknown[];
  } = {}
): Promise<{ fixture: ComponentFixture<T>; testBed: TestBedStatic }> {
  await TestBed.configureTestingModule({
    imports: [component, ...(options.imports ?? [])],
    providers: [
      provideRouter([]),
      provideHttpClient(),
      provideHttpClientTesting(),
      ...(options.providers ?? []),
    ],
  }).compileComponents();

  const fixture = TestBed.createComponent(component);
  fixture.detectChanges();

  return { fixture, testBed: TestBed };
}
