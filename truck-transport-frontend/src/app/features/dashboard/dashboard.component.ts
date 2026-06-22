import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { forkJoin } from 'rxjs';
import { TransportService, TripSummary } from '../../core/services/transport.service';
import { RepairService } from '../../core/services/repair.service';
import { LookupService } from '../../core/services/lookup.service';
import { AuthService } from '../../core/services/auth.service';
import { TransportTrip } from '../../core/models/transport-trip.model';
import { emptyRepairFilter, RepairSummary, TruckRepair } from '../../core/models/truck-repair.model';
import { LookupItem } from '../../core/models/lookup.model';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink, DecimalPipe, CurrencyPipe, ReactiveFormsModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit {
  private readonly transportService = inject(TransportService);
  private readonly repairService = inject(RepairService);
  private readonly lookupService = inject(LookupService);
  private readonly fb = inject(FormBuilder);
  readonly auth = inject(AuthService);

  readonly summary = signal<TripSummary>({
    totalTrips: 0,
    totalTonnes: 0,
    totalDieselLiters: 0,
    dayTrips: 0,
    nightTrips: 0,
  });
  readonly repairSummary = signal<RepairSummary>({ totalRepairs: 0, totalCost: 0 });
  readonly recentTrips = signal<TransportTrip[]>([]);
  readonly recentRepairs = signal<TruckRepair[]>([]);
  readonly trucks = signal<LookupItem[]>([]);
  readonly loadError = signal('');

  readonly repairFilterForm = this.fb.nonNullable.group({
    truckNumber: [''],
  });

  ngOnInit(): void {
    this.transportService.getSummary().subscribe({
      next: (summary) => this.summary.set(summary),
      error: () => this.loadError.set('Unable to load dashboard summary from the API.'),
    });

    this.transportService.getAll().subscribe({
      next: (trips) => this.recentTrips.set(trips.slice(0, 5)),
      error: () => this.loadError.set('Unable to load trip records from the API.'),
    });

    this.lookupService.getTrucks().subscribe({
      next: (items) => this.trucks.set([...items].sort((a, b) => a.label.localeCompare(b.label))),
    });

    this.loadRepairs();
  }

  applyRepairFilter(): void {
    this.loadRepairs(this.repairFilterForm.getRawValue());
  }

  resetRepairFilter(): void {
    this.repairFilterForm.reset({ truckNumber: '' });
    this.loadRepairs();
  }

  private loadRepairs(filter: { truckNumber: string } = { truckNumber: '' }): void {
    const repairFilter = { ...emptyRepairFilter(), truckNumber: filter.truckNumber };
    forkJoin({
      repairs: this.repairService.filter(repairFilter),
      summary: this.repairService.getSummary(repairFilter),
    }).subscribe({
      next: ({ repairs, summary }) => {
        this.recentRepairs.set(repairs.slice(0, 5));
        this.repairSummary.set(summary);
      },
      error: () => this.loadError.set('Unable to load repair records from the API.'),
    });
  }
}
