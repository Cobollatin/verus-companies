import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type AlertKind = 'success' | 'error' | 'info';

/** Status message. Errors are announced assertively, everything else politely. */
@Component({
  selector: 'app-alert',
  template: `<ng-content />`,
  styles: `
    :host {
      display: block;
      padding: 0.75rem 1rem;
      border-radius: var(--radius);
      border: 1px solid currentColor;
    }
    :host(.success) {
      color: var(--success);
      background: var(--success-bg);
    }
    :host(.error) {
      color: var(--danger);
      background: var(--danger-bg);
    }
    :host(.info) {
      color: var(--muted);
      background: var(--surface);
    }
  `,
  host: {
    '[class]': 'kind()',
    '[attr.role]': 'kind() === "error" ? "alert" : "status"',
  },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Alert {
  readonly kind = input<AlertKind>('info');
}
