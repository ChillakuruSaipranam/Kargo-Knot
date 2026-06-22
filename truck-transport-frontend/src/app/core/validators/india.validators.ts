import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/** Indian mobile: 10 digits starting with 6–9, optional +91 prefix. */
export const INDIAN_PHONE_PATTERN = /^(?:\+91[\s-]?)?[6-9](?:[\s-]?\d){9}$/;

/** Standard Indian vehicle registration, e.g. MH-12-AB-4521. */
export const INDIAN_VEHICLE_PATTERN = /^[A-Z]{2}[\s-]?\d{1,2}[\s-]?[A-Z]{1,3}[\s-]?\d{4}$/i;

export function indianPhoneValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = (control.value ?? '').trim();
    if (!value) {
      return null;
    }
    return INDIAN_PHONE_PATTERN.test(value) ? null : { indianPhone: true };
  };
}

export function indianVehicleValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = (control.value ?? '').trim();
    if (!value) {
      return null;
    }
    return INDIAN_VEHICLE_PATTERN.test(value) ? null : { indianVehicle: true };
  };
}

export function isValidIndianPhone(value: string): boolean {
  return INDIAN_PHONE_PATTERN.test(value.trim());
}

export function isValidIndianVehicle(value: string): boolean {
  return INDIAN_VEHICLE_PATTERN.test(value.trim());
}
