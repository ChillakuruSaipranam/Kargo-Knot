import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { TransportService, TripSummary } from '../../core/services/transport.service';
import { AuthService } from '../../core/services/auth.service';
import { TransportTrip } from '../../core/models/transport-trip.model';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink, DecimalPipe],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit {
  private readonly transportService = inject(TransportService);
  readonly auth = inject(AuthService);

  readonly summary = signal<TripSummary>({
    totalTrips: 0,
    totalTonnes: 0,
    dayTrips: 0,
    nightTrips: 0,
  });
  readonly recentTrips = signal<TransportTrip[]>([]);
  readonly loadError = signal('');

  ngOnInit(): void {
    this.transportService.getSummary().subscribe({
      next: (summary) => this.summary.set(summary),
      error: () => this.loadError.set('Unable to load dashboard summary from the API.'),
    });

    this.transportService.getAll().subscribe({
      next: (trips) => this.recentTrips.set(trips.slice(0, 5)),
      error: () => this.loadError.set('Unable to load trip records from the API.'),
    });
  }
}
