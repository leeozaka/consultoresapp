import { ChangeDetectionStrategy, Component, input, output, computed } from '@angular/core';

@Component({
  selector: 'app-pagination',
  templateUrl: './pagination.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PaginationComponent {
  readonly totalCount = input.required<number>();
  readonly page = input.required<number>();
  readonly pageSize = input.required<number>();

  readonly pageChange = output<number>();

  protected readonly totalPages = computed(() =>
    Math.ceil(this.totalCount() / this.pageSize())
  );

  protected readonly showPagination = computed(
    () => this.totalPages() > 1
  );

  protected readonly pages = computed<number[]>(() => {
    const total = this.totalPages();
    const current = this.page();
    const delta = 2;
    const range: number[] = [];

    for (let i = Math.max(1, current - delta); i <= Math.min(total, current + delta); i++) {
      range.push(i);
    }
    return range;
  });

  protected readonly hasPrev = computed(() => this.page() > 1);
  protected readonly hasNext = computed(() => this.page() < this.totalPages());

  protected goTo(page: number): void {
    if (page >= 1 && page <= this.totalPages()) {
      this.pageChange.emit(page);
    }
  }

  protected prev(): void {
    this.goTo(this.page() - 1);
  }

  protected next(): void {
    this.goTo(this.page() + 1);
  }
}
