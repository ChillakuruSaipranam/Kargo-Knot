import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map, Observable, catchError, of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { TransportTrip, TripFilter } from '../models/transport-trip.model';

interface ApiTrip {
  id: string;
  date: string;
  shift: string;
  truckNumber: string;
  quarryName: string;
  numberOfTrips: number;
  tonnes: number;
  dieselLiters: number;
  driverName: string;
  additionalDriverName: string | null;
  driverPhone: string;
  driverLicense: string;
  startTime: string;
  endTime: string;
  createdBy: string;
  createdAt: string;
}

interface ApiSummary {
  totalTrips: number;
  totalTonnes: number;
  totalDieselLiters: number;
  dayTrips: number;
  nightTrips: number;
}

interface ApiAnalytics extends ApiSummary {
  activeFilters: string[];
}

export interface TripSummary {
  totalTrips: number;
  totalTonnes: number;
  totalDieselLiters: number;
  dayTrips: number;
  nightTrips: number;
}

export interface TripAnalytics extends TripSummary {
  activeFilters: string[];
}

@Injectable({ providedIn: 'root' })
export class TransportService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/trips`;

  getAll(filter?: TripFilter): Observable<TransportTrip[]> {
    return this.http
      .get<ApiTrip[]>(this.apiUrl, { params: this.buildParams(filter) })
      .pipe(map((trips) => trips.map((trip) => this.mapTrip(trip))));
  }

  getById(id: string): Observable<TransportTrip | undefined> {
    return this.http.get<ApiTrip>(`${this.apiUrl}/${id}`).pipe(
      map((trip) => this.mapTrip(trip)),
      catchError(() => of(undefined))
    );
  }

  getSummary(): Observable<TripSummary> {
    return this.http.get<ApiSummary>(`${this.apiUrl}/summary`).pipe(map((summary) => this.mapSummary(summary)));
  }

  getAnalytics(filter?: TripFilter): Observable<TripAnalytics> {
    return this.http
      .get<ApiAnalytics>(`${this.apiUrl}/analytics`, { params: this.buildParams(filter) })
      .pipe(map((analytics) => ({ ...this.mapSummary(analytics), activeFilters: analytics.activeFilters ?? [] })));
  }

  add(trip: Omit<TransportTrip, 'id' | 'createdAt'>): Observable<TransportTrip> {
    return this.http
      .post<ApiTrip>(this.apiUrl, {
        date: trip.date,
        shift: trip.shift,
        truckNumber: trip.truckNumber,
        quarryName: trip.quarryName,
        tonnes: trip.tonnes,
        dieselLiters: trip.dieselLiters,
        driverName: trip.driverName,
        additionalDriverName: trip.additionalDriverName,
        driverPhone: trip.driverPhone,
        driverLicense: trip.driverLicense,
        startTime: trip.startTime,
        endTime: trip.endTime,
        numberOfTrips: trip.numberOfTrips,
      })
      .pipe(map((created) => this.mapTrip(created)));
  }

  update(id: string, changes: Partial<TransportTrip>): Observable<TransportTrip> {
    return this.http
      .put<ApiTrip>(`${this.apiUrl}/${id}`, {
        date: changes.date,
        shift: changes.shift,
        truckNumber: changes.truckNumber,
        quarryName: changes.quarryName,
        tonnes: changes.tonnes,
        dieselLiters: changes.dieselLiters,
        driverName: changes.driverName,
        additionalDriverName: changes.additionalDriverName,
        driverPhone: changes.driverPhone,
        driverLicense: changes.driverLicense,
        startTime: changes.startTime,
        endTime: changes.endTime,
        numberOfTrips: changes.numberOfTrips,
      })
      .pipe(map((trip) => this.mapTrip(trip)));
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  filter(filters: TripFilter): Observable<TransportTrip[]> {
    return this.getAll(filters);
  }

  private buildParams(filter?: TripFilter): HttpParams {
    let params = new HttpParams();
    if (!filter) {
      return params;
    }
    if (filter.dateFrom) params = params.set('dateFrom', filter.dateFrom);
    if (filter.dateTo) params = params.set('dateTo', filter.dateTo);
    if (filter.shift) params = params.set('shift', filter.shift);
    if (filter.truckNumber) params = params.set('truckNumber', filter.truckNumber);
    if (filter.quarryName) params = params.set('quarryName', filter.quarryName);
    if (filter.driverName) params = params.set('driverName', filter.driverName);
    if (filter.minTonnes !== null) params = params.set('minTonnes', filter.minTonnes);
    if (filter.maxTonnes !== null) params = params.set('maxTonnes', filter.maxTonnes);
    return params;
  }

  private mapSummary(summary: ApiSummary): TripSummary {
    return {
      totalTrips: summary.totalTrips,
      totalTonnes: summary.totalTonnes,
      totalDieselLiters: summary.totalDieselLiters ?? 0,
      dayTrips: summary.dayTrips,
      nightTrips: summary.nightTrips,
    };
  }

  private mapTrip(trip: ApiTrip): TransportTrip {
    return {
      id: trip.id,
      date: trip.date,
      shift: trip.shift as TransportTrip['shift'],
      truckNumber: trip.truckNumber,
      quarryName: trip.quarryName,
      numberOfTrips: trip.numberOfTrips,
      tonnes: trip.tonnes,
      dieselLiters: trip.dieselLiters,
      driverName: trip.driverName,
      additionalDriverName: trip.additionalDriverName ?? '',
      driverPhone: trip.driverPhone,
      driverLicense: trip.driverLicense,
      startTime: trip.startTime,
      endTime: trip.endTime,
      createdAt: trip.createdAt,
      createdBy: trip.createdBy,
    };
  }
}
