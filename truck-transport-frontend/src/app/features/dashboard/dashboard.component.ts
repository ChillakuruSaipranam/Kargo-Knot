import { DecimalPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TransportService } from '../../core/services/transport.service';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink, DecimalPipe],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent {
  private readonly transportService = inject(TransportService);
  readonly auth = inject(AuthService);
  readonly summary = this.transportService.getSummary();
  readonly recentTrips = this.transportService.getAll().slice(0, 5);
}
