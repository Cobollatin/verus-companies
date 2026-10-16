import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';

import type { RequestState } from '../../../shared/data/request-state';
import type { CreateCompanyRequest } from '../data-access/company.models';
import { CompanyForm } from './company-form';

describe('CompanyForm', () => {
  let fixture: ComponentFixture<CompanyForm>;
  let element: HTMLElement;
  let submitted: CreateCompanyRequest[];

  const input = (id: string): HTMLInputElement =>
    element.querySelector<HTMLInputElement>(`#${id}`)!;

  const type = (id: string, value: string): void => {
    const field = input(id);
    field.value = value;
    field.dispatchEvent(new Event('input'));
    field.dispatchEvent(new Event('blur'));
  };

  const submit = async (): Promise<void> => {
    element.querySelector('form')!.dispatchEvent(new Event('submit'));
    await fixture.whenStable();
  };

  const errors = (): string[] =>
    [...element.querySelectorAll('.field-error')].map((e) => e.textContent.trim());

  const setState = async (state: RequestState<unknown>): Promise<void> => {
    fixture.componentRef.setInput('state', state);
    await fixture.whenStable();
  };

  beforeEach(async () => {
    fixture = TestBed.createComponent(CompanyForm);
    element = fixture.nativeElement as HTMLElement;
    submitted = [];
    fixture.componentInstance.submitted.subscribe((request) => submitted.push(request));
    await fixture.whenStable();
  });

  it('shows required errors and does not submit an empty form', async () => {
    await submit();

    expect(submitted).toEqual([]);
    expect(errors()).toEqual(['Company name is required.', 'Website URL is required.']);
  });

  it('validates name length and URL format on the client', async () => {
    type('company-name', 'Ab');
    type('company-website', 'example.com');
    await fixture.whenStable();

    expect(errors()).toEqual([
      'Company name must be at least 3 characters long.',
      'Enter a full URL, e.g. https://www.example.com.',
    ]);
  });

  it('emits trimmed values when valid', async () => {
    type('company-name', '  Acme  ');
    type('company-website', ' https://acme.com ');
    await submit();

    expect(submitted).toEqual([{ name: 'Acme', websiteUrl: 'https://acme.com' }]);
  });

  it('shows server errors next to the matching fields', async () => {
    type('company-name', 'Apple');
    type('company-website', 'https://microsoft.com');

    await setState({
      kind: 'error',
      error: {
        status: 400,
        title: 'Validation failed',
        message: 'The company name does not match the website.',
        fieldErrors: { websiteUrl: ['The company name does not match the website.'] },
      },
    });

    expect(errors()).toEqual(['The company name does not match the website.']);
    expect(input('company-website').getAttribute('aria-invalid')).toBe('true');
  });

  it('disables the button while pending and resets after success', async () => {
    type('company-name', 'Acme');
    type('company-website', 'https://acme.com');

    await setState({ kind: 'pending' });
    expect(element.querySelector('button')!.disabled).toBe(true);

    await setState({ kind: 'success', value: {} });
    expect(input('company-name').value).toBe('');
    expect(element.querySelector('button')!.disabled).toBe(false);
  });
});
