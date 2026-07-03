import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map, Observable, catchError, of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { RepairFilter, RepairSummary, TruckRepair } from '../models/truck-repair.model';

interface ApiRepair {
  id: string;
  date: string;
  truckNumber: string;
  description: string;
  cost: number;
  driverName: string | null;
  createdBy: string;
  createdAt: string;
}

interface ApiRepairSummary {
  totalRepairs: number;
  totalCost: number;
}

@Injectable({ providedIn: 'root' })
export class RepairService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/repairs`;

  getAll(filter?: RepairFilter): Observable<TruckRepair[]> {
    return this.http
      .get<ApiRepair[]>(this.apiUrl, { params: this.buildParams(filter) })
      .pipe(map((repairs) => repairs.map((repair) => this.mapRepair(repair))));
  }

  getById(id: string): Observable<TruckRepair | undefined> {
    return this.http.get<ApiRepair>(`${this.apiUrl}/${id}`).pipe(
      map((repair) => this.mapRepair(repair)),
      catchError(() => of(undefined))
    );
  }

  getSummary(filter?: RepairFilter): Observable<RepairSummary> {
    return this.http.get<ApiRepairSummary>(`${this.apiUrl}/summary`, { params: this.buildParams(filter) }).pipe(
      map((summary) => ({
        totalRepairs: summary.totalRepairs,
        totalCost: summary.totalCost,
      }))
    );
  }

  add(repair: Omit<TruckRepair, 'id' | 'createdAt' | 'createdBy'>): Observable<TruckRepair> {
    return this.http
      .post<ApiRepair>(this.apiUrl, {
        date: repair.date,
        truckNumber: repair.truckNumber,
        description: repair.description,
        cost: repair.cost,
        driverName: repair.driverName || null,
      })
      .pipe(map((created) => this.mapRepair(created)));
  }

  update(id: string, changes: Partial<TruckRepair>): Observable<TruckRepair> {
    return this.http
      .put<ApiRepair>(`${this.apiUrl}/${id}`, {
        date: changes.date,
        truckNumber: changes.truckNumber,
        description: changes.description,
        cost: changes.cost,
        driverName: changes.driverName || null,
      })
      .pipe(map((repair) => this.mapRepair(repair)));
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  filter(filters: RepairFilter): Observable<TruckRepair[]> {
    return this.getAll(filters);
  }

  private buildParams(filter?: RepairFilter): HttpParams {
    let params = new HttpParams();
    if (!filter) {
      return params;
    }
    if (filter.dateFrom) params = params.set('dateFrom', filter.dateFrom);
    if (filter.dateTo) params = params.set('dateTo', filter.dateTo);
    if (filter.truckNumber) params = params.set('truckNumber', filter.truckNumber);
    if (filter.driverName) params = params.set('driverName', filter.driverName);
    if (filter.minCost !== null) params = params.set('minCost', filter.minCost);
    if (filter.maxCost !== null) params = params.set('maxCost', filter.maxCost);
    return params;
  }

  private mapRepair(repair: ApiRepair): TruckRepair {
    return {
      id: repair.id,
      date: repair.date,
      truckNumber: repair.truckNumber,
      description: repair.description,
      cost: repair.cost,
      driverName: repair.driverName ?? null,
      createdBy: repair.createdBy,
      createdAt: repair.createdAt,
    };
  }
}
