import { registerLocaleData } from '@angular/common';
import localePtBr from '@angular/common/locales/pt';

registerLocaleData(localePtBr, 'pt-BR');

// Mock browser APIs unavailable in jsdom
if (typeof IntersectionObserver === 'undefined') {
  (globalThis as unknown as Record<string, unknown>)['IntersectionObserver'] = class {
    observe() {}
    unobserve() {}
    disconnect() {}
  };
}

if (typeof ResizeObserver === 'undefined') {
  (globalThis as unknown as Record<string, unknown>)['ResizeObserver'] = class {
    observe() {}
    unobserve() {}
    disconnect() {}
  };
}
