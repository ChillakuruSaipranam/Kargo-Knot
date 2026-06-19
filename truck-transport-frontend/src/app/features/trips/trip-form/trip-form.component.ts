import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { TransportService } from '../../../core/services/transport.service';

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
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  isEditMode = false;
  tripId: string | null = null;
  submitMessage = '';
  loadError = '';

  readonly form = this.fb.nonNullable.group({
    date: ['', Validators.required],
    shift: ['Day' as 'Day' | 'Night', Validators.required],
    truckNumber: ['', [Validators.required, Validators.minLength(4)]],
    quarryName: ['', [Validators.required, Validators.minLength(3)]],
    tonnes: [0, [Validators.required, Validators.min(0.01)]],
    driverName: ['', [Validators.required, Validators.minLength(3)]],
    driverPhone: ['', [Validators.required, Validators.pattern(/^\+?[\d\s-]{10,}$/)]],
    driverLicense: ['', Validators.required],
    startTime: ['', Validators.required],
    endTime: ['', Validators.required],
  });

  ngOnInit(): void {
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
        error: () => {
          this.loadError = 'Unable to load trip record.';
        },
      });
    } else {
      this.form.patchValue({ date: new Date().toISOString().slice(0, 10) });
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
            tonnes: 0,
            driverName: '',
            driverPhone: '',
            driverLicense: '',
            startTime: '',
            endTime: '',
          });
        }

        setTimeout(() => this.router.navigate(['/trips']), 900);
      },
      error: () => {
        this.loadError = 'Unable to save trip record.';
      },
    });
  }
}
