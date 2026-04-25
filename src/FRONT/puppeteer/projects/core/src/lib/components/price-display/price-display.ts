import { ChangeDetectionStrategy, Component, input, computed } from '@angular/core';
import { CurrencyPipe } from '@angular/common';

type CurrencyCode = 'BRL' | 'USD' | 'EUR';

const CURRENCY_LOCALES: Record<CurrencyCode, string> = {
  BRL: 'pt-BR',
  USD: 'en-US',
  EUR: 'de-DE',
};

const LISTING_TYPE_SUFFIX: Record<string, string> = {
  Rent: '/mês',
  Both: '',
  Sale: '',
};

function isSupportedCurrency(code: string): code is CurrencyCode {
  return code === 'BRL' || code === 'USD' || code === 'EUR';
}

@Component({
  selector: 'app-price-display',
  imports: [],
  template: `
    <span class="price-display" [attr.aria-label]="ariaLabel()">
      <span class="price-display__amount">{{ formattedPrice() }}</span>
      @if (suffix()) {
        <span class="price-display__suffix">{{ suffix() }}</span>
      }
    </span>
  `,
  styles: [`
    .price-display {
      display: inline-flex;
      align-items: baseline;
      gap: 0.25rem;
      font-variant-numeric: tabular-nums;
    }
    .price-display__suffix {
      font-size: 0.875em;
      opacity: 0.7;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PriceDisplayComponent {
  readonly price = input.required<number>();
  readonly currency = input<string>('BRL');
  readonly listingType = input<string>('Sale');

  protected readonly formattedPrice = computed(() => {
    const rawCode = (this.currency() ?? '').trim().toUpperCase();
    const code: CurrencyCode = isSupportedCurrency(rawCode) ? rawCode : 'BRL';
    const locale = CURRENCY_LOCALES[code];
    const numericPrice = Number(this.price());
    const safePrice = Number.isFinite(numericPrice) ? numericPrice : 0;

    try {
      return new CurrencyPipe(locale).transform(safePrice, code, 'symbol', '1.0-0') ?? '';
    } catch {
      return new CurrencyPipe('pt-BR').transform(safePrice, 'BRL', 'symbol', '1.0-0') ?? '';
    }
  });

  protected readonly suffix = computed(
    () => LISTING_TYPE_SUFFIX[this.listingType()] ?? ''
  );

  protected readonly ariaLabel = computed(
    () => `Preço: ${this.formattedPrice()}${this.suffix()}`
  );
}
