import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  output,
} from '@angular/core';
import type { AbstractControl, ValidationErrors } from '@angular/forms';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import type { RequestState } from '../../../shared/data/request-state';
import type { CreateCompanyRequest } from '../data-access/company.models';

type Field = 'name' | 'websiteUrl';

const NAME_MIN_LENGTH = 3;
const NAME_MAX_LENGTH = 200;
const HTTP_URL = /^https?:\/\/[^\s/?#]+\.[^\s/?#]+(?:[/?#]\S*)?$/i;

/** Fast client-side feedback; the API stays the source of truth (including the relevance check). */
function httpUrl(control: AbstractControl<string>): ValidationErrors | null {
  const value = control.value.trim();
  return value === '' || HTTP_URL.test(value) ? null : { httpUrl: true };
}

@Component({
  selector: 'app-company-form',
  imports: [ReactiveFormsModule],
  templateUrl: './company-form.html',
  styleUrl: './company-form.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CompanyForm {
  /** State of the last submission; drives the busy state, server errors and the reset after success. */
  readonly state = input<RequestState<unknown>>({ kind: 'idle' });
  readonly submitted = output<CreateCompanyRequest>();

  protected readonly form = inject(NonNullableFormBuilder).group({
    name: [
      '',
      [
        Validators.required,
        Validators.minLength(NAME_MIN_LENGTH),
        Validators.maxLength(NAME_MAX_LENGTH),
      ],
    ],
    websiteUrl: ['', [Validators.required, httpUrl]],
  });

  protected readonly pending = computed(() => this.state().kind === 'pending');

  constructor() {
    effect(() => {
      const state = this.state();
      if (state.kind === 'success') {
        this.form.reset();
      } else if (state.kind === 'error') {
        this.applyServerErrors(state.error.fieldErrors);
      }
    });
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const { name, websiteUrl } = this.form.getRawValue();
    this.submitted.emit({ name: name.trim(), websiteUrl: websiteUrl.trim() });
  }

  protected errorFor(field: Field): string | null {
    const control = this.form.controls[field];
    if (!control.errors || !(control.touched || control.dirty)) {
      return null;
    }
    const errors = control.errors;
    if (typeof errors['server'] === 'string') return errors['server'];
    if (errors['required'])
      return field === 'name' ? 'Company name is required.' : 'Website URL is required.';
    if (errors['minlength'])
      return `Company name must be at least ${String(NAME_MIN_LENGTH)} characters long.`;
    if (errors['maxlength'])
      return `Company name must be at most ${String(NAME_MAX_LENGTH)} characters long.`;
    if (errors['httpUrl']) return 'Enter a full URL, e.g. https://www.example.com.';
    return 'Invalid value.';
  }

  private applyServerErrors(fieldErrors: Readonly<Record<string, readonly string[]>>): void {
    for (const field of ['name', 'websiteUrl'] as const) {
      const messages = fieldErrors[field];
      if (messages?.length) {
        const control = this.form.controls[field];
        control.setErrors({ ...control.errors, server: messages.join(' ') });
        control.markAsTouched();
      }
    }
  }
}
