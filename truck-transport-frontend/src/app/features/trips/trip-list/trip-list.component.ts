import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { forkJoin } from 'rxjs';
import { TransportService, TripAnalytics } from '../../../core/services/transport.service';
import { LookupService } from '../../../core/services/lookup.service';
import { AuthService } from '../../../core/services/auth.service';
import { LookupItem } from '../../../core/models/lookup.model';
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
  private readonly lookupService = inject(LookupService);
  readonly auth = inject(AuthService);

  readonly trucks = signal<LookupItem[]>([]);
  readonly quarries = signal<LookupItem[]>([]);
  readonly drivers = signal<LookupItem[]>([]);

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
  readonly summary = signal<TripAnalytics>({
    totalTrips: 0,
    totalTonnes: 0,
    totalDieselLiters: 0,
    dayTrips: 0,
    nightTrips: 0,
    activeFilters: [],
  });
  readonly loadError = signal('');
  readonly isLoading = signal(true);
  readonly hasActiveFilters = signal(false);

  ngOnInit(): void {
    this.loadLookups();
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

  private loadLookups(): void {
    this.lookupService.getTrucks().subscribe({
      next: (items) => this.trucks.set(this.sortByLabel(items)),
    });
    this.lookupService.getQuarries().subscribe({
      next: (items) => this.quarries.set(this.sortByLabel(items)),
    });
    this.lookupService.getDrivers().subscribe({
      next: (items) => this.drivers.set(this.sortByLabel(items)),
    });
  }

  private loadTrips(filter = emptyTripFilter()): void {
    this.isLoading.set(true);
    this.loadError.set('');
    this.hasActiveFilters.set(this.isFilterActive(filter));

    forkJoin({
      trips: this.transportService.filter(filter),
      analytics: this.transportService.getAnalytics(filter),
    }).subscribe({
      next: ({ trips, analytics }) => {
        this.filteredTrips.set(trips);
        this.summary.set(analytics);
        this.isLoading.set(false);
      },
      error: () => {
        this.loadError.set('Unable to load trip records from the API.');
        this.filteredTrips.set([]);
        this.isLoading.set(false);
      },
    });
  }

  private isFilterActive(filter: ReturnType<typeof emptyTripFilter>): boolean {
    return !!(
      filter.dateFrom ||
      filter.dateTo ||
      filter.shift ||
      filter.truckNumber ||
      filter.quarryName ||
      filter.driverName ||
      filter.minTonnes !== null ||
      filter.maxTonnes !== null
    );
  }

  private sortByLabel(items: LookupItem[]): LookupItem[] {
    return [...items].sort((a, b) => a.label.localeCompare(b.label));
  }
}
