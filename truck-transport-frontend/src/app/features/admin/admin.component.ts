import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LookupService } from '../../core/services/lookup.service';
import { LookupItem } from '../../core/models/lookup.model';
import { indianPhoneValidator, indianVehicleValidator } from '../../core/validators/india.validators';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './admin.component.html',
  styleUrl: './admin.component.scss',
})
export class AdminComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly lookupService = inject(LookupService);

  readonly trucks = signal<LookupItem[]>([]);
  readonly quarries = signal<LookupItem[]>([]);
  readonly drivers = signal<LookupItem[]>([]);
  readonly loadError = signal('');

  readonly truckMessage = signal('');
  readonly truckError = signal('');
  readonly quarryMessage = signal('');
  readonly quarryError = signal('');
  readonly driverMessage = signal('');
  readonly driverError = signal('');

  readonly truckForm = this.fb.nonNullable.group({
    id: [''],
    number: ['', [Validators.required, indianVehicleValidator()]],
  });

  readonly quarryForm = this.fb.nonNullable.group({
    id: [''],
    name: ['', Validators.required],
  });

  readonly driverForm = this.fb.nonNullable.group({
    id: [''],
    name: ['', Validators.required],
    phone: ['', [Validators.required, indianPhoneValidator()]],
    license: ['', Validators.required],
  });

  ngOnInit(): void {
    this.loadLookups();
  }

  saveTruck(): void {
    if (this.truckForm.invalid) {
      this.truckForm.markAllAsTouched();
      return;
    }

    const id = this.truckForm.controls.id.value;
    const number = this.truckForm.controls.number.value.trim();
    const request = id ? this.lookupService.updateTruck(id, number) : this.lookupService.createTruck(number);

    request.subscribe({
      next: (item) => {
        this.trucks.update((items) => this.mergeItem(items, item));
        this.truckMessage.set(id ? 'Truck updated successfully.' : 'Truck added successfully.');
        this.truckError.set('');
        this.resetTruckForm();
      },
      error: (error) => this.truckError.set(error.error?.message ?? 'Unable to save truck.'),
    });
  }

  editTruck(item: LookupItem): void {
    this.truckForm.patchValue({ id: item.id, number: item.label });
    this.truckMessage.set('Editing selected truck. Save to apply changes.');
    this.truckError.set('');
  }

  deleteTruck(id: string): void {
    if (!confirm('Delete this truck from master data?')) {
      return;
    }
    this.lookupService.deleteTruck(id).subscribe({
      next: () => this.trucks.update((items) => items.filter((item) => item.id !== id)),
      error: () => this.truckError.set('Unable to delete truck.'),
    });
  }

  resetTruckForm(): void {
    this.truckForm.reset({ id: '', number: '' });
    this.truckMessage.set('');
    this.truckError.set('');
  }

  saveQuarry(): void {
    if (this.quarryForm.invalid) {
      this.quarryForm.markAllAsTouched();
      return;
    }

    const id = this.quarryForm.controls.id.value;
    const name = this.quarryForm.controls.name.value.trim();
    const request = id ? this.lookupService.updateQuarry(id, name) : this.lookupService.createQuarry(name);

    request.subscribe({
      next: (item) => {
        this.quarries.update((items) => this.mergeItem(items, item));
        this.quarryMessage.set(id ? 'Quarry updated successfully.' : 'Quarry added successfully.');
        this.quarryError.set('');
        this.resetQuarryForm();
      },
      error: (error) => this.quarryError.set(error.error?.message ?? 'Unable to save quarry.'),
    });
  }

  editQuarry(item: LookupItem): void {
    this.quarryForm.patchValue({ id: item.id, name: item.label });
    this.quarryMessage.set('Editing selected quarry. Save to apply changes.');
    this.quarryError.set('');
  }

  deleteQuarry(id: string): void {
    if (!confirm('Delete this quarry / crusher from master data?')) {
      return;
    }
    this.lookupService.deleteQuarry(id).subscribe({
      next: () => this.quarries.update((items) => items.filter((item) => item.id !== id)),
      error: () => this.quarryError.set('Unable to delete quarry / crusher.'),
    });
  }

  resetQuarryForm(): void {
    this.quarryForm.reset({ id: '', name: '' });
    this.quarryMessage.set('');
    this.quarryError.set('');
  }

  saveDriver(): void {
    if (this.driverForm.invalid) {
      this.driverForm.markAllAsTouched();
      return;
    }

    const id = this.driverForm.controls.id.value;
    const name = this.driverForm.controls.name.value.trim();
    const phone = this.driverForm.controls.phone.value.trim();
    const license = this.driverForm.controls.license.value.trim();
    const request = id
      ? this.lookupService.updateDriver(id, name, phone, license)
      : this.lookupService.createDriver(name, phone, license);

    request.subscribe({
      next: (item) => {
        this.drivers.update((items) => this.mergeItem(items, item));
        this.driverMessage.set(id ? 'Driver updated successfully.' : 'Driver added successfully.');
        this.driverError.set('');
        this.resetDriverForm();
      },
      error: (error) => this.driverError.set(error.error?.message ?? 'Unable to save driver.'),
    });
  }

  editDriver(item: LookupItem): void {
    this.driverForm.patchValue({
      id: item.id,
      name: item.label,
      phone: item.phone || '',
      license: item.license || '',
    });
    this.driverMessage.set('Editing selected driver. Save to apply changes.');
    this.driverError.set('');
  }

  deleteDriver(id: string): void {
    if (!confirm('Delete this driver from master data?')) {
      return;
    }
    this.lookupService.deleteDriver(id).subscribe({
      next: () => this.drivers.update((items) => items.filter((item) => item.id !== id)),
      error: () => this.driverError.set('Unable to delete driver.'),
    });
  }

  resetDriverForm(): void {
    this.driverForm.reset({ id: '', name: '', phone: '', license: '' });
    this.driverMessage.set('');
    this.driverError.set('');
  }

  private loadLookups(): void {
    this.lookupService.getTrucks().subscribe({ next: (items) => this.trucks.set(this.sortByLabel(items)), error: () => this.loadError.set('Unable to load truck list.') });
    this.lookupService.getQuarries().subscribe({ next: (items) => this.quarries.set(this.sortByLabel(items)), error: () => this.loadError.set('Unable to load quarry list.') });
    this.lookupService.getDrivers().subscribe({ next: (items) => this.drivers.set(this.sortByLabel(items)), error: () => this.loadError.set('Unable to load driver list.') });
  }

  private mergeItem(items: LookupItem[], item: LookupItem): LookupItem[] {
    const next = items.filter((current) => current.id !== item.id);
    return this.sortByLabel([...next, item]);
  }

  private sortByLabel(items: LookupItem[]): LookupItem[] {
    return [...items].sort((a, b) => a.label.localeCompare(b.label));
  }
}
