import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CurrencyPipe } from '@angular/common';
import { forkJoin } from 'rxjs';
import { RepairService } from '../../../core/services/repair.service';
import { LookupService } from '../../../core/services/lookup.service';
import { AuthService } from '../../../core/services/auth.service';
import { LookupItem } from '../../../core/models/lookup.model';
import { emptyRepairFilter, RepairSummary, TruckRepair } from '../../../core/models/truck-repair.model';

@Component({
  selector: 'app-repair-list',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, CurrencyPipe],
  templateUrl: './repair-list.component.html',
  styleUrl: './repair-list.component.scss',
})
export class RepairListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly repairService = inject(RepairService);
  private readonly lookupService = inject(LookupService);
  readonly auth = inject(AuthService);

  readonly trucks = signal<LookupItem[]>([]);
  readonly filterForm = this.fb.nonNullable.group({
    dateFrom: [''],
    dateTo: [''],
    truckNumber: [''],
    minCost: [null as number | null],
    maxCost: [null as number | null],
  });

  readonly repairs = signal<TruckRepair[]>([]);
  readonly summary = signal<RepairSummary>({ totalRepairs: 0, totalCost: 0 });
  readonly loadError = signal('');
  readonly isLoading = signal(true);
  readonly hasActiveFilters = signal(false);

  ngOnInit(): void {
    this.lookupService.getTrucks().subscribe({
      next: (items) => this.trucks.set([...items].sort((a, b) => a.label.localeCompare(b.label))),
    });
    this.loadRepairs();
  }

  applyFilters(): void {
    this.loadRepairs(this.filterForm.getRawValue());
  }

  resetFilters(): void {
    this.filterForm.reset(emptyRepairFilter());
    this.loadRepairs();
  }

  deleteRepair(id: string): void {
    if (!this.auth.isAdmin()) {
      return;
    }
    if (confirm('Delete this repair record permanently?')) {
      this.repairService.delete(id).subscribe({
        next: () => this.applyFilters(),
        error: () => this.loadError.set('Unable to delete repair record.'),
      });
    }
  }

  private loadRepairs(filter = emptyRepairFilter()): void {
    this.isLoading.set(true);
    this.loadError.set('');
    this.hasActiveFilters.set(this.isFilterActive(filter));

    forkJoin({
      repairs: this.repairService.filter(filter),
      summary: this.repairService.getSummary(filter),
    }).subscribe({
      next: ({ repairs, summary }) => {
        this.repairs.set(repairs);
        this.summary.set(summary);
        this.isLoading.set(false);
      },
      error: () => {
        this.loadError.set('Unable to load repair records from the API.');
        this.repairs.set([]);
        this.isLoading.set(false);
      },
    });
  }

  private isFilterActive(filter: ReturnType<typeof emptyRepairFilter>): boolean {
    return !!(
      filter.dateFrom ||
      filter.dateTo ||
      filter.truckNumber ||
      filter.minCost !== null ||
      filter.maxCost !== null
    );
  }
}
