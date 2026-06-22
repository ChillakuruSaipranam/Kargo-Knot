import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { LookupService } from '../../../core/services/lookup.service';
import { RepairService } from '../../../core/services/repair.service';
import { LookupItem } from '../../../core/models/lookup.model';
import { indianVehicleValidator, isValidIndianVehicle } from '../../../core/validators/india.validators';

@Component({
  selector: 'app-repair-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './repair-form.component.html',
  styleUrl: './repair-form.component.scss',
})
export class RepairFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly repairService = inject(RepairService);
  private readonly lookupService = inject(LookupService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  isEditMode = false;
  repairId: string | null = null;
  submitMessage = '';
  readonly loadError = signal('');
  readonly lookupError = signal('');
  readonly trucks = signal<LookupItem[]>([]);
  readonly newTruckNumber = signal('');

  readonly form = this.fb.nonNullable.group({
    date: ['', Validators.required],
    truckNumber: ['', [Validators.required, indianVehicleValidator()]],
    description: ['', [Validators.required, Validators.minLength(3)]],
    cost: [0, [Validators.required, Validators.min(0)]],
  });

  ngOnInit(): void {
    this.loadLookups();
    this.repairId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.repairId;

    if (this.repairId) {
      this.repairService.getById(this.repairId).subscribe({
        next: (repair) => {
          if (!repair) {
            this.router.navigate(['/repairs']);
            return;
          }
          this.form.patchValue(repair);
        },
        error: () => this.loadError.set('Unable to load repair record.'),
      });
    } else {
      this.form.patchValue({ date: new Date().toISOString().slice(0, 10) });
    }
  }

  addTruck(): void {
    const number = this.newTruckNumber().trim();
    if (!number) {
      this.lookupError.set('Truck number is required.');
      return;
    }
    if (!isValidIndianVehicle(number)) {
      this.lookupError.set('Enter a valid Indian vehicle number (e.g. MH-12-AB-4521).');
      return;
    }

    this.lookupService.createTruck(number).subscribe({
      next: (item) => {
        this.trucks.update((items) => this.sortByLabel([...items, item]));
        this.form.patchValue({ truckNumber: item.label });
        this.newTruckNumber.set('');
        this.lookupError.set('');
      },
      error: () => this.lookupError.set('Unable to add truck number.'),
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const payload = this.form.getRawValue();
    const request$ =
      this.isEditMode && this.repairId
        ? this.repairService.update(this.repairId, payload)
        : this.repairService.add(payload);

    request$.subscribe({
      next: () => {
        this.submitMessage = this.isEditMode
          ? 'Repair record updated successfully.'
          : 'Repair record registered successfully.';
        setTimeout(() => this.router.navigate(['/repairs']), 900);
      },
      error: () => this.loadError.set('Unable to save repair record.'),
    });
  }

  hasError(controlName: keyof typeof this.form.controls, errorKey: string): boolean {
    const control = this.form.controls[controlName];
    return control.touched && control.hasError(errorKey);
  }

  private loadLookups(): void {
    this.lookupService.getTrucks().subscribe({
      next: (items) => this.trucks.set(this.sortByLabel(items)),
      error: () => this.lookupError.set('Unable to load truck numbers.'),
    });
  }

  private sortByLabel(items: LookupItem[]): LookupItem[] {
    return [...items].sort((a, b) => a.label.localeCompare(b.label));
  }
}
