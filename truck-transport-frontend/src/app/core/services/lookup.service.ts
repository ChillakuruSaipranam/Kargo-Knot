import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LookupItem } from '../models/lookup.model';

@Injectable({ providedIn: 'root' })
export class LookupService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/lookups`;

  getTrucks(): Observable<LookupItem[]> {
    return this.http.get<LookupItem[]>(`${this.baseUrl}/trucks`);
  }

  createTruck(number: string): Observable<LookupItem> {
    return this.http.post<LookupItem>(`${this.baseUrl}/trucks`, { number });
  }

  updateTruck(id: string, number: string): Observable<LookupItem> {
    return this.http.put<LookupItem>(`${this.baseUrl}/trucks/${id}`, { number });
  }

  deleteTruck(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/trucks/${id}`);
  }

  getQuarries(): Observable<LookupItem[]> {
    return this.http.get<LookupItem[]>(`${this.baseUrl}/quarries`);
  }

  createQuarry(name: string): Observable<LookupItem> {
    return this.http.post<LookupItem>(`${this.baseUrl}/quarries`, { name });
  }

  updateQuarry(id: string, name: string): Observable<LookupItem> {
    return this.http.put<LookupItem>(`${this.baseUrl}/quarries/${id}`, { name });
  }

  deleteQuarry(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/quarries/${id}`);
  }

  getDrivers(): Observable<LookupItem[]> {
    return this.http.get<LookupItem[]>(`${this.baseUrl}/drivers`);
  }

  createDriver(name: string, phone: string, license: string): Observable<LookupItem> {
    return this.http.post<LookupItem>(`${this.baseUrl}/drivers`, { name, phone, license });
  }

  updateDriver(id: string, name: string, phone: string, license: string): Observable<LookupItem> {
    return this.http.put<LookupItem>(`${this.baseUrl}/drivers/${id}`, { name, phone, license });
  }

  deleteDriver(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/drivers/${id}`);
  }
}
