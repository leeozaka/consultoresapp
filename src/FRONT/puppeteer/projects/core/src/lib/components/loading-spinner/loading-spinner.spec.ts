import { describe, it, expect, beforeEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Component } from '@angular/core';
import { By } from '@angular/platform-browser';
import { LoadingSpinnerComponent } from './loading-spinner';

describe('LoadingSpinnerComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [LoadingSpinnerComponent] });
  });

  it('renders a loading indicator with default size', () => {
    const fixture = TestBed.createComponent(LoadingSpinnerComponent);
    fixture.detectChanges();
    const el = fixture.debugElement.query(By.css('[role="status"]'));
    expect(el).not.toBeNull();
  });

  it('applies Tailwind size class on the SVG when size input is given', () => {
    const fixture = TestBed.createComponent(LoadingSpinnerComponent);
    fixture.componentRef.setInput('size', 'lg');
    fixture.detectChanges();
    const svg = fixture.debugElement.query(By.css('svg')).nativeElement as SVGElement;
    // 'lg' maps to Tailwind size-10 via svgClass()
    // Use getAttribute because SVGElement.className is an SVGAnimatedString, not a plain string
    expect(svg.getAttribute('class')).toContain('size-10');
  });
});
