import { describe, it, expect } from 'vitest';
import { RelativeTimePipe } from './relative-time.pipe';

describe('RelativeTimePipe', () => {
  const pipe = new RelativeTimePipe();

  function isoSecondsAgo(seconds: number): string {
    return new Date(Date.now() - seconds * 1000).toISOString();
  }

  it('returns "agora" for a date less than 60 seconds ago', () => {
    expect(pipe.transform(isoSecondsAgo(30))).toBe('agora');
  });

  it('returns "há 1 minuto" for a date 1 minute ago', () => {
    expect(pipe.transform(isoSecondsAgo(60))).toBe('há 1 minuto');
  });

  it('returns "há 5 minutos" for a date 5 minutes ago', () => {
    expect(pipe.transform(isoSecondsAgo(300))).toBe('há 5 minutos');
  });

  it('returns "há 1 hora" for a date 1 hour ago', () => {
    expect(pipe.transform(isoSecondsAgo(3600))).toBe('há 1 hora');
  });

  it('returns "há 3 horas" for a date 3 hours ago', () => {
    expect(pipe.transform(isoSecondsAgo(3 * 3600))).toBe('há 3 horas');
  });

  it('returns "há 1 dia" for a date 24 hours ago', () => {
    expect(pipe.transform(isoSecondsAgo(24 * 3600))).toBe('há 1 dia');
  });

  it('returns "há 7 dias" for a date 7 days ago', () => {
    expect(pipe.transform(isoSecondsAgo(7 * 24 * 3600))).toBe('há 7 dias');
  });

  it('handles null/undefined gracefully', () => {
    expect(pipe.transform(null as unknown as string)).toBe('');
    expect(pipe.transform(undefined as unknown as string)).toBe('');
  });
});
