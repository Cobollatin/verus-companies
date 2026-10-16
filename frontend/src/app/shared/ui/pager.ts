import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

@Component({
  selector: 'app-pager',
  template: `
    <nav aria-label="Pagination">
      <button type="button" [disabled]="!hasPrevious()" (click)="pageChange.emit(page() - 1)">
        Previous
      </button>
      <span aria-live="polite">Page {{ page() }} of {{ totalPages() || 1 }}</span>
      <button type="button" [disabled]="!hasNext()" (click)="pageChange.emit(page() + 1)">
        Next
      </button>
    </nav>
  `,
  styles: `
    nav {
      display: flex;
      align-items: center;
      justify-content: flex-end;
      gap: 0.75rem;
      margin-top: 0.75rem;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Pager {
  readonly page = input.required<number>();
  readonly totalPages = input.required<number>();
  readonly hasPrevious = input.required<boolean>();
  readonly hasNext = input.required<boolean>();
  readonly pageChange = output<number>();
}
