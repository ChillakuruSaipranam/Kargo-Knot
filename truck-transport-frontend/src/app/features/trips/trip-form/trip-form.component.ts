import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { LookupService } from '../../../core/services/lookup.service';
import { TransportService } from '../../../core/services/transport.service';
import { LookupItem } from '../../../core/models/lookup.model';
import { indianPhoneValidator, indianVehicleValidator } from '../../../core/validators/india.validators';

@Component({
  selector: 'app-trip-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './trip-form.component.html',
  styleUrl: './trip-form.component.scss',
})
export class TripFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly transportService = inject(TransportService);
  private readonly lookupService = inject(LookupService);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  isEditMode = false;
  tripId: string | null = null;
  submitMessage = '';
  readonly loadError = signal('');
  readonly lookupError = signal('');

  readonly trucks = signal<LookupItem[]>([]);
  readonly quarries = signal<LookupItem[]>([]);
  readonly drivers = signal<LookupItem[]>([]);


  readonly form = this.fb.nonNullable.group({
    date: ['', Validators.required],
    shift: ['Day' as 'Day' | 'Night', Validators.required],
    truckNumber: ['', [Validators.required, indianVehicleValidator()]],
    quarryName: ['', Validators.required],
    numberOfTrips: [1, [Validators.required, Validators.min(1), Validators.max(50)]],
    tonnes: [0, [Validators.min(0)]],
    dieselLiters: [0, [Validators.min(0)]],
    driverName: ['', Validators.required],
    additionalDriverName: [''],
    driverPhone: ['', [Validators.required, indianPhoneValidator()]],
    driverLicense: ['', Validators.required],
    startTime: [''],
    endTime: [''],
  });

  ngOnInit(): void {
    this.loadLookups();
    this.tripId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.tripId;

    if (this.tripId) {
      this.transportService.getById(this.tripId).subscribe({
        next: (trip) => {
          if (!trip) {
            this.router.navigate(['/trips']);
            return;
          }
          this.form.patchValue(trip);
        },
        error: () => this.loadError.set('Unable to load trip record.'),
      });
    } else {
      this.form.patchValue({ date: new Date().toISOString().slice(0, 10) });
    }
  }

  onDriverSelected(): void {
    const name = this.form.controls.driverName.value;
    const driver = this.drivers().find((item) => item.label === name);
    if (driver?.phone && driver.license) {
      this.form.patchValue({
        driverPhone: driver.phone,
        driverLicense: driver.license,
      });
    }
  }


  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const user = this.auth.user();
    if (!user) {
      return;
    }

    const payload = {
      ...this.form.getRawValue(),
      createdBy: user.email,
    };

    const request$ =
      this.isEditMode && this.tripId
        ? this.transportService.update(this.tripId, payload)
        : this.transportService.add(payload);

    request$.subscribe({
      next: () => {
        this.submitMessage = this.isEditMode
          ? 'Trip record updated successfully.'
          : 'Trip record registered successfully.';

        if (!this.isEditMode) {
          this.form.reset({
            date: new Date().toISOString().slice(0, 10),
            shift: 'Day',
            truckNumber: '',
            quarryName: '',
            numberOfTrips: 1,
            tonnes: 0,
            dieselLiters: 0,
            driverName: '',
            additionalDriverName: '',
            driverPhone: '',
            driverLicense: '',
            startTime: '',
            endTime: '',
          });
        }

        setTimeout(() => this.router.navigate(['/trips']), 900);
      },
      error: () => this.loadError.set('Unable to save trip record.'),
    });
  }

  private loadLookups(): void {
    this.lookupService.getTrucks().subscribe({
      next: (items) => this.trucks.set(this.sortByLabel(items)),
      error: () => this.lookupError.set('Unable to load truck numbers.'),
    });
    this.lookupService.getQuarries().subscribe({
      next: (items) => this.quarries.set(this.sortByLabel(items)),
      error: () => this.lookupError.set('Unable to load quarries / crushers.'),
    });
    this.lookupService.getDrivers().subscribe({
      next: (items) => this.drivers.set(this.sortByLabel(items)),
      error: () => this.lookupError.set('Unable to load drivers.'),
    });
  }

  private sortByLabel(items: LookupItem[]): LookupItem[] {
    return [...items].sort((a, b) => a.label.localeCompare(b.label));
  }

  hasError(controlName: keyof typeof this.form.controls, errorKey: string): boolean {
    const control = this.form.controls[controlName];
    return control.touched && control.hasError(errorKey);
  }
}
