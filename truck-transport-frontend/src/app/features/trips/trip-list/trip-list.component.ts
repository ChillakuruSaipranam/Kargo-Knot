import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { TransportService } from '../../../core/services/transport.service';
import { AuthService } from '../../../core/services/auth.service';
import { emptyTripFilter, TransportTrip } from '../../../core/models/transport-trip.model';

@Component({
  selector: 'app-trip-list',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, DecimalPipe],
  templateUrl: './trip-list.component.html',
  styleUrl: './trip-list.component.scss',
})
export class TripListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly transportService = inject(TransportService);
  readonly auth = inject(AuthService);

  readonly filterForm = this.fb.nonNullable.group({
    dateFrom: [''],
    dateTo: [''],
    shift: ['' as '' | 'Day' | 'Night'],
    truckNumber: [''],
    quarryName: [''],
    driverName: [''],
    minTonnes: [null as number | null],
    maxTonnes: [null as number | null],
  });

  readonly filteredTrips = signal<TransportTrip[]>([]);
  readonly loadError = signal('');
  readonly isLoading = signal(true);

  ngOnInit(): void {
    this.loadTrips();
  }

  applyFilters(): void {
    this.loadTrips(this.filterForm.getRawValue());
  }

  resetFilters(): void {
    this.filterForm.reset(emptyTripFilter());
    this.loadTrips();
  }

  deleteTrip(id: string): void {
    if (!this.auth.isAdmin()) {
      return;
    }
    if (confirm('Delete this trip record permanently?')) {
      this.transportService.delete(id).subscribe({
        next: () => this.applyFilters(),
        error: () => this.loadError.set('Unable to delete trip record.'),
      });
    }
  }

  private loadTrips(filter = emptyTripFilter()): void {
    this.isLoading.set(true);
    this.loadError.set('');
    this.transportService.filter(filter).subscribe({
      next: (trips) => {
        this.filteredTrips.set(trips);
        this.isLoading.set(false);
      },
      error: () => {
        this.loadError.set('Unable to load trip records from the API.');
        this.filteredTrips.set([]);
        this.isLoading.set(false);
      },
    });
  }
}
